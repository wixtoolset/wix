// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.Collections.Generic;
    using System.IO;
    using WixInternal.Core.TestPackage;
    using WixInternal.TestSupport;
    using Xunit;

    public class HardlinkFixture
    {
        [Fact]
        public void CanLayoutUncompressedFilesWithHardlinks()
        {
            var sourceModified = BuildAndModifyLaidOutFile(suppressHardlinks: false);

            Assert.True(sourceModified, "Expected the laid out file to be hard linked to its source.");
        }

        [Fact]
        public void CanLayoutUncompressedFilesWithoutHardlinks()
        {
            var sourceModified = BuildAndModifyLaidOutFile(suppressHardlinks: true);

            Assert.False(sourceModified, "Expected the laid out file to be copied instead of hard linked to its source.");
        }

        private static bool BuildAndModifyLaidOutFile(bool suppressHardlinks)
        {
            var folder = TestData.Get(@"TestData\SingleFile");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                // Copy the source data next to the output so hard links are possible (same volume).
                var dataFolder = Path.Combine(baseFolder, "data");
                var sourcePath = Path.Combine(dataFolder, "test.txt");
                Directory.CreateDirectory(dataFolder);
                File.Copy(Path.Combine(folder, @"data\test.txt"), sourcePath);
                var originalSource = File.ReadAllText(sourcePath);

                var args = new List<string>
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", dataFolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.msi")
                };

                if (suppressHardlinks)
                {
                    args.Add("-nohardlinks");
                }

                var result = WixRunner.Execute(args.ToArray());
                result.AssertSuccess();

                var laidOutPath = Path.Combine(baseFolder, @"bin\PFiles\Example Corporation MsiPackage\test.txt");
                Assert.True(File.Exists(laidOutPath));

                File.WriteAllText(laidOutPath, "modified after layout");

                return File.ReadAllText(sourcePath) != originalSource;
            }
        }
    }
}
