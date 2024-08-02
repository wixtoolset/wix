// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.MsiE2E;

using System;
using System.IO;
using WixTestTools;
using Xunit;
using Xunit.Abstractions;

public class RemoveFolderExTests : MsiE2ETests
{
    public RemoveFolderExTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Fact]
    public void CanRemoveFolderExOnInstallAndUninstall()
    {
        var removeFolderExTestDir1 =    "C:\\RemoveFolderExTest";
        var removeFolderExTestFile1 =   Path.Combine(removeFolderExTestDir1, "testfile.txt");
        var removeFolderExTestDir2 =    Path.Combine(removeFolderExTestDir1, "TestFolder1");
        var removeFolderExTestFile2 =   Path.Combine(removeFolderExTestDir1, "TestFolder1", "testfile");

        try
        {
            var product = this.CreatePackageInstaller("RemoveFolderExTest");

            Directory.CreateDirectory(removeFolderExTestDir1);
            File.Create(removeFolderExTestFile1).Dispose();
            Directory.CreateDirectory(removeFolderExTestDir2);
            File.Create(removeFolderExTestFile2).Dispose();

            if( !Directory.Exists(removeFolderExTestDir1)
                || !File.Exists(removeFolderExTestFile1)
                || !Directory.Exists(removeFolderExTestDir2)
                || !File.Exists(removeFolderExTestFile2))
            {
                Assert.Fail("Failed to create initial folder and file structure before install test");
            }

            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            Assert.False(Directory.Exists(removeFolderExTestDir1), $"Failed to remove {removeFolderExTestDir1} on install");
            Assert.False(File.Exists(removeFolderExTestFile1), $"Failed to remove {removeFolderExTestFile1} on install");
            Assert.False(Directory.Exists(removeFolderExTestDir2), $"Failed to remove {removeFolderExTestDir2} on install");
            Assert.False(File.Exists(removeFolderExTestFile1), $"Failed to remove {removeFolderExTestFile2} on install");


            Directory.CreateDirectory(removeFolderExTestDir1);
            File.Create(removeFolderExTestFile1).Dispose();
            Directory.CreateDirectory(removeFolderExTestDir2);
            File.Create(removeFolderExTestFile2).Dispose();

            if (!Directory.Exists(removeFolderExTestDir1)
                || !File.Exists(removeFolderExTestFile1)
                || !Directory.Exists(removeFolderExTestDir2)
                || !File.Exists(removeFolderExTestFile2))
            {
                Assert.Fail("Failed to create initial folder and file structure before uninstall test");
            }

            product.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            Assert.False(Directory.Exists(removeFolderExTestDir1), $"Failed to remove {removeFolderExTestDir1} on uninstall");
            Assert.False(File.Exists(removeFolderExTestFile1), $"Failed to remove {removeFolderExTestFile1} on uninstall");
            Assert.False(Directory.Exists(removeFolderExTestDir2), $"Failed to remove {removeFolderExTestDir2} on uninstall");
            Assert.False(File.Exists(removeFolderExTestFile2), $"Failed to remove {removeFolderExTestFile2} on uninstall");

        }
        finally
        {
            try
            {
                Directory.Delete(removeFolderExTestDir1, true);
            }
            catch
            {
            }
        }
    }
}
