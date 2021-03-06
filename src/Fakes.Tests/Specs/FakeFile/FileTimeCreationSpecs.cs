using System;
using System.IO;
using FluentAssertions;
using TestableFileSystem.Fakes.Builders;
using TestableFileSystem.Interfaces;
using Xunit;

namespace TestableFileSystem.Fakes.Tests.Specs.FakeFile
{
    public sealed class FileTimeCreationSpecs
    {
        private static readonly DateTime DefaultTimeUtc = 1.February(2034).At(12, 34, 56).AsUtc();
        private static readonly DateTime DefaultTime = DefaultTimeUtc.ToLocalTime();
        private static readonly DateTime HighTime = DateTime.MaxValue.AddDays(-2).AsUtc().ToLocalTime();
        private static readonly DateTime ZeroFileTime = 1.January(1601).AsUtc().ToLocalTime();

        [Fact]
        private void When_getting_creation_time_in_local_zone_for_null_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            // ReSharper disable once AssignNullToNotNullAttribute
            Action action = () => fileSystem.File.GetCreationTime(null);

            // Assert
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_null_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            // ReSharper disable once AssignNullToNotNullAttribute
            Action action = () => fileSystem.File.SetCreationTime(null, DefaultTime);

            // Assert
            action.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        private void When_getting_creation_time_in_local_zone_for_empty_string_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            Action action = () => fileSystem.File.GetCreationTime(string.Empty);

            // Assert
            action.ShouldThrow<ArgumentException>().WithMessage("The path is not of a legal form.*");
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_empty_string_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            Action action = () => fileSystem.File.SetCreationTime(string.Empty, DefaultTime);

            // Assert
            action.ShouldThrow<ArgumentException>().WithMessage("The path is not of a legal form.*");
        }

        [Fact]
        private void When_getting_creation_time_in_local_zone_for_whitespace_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            Action action = () => fileSystem.File.GetCreationTime(" ");

            // Assert
            action.ShouldThrow<ArgumentException>().WithMessage("The path is not of a legal form.*");
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_whitespace_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            Action action = () => fileSystem.File.SetCreationTime(" ", DefaultTime);

