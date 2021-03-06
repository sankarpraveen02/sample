﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using TestableFileSystem.Interfaces;
using TestableFileSystem.Wrappers;

namespace TestableFileSystem.Fakes
{
    internal sealed class FileEntry : BaseEntry
    {
        private const FileAttributes FileAttributesToDiscard = FileAttributes.Directory | FileAttributes.Device |
            FileAttributes.Normal | FileAttributes.SparseFile | FileAttributes.ReparsePoint | FileAttributes.Compressed |
            FileAttributes.Encrypted | FileAttributes.IntegrityStream;

        [NotNull]
        [ItemNotNull]
        private readonly List<byte[]> blocks = new List<byte[]>();

        public long Size { get; private set; }

        [CanBeNull]
        private FakeFileStream activeWriter;

        private bool deleteOnClose;

        [NotNull]
        [ItemNotNull]
        private readonly IList<FakeFileStream> activeReaders = new List<FakeFileStream>();

        [NotNull]
        private readonly object readerWriterLock = new object();

        private long creationTimeStampUtc;
        private long lastWriteTimeStampUtc;
        private long lastAccessTimeStampUtc;

        [NotNull]
        public DirectoryEntry Parent { get; private set; }

        public override DateTime CreationTime
        {
            get => DateTime.FromFileTime(creationTimeStampUtc);
            set => creationTimeStampUtc = value.ToFileTime();
        }

        public override DateTime CreationTimeUtc
        {
            get => DateTime.FromFileTimeUtc(creationTimeStampUtc);
            set => creationTimeStampUtc = value.ToFileTimeUtc();
        }

        public override DateTime LastAccessTime
        {
            get => DateTime.FromFileTime(lastAccessTimeStampUtc);
            set => lastAccessTimeStampUtc = value.ToFileTime();
        }

        public override DateTime LastAccessTimeUtc
        {
            get => DateTime.FromFileTimeUtc(lastAccessTimeStampUtc);
            set => lastAccessTimeStampUtc = value.ToFileTimeUtc();
        }

        public override DateTime LastWriteTime
        {
            get => DateTime.FromFileTime(lastWriteTimeStampUtc);
            set => lastWriteTimeStampUtc = value.ToFileTime();
        }

        public override DateTime LastWriteTimeUtc
        {
            get => DateTime.FromFileTimeUtc(lastWriteTimeStampUtc);
            set => lastWriteTimeStampUtc = value.ToFileTimeUtc();
        }

        public FileEntry([NotNull] string name, [NotNull] DirectoryEntry parent)
            : base(name)
        {
            Guard.NotNull(parent, nameof(parent));
            AssertParentIsValid(parent);

            Parent = parent;
            Attributes = FileAttributes.Archive;

            CreationTimeUtc = parent.SystemClock.UtcNow();
            HandleFileChanged();
        }

        [AssertionMethod]
        private void AssertParentIsValid([NotNull] DirectoryEntry parent)
        {
            if (parent.Parent == null)
            {
                throw new InvalidOperationException("File cannot exist at the root of the filesystem.");
            }
        }

        private void HandleFileChanged()
        {
            HandleFileAccessed();
            LastWriteTimeUtc = LastAccessTimeUtc;
        }

        private void HandleFileAccessed()
        {
            LastAccessTimeUtc = Parent.SystemClock.UtcNow();
        }

        public bool IsOpen()
        {
            lock (readerWriterLock)
            {
                return activeWriter != null || activeReaders.Any();
            }
        }

        public void EnableDeleteOnClose()
        {
            deleteOnClose = true;
        }

