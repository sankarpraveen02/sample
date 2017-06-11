﻿using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using TestableFileSystem.Fakes.Tests.Builders;
using Xunit;

namespace TestableFileSystem.Fakes.Tests.Specs
{
    public sealed class DirectoryEntrySpecs
    {
        [Fact]
        private void When_creating_drive_letter_in_root_it_must_succeed()
        {
            // Arrange
            DirectoryEntry root = DirectoryEntry.CreateRoot();
            var path = new AbsolutePath(@"C:\");

            // Act
            DirectoryEntry drive = root.CreateDirectory(path);

            // Assert
            drive.Name.Should().Be("C:");
            drive.Parent.Should().Be(root);
        }

        [Fact]
        private void When_creating_network_share_in_root_it_must_succeed()
        {
            // Arrange
            DirectoryEntry root = DirectoryEntry.CreateRoot();
            var path = new AbsolutePath(@"\\teamserver");

            // Act
            DirectoryEntry drive = root.CreateDirectory(path);

            // Assert
            drive.Name.Should().Be(@"\\teamserver");
            drive.Parent.Should().Be(root);
        }

        [Fact]
        private void When_creating_directory_in_root_it_must_fail()
        {
            // Arrange
            DirectoryEntry root = DirectoryEntry.CreateRoot();
            AbsolutePath path = new AbsolutePath(@"C:\some").MoveDown();

            // Act
            Action action = () => root.CreateDirectory(path);

            // Assert
            action.ShouldThrow<InvalidOperationException>()
                .WithMessage("Drive letter or network share must be created at this level.");
        }

        [Fact]
        private void When_creating_drive_letter_in_subdirectory_it_must_fail()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingDirectory(@"\\teamserver")
                .Build();
            var path = new AbsolutePath(@"\\filestorage");

            // Act
            Action action = () => root.Directories[@"\\teamserver"].CreateDirectory(path);

            // Assert
            action.ShouldThrow<InvalidOperationException>()
                .WithMessage("Drive letter or network share cannot be created at this level.");
        }

        [Fact]
        private void When_creating_network_share_in_subdirectory_it_must_fail()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingDirectory(@"C:")
                .Build();
            var path = new AbsolutePath("C:");

            // Act
            Action action = () => root.Directories["C:"].CreateDirectory(path);

            // Assert
            action.ShouldThrow<InvalidOperationException>()
                .WithMessage("Drive letter or network share cannot be created at this level.");
        }

        [Fact]
        private void When_getting_file_that_does_not_exist_it_must_be_created()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingDirectory(@"C:")
                .Build();
            var path = new AbsolutePath(@"C:\file.txt");

            // Act
            FileEntry file = root.GetOrCreateFile(path, false);

