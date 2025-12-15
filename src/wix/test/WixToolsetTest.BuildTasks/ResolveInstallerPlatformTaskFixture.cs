// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BuildTasks
{
    using WixInternal.TestSupport;
    using WixToolset.BuildTasks;
    using Xunit;

    public class ResolveInstallerPlatformTaskFixture
    {

        [Fact]
        public void Execute_WithOnlyInstallerPlatform_ResolvesCorrectly()
        {
            var logger = new FakeMsbuildLogger();

            var task = new ResolveInstallerPlatform(logger)
            {
                InstallerPlatform = "x64",
                Platform = "",
            };

            Assert.True(task.Execute());

            Assert.Empty(logger.Messages);
            Assert.Equal("x64", task.ResolvedInstallerPlatform);
            Assert.Equal("x64", task.ResolvedPlatform);
        }

        [Fact]
        public void Execute_WithInvalidRuntimeIdentifier_LogsErrorAndReturnsFalse()
        {
            var logger = new FakeMsbuildLogger();

            var task = new ResolveInstallerPlatform(logger)
            {
                RuntimeIdentifier = "win10",
            };

            Assert.False(task.Execute());

            WixAssert.CompareLineByLine(
                new[] { "Error: The RuntimeIdentifier 'win10' is not valid." },
                logger.Messages.ToArray());
        }

        [Fact]
        public void Execute_WithNonWindowsRuntimeIdentifier_LogsErrorAndReturnsFalse()
        {
            var logger = new FakeMsbuildLogger();

            var task = new ResolveInstallerPlatform(logger)
            {
                RuntimeIdentifier = "linux-x64",
            };

            Assert.False(task.Execute());

            WixAssert.CompareLineByLine(
                new[] { "Error: The RuntimeIdentifier 'linux-x64' is not a valid Windows RuntimeIdentifier." },
                logger.Messages.ToArray());
        }

        [Fact]
        public void Execute_WithRuntimeIdentifierSpecifyingMultiplePlatforms_LogsErrorAndReturnsFalse()
        {
            var logger = new FakeMsbuildLogger();

            var task = new ResolveInstallerPlatform(logger)
            {
                RuntimeIdentifier = "win10-x64-arm64",
                Platform = "",
            };

            Assert.False(task.Execute());

            WixAssert.CompareLineByLine(
                new[] { "Error: The RuntimeIdentifier 'win10-x64-arm64' specifies multiple platforms which is not supported." },
                logger.Messages.ToArray());

            // Despite the logged error the platform resolver sets the first platform it finds.
            Assert.Equal("x64", task.ResolvedInstallerPlatform);
            Assert.Equal("x64", task.ResolvedPlatform);
        }

        [Fact]
        public void Execute_WithRuntimeIdentifierAndInitialInstallerPlatformMismatch_LogsErrorAndReturnsFalse()
        {
            var logger = new FakeMsbuildLogger();

            var task = new ResolveInstallerPlatform(logger)
            {
                RuntimeIdentifier = "win10-x64",
                InitialInstallerPlatform = "x86",
                Platform = "",
            };

            Assert.False(task.Execute());

            WixAssert.CompareLineByLine(
                new[] { "Error: The RuntimeIdentifier 'win10-x64' resolves to platform 'x64', which conflicts with the provided InstallerPlatform 'x86'." },
                logger.Messages.ToArray());

            Assert.Null(task.ResolvedInstallerPlatform);
            Assert.Null(task.ResolvedPlatform);
        }

        [Fact]
        public void Execute_WithRuntimeIdentifierAndInitialInstallerPlatformMatches_ResolvesToPlatform()
        {
            var logger = new FakeMsbuildLogger();

            var task = new ResolveInstallerPlatform(logger)
            {
                RuntimeIdentifier = "win10-x64",
                InitialInstallerPlatform = "x64",
                Platform = "",
            };

            Assert.True(task.Execute());

            Assert.Empty(logger.Messages);
            Assert.Equal("x64", task.ResolvedInstallerPlatform);
            Assert.Equal("x64", task.ResolvedPlatform);
        }

        [Fact]
        public void Execute_WithMismatchedPlatformProperty_LogsWarningButReturnsTrue()
        {
            var logger = new FakeMsbuildLogger();

            var task = new ResolveInstallerPlatform(logger)
            {
                RuntimeIdentifier = "win10-x64",
                Platform = "x86",
            };

            Assert.True(task.Execute());

            WixAssert.CompareLineByLine(
                new[] { "Warning: The provided Platform 'x86' does not match the resolved InstallerPlatform 'x64'. The output will be built using 'x64'." },
                logger.Messages.ToArray());

            Assert.Equal("x64", task.ResolvedInstallerPlatform);
            Assert.Null(task.ResolvedPlatform);
        }
    }
}
