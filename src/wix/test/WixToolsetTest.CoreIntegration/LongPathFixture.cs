// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixInternal.Core.TestPackage;
    using WixInternal.TestSupport;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using Xunit;

    public class LongPathFixture
    {
        [Fact]
        public void TestLongPathSupport()
        {
            var testDataFolder = TestData.Get(@"TestData", "SingleFile");

            using (var fs = new DisposableFileSystem())
            {
                var folder = fs.GetFolder();

                while (folder.Length < 500)
                {
                    folder = Path.Combine(folder, new string('z', 100));
                }

                CopyDirectory(testDataFolder, folder);

                var baseFolder = fs.GetFolder();

                while (baseFolder.Length < 500)
                {
                    baseFolder = Path.Combine(baseFolder, new string('a', 100));
                }

                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var outputPath = Path.Combine(baseFolder, "bin", "test.msi");

                var result = WixRunner.Execute(
                [
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", outputPath,
                ]);

                if (result.ExitCode == 223/*WindowsInstallerBackendErrors.OpenDatabaseFailed*/
                    && result.Messages.Length > 0)
                {
                    // In a bit of a twist, WiX's long-path support relies on 8.3 short file names. As
                    // [Raymond Chen suggests](https://devblogs.microsoft.com/oldnewthing/20181004-00/?p=99895),
                    // not all file systems support 8.3 names. That includes ReFS, the file system used
                    // by Dev Drives. So pass the test if the failure is due to missing 8.3 names.
                    Assert.Equal(outputPath, result.Messages[0].MessageArgs[0]);
                    Assert.Equal("The Windows Installer service failed to start. Contact your support personnel.", result.Messages[0].MessageArgs[1]);

                    return;
                }

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(baseFolder, "bin", "test.msi")));
                Assert.True(File.Exists(Path.Combine(baseFolder, "bin", "test.wixpdb")));
                Assert.True(File.Exists(Path.Combine(baseFolder, "bin", "PFiles", "Example Corporation MsiPackage", "test.txt")));

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, "bin", "test.wixpdb"));

                var section = intermediate.Sections.Single();

                var fileSymbol = section.Symbols.OfType<FileSymbol>().First();
                WixAssert.StringEqual(Path.Combine(folder, @"data\test.txt"), fileSymbol[FileSymbolFields.Source].AsPath().Path);
                WixAssert.StringEqual(@"test.txt", fileSymbol[FileSymbolFields.Source].PreviousValue.AsPath().Path);
            }
        }

        private static void CopyDirectory(string sourceFolder, string targetFolder)
        {
            // Ensure the target directory exists
            Directory.CreateDirectory(targetFolder);

            // Copy all files
            foreach (var file in Directory.GetFiles(sourceFolder))
            {
                var targetFile = Path.Combine(targetFolder, Path.GetFileName(file));

                File.Copy(file, targetFile);
            }

            // Recursively copy subdirectories
            foreach (var subFolder in Directory.GetDirectories(sourceFolder))
            {
                var targetSubFolder = Path.Combine(targetFolder, Path.GetFileName(subFolder));

                CopyDirectory(subFolder, targetSubFolder);
            }
        }
    }
}