        [NotNull]
        public IFileStream Open(FileMode mode, FileAccess access, [NotNull] AbsolutePath path)
        {
            Guard.NotNull(path, nameof(path));

            bool isReaderOnly = access == FileAccess.Read;
            bool truncate = false;
            bool seekToEnd = false;

            switch (mode)
            {
                case FileMode.CreateNew:
                case FileMode.Create:
                case FileMode.Truncate:
                    truncate = true;
                    isReaderOnly = false;
                    break;
                case FileMode.Append:
                    seekToEnd = true;
                    isReaderOnly = false;
                    break;
            }

            FakeFileStream stream;

            lock (readerWriterLock)
            {
                if (activeWriter != null)
                {
                    throw ErrorFactory.System.FileIsInUse(path.GetText());
                }

                if (!isReaderOnly && activeReaders.Any())
                {
                    throw ErrorFactory.System.FileIsInUse(path.GetText());
                }

                stream = new FakeFileStream(this, access);

                if (isReaderOnly)
                {
                    activeReaders.Add(stream);
                }
                else
                {
                    activeWriter = stream;
                }
            }

            if (seekToEnd)
            {
                stream.Seek(0, SeekOrigin.End);
                stream.SetAppendOffsetToCurrentPosition();
            }

            if (truncate)
            {
                stream.SetLength(0);
            }

            return new FileStreamWrapper(stream, path.GetText, () => false, () => throw new NotSupportedException(),
                _ => stream.Flush());
        }

        public void MoveTo([NotNull] string newName, [NotNull] DirectoryEntry newParent)
        {
            Guard.NotNullNorWhiteSpace(newName, nameof(newName));
            Guard.NotNull(newParent, nameof(newParent));

            AssertParentIsValid(newParent);

            Name = newName;
            Parent = newParent;
        }

        private void CloseStream([NotNull] FakeFileStream stream)
        {
            lock (readerWriterLock)
            {
                if (activeWriter == stream)
                {
                    activeWriter = null;
                }

                activeReaders.Remove(stream);

                if (deleteOnClose && !IsOpen())
                {
                    Parent.DeleteFile(Name);
                }
            }
        }

        public override string ToString()
        {
            return $"File: {Name} ({Size} bytes)";
        }

        protected override FileAttributes FilterAttributes(FileAttributes attributes)
        {
            FileAttributes filtered = attributes & ~(FileAttributes.Normal | FileAttributesToDiscard);
            return filtered == 0 ? FileAttributes.Normal : filtered;
        }

        private sealed class FakeFileStream : Stream
        {
            [NotNull]
            private readonly FileEntry owner;

            private const int BlockSize = 4096;

            private long position;
            private bool hasAccessed;
            private bool hasUpdated;
            private bool isClosed;

            [CanBeNull]
            private long? appendOffset;

            [CanBeNull]
            private long? newLength;

            public override bool CanRead { get; }

            public override bool CanSeek => true;

            public override bool CanWrite { get; }

            public override long Length => newLength ?? owner.Size;

            public override long Position
            {
                get => position;
                set
                {
                    AssertNotClosed();

                    if (value < 0)
                    {
                        throw new ArgumentOutOfRangeException(nameof(value));
                    }

                    if (appendOffset != null && value < appendOffset)
                    {
                        throw ErrorFactory.System.CannotSeekToPositionBeforeAppend();
                    }

                    if (value > Length)
                    {
                        SetLength(value);
                    }

                    position = value;
                }
            }

            public FakeFileStream([NotNull] FileEntry owner, FileAccess access)
            {
                Guard.NotNull(owner, nameof(owner));

                this.owner = owner;
                CanRead = access.HasFlag(FileAccess.Read);
                CanWrite = access.HasFlag(FileAccess.Write);
            }

            public void SetAppendOffsetToCurrentPosition()
            {
                appendOffset = Position;
            }

