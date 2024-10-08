// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using Example.Extension;
    using WixInternal.TestSupport;
    using WixToolset.Core.Burn;
    using WixInternal.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Symbols;
    using WixToolset.Dtf.Resources;
    using Xunit;

    public class BundleFixture
    {
        [Fact]
        public void CanBuildBundleWithBindVariableVersion()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundleBindVariables", "BindVarBundleVersion.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(exePath));
            }
        }

        [Fact]
        public void CanBuildMultiFileBundle()
        {
            var folder = TestData.Get(@"TestData\SimpleBundle");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "MultiFileBootstrapperApplication.wxs"),
                    Path.Combine(folder, "MultiFileBundle.wxs"),
                    "-loc", Path.Combine(folder, "Bundle.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.exe")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.exe")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.wixpdb")));
            }
        }

        [Fact]
        public void CanBuildSimpleBundle()
        {
            var folder = TestData.Get(@"TestData\SimpleBundle");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");
                var pdbPath = Path.Combine(baseFolder, @"bin\test.wixpdb");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Bundle.wxs"),
                    "-loc", Path.Combine(folder, "Bundle.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                result.AssertSuccess();
                Assert.DoesNotContain(result.Messages, m => m.Level == MessageLevel.Warning);

                Assert.True(File.Exists(exePath));
                Assert.True(File.Exists(pdbPath));

                using (var wixOutput = WixOutput.Read(pdbPath))
                {

                    var intermediate = Intermediate.Load(wixOutput);
                    var section = intermediate.Sections.Single();

                    var bundleSymbol = section.Symbols.OfType<WixBundleSymbol>().Single();
                    WixAssert.StringEqual("1.0.0.0", bundleSymbol.Version);

                    var previousVersion = bundleSymbol.Fields[(int)WixBundleSymbolFields.Version].PreviousValue;
                    WixAssert.StringEqual("!(bind.packageVersion.test.msi)", previousVersion.AsString());

                    var msiSymbol = section.Symbols.OfType<WixBundlePackageSymbol>().Single();
                    WixAssert.StringEqual("test.msi", msiSymbol.Id.Id);

                    var extractResult = BundleExtractor.ExtractBAContainer(null, exePath, baFolderPath, extractFolderPath);
                    extractResult.AssertSuccess();

                    var burnManifestData = wixOutput.GetData(BurnConstants.BurnManifestWixOutputStreamName);
                    var extractedBurnManifestData = File.ReadAllText(Path.Combine(baFolderPath, "manifest.xml"), Encoding.UTF8);
                    WixAssert.StringEqual(extractedBurnManifestData, burnManifestData);

                    var baManifestData = wixOutput.GetData(BurnConstants.BootstrapperApplicationDataWixOutputStreamName);
                    var extractedBaManifestData = File.ReadAllText(Path.Combine(baFolderPath, "BootstrapperApplicationData.xml"), Encoding.UTF8);
                    WixAssert.StringEqual(extractedBaManifestData, baManifestData);

                    var bextManifestData = wixOutput.GetData(BurnConstants.BootstrapperExtensionDataWixOutputStreamName);
                    var extractedBextManifestData = File.ReadAllText(Path.Combine(baFolderPath, "BootstrapperExtensionData.xml"), Encoding.UTF8);
                    WixAssert.StringEqual(extractedBextManifestData, bextManifestData);

                    foreach (XmlAttribute attribute in extractResult.ManifestDocument.DocumentElement.Attributes)
                    {
                        switch (attribute.LocalName)
                        {
                            case "EngineVersion":
                                Assert.True(Version.TryParse(attribute.Value, out var _));
                                break;
                            case "ProtocolVersion":
                                WixAssert.StringEqual("1", attribute.Value);
                                break;
                            case "Win64":
                                WixAssert.StringEqual("no", attribute.Value);
                                break;
                            case "xmlns":
                                WixAssert.StringEqual("http://wixtoolset.org/schemas/v4/2008/Burn", attribute.Value);
                                break;
                            default:
                                Assert.Fail($"Attribute: '{attribute.LocalName}', Value: '{attribute.Value}'");
                                break;
                        }
                    }

                    var logElements = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:Log");
                    WixAssert.CompareLineByLine(new[]
                    {
                        "<Log PathVariable='WixBundleLog' Prefix='~TestBundle' Extension='log' />",
                    }, logElements);

                    var registrationElements = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:Registration");
                    WixAssert.CompareLineByLine(new[]
                    {
                        $"<Registration Id='{bundleSymbol.BundleId}' ExecutableName='test.exe' PerMachine='yes' Tag='' Version='1.0.0.0' ProviderKey='{bundleSymbol.BundleId}'>" +
                        "<Arp DisplayName='~TestBundle' DisplayVersion='1.0.0.0' InProgressDisplayName='~InProgressTestBundle' Publisher='Example Corporation' />" +
                        "</Registration>",
                    }, registrationElements);

                    var ignoreAttributesByElementName = new Dictionary<string, List<string>>() { { "Payload", new List<string> { "FileSize", "Hash" } } };
                    var msiPayloads = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:Payload[@Id='test.msi']", ignoreAttributesByElementName);
                    WixAssert.CompareLineByLine(new[]
                    {
                        "<Payload Id='test.msi' FilePath='test.msi' FileSize='*' Hash='*' Packaging='embedded' SourcePath='a0' Container='WixAttachedContainer' />",
                    }, msiPayloads);

                    var msiProperties = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:Chain/burn:MsiPackage[@Id='test.msi']/burn:MsiProperty", ignoreAttributesByElementName);
                    WixAssert.CompareLineByLine(new[]
                    {
                        "<MsiProperty Id='TEST' Value='1' />",
                        "<MsiProperty Id='TESTBLANK' Value='' />",
                        "<MsiProperty Id='ARPSYSTEMCOMPONENT' Value='1' />",
                        "<MsiProperty Id='MSIFASTINSTALL' Value='7' />",
                    }, msiProperties);
                }

                var manifestResource = new Resource(ResourceType.Manifest, "#1", 1033);
                manifestResource.Load(exePath);
                var actualManifestData = Encoding.UTF8.GetString(manifestResource.Data);
                WixAssert.StringEqual("﻿<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n" +
                                      "<assembly xmlns=\"urn:schemas-microsoft-com:asm.v1\" manifestVersion=\"1.0\">" +
                                      "<assemblyIdentity name=\"WixToolset.Burn\" version=\"5.0.0.0\" type=\"win32\"></assemblyIdentity><description>WiX Toolset Bootstrapper Engine</description>" +
                                      "<trustInfo xmlns=\"urn:schemas-microsoft-com:asm.v3\"><security><requestedPrivileges><requestedExecutionLevel level=\"asInvoker\" uiAccess=\"false\"></requestedExecutionLevel></requestedPrivileges></security></trustInfo>" +
                                      "<application xmlns=\"urn:schemas-microsoft-com:asm.v3\"><windowsSettings>" +
                                      "<dpiAware xmlns=\"http://schemas.microsoft.com/SMI/2005/WindowsSettings\">true/pm</dpiAware>" +
                                      "<dpiAwareness xmlns=\"http://schemas.microsoft.com/SMI/2016/WindowsSettings\">PerMonitorV2, PerMonitor, System</dpiAwareness>" +
                                      "<longPathAware xmlns=\"http://schemas.microsoft.com/SMI/2016/WindowsSettings\">true</longPathAware>" +
                                      "</windowsSettings></application>" +
                                      "<ms_compatibility:compatibility xmlns:ms_compatibility=\"urn:schemas-microsoft-com:compatibility.v1\" xmlns=\"urn:schemas-microsoft-com:compatibility.v1\">" +
                                      "<ms_compatibility:application xmlns:ms_compatibility=\"urn:schemas-microsoft-com:compatibility.v1\">" +
                                      "<ms_compatibility:supportedOS xmlns:ms_compatibility=\"urn:schemas-microsoft-com:compatibility.v1\" Id=\"{e2011457-1546-43c5-a5fe-008deee3d3f0}\"></ms_compatibility:supportedOS>" +
                                      "<ms_compatibility:supportedOS xmlns:ms_compatibility=\"urn:schemas-microsoft-com:compatibility.v1\" Id=\"{35138b9a-5d96-4fbd-8e2d-a2440225f93a}\"></ms_compatibility:supportedOS>" +
                                      "<ms_compatibility:supportedOS xmlns:ms_compatibility=\"urn:schemas-microsoft-com:compatibility.v1\" Id=\"{4a2f28e3-53b9-4441-ba9c-d69d4a4a6e38}\"></ms_compatibility:supportedOS>" +
                                      "<ms_compatibility:supportedOS xmlns:ms_compatibility=\"urn:schemas-microsoft-com:compatibility.v1\" Id=\"{1f676c76-80e1-4239-95bb-83d0f6d0da78}\"></ms_compatibility:supportedOS>" +
                                      "<ms_compatibility:supportedOS xmlns:ms_compatibility=\"urn:schemas-microsoft-com:compatibility.v1\" Id=\"{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}\"></ms_compatibility:supportedOS>" +
                                      "</ms_compatibility:application></ms_compatibility:compatibility>" +
                                      "</assembly>", actualManifestData);
            }
        }

        [Fact]
        public void CanBuildX64Bundle()
        {
            var folder = TestData.Get(@"TestData\SimpleBundle");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");
                var pdbPath = Path.Combine(baseFolder, @"bin\test.wixpdb");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var attachedFolderPath = Path.Combine(baseFolder, "attached");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    "-arch", "x64",
                    Path.Combine(folder, "Bundle.wxs"),
                    "-loc", Path.Combine(folder, "Bundle.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(exePath));
                Assert.True(File.Exists(pdbPath));

                var manifestResource = new Resource(ResourceType.Manifest, "#1", 1033);
                manifestResource.Load(exePath);
                var actualManifestData = Encoding.UTF8.GetString(manifestResource.Data);
                WixAssert.StringEqual("﻿<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\r\n" +
                                      "<assembly xmlns=\"urn:schemas-microsoft-com:asm.v1\" manifestVersion=\"1.0\">" +
                                      "<assemblyIdentity name=\"WixToolset.Burn\" version=\"5.0.0.0\" type=\"win32\"></assemblyIdentity><description>WiX Toolset Bootstrapper Engine</description>" +
                                      "<trustInfo xmlns=\"urn:schemas-microsoft-com:asm.v3\"><security><requestedPrivileges><requestedExecutionLevel level=\"asInvoker\" uiAccess=\"false\"></requestedExecutionLevel></requestedPrivileges></security></trustInfo>" +
                                      "<application xmlns=\"urn:schemas-microsoft-com:asm.v3\"><windowsSettings>" +
                                      "<dpiAware xmlns=\"http://schemas.microsoft.com/SMI/2005/WindowsSettings\">true/pm</dpiAware>" +
                                      "<dpiAwareness xmlns=\"http://schemas.microsoft.com/SMI/2016/WindowsSettings\">PerMonitorV2, PerMonitor, System</dpiAwareness>" +
                                      "<longPathAware xmlns=\"http://schemas.microsoft.com/SMI/2016/WindowsSettings\">true</longPathAware>" +
                                      "</windowsSettings></application>" +
                                      "<ms_compatibility:compatibility xmlns:ms_compatibility=\"urn:schemas-microsoft-com:compatibility.v1\" xmlns=\"urn:schemas-microsoft-com:compatibility.v1\">" +
                                      "<ms_compatibility:application xmlns:ms_compatibility=\"urn:schemas-microsoft-com:compatibility.v1\">" +
                                      "<ms_compatibility:supportedOS xmlns:ms_compatibility=\"urn:schemas-microsoft-com:compatibility.v1\" Id=\"{e2011457-1546-43c5-a5fe-008deee3d3f0}\"></ms_compatibility:supportedOS>" +
                                      "<ms_compatibility:supportedOS xmlns:ms_compatibility=\"urn:schemas-microsoft-com:compatibility.v1\" Id=\"{35138b9a-5d96-4fbd-8e2d-a2440225f93a}\"></ms_compatibility:supportedOS>" +
                                      "<ms_compatibility:supportedOS xmlns:ms_compatibility=\"urn:schemas-microsoft-com:compatibility.v1\" Id=\"{4a2f28e3-53b9-4441-ba9c-d69d4a4a6e38}\"></ms_compatibility:supportedOS>" +
                                      "<ms_compatibility:supportedOS xmlns:ms_compatibility=\"urn:schemas-microsoft-com:compatibility.v1\" Id=\"{1f676c76-80e1-4239-95bb-83d0f6d0da78}\"></ms_compatibility:supportedOS>" +
                                      "<ms_compatibility:supportedOS xmlns:ms_compatibility=\"urn:schemas-microsoft-com:compatibility.v1\" Id=\"{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}\"></ms_compatibility:supportedOS>" +
                                      "</ms_compatibility:application></ms_compatibility:compatibility>" +
                                      "</assembly>", actualManifestData);

                var extractResult = BundleExtractor.ExtractAllContainers(null, exePath, baFolderPath, attachedFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                foreach (XmlAttribute attribute in extractResult.ManifestDocument.DocumentElement.Attributes)
                {
                    switch (attribute.LocalName)
                    {
                        case "EngineVersion":
                            Assert.True(Version.TryParse(attribute.Value, out var _));
                            break;
                        case "ProtocolVersion":
                            WixAssert.StringEqual("1", attribute.Value);
                            break;
                        case "Win64":
                            WixAssert.StringEqual("yes", attribute.Value);
                            break;
                        case "xmlns":
                            WixAssert.StringEqual("http://wixtoolset.org/schemas/v4/2008/Burn", attribute.Value);
                            break;
                        default:
                            Assert.Fail($"Attribute: '{attribute.LocalName}', Value: '{attribute.Value}'");
                            break;
                    }
                }
            }
        }

        [Fact]
        public void CanBuildSimpleBundleUsingExtensionBA()
        {
            var folder = TestData.Get(@"TestData\SimpleBundle");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "MultiFileBundle.wxs"),
                    "-loc", Path.Combine(folder, "Bundle.en-us.wxl"),
                    "-ext", ExtensionPaths.ExampleExtensionPath,
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.exe")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.exe")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.wixpdb")));
            }
        }

        [Fact]
        public void CanBuildSimpleBundleUsingInclude()
        {
            var folder = TestData.Get(@"TestData", "IncludePath");
            var dataFolder = TestData.Get(@"TestData", "SimpleBundle", "data");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");
                var pdbPath = Path.Combine(baseFolder, @"bin\test.wixpdb");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Bundle.wxs"),
                    "-loc", Path.Combine(folder, "Bundle.en-us.wxl"),
                    "-bindpath", dataFolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                result.AssertSuccess();

                using (var wixOutput = WixOutput.Read(pdbPath))
                {

                    var intermediate = Intermediate.Load(wixOutput);
                    var section = intermediate.Sections.Single();

                    var bundleSymbol = section.Symbols.OfType<WixBundleSymbol>().Single();
                    WixAssert.StringEqual("~IncludeTestBundle", bundleSymbol.Name);
                }
            }
        }

        [Fact]
        public void CanBuildSingleExeBundle()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "SingleExeBundle", "SingleExePackageGroup.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(exePath));
            }
        }

        [Fact]
        public void CanBuildSingleExeRemotePayloadBundle()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");
                var pdbPath = Path.Combine(baseFolder, @"bin\test.wixpdb");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "SingleExeBundle", "SingleExeRemotePayload.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(exePath));
                Assert.True(File.Exists(pdbPath));

                using (var wixOutput = WixOutput.Read(pdbPath))
                {
                    var intermediate = Intermediate.Load(wixOutput);
                    var section = intermediate.Sections.Single();

                    var packageSymbol = section.Symbols.OfType<WixBundlePackageSymbol>().Where(x => x.Id.Id == "NetFx462Web").Single();
                    Assert.Equal(Int64.MaxValue, packageSymbol.InstallSize);

                    var payloadSymbol = section.Symbols.OfType<WixBundlePayloadSymbol>().Where(x => x.Id.Id == "NetFx462Web").Single();
                    Assert.Equal(Int64.MaxValue, payloadSymbol.FileSize);
                }
            }
        }

        [Fact]
        public void CannotBuildBundleMissingMsiSource()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundleWithMissingSource", "BundleMissingMsiSource.wxs"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.exe")
                });

                var message = result.Messages.Where(m => m.Level == MessageLevel.Error).Select(m => m.ToString().Replace(folder, "<testdata>")).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"The MsiPackage element's Name or SourceFile attribute was not found; one of these is required."
                }, message);
            }
        }

        [Fact]
        public void CannotBuildBundleMissingMsuSource()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundleWithMissingSource", "BundleMissingMsuSource.wxs"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.exe")
                });

                var message = result.Messages.Where(m => m.Level == MessageLevel.Error).Select(m => m.ToString().Replace(folder, "<testdata>")).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"The MsuPackage element's Name or SourceFile attribute was not found; one of these is required."
                }, message);
            }
        }

        [Fact]
        public void CannotBuildBundleWithInvalidIcon()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundleWithInvalid", "BundleWithInvalidIcon.wxs"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.exe")
                });

                var message = result.Messages.Where(m => m.Level == MessageLevel.Error).Select(m => m.ToString().Replace(folder, "<testdata>")).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"Failed to update resources in the bundle. Ensure the bundle icon file is an icon file at '<testdata>\.Data\burn.exe'. Detail: Failed to save resource. Error: 87"
                }, message);
            }
        }

        [Fact]
        public void CannotBuildBundleWithInvalidUpgradeCode()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundleLocalized", "BundleWithLocalizedUpgradeCode.wxs"),
                    "-loc", Path.Combine(folder, "BundleLocalized", "BundleWithInvalidUpgradeCode.wxl"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.exe")
                });

                var message = result.Messages.Where(m => m.Level == MessageLevel.Error).Select(m => m.ToString().Replace(folder, "<testdata>")).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "The Bundle/@UpgradeCode attribute's value, 'NOT-A-GUID', is not a legal guid value."
                }, message);
            }
        }

        [Fact]
        public void CanBuildUncompressedBundle()
        {
            var folder = TestData.Get(@"TestData") + Path.DirectorySeparatorChar;

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder() + Path.DirectorySeparatorChar;
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");
                var trackingFile = Path.Combine(intermediateFolder, "trackingFile.txt");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundleUncompressed", "UncompressedBundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                    "-trackingFile", trackingFile
                });

                result.AssertSuccess();

                Assert.True(File.Exists(exePath));
                Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(exePath), "test.txt")));

                var trackedLines = File.ReadAllLines(trackingFile).Select(s => s.Replace(baseFolder, null, StringComparison.OrdinalIgnoreCase).Replace(folder, null, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(s => s)
                    .ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "BuiltPdbOutput\tbin\\test.wixpdb",
                    "BuiltTargetOutput\tbin\\test.exe",
                    "CopiedOutput\tbin\\test.txt",
                    "Input\tSimpleBundle\\data\\fakeba.dll",
                    "Input\tSimpleBundle\\data\\MsiPackage\\test.txt"
                }, trackedLines);
            }
        }

        [Fact]
        public void CannotBuildWithDuplicateCacheIds()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BadInput", "DuplicateCacheIds.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                Assert.Equal(8001, result.ExitCode);
            }
        }

        [Fact]
        public void CannotBuildWithDuplicatePayloadNames()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin", "test.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BadInput", "DuplicatePayloadNames.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                var messages = result.Messages.Select(WixMessageFormatter.FormatMessage).OrderBy(m => m).ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    "Error 8002: The Payload 'DuplicatePayloadNames.wxs' has a duplicate Name 'fakeba.dll' in the BA container. When extracting the container at runtime, the file will get overwritten.",
                    "Error 8002: The Payload 'uxmKgAFS4cS31ZH_Myfqo5J4kHixQ' has a duplicate Name 'BootstrapperExtensionData.xml' in the BA container. When extracting the container at runtime, the file will get overwritten.",
                    "Error 8002: The Payload 'uxTxMXPVMXwQrPTMIGa5WGt93w0Ns' has a duplicate Name 'BootstrapperApplicationData.xml' in the BA container. When extracting the container at runtime, the file will get overwritten.",
                    "Error 8003: The location of the payload related to the previous error.",
                    "Error 8003: The location of the payload related to the previous error.",
                    "Error 8003: The location of the payload related to the previous error.",
                    "Error 8004: The external Container 'MsiPackagesContainer' has a duplicate Name 'ContainerCollision'. When building the bundle or laying out the bundle, the file will get overwritten.",
                    "Error 8004: The external Payload 'HiddenPersistedBundleVariable.wxs' has a duplicate Name 'PayloadCollision'. When building the bundle or laying out the bundle, the file will get overwritten.",
                    "Error 8005: The location of the symbol related to the previous error.",
                    "Error 8005: The location of the symbol related to the previous error.",
                    "Error 8006: The Payload 'test.msi' has a duplicate Name 'test.msi' in package 'test.msi'. When caching the package, the file will get overwritten.",
                    "Error 8007: The location of the payload related to the previous error.",
                    "Error 8500: The Payload 'Auto2' has a duplicate Name 'burn.exe' in the attached container. When extracting the bundle with `wix burn extract`, the file will get overwritten.",
                    "Error 8501: The location of the payload related to the previous error."
                }, messages);
            }
        }

        [Fact]
        public void CannotBuildWithOrphanPayload()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BadInput", "OrphanPayload.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "MinimalPackageGroup.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                Assert.Equal(7000, result.ExitCode);
            }
        }

        [Fact]
        public void CannotBuildWithPackageInMultipleContainers()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BadInput", "PackageInMultipleContainers.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "MinimalPackageGroup.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                Assert.Equal(7001, result.ExitCode);
            }
        }

        [Fact]
        public void CanBuildWithSubfolderContainer()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin", "test.exe");
                var containerPath = Path.Combine(baseFolder, "bin", "Data", "c1");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Bundle", "SubfolderContainer.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(containerPath), $"Failed to find external container: {containerPath}");
            }
        }

        [Fact]
        public void CannotBuildWithUnscheduledPackage()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BadInput", "UnscheduledPackage.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                Assert.Equal(7003, result.ExitCode);
            }
        }

        [Fact]
        public void CannotBuildWithUnscheduledRollbackBoundary()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BadInput", "UnscheduledRollbackBoundary.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                Assert.Equal(7004, result.ExitCode);
            }
        }

        [Fact]
        public void CannotBuildWithMissingBootstrapperApplication()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, "bin", "test.exe");

                var result =  WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundleWithInvalid", "BundleWithMissingBA.wxs"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                var messages = result.Messages.Select(WixMessageFormatter.FormatMessage).ToArray();

                WixAssert.CompareLineByLine(new[]
                {
                    "Error 8015: A BundleApplication is required to build a bundle."
                }, messages);
            }
        }

        [Fact]
        public void CanBuildBundleWithMsiPackageWithoutComponents()
        {
            var folder = TestData.Get(@"TestData\BundleWithComponentlessPackage");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Bundle.wxs"),
                    "-loc", Path.Combine(folder, "Bundle.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.exe")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.exe")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.wixpdb")));
            }
        }
    }
}
