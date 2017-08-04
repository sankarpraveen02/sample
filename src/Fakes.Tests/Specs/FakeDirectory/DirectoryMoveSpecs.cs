﻿using System;
using System.IO;
using FluentAssertions;
using TestableFileSystem.Fakes.Builders;
using TestableFileSystem.Interfaces;
using Xunit;

namespace TestableFileSystem.Fakes.Tests.Specs.FakeDirectory
{
    public sealed class DirectoryMoveSpecs
    {
        [Fact]
        private void When_moving_directory_for_null_source_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            // ReSharper disable once AssignNullToNotNullAttribute
            Action action = () => fileSystem.Directory.Move(null, @"c:\newdir");

            // Assert
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        private void When_moving_directory_for_null_destination_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            // ReSharper disable once AssignNullToNotNullAttribute
            Action action = () => fileSystem.Directory.Move(@"c:\missing", null);

            // Assert
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        private void When_moving_directory_for_empty_string_source_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            Action action = () => fileSystem.Directory.Move(string.Empty, @"c:\newdir");

            // Assert
            action.ShouldThrow<ArgumentException>().WithMessage("Empty file name is not legal.*");
        }

        [Fact]
        private void When_moving_directory_for_empty_string_destination_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            Action action = () => fileSystem.Directory.Move(@"c:\missing", string.Empty);

            // Assert
            action.ShouldThrow<ArgumentException>().WithMessage("Empty file name is not legal.*");
        }

        [Fact]
        private void When_moving_directory_for_whitespace_source_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            Action action = () => fileSystem.Directory.Move(" ", @"c:\newdir");

            // Assert
            action.ShouldThrow<ArgumentException>().WithMessage("The path is not of a legal form.*");
        }

        [Fact]
        private void When_moving_directory_for_whitespace_destination_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            Action action = () => fileSystem.Directory.Move(@"c:\missing", " ");

            // Assert
            action.ShouldThrow<ArgumentException>().WithMessage("The path is not of a legal form.*");
        }

        [Fact]
        private void When_moving_directory_for_invalid_root_source_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            Action action = () => fileSystem.Directory.Move("::", @"c:\newdir");

            // Assert
            action.ShouldThrow<NotSupportedException>().WithMessage("The given path's format is not supported.");
        }

        [Fact]
        private void When_moving_directory_for_invalid_root_destination_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            Action action = () => fileSystem.Directory.Move(@"c:\missing", "::");

            // Assert
            action.ShouldThrow<NotSupportedException>().WithMessage("The given path's format is not supported.");
        }

        [Fact]
        private void When_moving_directory_for_invalid_characters_in_source_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            Action action = () => fileSystem.Directory.Move(@"c:\dir?i", @"c:\newdir");

            // Assert
            action.ShouldThrow<ArgumentException>().WithMessage("Illegal characters in path.*");
        }

        [Fact]
        private void When_moving_directory_for_invalid_characters_in_destination_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            Action action = () => fileSystem.Directory.Move(@"c:\missing", @"c:\dir?i");

            // Assert
            action.ShouldThrow<ArgumentException>().WithMessage("Illegal characters in path.*");
        }

        // TODO: Add additional specs.

        [Fact]
        private void When_moving_directory_from_missing_directory_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            Action action = () => fileSystem.Directory.Move(@"c:\missing-folder", @"c:\new-folder");

            // Assert
            action.ShouldThrow<DirectoryNotFoundException>()
                .WithMessage(@"Could not find a part of the path 'c:\missing-folder'.");
        }

        [Fact]
        private void When_moving_directory_to_existing_directory_it_must_fail()
        {
            // Arrange
            const string sourcePath = @"c:\existing-folder";
            const string destinationPath = @"c:\new-folder";

            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingDirectory(sourcePath)
                .IncludingDirectory(destinationPath)
                .Build();

            // Act
            Action action = () => fileSystem.Directory.Move(sourcePath, destinationPath);

            // Assert
            action.ShouldThrow<IOException>().WithMessage(@"Cannot create a file when that file already exists");
        }

        [Fact]
        private void When_moving_directory_to_existing_file_it_must_fail()
        {
            // Arrange
            const string sourcePath = @"c:\existing-folder";
            const string destinationPath = @"c:\new-name";

            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingDirectory(sourcePath)
                .IncludingEmptyFile(destinationPath)
                .Build();

            // Act
            Action action = () => fileSystem.Directory.Move(sourcePath, destinationPath);

            // Assert
            action.ShouldThrow<IOException>().WithMessage(@"Cannot create a file when that file already exists");
        }

        [Fact]
        private void When_moving_directory_accross_volumes_it_must_fail()
        {
            // Arrange
            const string sourcePath = @"c:\existing-folder";
            const string destinationPath = @"e:\new-name";

            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingDirectory(sourcePath)
                .Build();

            // Act
            Action action = () => fileSystem.Directory.Move(sourcePath, destinationPath);

            // Assert
            action.ShouldThrow<IOException>().WithMessage(@"Source and destination path must have identical roots. Move will not work across volumes.");
        }

        [Fact]
        private void When_moving_directory_that_contains_single_file_it_must_succeed()
        {
            // Arrange
            const string sourcePath = @"c:\existing-folder";
            const string destinationPath = @"c:\new-folder";

            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingEmptyFile(sourcePath + @"\file.txt")
                .Build();

            // Act
            fileSystem.Directory.Move(sourcePath, destinationPath);

            // Assert
            fileSystem.Directory.Exists(sourcePath).Should().Be(false);
            fileSystem.Directory.Exists(destinationPath).Should().Be(true);
            fileSystem.File.Exists(destinationPath + @"\file.txt").Should().BeTrue();
        }

        [Fact]
        private void When_moving_directory_from_drive_it_must_fail()
        {
            // Arrange
            const string sourcePath = @"e:\";

            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingDirectory(sourcePath)
                .Build();

            // Act
            Action action = () => fileSystem.Directory.Move(sourcePath, @"e:\new-folder");

            // Assert
            action.ShouldThrow<IOException>().WithMessage(@"Access to the path 'e:\' is denied.");
        }

        [Fact]
        private void When_moving_directory_to_drive_it_must_fail()
        {
            // Arrange
            const string sourcePath = @"c:\existing-folder";

            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingDirectory(sourcePath)
                .Build();

            // Act
            Action action = () => fileSystem.Directory.Move(sourcePath, @"c:\");

            // Assert
            action.ShouldThrow<IOException>().WithMessage(@"The filename, directory name, or volume label syntax is incorrect");
        }
    }
}