            // Assert
            action.ShouldThrow<ArgumentException>().WithMessage("The path is not of a legal form.*");
        }

        [Fact]
        private void When_getting_creation_time_in_local_zone_for_invalid_root_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            Action action = () => fileSystem.File.GetCreationTime("::");

            // Assert
            action.ShouldThrow<NotSupportedException>().WithMessage("The given path's format is not supported.");
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_invalid_root_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            Action action = () => fileSystem.File.SetCreationTime("::", DefaultTime);

            // Assert
            action.ShouldThrow<NotSupportedException>().WithMessage("The given path's format is not supported.");
        }

        [Fact]
        private void When_getting_creation_time_in_local_zone_for_invalid_characters_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            Action action = () => fileSystem.File.GetCreationTime(@"c:\dir?i");

            // Assert
            action.ShouldThrow<ArgumentException>().WithMessage("Illegal characters in path.*");
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_invalid_characters_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            Action action = () => fileSystem.File.SetCreationTime(@"c:\dir?i", DefaultTime);

            // Assert
            action.ShouldThrow<ArgumentException>().WithMessage("Illegal characters in path.*");
        }

        [Fact]
        private void When_getting_creation_time_in_local_zone_for_missing_file_it_must_succeed()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingDirectory(@"C:\some")
                .Build();

            // Act
            DateTime time = fileSystem.File.GetCreationTime(@"C:\some\file.txt");

            // Assert
            time.Should().Be(ZeroFileTime);
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_missing_file_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingDirectory(@"C:\some")
                .Build();

            // Act
            Action action = () => fileSystem.File.SetCreationTime(@"C:\some\file.txt", DefaultTime);

            // Assert
            action.ShouldThrow<FileNotFoundException>().WithMessage(@"Could not find file 'C:\some\file.txt'.");
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_existing_file_it_must_succeed()
        {
            // Arrange
            const string path = @"C:\some.txt";

            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingEmptyFile(path)
                .Build();

            // Act
            fileSystem.File.SetCreationTime(path, DefaultTime);

            // Assert
            fileSystem.File.GetCreationTime(path).Should().Be(DefaultTime);
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_existing_file_with_trailing_whitespace_it_must_succeed()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingEmptyFile(@"C:\some.txt")
                .Build();

            // Act
            fileSystem.File.SetCreationTime(@"C:\some.txt  ", DefaultTime);

            // Assert
            fileSystem.File.GetCreationTime(@"C:\some.txt  ").Should().Be(DefaultTime);
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_existing_readonly_file_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingEmptyFile(@"C:\some.txt", FileAttributes.ReadOnly)
                .Build();

            // Act
            Action action = () => fileSystem.File.SetCreationTime(@"C:\some.txt", DefaultTime);

            // Assert
            action.ShouldThrow<UnauthorizedAccessException>().WithMessage(@"Access to the path 'C:\some.txt' is denied.");
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_existing_file_in_readonly_directory_it_must_succeed()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingDirectory(@"c:\folder", FileAttributes.ReadOnly)
                .IncludingEmptyFile(@"C:\folder\some.txt")
                .Build();

            // Act
            fileSystem.File.SetCreationTime(@"C:\folder\some.txt", DefaultTime);

            // Assert
            fileSystem.File.GetCreationTime(@"C:\folder\some.txt").Should().Be(DefaultTime);
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_opened_file_it_must_fail()
        {
            // Arrange
            const string path = @"C:\folder\some.txt";

            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingEmptyFile(path)
                .Build();

            using (fileSystem.File.OpenRead(path))
            {
                // Act
                Action action = () => fileSystem.File.SetCreationTime(path, DefaultTime);

                // Assert
                action.ShouldThrow<IOException>().WithMessage(
                    @"The process cannot access the file 'C:\folder\some.txt' because it is being used by another process.");
            }
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_existing_file_to_MinValue_it_must_fail()
        {
            // Arrange
            const string path = @"C:\some.txt";

            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingEmptyFile(path)
                .Build();

            // Act
            Action action = () => fileSystem.File.SetCreationTime(path, DateTime.MinValue);

            // Assert
            action.ShouldThrow<ArgumentOutOfRangeException>()
                .WithMessage("The UTC time represented when the offset is applied must be between year 0 and 10,000.*");
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_existing_file_to_MaxValue_it_must_succeed()
        {
            // Arrange
            const string path = @"C:\some.txt";

            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingEmptyFile(path)
                .Build();

            // Act
            fileSystem.File.SetCreationTime(path, HighTime);

            // Assert
            fileSystem.File.GetCreationTime(path).Should().Be(HighTime);
        }

        [Fact]
        private void When_getting_creation_time_in_local_zone_for_drive_it_must_succeed()
        {
            // Arrange
            const string path = @"C:\";

            var clock = new SystemClock { UtcNow = () => DefaultTimeUtc };

            IFileSystem fileSystem = new FakeFileSystemBuilder(clock)
                .IncludingDirectory(path)
                .Build();

            // Act
            DateTime time = fileSystem.File.GetCreationTime(path);

            // Assert
            time.Should().Be(DefaultTime);
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_drive_it_must_fail()
        {
            // Arrange
            const string path = @"C:\";

            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingDirectory(path)
                .Build();

            // Act
            Action action = () => fileSystem.File.SetCreationTime(path, DefaultTime);

            // Assert
            action.ShouldThrow<ArgumentException>().WithMessage("Path must not be a drive.*");
        }

        [Fact]
        private void When_getting_creation_time_in_local_zone_using_absolute_path_without_drive_letter_it_must_succeed()
        {
            // Arrange
            var clock = new SystemClock { UtcNow = () => DefaultTimeUtc };

            IFileSystem fileSystem = new FakeFileSystemBuilder(clock)
                .IncludingDirectory(@"c:\some")
                .IncludingEmptyFile(@"C:\file.txt")
                .Build();

            fileSystem.Directory.SetCurrentDirectory(@"C:\some");

            // Act
            DateTime time = fileSystem.File.GetCreationTime(@"\file.txt");

            // Assert
            time.Should().Be(DefaultTime);
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_using_absolute_path_without_drive_letter_it_must_succeed()
        {
            // Arrange
            var clock = new SystemClock { UtcNow = () => DefaultTimeUtc };

            IFileSystem fileSystem = new FakeFileSystemBuilder(clock)
                .IncludingDirectory(@"c:\some")
                .IncludingEmptyFile(@"C:\file.txt")
                .Build();

            fileSystem.Directory.SetCurrentDirectory(@"C:\some");

            // Act
            fileSystem.File.SetCreationTime(@"\file.txt", DefaultTime);

            // Assert
            fileSystem.File.GetCreationTime(@"c:\file.txt").Should().Be(DefaultTime);
        }

        [Fact]
        private void When_getting_creation_time_in_local_zone_for_existing_relative_local_file_it_must_succeed()
        {
            // Arrange
            var clock = new SystemClock { UtcNow = () => DefaultTimeUtc };

            IFileSystem fileSystem = new FakeFileSystemBuilder(clock)
                .IncludingEmptyFile(@"C:\folder\some.txt")
                .Build();

            fileSystem.Directory.SetCurrentDirectory(@"c:\folder");

            // Act
            DateTime time = fileSystem.File.GetCreationTime(@"some.txt");

            // Assert
            time.Should().Be(DefaultTime);
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_existing_relative_local_file_it_must_succeed()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingEmptyFile(@"C:\folder\some.txt")
                .Build();

            fileSystem.Directory.SetCurrentDirectory(@"c:\folder");

            // Act
            fileSystem.File.SetCreationTime(@"some.txt", DefaultTime);

            // Assert
            fileSystem.File.GetCreationTime(@"C:\folder\some.txt").Should().Be(DefaultTime);
        }

        [Fact]
        private void When_getting_creation_time_in_local_zone_for_existing_local_file_with_different_casing_it_must_succeed()
        {
            // Arrange
            var clock = new SystemClock { UtcNow = () => DefaultTimeUtc };

            IFileSystem fileSystem = new FakeFileSystemBuilder(clock)
                .IncludingEmptyFile(@"C:\FOLDER\some.TXT")
                .Build();

            // Act
            DateTime time = fileSystem.File.GetCreationTime(@"C:\folder\SOME.txt");

            // Assert
            time.Should().Be(DefaultTime);
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_existing_local_file_with_different_casing_it_must_succeed()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingEmptyFile(@"C:\FOLDER\some.TXT")
                .Build();

            // Act
            fileSystem.File.SetCreationTime(@"C:\folder\SOME.txt", DefaultTime);

            // Assert
            fileSystem.File.GetCreationTime(@"C:\FOLDER\some.TXT").Should().Be(DefaultTime);
        }

        [Fact]
        private void When_getting_creation_time_in_local_zone_for_missing_parent_directory_it_must_succeed()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            DateTime time = fileSystem.File.GetCreationTime(@"C:\some\file.txt");

            // Assert
            time.Should().Be(ZeroFileTime);
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_missing_parent_directory_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            Action action = () => fileSystem.File.SetCreationTime(@"C:\some\file.txt", DefaultTime);

            // Assert
            action.ShouldThrow<DirectoryNotFoundException>()
                .WithMessage(@"Could not find a part of the path 'C:\some\file.txt'.");
        }

        [Fact]
        private void When_getting_creation_time_in_local_zone_for_existing_directory_it_must_succeed()
        {
            // Arrange
            const string path = @"C:\some\subfolder";

            var clock = new SystemClock { UtcNow = () => DefaultTimeUtc };

            IFileSystem fileSystem = new FakeFileSystemBuilder(clock)
                .IncludingDirectory(path)
                .Build();

            // Act
            DateTime time = fileSystem.File.GetCreationTime(path);

            // Assert
            time.Should().Be(DefaultTime);
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_existing_directory_it_must_fail()
        {
            // Arrange
            const string path = @"C:\some\subfolder";

            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingDirectory(path)
                .Build();

            // Act
            Action action = () => fileSystem.File.SetCreationTime(path, DefaultTime);

            // Assert
            action.ShouldThrow<DirectoryNotFoundException>()
                .WithMessage(@"Could not find a part of the path 'C:\some\subfolder'.");
        }

        [Fact]
        private void When_getting_creation_time_in_local_zone_for_parent_directory_that_exists_as_file_it_must_succeed()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingEmptyFile(@"c:\some\file.txt")
                .Build();

            // Act
            DateTime time = fileSystem.File.GetCreationTime(@"c:\some\file.txt\nested.txt");

            // Assert
            time.Should().Be(ZeroFileTime);
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_parent_directory_that_exists_as_file_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingEmptyFile(@"c:\some\file.txt")
                .Build();

            // Act
            Action action = () => fileSystem.File.SetCreationTime(@"c:\some\file.txt\nested.txt", DefaultTime);

            // Assert
            action.ShouldThrow<DirectoryNotFoundException>()
                .WithMessage(@"Could not find a part of the path 'c:\some\file.txt\nested.txt'.");
        }

        [Fact]
        private void When_getting_creation_time_in_local_zone_for_parent_parent_directory_that_exists_as_file_it_must_succeed()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingEmptyFile(@"c:\some\file.txt")
                .Build();

            // Act
            DateTime time = fileSystem.File.GetCreationTime(@"c:\some\file.txt\nested.txt\more.txt");

            // Assert
            time.Should().Be(ZeroFileTime);
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_parent_parent_directory_that_exists_as_file_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingEmptyFile(@"c:\some\file.txt")
                .Build();

            // Act
            Action action = () => fileSystem.File.SetCreationTime(@"c:\some\file.txt\nested.txt\more.txt", DefaultTime);

            // Assert
            action.ShouldThrow<DirectoryNotFoundException>()
                .WithMessage(@"Could not find a part of the path 'c:\some\file.txt\nested.txt\more.txt'.");
        }

        [Fact]
        private void When_getting_creation_time_in_local_zone_for_missing_network_share_it_must_fail()
        {
            // Arrange
            const string path = @"\\server\share\missing.txt";

            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            Action action = () => fileSystem.File.GetCreationTime(path);

            // Assert
            action.ShouldThrow<IOException>().WithMessage("The network path was not found");
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_missing_network_share_it_must_fail()
        {
            // Arrange
            const string path = @"\\server\share\missing.docx";

            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            Action action = () => fileSystem.File.SetCreationTime(path, DefaultTime);

            // Assert
            action.ShouldThrow<IOException>().WithMessage("The network path was not found");
        }

        [Fact]
        private void When_getting_creation_time_in_local_zone_for_existing_network_share_it_must_succeed()
        {
            // Arrange
            const string path = @"\\server\share";

            var clock = new SystemClock { UtcNow = () => DefaultTimeUtc };

            IFileSystem fileSystem = new FakeFileSystemBuilder(clock)
                .IncludingDirectory(path)
                .Build();

            // Act
            DateTime time = fileSystem.File.GetCreationTime(path);

            // Assert
            time.Should().Be(DefaultTime);
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_existing_network_share_it_must_fail()
        {
            // Arrange
            const string path = @"\\server\share";

            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingDirectory(path)
                .Build();

            // Act
            Action action = () => fileSystem.File.SetCreationTime(path, DefaultTime);

            // Assert
            action.ShouldThrow<UnauthorizedAccessException>().WithMessage(@"Access to the path '\\server\share' is denied.");
        }

        [Fact]
        private void When_getting_creation_time_in_local_zone_for_missing_remote_file_it_must_succeed()
        {
            // Arrange
            const string path = @"\\server\share\missing.txt";

            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingDirectory(@"\\server\share")
                .Build();

            // Act
            DateTime time = fileSystem.File.GetCreationTime(path);

            // Assert
            time.Should().Be(ZeroFileTime);
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_missing_remote_file_it_must_fail()
        {
            // Arrange
            const string path = @"\\server\share\missing.docx";

            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingDirectory(@"\\server\share")
                .Build();

            // Act
            Action action = () => fileSystem.File.SetCreationTime(path, DefaultTime);

            // Assert
            action.ShouldThrow<FileNotFoundException>().WithMessage(@"Could not find file '\\server\share\missing.docx'.");
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_existing_remote_file_it_must_succeed()
        {
            // Arrange
            const string path = @"\\server\share\personal.docx";

            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingEmptyFile(path)
                .Build();

            // Act
            fileSystem.File.SetCreationTime(path, DefaultTime);

            // Assert
            fileSystem.File.GetCreationTime(path).Should().Be(DefaultTime);
        }

        [Fact]
        private void When_getting_creation_time_in_local_zone_for_reserved_name_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            Action action = () => fileSystem.File.GetCreationTime("COM1");

            // Assert
            action.ShouldThrow<PlatformNotSupportedException>().WithMessage("Reserved names are not supported.");
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_reserved_name_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .Build();

            // Act
            Action action = () => fileSystem.File.SetCreationTime("COM1", DefaultTime);

            // Assert
            action.ShouldThrow<PlatformNotSupportedException>().WithMessage("Reserved names are not supported.");
        }

        [Fact]
        private void When_getting_creation_time_in_local_zone_for_missing_extended_local_file_it_must_succeed()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingDirectory(@"c:\some")
                .Build();

            // Act
            DateTime time = fileSystem.File.GetCreationTime(@"\\?\C:\some\missing.txt");

            // Assert
            time.Should().Be(ZeroFileTime);
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_missing_extended_local_file_it_must_fail()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingDirectory(@"c:\some")
                .Build();

            // Act
            Action action = () => fileSystem.File.SetCreationTime(@"\\?\C:\some\missing.txt", DefaultTime);

            // Assert
            action.ShouldThrow<FileNotFoundException>().WithMessage(@"Could not find file '\\?\C:\some\missing.txt'.");
        }

        [Fact]
        private void When_setting_creation_time_in_local_zone_for_existing_extended_local_file_it_must_succeed()
        {
            // Arrange
            IFileSystem fileSystem = new FakeFileSystemBuilder()
                .IncludingEmptyFile(@"C:\some\other.txt")
                .Build();

            // Act
            fileSystem.File.SetCreationTime(@"\\?\C:\some\other.txt", DefaultTime);

            // Assert
            fileSystem.File.GetCreationTime(@"\\?\C:\some\other.txt").Should().Be(DefaultTime);
        }
    }
}