            // Assert
            file.Should().NotBeNull();
            root.Directories["C:"].Files["file.txt"].Should().Be(file);
        }

        [Fact]
        private void When_getting_file_that_does_not_exist_it_must_create_directory_tree()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingDirectory(@"C:")
                .Build();
            var path = new AbsolutePath(@"C:\some\path\to\file.txt");

            // Act
            FileEntry file = root.GetOrCreateFile(path, true);

            // Assert
            file.Should().NotBeNull();
            root.Directories["C:"].Directories["some"].Directories["path"].Directories["to"].Files["file.txt"]
                .Should().Be(file);
        }

        [Fact]
        private void When_getting_file_from_missing_folder_it_must_fail()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingDirectory(@"C:")
                .Build();
            var path = new AbsolutePath(@"C:\some\file.txt");

            // Act
            Action action = () => root.GetOrCreateFile(path, false);

            // Assert
            action.ShouldThrow<DirectoryNotFoundException>()
                .WithMessage(@"Could not find a part of the path 'C:\some\file.txt'.");
        }

        [Fact]
        private void When_getting_file_that_exists_it_must_be_returned()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingFile(@"C:\file.txt")
                .Build();
            var path = new AbsolutePath(@"C:\file.txt");

            // Act
            FileEntry file = root.GetOrCreateFile(path, false);

            // Assert
            file.Should().NotBeNull();
            root.Directories["c:"].Files["FILE.txt"].Should().Be(file);
        }

        [Fact]
        private void When_trying_to_get_file_that_exists_it_must_be_returned()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingFile(@"C:\file.txt")
                .Build();
            var path = new AbsolutePath(@"C:\file.txt");

            // Act
            FileEntry file = root.TryGetExistingFile(path);

            // Assert
            file.Should().NotBeNull();
            root.Directories["C:"].Files["file.txt"].Should().Be(file);
        }

        [Fact]
        private void When_trying_to_get_file_that_does_not_exist_it_must_return_null()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingDirectory("C:")
                .Build();
            var path = new AbsolutePath(@"C:\file.txt");

            // Act
            FileEntry file = root.TryGetExistingFile(path);

            // Assert
            file.Should().BeNull();
        }

        [Fact]
        private void When_trying_to_get_file_from_missing_folder_it_must_fail()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder().Build();
            var path = new AbsolutePath(@"C:\some\file.txt");

            // Act
            Action action = () => root.TryGetExistingFile(path);

            // Assert
            action.ShouldThrow<DirectoryNotFoundException>()
                .WithMessage(@"Could not find a part of the path 'C:\some\file.txt'.");
        }

        [Fact]
        private void When_creating_directory_that_does_not_exist_it_must_create_directory_tree()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder().Build();
            var path = new AbsolutePath(@"C:\some\path\to\here");

            // Act
            DirectoryEntry directory = root.CreateDirectory(path);

            // Assert
            directory.Should().NotBeNull();
            directory.Attributes.Should().Be(FileAttributes.Directory);
            root.Directories["C:"].Directories["some"].Directories["path"].Directories["to"].Directories["here"].Should()
                .Be(directory);
        }

        [Fact]
        private void When_creating_directory_that_exists_it_must_be_returned()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingDirectory(@"C:\some")
                .Build();

            var path = new AbsolutePath(@"C:\some");

            // Act
            DirectoryEntry directory = root.CreateDirectory(path);

            // Assert
            directory.Should().NotBeNull();
            root.Directories["C:"].Directories["some"].Should().Be(directory);
        }

        [Fact]
        private void When_trying_to_get_directory_that_exists_it_must_be_returned()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingDirectory(@"C:\some\where")
                .Build();

            var path = new AbsolutePath(@"C:\some\where");

            // Act
            DirectoryEntry directory = root.TryGetExistingDirectory(path);

            // Assert
            directory.Should().NotBeNull();
            root.Directories["C:"].Directories["some"].Directories["where"].Should().Be(directory);
        }

        [Fact]
        private void When_trying_to_get_directory_that_does_not_exist_it_must_return_null()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder().Build();
            var path = new AbsolutePath(@"C:\some\where");

            // Act
            DirectoryEntry directory = root.TryGetExistingDirectory(path);

            // Assert
            directory.Should().BeNull();
        }

        [Fact]
        private void When_enumerating_directories_it_must_return_them()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingDirectory(@"C:\base\some\where")
                .IncludingDirectory(@"C:\base\other")
                .Build();

            var path = new AbsolutePath(@"C:\base");

            // Act
            string[] directories = root.EnumerateDirectories(path, null).ToArray();

            // Assert
            directories.Should().NotBeNull();
            directories.Should().HaveCount(2);

            directories.Should().Contain(@"C:\base\some");
            directories.Should().Contain(@"C:\base\other");
        }

        [Fact]
        private void When_enumerating_directories_it_must_return_directories_matching_pattern()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingDirectory(@"C:\base\some\where")
                .IncludingDirectory(@"C:\base\other")
                .IncludingDirectory(@"C:\base\skip")
                .Build();

            var path = new AbsolutePath(@"C:\base");

            // Act
            string[] directories = root.EnumerateDirectories(path, "*o*").ToArray();

            // Assert
            directories.Should().NotBeNull();
            directories.Should().HaveCount(2);

            directories.Should().Contain(@"C:\base\some");
            directories.Should().Contain(@"C:\base\other");
        }

        [Fact]
        private void When_enumerating_directories_it_must_return_directories_matching_subpattern()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingDirectory(@"C:\excluded")
                .IncludingDirectory(@"C:\base\some\where")
                .IncludingDirectory(@"C:\base\other")
                .IncludingDirectory(@"C:\base\skip")
                .Build();

            var path = new AbsolutePath(@"C:\");

            // Act
            string[] directories = root.EnumerateDirectories(path, @"base\*o*").ToArray();

            // Assert
            directories.Should().NotBeNull();
            directories.Should().HaveCount(2);

            directories.Should().Contain(@"C:\base\some");
            directories.Should().Contain(@"C:\base\other");
        }

        [Fact]
        private void When_enumerating_files_it_must_return_them()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingFile(@"C:\base\where.txt")
                .IncludingFile(@"C:\base\other.txt")
                .Build();

            var path = new AbsolutePath(@"C:\base");

            // Act
            string[] files = root.EnumerateFiles(path, null, null).ToArray();

            // Assert
            files.Should().NotBeNull();
            files.Should().HaveCount(2);

            files.Should().Contain(@"C:\base\where.txt");
            files.Should().Contain(@"C:\base\other.txt");
        }

        [Fact]
        private void When_enumerating_files_it_must_return_files_matching_pattern()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingFile(@"C:\base\where.txt")
                .IncludingFile(@"C:\base\other.doc")
                .Build();

            var path = new AbsolutePath(@"C:\base");

            // Act
            string[] files = root.EnumerateFiles(path, "*.doc", null).ToArray();

            // Assert
            files.Should().NotBeNull();
            files.Should().HaveCount(1);

            files.Should().Contain(@"C:\base\other.doc");
        }

        [Fact]
        private void When_enumerating_files_it_must_return_files_matching_subpatterns()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingFile(@"C:\root.doc")
                .IncludingFile(@"C:\base\more.doc")
                .IncludingFile(@"C:\base\first\where.txt")
                .IncludingFile(@"C:\base\second\other.doc")
                .IncludingFile(@"C:\base\second\deeper\skip.doc")
                .Build();

            var path = new AbsolutePath(@"C:\");

            // Act
            string[] files = root.EnumerateFiles(path, @"base\second\*.doc", null).ToArray();

            // Assert
            files.Should().NotBeNull();
            files.Should().HaveCount(1);

            files.Should().Contain(@"C:\base\second\other.doc");
        }

        [Fact]
        private void When_enumerating_files_recursively_it_must_return_all()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingFile(@"C:\base\where.txt")
                .IncludingFile(@"C:\base\other.txt")
                .IncludingFile(@"C:\base\more\nested\file.txt")
                .Build();

            var path = new AbsolutePath(@"C:\base");

            // Act
            string[] files = root.EnumerateFiles(path, null, SearchOption.AllDirectories).ToArray();

            // Assert
            files.Should().NotBeNull();
            files.Should().HaveCount(3);

            files.Should().Contain(@"C:\base\where.txt");
            files.Should().Contain(@"C:\base\other.txt");
            files.Should().Contain(@"C:\base\more\nested\file.txt");
        }

        [Fact]
        private void When_enumerating_files_recursively_it_must_return_all_files_matching_pattern()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingFile(@"C:\base\one.txt")
                .IncludingFile(@"C:\base\deep\two.txt")
                .IncludingFile(@"C:\base\more\nested\three.txt")
                .IncludingFile(@"C:\base\more\nested\four.txt")
                .Build();

            var path = new AbsolutePath(@"C:\base");

            // Act
            string[] files = root.EnumerateFiles(path, "*o*.txt", SearchOption.AllDirectories).ToArray();

            // Assert
            files.Should().NotBeNull();
            files.Should().HaveCount(3);

            files.Should().Contain(@"C:\base\one.txt");
            files.Should().Contain(@"C:\base\deep\two.txt");
            files.Should().Contain(@"C:\base\more\nested\four.txt");
        }

        [Fact]
        private void When_enumerating_files_recursively_it_must_return_all_files_matching_subpatterns()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingFile(@"C:\root.doc")
                .IncludingFile(@"C:\base\more.doc")
                .IncludingFile(@"C:\base\first\where.txt")
                .IncludingFile(@"C:\base\second\other.doc")
                .IncludingFile(@"C:\base\second\deeper\skip.doc")
                .IncludingFile(@"C:\base\second\deeper\not.txt")
                .Build();

            var path = new AbsolutePath(@"C:\");

            // Act
            string[] files = root.EnumerateFiles(path, @"base\second\*.doc", SearchOption.AllDirectories).ToArray();

            // Assert
            files.Should().NotBeNull();
            files.Should().HaveCount(2);

            files.Should().Contain(@"C:\base\second\other.doc");
            files.Should().Contain(@"C:\base\second\deeper\skip.doc");
        }

        [Fact]
        private void When_setting_directory_attributes_to_temporary_it_must_fail()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingDirectory(@"C:\some")
                .Build();

            DirectoryEntry directory = root.Directories["C:"].Directories["some"];

            // Act
            Action action = () => directory.Attributes = FileAttributes.Temporary;

            // Assert
            action.ShouldThrow<ArgumentException>().WithMessage("Invalid File or Directory attributes value.*");
        }

        [Fact]
        private void When_setting_directory_attributes_to_all_except_temporary_it_must_filter_and_succeed()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingDirectory(@"C:\some")
                .Build();

            DirectoryEntry directory = root.Directories["C:"].Directories["some"];

            // Act
            directory.Attributes = FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System |
                FileAttributes.Directory | FileAttributes.Archive | FileAttributes.Device | FileAttributes.Normal |
                FileAttributes.SparseFile | FileAttributes.ReparsePoint | FileAttributes.Compressed | FileAttributes.Offline |
                FileAttributes.NotContentIndexed | FileAttributes.Encrypted | FileAttributes.IntegrityStream |
                FileAttributes.NoScrubData;

            // Assert
            directory.Attributes.Should().Be(FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System |
                FileAttributes.Directory | FileAttributes.Archive | FileAttributes.Offline | FileAttributes.NotContentIndexed |
                FileAttributes.NoScrubData | FileAttributes.ReparsePoint);
        }

        [Fact]
        private void When_setting_directory_attributes_to_a_discarded_value_it_must_preserve_directory_attribute()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingDirectory(@"C:\some")
                .Build();

            DirectoryEntry directory = root.Directories["C:"].Directories["some"];

            // Act
            directory.Attributes = FileAttributes.SparseFile;

            // Assert
            directory.Attributes.Should().Be(FileAttributes.Directory);
        }

        [Fact]
        private void When_setting_directory_attributes_to_another_value_it_must_preserve_directory_attribute()
        {
            // Arrange
            DirectoryEntry root = new DirectoryTreeBuilder()
                .IncludingDirectory(@"C:\some")
                .Build();

            DirectoryEntry directory = root.Directories["C:"].Directories["some"];

            // Act
            directory.Attributes = FileAttributes.Hidden;

            // Assert
            directory.Attributes.Should().Be(FileAttributes.Directory | FileAttributes.Hidden);
        }
    }
}