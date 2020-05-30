// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Xml.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class PatchFixture
    {
        private static readonly XNamespace PatchNamespace = "http://www.microsoft.com/msi/patch_applicability.xsd";

        [Fact]
        public void CanBuildSimplePatch()
        {
            var folder = TestData.Get(@"TestData\PatchSingle");

            using (var fs = new DisposableFileSystem())
            {
                var tempFolder = fs.GetFolder();

                var baselinePdb = BuildMsi("Baseline.msi", folder, tempFolder, "1.0.0", "1.0.0", "1.0.0");
                var update1Pdb = BuildMsi("Update.msi", folder, tempFolder, "1.0.1", "1.0.1", "1.0.1");
                var patchPdb = BuildMsp("Patch1.msp", folder, tempFolder, "1.0.1");
                var baselinePath = Path.ChangeExtension(baselinePdb, ".msp");
                var patchPath = Path.ChangeExtension(patchPdb, ".msp");

                Assert.True(File.Exists(baselinePdb));
                Assert.True(File.Exists(update1Pdb));

                var doc = GetExtractPatchXml(patchPath);
                Assert.Equal("{7D326855-E790-4A94-8611-5351F8321FCA}", doc.Root.Element(PatchNamespace + "TargetProductCode").Value);

                var names = Query.GetSubStorageNames(patchPath);
                Assert.Equal(new[] { "#RTM.1", "RTM.1" }, names);

                var cab = Path.Combine(tempFolder, "foo.cab");
                Query.ExtractStream(patchPath, "foo.cab", cab);
                Assert.True(File.Exists(cab));

                var files = Query.GetCabinetFiles(cab);
                Assert.Equal(new[] { "a.txt", "b.txt" }, files.Select(f => f.Name).ToArray());
            }
        }

        private static string BuildMsi(string outputName, string sourceFolder, string baseFolder, string defineV, string defineA, string defineB)
        {
            var outputPath = Path.Combine(baseFolder, Path.Combine("bin", outputName));

            var result = WixRunner.Execute(new[]
            {
                "build",
                Path.Combine(sourceFolder, @"Package.wxs"),
                "-d", "V=" + defineV,
                "-d", "A=" + defineA,
                "-d", "B=" + defineB,
                "-bindpath", Path.Combine(sourceFolder, ".data"),
                "-intermediateFolder", Path.Combine(baseFolder, "obj"),
                "-o", outputPath
            });

            result.AssertSuccess();

            return Path.ChangeExtension(outputPath, ".wixpdb");
        }

        private static string BuildMsp(string outputName, string sourceFolder, string baseFolder, string defineV)
        {
            var outputPath = Path.Combine(baseFolder, Path.Combine("bin", outputName));

            var result = WixRunner.Execute(new[]
            {
                "build",
                Path.Combine(sourceFolder, @"Patch.wxs"),
                "-d", "V=" + defineV,
                "-bindpath", Path.Combine(baseFolder, "bin"),
                "-intermediateFolder", Path.Combine(baseFolder, "obj"),
                "-o", outputPath
            });

            result.AssertSuccess();

            return Path.ChangeExtension(outputPath, ".wixpdb");
        }

        private static XDocument GetExtractPatchXml(string path)
        {
            var buffer = new StringBuilder(65535);
            var size = buffer.Capacity;

            var er = MsiExtractPatchXMLData(path, 0, buffer, ref size);
            if (er != 0)
            {
                throw new Win32Exception(er);
            }

            return XDocument.Parse(buffer.ToString());
        }

        [DllImport("msi.dll", EntryPoint = "MsiExtractPatchXMLDataW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int MsiExtractPatchXMLData(string szPatchPath, int dwReserved, StringBuilder szXMLData, ref int pcchXMLData);

        [DllImport("msi.dll", EntryPoint = "MsiApplyPatchW", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int MsiApplyPatch(string szPatchPackage, string szInstallPackage, int eInstallType, string szCommandLine);
    }
}
