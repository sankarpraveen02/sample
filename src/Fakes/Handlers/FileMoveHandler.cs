﻿using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using TestableFileSystem.Fakes.HandlerArguments;
using TestableFileSystem.Fakes.Resolvers;
using TestableFileSystem.Interfaces;

namespace TestableFileSystem.Fakes.Handlers
{
    internal sealed class FileMoveHandler : FakeOperationHandler<FileMoveArguments, object>
    {
        // TODO: Implement timings - https://support.microsoft.com/en-us/help/299648/description-of-ntfs-date-and-time-stamps-for-files-and-folders

        public FileMoveHandler([NotNull] DirectoryEntry root)
            : base(root)
        {
        }

        public override object Handle(FileMoveArguments arguments)
        {
            Guard.NotNull(arguments, nameof(arguments));

            FileEntry sourceFile = ResolveSourceFile(arguments.SourcePath);
            AssertHasExclusiveAccess(sourceFile);

            DirectoryEntry destinationDirectory =
                ResolveDestinationDirectory(arguments.SourcePath, arguments.DestinationPath, sourceFile);

            string newFileName = arguments.DestinationPath.Components.Last();
            MoveFile(sourceFile, destinationDirectory, newFileName);

            return Missing.Value;
        }

        [NotNull]
        private FileEntry ResolveSourceFile([NotNull] AbsolutePath sourcePath)
        {
            var sourceResolver = new FileResolver(Root)
            {
                ErrorFileFoundAsDirectory = incomingPath => ErrorFactory.System.FileNotFound(incomingPath),
                ErrorDirectoryFoundAsFile = incomingPath => ErrorFactory.System.FileNotFound(incomingPath),
                ErrorLastDirectoryFoundAsFile = incomingPath => ErrorFactory.System.FileNotFound(incomingPath),
                ErrorDirectoryNotFound = incomingPath => ErrorFactory.System.FileNotFound(incomingPath),
                ErrorPathIsVolumeRoot = incomingPath => ErrorFactory.System.FileNotFound(incomingPath),
                ErrorNetworkShareNotFound = incomingPath => ErrorFactory.System.FileNotFound(incomingPath)
            };

            return sourceResolver.ResolveExistingFile(sourcePath);
        }

        [NotNull]
        private DirectoryEntry ResolveDestinationDirectory([NotNull] AbsolutePath sourcePath,
            [NotNull] AbsolutePath destinationPath, [NotNull] FileEntry sourceFile)
        {
            var destinationResolver = new FileResolver(Root)
            {
                ErrorFileFoundAsDirectory = _ => ErrorFactory.System.CannotCreateFileBecauseFileAlreadyExists(),
                ErrorDirectoryFoundAsFile = _ => ErrorFactory.System.DirectoryNotFound(),
                ErrorLastDirectoryFoundAsFile = _ => ErrorFactory.System.ParameterIsIncorrect(),
                ErrorDirectoryNotFound = _ => ErrorFactory.System.DirectoryNotFound(),
                ErrorPathIsVolumeRoot = _ => ErrorFactory.System.FileOrDirectoryOrVolumeIsIncorrect(),
                ErrorFileExists = _ => ErrorFactory.System.CannotCreateFileBecauseFileAlreadyExists()
            };

            bool isFileCasingChangeOnly = IsFileCasingChangeOnly(sourcePath, destinationPath);

            return isFileCasingChangeOnly
                ? sourceFile.Parent
                : destinationResolver.ResolveContainingDirectoryForMissingFile(destinationPath);
        }

        private static void AssertHasExclusiveAccess([NotNull] FileEntry file)
        {
            if (file.IsOpen())
            {
                throw ErrorFactory.System.FileIsInUse();
            }
        }

        private bool IsFileCasingChangeOnly([NotNull] AbsolutePath sourcePath, [NotNull] AbsolutePath destinationPath)
        {
            return sourcePath.Components.SequenceEqual(destinationPath.Components, StringComparer.OrdinalIgnoreCase);
        }

        private static void MoveFile([NotNull] FileEntry sourceFile, [NotNull] DirectoryEntry destinationDirectory,
            [NotNull] string newFileName)
        {
            sourceFile.Parent.Detach(sourceFile);
            sourceFile.MoveTo(newFileName, destinationDirectory);
            destinationDirectory.Attach(sourceFile);
        }
    }
}
