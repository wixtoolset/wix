// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.MsiE2E;

using System;
using System.IO;
using WixTestTools;
using Xunit;
using Xunit.Abstractions;

public class TouchFileTests : MsiE2ETests
{
    public TouchFileTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [RuntimeFact]
    public void CanValidateTouchFile()
    {
        var touchFileTestPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "touch-file-test.txt");

        try
        {
            var touchFileTime = new DateTime(2004, 4, 5, 0, 0, 0, DateTimeKind.Utc);

            File.WriteAllText(touchFileTestPath, "This file exists to test CanValidateTouchFile()");
            File.SetCreationTimeUtc(touchFileTestPath, touchFileTime);
            File.SetLastAccessTimeUtc(touchFileTestPath, touchFileTime);
            File.SetLastWriteTimeUtc(touchFileTestPath, touchFileTime);

            var product = this.CreatePackageInstaller("TouchFile");

            var justBeforeInstall = DateTime.UtcNow;
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            var touchFile = new FileInfo(touchFileTestPath);
            Assert.Equal(touchFileTime, touchFile.CreationTimeUtc);
            Assert.Equal(touchFileTime, touchFile.LastAccessTimeUtc);
            Assert.True(touchFile.LastWriteTimeUtc >= justBeforeInstall, $"Touch file {touchFileTestPath} last write time: {touchFile.LastWriteTimeUtc} of file should have been updated to at least: {justBeforeInstall}");

            product.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);
        }
        finally
        {
            File.Delete(touchFileTestPath);
        }
    }
}