            public override void Flush()
            {
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                AssertNotClosed();

                switch (origin)
                {
                    case SeekOrigin.Begin:
                        if (offset < 0)
                        {
                            throw new ArgumentOutOfRangeException(nameof(offset));
                        }

                        Position = offset;
                        break;

                    case SeekOrigin.Current:
                        if (Position + offset < 0)
                        {
                            throw new ArgumentOutOfRangeException(nameof(offset));
                        }

                        Position += offset;
                        break;

                    case SeekOrigin.End:
                        if (Length + offset < 0)
                        {
                            throw new ArgumentOutOfRangeException(nameof(offset));
                        }

                        Position = Length + offset;
                        break;
                    default:
                        throw ErrorFactory.Internal.EnumValueUnsupported(origin);
                }

                return Position;
            }

            public override void SetLength(long value)
            {
                AssertNotClosed();
                AssertIsWriteable();

                if (value == Length)
                {
                    return;
                }

                EnsureCapacity(value);
                newLength = value;

                if (Position > Length)
                {
                    Position = Length;
                }

                hasUpdated = true;
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                AssertNotClosed();
                AssertIsReadable();

                var segment = new ArraySegment<byte>(buffer, offset, count);
                if (segment.Count == 0 || Position == Length)
                {
                    return 0;
                }

                int sumBytesRead = 0;

                int blockIndex = (int)(Position / BlockSize);
                int offsetInCurrentBlock = (int)(Position % BlockSize);

                while (count > 0 && Position < Length)
                {
                    int bytesToRead = Math.Min(BlockSize - offsetInCurrentBlock, count);
                    bytesToRead = Math.Min(bytesToRead, (int)(Length - Position));

                    Buffer.BlockCopy(owner.blocks[blockIndex], offsetInCurrentBlock, buffer, offset, bytesToRead);

                    offset += bytesToRead;
                    count -= bytesToRead;

                    Position += bytesToRead;
                    sumBytesRead += bytesToRead;

                    blockIndex++;
                    offsetInCurrentBlock = 0;
                }

                hasAccessed = true;
                return sumBytesRead;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                AssertNotClosed();
                AssertIsWriteable();

                var segment = new ArraySegment<byte>(buffer, offset, count);
                if (segment.Count == 0)
                {
                    return;
                }

                EnsureCapacity(Position + count);

                int blockIndex = (int)(Position / BlockSize);
                int bytesFreeInCurrentBlock = BlockSize - (int)(Position % BlockSize);

                long newPosition = position;

                while (count > 0)
                {
                    int bytesToWrite = Math.Min(bytesFreeInCurrentBlock, count);

                    int offsetInBlock = BlockSize - bytesFreeInCurrentBlock;
                    Buffer.BlockCopy(buffer, offset, owner.blocks[blockIndex], offsetInBlock, bytesToWrite);

                    offset += bytesToWrite;
                    count -= bytesToWrite;

                    newPosition += bytesToWrite;

                    blockIndex++;
                    bytesFreeInCurrentBlock = BlockSize;
                }

                Position = newPosition;
                hasUpdated = true;
            }

            private void EnsureCapacity(long bytesNeeded)
            {
                long bytesAvailable = owner.blocks.Count * BlockSize;
                while (bytesAvailable < bytesNeeded)
                {
                    owner.blocks.Add(new byte[BlockSize]);
                    bytesAvailable += BlockSize;
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    if (!isClosed)
                    {
                        isClosed = true;

                        if (newLength != null)
                        {
                            owner.Size = newLength.Value;
                        }

                        if (hasUpdated)
                        {
                            owner.HandleFileChanged();
                        }
                        else if (hasAccessed)
                        {
                            owner.HandleFileAccessed();
                        }

                        owner.CloseStream(this);
                    }
                }

                base.Dispose(disposing);
            }

            private void AssertNotClosed()
            {
                if (isClosed)
                {
                    throw new ObjectDisposedException(string.Empty, "Cannot access a closed file.");
                }
            }

            private void AssertIsReadable()
            {
                if (!CanRead)
                {
                    throw new NotSupportedException("Stream does not support reading.");
                }
            }

            private void AssertIsWriteable()
            {
                if (!CanWrite)
                {
                    throw new NotSupportedException("Stream does not support writing.");
                }
            }
        }
    }
}
