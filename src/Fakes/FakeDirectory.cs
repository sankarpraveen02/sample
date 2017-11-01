﻿using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using TestableFileSystem.Fakes.HandlerArguments;
using TestableFileSystem.Fakes.Handlers;
using TestableFileSystem.Fakes.Resolvers;
using TestableFileSystem.Interfaces;

namespace TestableFileSystem.Fakes
{
    public sealed class FakeDirectory : IDirectory
    {
        [NotNull]
        private readonly DirectoryEntry root;

        [NotNull]
        private readonly FakeFileSystem owner;

        internal FakeDirectory([NotNull] DirectoryEntry root, [NotNull] FakeFileSystem owner)
        {
            Guard.NotNull(root, nameof(root));
            Guard.NotNull(owner, nameof(owner));

            this.root = root;
            this.owner = owner;
        }

        public IDirectoryInfo GetParent(string path)
        {
            Guard.NotNull(path, nameof(path));
            AssertPathIsNotWhiteSpace(path);

            AbsolutePath absolutePath = owner.ToAbsolutePath(path);

            AbsolutePath parentPath = absolutePath.TryGetParentPath();
            return parentPath == null ? null : owner.ConstructDirectoryInfo(parentPath);
        }

        private static void AssertPathIsNotWhiteSpace([NotNull] string path)
        {
            if (path.Length == 0)
            {
                throw ErrorFactory.System.PathCannotBeEmptyOrWhitespace(nameof(path));
            }
        }

        public string GetDirectoryRoot(string path)
        {
            Guard.NotNull(path, nameof(path));

            AbsolutePath absolutePath = owner.ToAbsolutePath(path);
            return absolutePath.GetAncestorPath(0).GetText();
        }

        public string[] GetFiles(string path, string searchPattern = "*",
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            Guard.NotNull(path, nameof(path));
            Guard.NotNull(searchPattern, nameof(searchPattern));

            AbsolutePath absolutePath = owner.ToAbsolutePath(path);

            var handler = new DirectoryEnumerateEntriesHandler(root);
            var arguments =
                new DirectoryEnumerateEntriesArguments(absolutePath, path, searchPattern, searchOption, EnumerationFilter.Files);

            return handler.Handle(arguments);
        }

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*",
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            // Not using deferred execution because we are running inside a lock.
            return GetFiles(path, searchPattern, searchOption);
        }

