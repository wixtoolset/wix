// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixBuildTools.TestSupport
{
    using System;

    /// <summary>
    /// This class builds on top of DisposableFileSystem
    /// to make it easy to write a test that needs a whole folder of test data copied to a temp location
    /// that will automatically be cleaned up at the end of the test.
    /// </summary>
    public class TestDataFolderFileSystem : IDisposable
    {
        private DisposableFileSystem fileSystem;

        public string BaseFolder { get; private set; }

        public void Dispose()
        {
            this.fileSystem?.Dispose();
        }

        public void Initialize(string sourceDirectoryPath)
        {
            if (this.fileSystem != null)
            {
                throw new InvalidOperationException();
            }
            this.fileSystem = new DisposableFileSystem();

            this.BaseFolder = this.fileSystem.GetFolder();

            RobocopyFolder(sourceDirectoryPath, this.BaseFolder);
        }

        private static ExternalExecutableResult RobocopyFolder(string sourceFolderPath, string destinationFolderPath)
        {
            var args = $"\"{sourceFolderPath}\" \"{destinationFolderPath}\" /E /R:1 /W:1";
            return RobocopyRunner.Execute(args);
        }
    }
}