        public string[] GetDirectories(string path, string searchPattern = "*",
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            Guard.NotNull(path, nameof(path));
            Guard.NotNull(searchPattern, nameof(searchPattern));

            AbsolutePath absolutePath = owner.ToAbsolutePath(path);

            var handler = new DirectoryEnumerateEntriesHandler(root);
            var arguments = new DirectoryEnumerateEntriesArguments(absolutePath, path, searchPattern, searchOption,
                EnumerationFilter.Directories);

            return handler.Handle(arguments);
        }

        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*",
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            // Not using deferred execution because we are running inside a lock.
            return GetDirectories(path, searchPattern, searchOption);
        }

        public string[] GetFileSystemEntries(string path, string searchPattern = "*",
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            Guard.NotNull(path, nameof(path));
            Guard.NotNull(searchPattern, nameof(searchPattern));

            AbsolutePath absolutePath = owner.ToAbsolutePath(path);

            var handler = new DirectoryEnumerateEntriesHandler(root);
            var arguments =
                new DirectoryEnumerateEntriesArguments(absolutePath, path, searchPattern, searchOption, EnumerationFilter.All);

            return handler.Handle(arguments);
        }

        public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern = "*",
            SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            // Not using deferred execution because we are running inside a lock.
            return GetFileSystemEntries(path, searchPattern, searchOption);
        }

        public bool Exists(string path)
        {
            try
            {
                AbsolutePath absolutePath = string.IsNullOrWhiteSpace(path) ? null : owner.ToAbsolutePath(path);

                var handler = new DirectoryExistsHandler(root);
                var arguments = new EntryExistsArguments(absolutePath);

                return handler.Handle(arguments);
            }
            catch (Exception ex) when (ShouldSuppress(ex))
            {
                return false;
            }
        }

        private static bool ShouldSuppress([NotNull] Exception ex)
        {
            return ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException ||
                ex is NotSupportedException;
        }

        public IDirectoryInfo CreateDirectory(string path)
        {
            Guard.NotNull(path, nameof(path));
            AssertPathIsNotWhiteSpace(path);

            AbsolutePath absolutePath = owner.ToAbsolutePath(path);

            var handler = new DirectoryCreateHandler(root);
            var arguments = new DirectoryCreateArguments(absolutePath, false);

            handler.Handle(arguments);

            return owner.ConstructDirectoryInfo(absolutePath);
        }

        public void Delete(string path, bool recursive = false)
        {
            Guard.NotNull(path, nameof(path));

            AbsolutePath absolutePath = owner.ToAbsolutePath(path);

            var handler = new DirectoryDeleteHandler(root, owner.CurrentDirectoryManager);
            var arguments = new DirectoryDeleteArguments(absolutePath, recursive);

            handler.Handle(arguments);
        }

        public void Move(string sourceDirName, string destDirName)
        {
            Guard.NotNull(sourceDirName, nameof(sourceDirName));
            AssertFileNameIsNotEmpty(sourceDirName);

            Guard.NotNull(destDirName, nameof(destDirName));
            AssertFileNameIsNotEmpty(destDirName);

            AbsolutePath sourcePath = owner.ToAbsolutePath(sourceDirName);
            AbsolutePath destinationPath = owner.ToAbsolutePath(destDirName);

            var handler = new DirectoryMoveHandler(root, owner.CurrentDirectoryManager);
            var arguments = new EntryMoveArguments(sourcePath, destinationPath);

            handler.Handle(arguments);
        }

        private static void AssertFileNameIsNotEmpty([NotNull] string path)
        {
            if (path.Length == 0)
            {
                throw ErrorFactory.System.EmptyFileNameIsNotLegal(nameof(path));
            }
        }

        public string GetCurrentDirectory()
        {
            return owner.CurrentDirectoryManager.GetValue().GetText();
        }

        public void SetCurrentDirectory(string path)
        {
            Guard.NotNull(path, nameof(path));
            AssertPathIsNotWhiteSpace(path);

            AbsolutePath absolutePath = owner.ToAbsolutePath(path);

            var resolver = new DirectoryResolver(root)
            {
                ErrorLastDirectoryFoundAsFile = _ => ErrorFactory.System.DirectoryNameIsInvalid()
            };
            resolver.ResolveDirectory(absolutePath);

            owner.CurrentDirectoryManager.SetValue(absolutePath);
        }

        public DateTime GetCreationTime(string path)
        {
            Guard.NotNull(path, nameof(path));

            return owner.File.GetCreationTime(path);
        }

        public DateTime GetCreationTimeUtc(string path)
        {
            Guard.NotNull(path, nameof(path));

            return owner.File.GetCreationTimeUtc(path);
        }

        public void SetCreationTime(string path, DateTime creationTime)
        {
            // TODO: Implement events that change directory timings
            // https://support.microsoft.com/en-us/help/299648/description-of-ntfs-date-and-time-stamps-for-files-and-folders
            // https://stackoverflow.com/questions/1025187/rules-for-date-modified-of-folders-in-windows-explorer
            // https://msdn.microsoft.com/nl-nl/library/windows/desktop/ms724290(v=vs.85).aspx

            throw new NotImplementedException();
        }

        public void SetCreationTimeUtc(string path, DateTime creationTimeUtc)
        {
            throw new NotImplementedException();
        }

        public DateTime GetLastAccessTime(string path)
        {
            Guard.NotNull(path, nameof(path));

            return owner.File.GetLastAccessTime(path);
        }

        public DateTime GetLastAccessTimeUtc(string path)
        {
            Guard.NotNull(path, nameof(path));

            return owner.File.GetLastAccessTimeUtc(path);
        }

        public void SetLastAccessTime(string path, DateTime lastAccessTime)
        {
            throw new NotImplementedException();
        }

        public void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc)
        {
            throw new NotImplementedException();
        }

        public DateTime GetLastWriteTime(string path)
        {
            Guard.NotNull(path, nameof(path));

            return owner.File.GetLastWriteTime(path);
        }

        public DateTime GetLastWriteTimeUtc(string path)
        {
            Guard.NotNull(path, nameof(path));

            return owner.File.GetLastWriteTimeUtc(path);
        }

        public void SetLastWriteTime(string path, DateTime lastWriteTime)
        {
            throw new NotImplementedException();
        }

        public void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
        {
            throw new NotImplementedException();
        }
    }
}
