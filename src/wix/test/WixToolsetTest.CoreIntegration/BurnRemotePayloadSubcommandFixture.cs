// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class BurnRemotePayloadSubcommandFixture
    {
        [Fact]
        public void CanGetRemoteBundlePayload()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var outputFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(outputFolder, "obj");
                var exePath = Path.Combine(outputFolder, @"bin\test.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "RemotePayload", "DiversePayloadsBundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                result.AssertSuccess();

                File.Copy(Path.Combine(folder, ".Data", "signed_bundle_engine.exe"), Path.Combine(outputFolder, "bin", "signed_bundle_engine.exe"));

                // None
                var noneOutFile = Path.Combine(outputFolder, "none_out.xml");

                result = WixRunner.Execute(new[]
                {
                    "burn", "remotepayload",
                    exePath,
                    "-o", noneOutFile,
                    "-packagetype", "bundle",
                    "-bundlepayloadgeneration", "none",
                });

                result.AssertSuccess();

                var xml = File.ReadAllText(noneOutFile);
                var ignoreAttributesByElementName = new Dictionary<string, List<string>>
                {
                    { "BundlePackagePayload", new List<string> { "Size", "Hash" } },
                    { "RemoteBundle", new List<string> { "BundleId", "EngineVersion", "ProviderKey" } },
                    { "Payload", new List<string> { "Size", "Hash" } },
                };
                WixAssert.StringEqual(
                    "<BundlePackage>" +
                    "<BundlePackagePayload Name='test.exe' ProductName='DiversePayloadsBundle' Description='DiversePayloadsBundle' Hash='*' Size='*' Version='1.0.0.0'>" +
                    "<RemoteBundle BundleId='*' DisplayName='DiversePayloadsBundle' EngineVersion='*' InstallSize='3790116' ManifestNamespace='http://wixtoolset.org/schemas/v4/2008/Burn' PerMachine='yes' ProviderKey='*' ProtocolVersion='1' Version='1.0.0.0' Win64='no' UpgradeCode='{FEF1D2B8-4737-4A2A-9F91-77F7294FB55B}' />" +
                    "</BundlePackagePayload>" +
                    "</BundlePackage>", xml.GetTestXml(ignoreAttributesByElementName));

                // ExternalWithoutDownloadUrl
                var externalWithoutDownloadUrlOutFile = Path.Combine(outputFolder, "externalWithoutDownloadUrl_out.xml");

                result = WixRunner.Execute(new[]
                {
                    "burn", "remotepayload",
                    exePath,
                    "-o", externalWithoutDownloadUrlOutFile,
                    "-packagetype", "bundle",
                    "-bundlepayloadgeneration", "externalWithoutDownloadUrl",
                });

                result.AssertSuccess();

                xml = File.ReadAllText(externalWithoutDownloadUrlOutFile);
                WixAssert.StringEqual(
                    "<BundlePackage>" +
                    "<BundlePackagePayload Name='test.exe' ProductName='DiversePayloadsBundle' Description='DiversePayloadsBundle' Hash='*' Size='*' Version='1.0.0.0'>" +
                    "<RemoteBundle BundleId='*' DisplayName='DiversePayloadsBundle' EngineVersion='*' InstallSize='3790116' ManifestNamespace='http://wixtoolset.org/schemas/v4/2008/Burn' PerMachine='yes' ProviderKey='*' ProtocolVersion='1' Version='1.0.0.0' Win64='no' UpgradeCode='{FEF1D2B8-4737-4A2A-9F91-77F7294FB55B}' />" +
                    "</BundlePackagePayload>" +
                    "<Payload Name='External.cab' Hash='*' Size='*' />" +
                    "<Payload Name='test.msi' Hash='*' Size='*' />" +
                    "<Payload Name='test.txt' Hash='*' Size='*' />" +
                    "<Payload Name='Shared.dll' Hash='*' Size='*' />" +
                    "</BundlePackage>", xml.GetTestXml(ignoreAttributesByElementName));

                // External
                var externalOutFile = Path.Combine(outputFolder, "external_out.xml");

                result = WixRunner.Execute(new[]
                {
                    "burn", "remotepayload",
                    exePath,
                    "-o", externalOutFile,
                    "-packagetype", "bundle",
                    "-bundlepayloadgeneration", "external",
                });

                result.AssertSuccess();

                xml = File.ReadAllText(externalOutFile);
                WixAssert.StringEqual(
                    "<BundlePackage>" +
                    "<BundlePackagePayload Name='test.exe' ProductName='DiversePayloadsBundle' Description='DiversePayloadsBundle' Hash='*' Size='*' Version='1.0.0.0'>" +
                    "<RemoteBundle BundleId='*' DisplayName='DiversePayloadsBundle' EngineVersion='*' InstallSize='3790116' ManifestNamespace='http://wixtoolset.org/schemas/v4/2008/Burn' PerMachine='yes' ProviderKey='*' ProtocolVersion='1' Version='1.0.0.0' Win64='no' UpgradeCode='{FEF1D2B8-4737-4A2A-9F91-77F7294FB55B}' />" +
                    "</BundlePackagePayload>" +
                    "<Payload Name='External.cab' Hash='*' Size='*' />" +
                    "<Payload Name='Windows8.1-KB2937592-x86.msu' Hash='*' Size='*' />" +
                    "<Payload Name='test.msi' Hash='*' Size='*' />" +
                    "<Payload Name='test.txt' Hash='*' Size='*' />" +
                    "<Payload Name='Shared.dll' Hash='*' Size='*' />" +
                    "</BundlePackage>", xml.GetTestXml(ignoreAttributesByElementName));

                // All
                var allOutFile = Path.Combine(outputFolder, "all_out.xml");

                result = WixRunner.Execute(new[]
                {
                    "burn", "remotepayload",
                    exePath,
                    "-o", allOutFile,
                    "-packagetype", "bundle",
                    "-bundlepayloadgeneration", "all",
                });

                result.AssertSuccess();

                xml = File.ReadAllText(allOutFile);
                WixAssert.StringEqual(
                    "<BundlePackage>" +
                    "<BundlePackagePayload Name='test.exe' ProductName='DiversePayloadsBundle' Description='DiversePayloadsBundle' Hash='*' Size='*' Version='1.0.0.0'>" +
                    "<RemoteBundle BundleId='*' DisplayName='DiversePayloadsBundle' EngineVersion='*' InstallSize='3790116' ManifestNamespace='http://wixtoolset.org/schemas/v4/2008/Burn' PerMachine='yes' ProviderKey='*' ProtocolVersion='1' Version='1.0.0.0' Win64='no' UpgradeCode='{FEF1D2B8-4737-4A2A-9F91-77F7294FB55B}' />" +
                    "</BundlePackagePayload>" +
                    "<Payload Name='External.cab' Hash='*' Size='*' />" +
                    "<Payload Name='signed_bundle_engine.exe' ProductName='~TestBundle' Description='~TestBundle' Hash='*' Size='*' Version='1.0.0.0' />" +
                    "<Payload Name='Windows8.1-KB2937592-x86.msu' Hash='*' Size='*' />" +
                    "<Payload Name='test.msi' Hash='*' Size='*' />" +
                    "<Payload Name='test.txt' Hash='*' Size='*' />" +
                    "<Payload Name='Shared.dll' Hash='*' Size='*' />" +
                    "</BundlePackage>", xml.GetTestXml(ignoreAttributesByElementName));
            }
        }

        [Fact]
        public void CanGetRemoteBundlePayloadWithCertificate()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var outputFolder = fs.GetFolder();
                var outFile = Path.Combine(outputFolder, "out.xml");
                var remotePayloadSourceFile = Path.Combine(outputFolder, "remotePayload.wxs");
                var intermediateFolder = Path.Combine(outputFolder, "obj");
                var bundleFile = Path.Combine(intermediateFolder, "out.exe");
                var baFolderPath = Path.Combine(outputFolder, "ba");
                var extractFolderPath = Path.Combine(outputFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "burn", "remotepayload",
                    "-usecertificate",
                    "-downloadUrl", "http://wixtoolset.org/{0}",
                    Path.Combine(folder, ".Data", "signed_wix314_4118_engine.exe"),
                    "-o", outFile,
                    "-packagetype", "bundle",
                });

                result.AssertSuccess();

                var elements = File.ReadAllLines(outFile);
                elements = elements.Select(s => s.Replace("\"", "'")).ToArray();

                WixAssert.CompareLineByLine(new[]
                {
                    @"<BundlePackage Visible='yes' CacheId='{C0BA713B-9CFE-42DF-B92C-883F6846B4BA}v3.14.0.4118C95FC39334E667F3DD3D'>",
                    @"  <BundlePackagePayload Name='signed_wix314_4118_engine.exe' ProductName='WiX Toolset v3.14.0.4118' Description='WiX Toolset v3.14.0.4118' DownloadUrl='http://wixtoolset.org/{0}' CertificatePublicKey='03169B5A32E602D436FC14EC14C435D7309945D4' CertificateThumbprint='C95FC39334E667F3DD3D82AF382E05719B88F7C1' Size='1088640' Version='3.14.0.4118'>",
                    @"    <RemoteBundle BundleId='{C0BA713B-9CFE-42DF-B92C-883F6846B4BA}' DisplayName='WiX Toolset v3.14.0.4118' InstallSize='188426175' ManifestNamespace='http://schemas.microsoft.com/wix/2008/Burn' PerMachine='yes' ProviderKey='{c0ba713b-9cfe-42df-b92c-883f6846b4ba}' ProtocolVersion='1' Version='3.14.0.4118' Win64='no' UpgradeCode='{65E893AD-EDD5-4E7D-80CA-F0F50F383532}' />",
                    @"  </BundlePackagePayload>",
                    @"</BundlePackage>",
                }, elements);

                var remotePayloadSourceText = "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>" +
                    "  <Fragment>" +
                    "    <PackageGroup Id='BundlePackages'>" +
                    String.Join(Environment.NewLine, elements) +
                    "    </PackageGroup>" +
                    "  </Fragment>" +
                    "</Wix>";

                File.WriteAllText(remotePayloadSourceFile, remotePayloadSourceText);

                result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    remotePayloadSourceFile,
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                     "-o", bundleFile
                });

                result.AssertSuccess();

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundleFile, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var msuPackages = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:Chain/burn:BundlePackage");
                WixAssert.CompareLineByLine(new string[]
                {
                    "<BundlePackage Id='signed_wix314_4118_engine.exe' Cache='keep' CacheId='{C0BA713B-9CFE-42DF-B92C-883F6846B4BA}v3.14.0.4118C95FC39334E667F3DD3D' InstallSize='188426175' Size='1088640' PerMachine='yes' Permanent='no' Vital='yes' RollbackBoundaryForward='WixDefaultBoundary' RollbackBoundaryBackward='WixDefaultBoundary' LogPathVariable='WixBundleLog_signed_wix314_4118_engine.exe' RollbackLogPathVariable='WixBundleRollbackLog_signed_wix314_4118_engine.exe' BundleId='{C0BA713B-9CFE-42DF-B92C-883F6846B4BA}' Version='3.14.0.4118' InstallArguments='' UninstallArguments='' RepairArguments='' SupportsBurnProtocol='yes' Win64='no'>" +
                    "<Provides Key='{c0ba713b-9cfe-42df-b92c-883f6846b4ba}' Version='3.14.0.4118' DisplayName='WiX Toolset v3.14.0.4118' Imported='yes' />" +
                    "<RelatedBundle Id='{65E893AD-EDD5-4E7D-80CA-F0F50F383532}' Action='Upgrade' />" +
                    "<PayloadRef Id='signed_wix314_4118_engine.exe' />" +
                    "</BundlePackage>",
                }, msuPackages);
            }
        }

        [Fact]
        public void CanGetRemoteV3BundlePayload()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var outputFolder = fs.GetFolder();
                var outFile = Path.Combine(outputFolder, "out.xml");

                var result = WixRunner.Execute(new[]
                {
                    "burn", "remotepayload",
                    Path.Combine(folder, ".Data", "v3bundle.exe"),
                    "-downloadurl", "https://www.example.com/files/{0}",
                    "-o", outFile,
                    "-packagetype", "bundle",
                });

                result.AssertSuccess();

                var elements = File.ReadAllLines(outFile);
                elements = elements.Select(s => s.Replace("\"", "'")).ToArray();

                WixAssert.CompareLineByLine(new[]
                {
                    "<BundlePackage Visible='yes'>",
                    "  <BundlePackagePayload Name='v3bundle.exe' ProductName='CustomV3Theme' Description='CustomV3Theme' DownloadUrl='https://www.example.com/files/{0}' Hash='80739E7B8C31D75B4CDC48D60D74F5E481CB904212A3AE3FB0920365A163FBF32B0C5C175AB516D4124F107923E96200605DE1D560D362FEB47350FA727823B4' Size='648397' Version='1.0.0.0'>",
                    "    <RemoteBundle BundleId='{215A70DB-AB35-48C7-BE51-D66EAAC87177}' DisplayName='CustomV3Theme' InstallSize='1135' ManifestNamespace='http://schemas.microsoft.com/wix/2008/Burn' PerMachine='yes' ProviderKey='{215a70db-ab35-48c7-be51-d66eaac87177}' ProtocolVersion='1' Version='1.0.0.0' Win64='no' UpgradeCode='{2BF4C01F-C132-4E70-97AB-2BC68C7CCD10}' />",
                    "  </BundlePackagePayload>",
                    "</BundlePackage>",
                }, elements);
            }
        }

        [Fact]
        public void CanGetRemoteExePayload()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var outputFolder = fs.GetFolder();
                var outFile = Path.Combine(outputFolder, "out.xml");

                var result = WixRunner.Execute(new[]
                {
                    "burn", "remotepayload",
                    Path.Combine(folder, ".Data", "burn.exe"),
                    "-downloadurl", "https://www.example.com/files/{0}",
                    "-o", outFile
                });

                result.AssertSuccess();

                var elements = File.ReadAllLines(outFile);
                elements = elements.Select(s => s.Replace("\"", "'")).ToArray();

                WixAssert.CompareLineByLine(new[]
                {
                    @"<ExePackage>",
                    @"  <ExePackagePayload Name='burn.exe' ProductName='Windows Installer XML Toolset' Description='WiX Toolset Bootstrapper' DownloadUrl='https://www.example.com/files/{0}' Hash='F6E722518AC3AB7E31C70099368D5770788C179AA23226110DCF07319B1E1964E246A1E8AE72E2CF23E0138AFC281BAFDE45969204405E114EB20C8195DA7E5E' Size='463360' Version='3.14.0.1703' />",
                    @"</ExePackage>",
                }, elements);
            }
        }

        [Fact]
        public void CanGetRemoteMsuPayload()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var outputFolder = fs.GetFolder();
                var outFile = Path.Combine(outputFolder, "out.xml");

                var result = WixRunner.Execute(new[]
                {
                    "burn", "remotepayload",
                    Path.Combine(folder, ".Data", "Windows8.1-KB2937592-x86.msu"),
                    "-o", outFile
                });

                result.AssertSuccess();

                var elements = File.ReadAllLines(outFile);
                elements = elements.Select(s => s.Replace("\"", "'")).ToArray();

                WixAssert.CompareLineByLine(new[]
                {
                    @"<MsuPackage>",
                    @"  <MsuPackagePayload Name='Windows8.1-KB2937592-x86.msu' Hash='904ADEA6AB675ACE16483138BF3F5850FD56ACB6E3A13AFA7263ED49C68CCE6CF84D6AAD6F99AAF175A95EE1A56C787C5AD968019056490B1073E7DBB7B9B7BE' Size='309544' />",
                    @"</MsuPackage>"
                }, elements);
            }
        }

        [Fact]
        public void CanGetRemoteMsuPayloadWithCertificate()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var outputFolder = fs.GetFolder();
                var outFile = Path.Combine(outputFolder, "out.xml");
                var remotePayloadSourceFile = Path.Combine(outputFolder, "remotePayload.wxs");
                var intermediateFolder = Path.Combine(outputFolder, "obj");
                var bundleFile = Path.Combine(intermediateFolder, "out.exe");
                var baFolderPath = Path.Combine(outputFolder, "ba");
                var extractFolderPath = Path.Combine(outputFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "burn", "remotepayload",
                    "-usecertificate",
                    "-downloadUrl", "http://wixtoolset.org/{0}",
                    Path.Combine(folder, ".Data", "Windows8.1-KB2937592-x86.msu"),
                    "-o", outFile
                });

                result.AssertSuccess();

                var elements = File.ReadAllLines(outFile);
                elements = elements.Select(s => s.Replace("\"", "'")).ToArray();

                WixAssert.CompareLineByLine(new[]
                {
                    @"<MsuPackage CacheId='904ADEA6AB675ACE16483138BF3F5850FD56ACB6E3A1108E2BA23632620C427C'>",
                    @"  <MsuPackagePayload Name='Windows8.1-KB2937592-x86.msu' DownloadUrl='http://wixtoolset.org/{0}' CertificatePublicKey='A260A870BE1145ED71E2BB5AA19463A4FE9DCC41' CertificateThumbprint='108E2BA23632620C427C570B6D9DB51AC31387FE' Size='309544' />",
                    @"</MsuPackage>"
                }, elements);

                // Append required attributes to build.
                elements[0] = elements[0].Replace(">", " DetectCondition='test'>");

                var remotePayloadSourceText = "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>" +
                    "  <Fragment>" +
                    "    <PackageGroup Id='BundlePackages'>" +
                    String.Join(Environment.NewLine, elements) +
                    "    </PackageGroup>" +
                    "  </Fragment>" +
                    "</Wix>";

                File.WriteAllText(remotePayloadSourceFile, remotePayloadSourceText);

                result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    remotePayloadSourceFile,
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                     "-o", bundleFile
                });

                result.AssertSuccess();

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundleFile, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var msuPackages = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:Chain/burn:MsuPackage");
                WixAssert.CompareLineByLine(new string[]
                {
                    "<MsuPackage Id='Windows8.1_KB2937592_x86.msu' Cache='keep' CacheId='904ADEA6AB675ACE16483138BF3F5850FD56ACB6E3A1108E2BA23632620C427C' InstallSize='309544' Size='309544' PerMachine='yes' Permanent='no' Vital='yes' RollbackBoundaryForward='WixDefaultBoundary' RollbackBoundaryBackward='WixDefaultBoundary' DetectCondition='test'>" +
                    "<PayloadRef Id='Windows8.1_KB2937592_x86.msu' />" +
                    "</MsuPackage>",
                }, msuPackages);
            }
        }

        [Fact]
        public void CanGetRemotePayloadWithCertificate()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var outputFolder = fs.GetFolder();
                var outFile = Path.Combine(outputFolder, "out.xml");
                var remotePayloadSourceFile = Path.Combine(outputFolder, "remotePayload.wxs");
                var intermediateFolder = Path.Combine(outputFolder, "obj");
                var bundleFile = Path.Combine(intermediateFolder, "out.exe");
                var baFolderPath = Path.Combine(outputFolder, "ba");
                var extractFolderPath = Path.Combine(outputFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "burn", "remotepayload",
                    "-usecertificate",
                    "-downloadUrl", "http://wixtoolset.org/{0}",
                    Path.Combine(folder, ".Data", "burn.exe"),
                    Path.Combine(folder, ".Data", "signed_cab1.cab"),
                    "-o", outFile
                });

                result.AssertSuccess();

                var elements = File.ReadAllLines(outFile);
                elements = elements.Select(s => s.Replace("\"", "'")).ToArray();

                WixAssert.CompareLineByLine(new[]
                {
                    @"<ExePackage>",
                    @"  <ExePackagePayload Name='burn.exe' ProductName='Windows Installer XML Toolset' Description='WiX Toolset Bootstrapper' DownloadUrl='http://wixtoolset.org/{0}' Hash='F6E722518AC3AB7E31C70099368D5770788C179AA23226110DCF07319B1E1964E246A1E8AE72E2CF23E0138AFC281BAFDE45969204405E114EB20C8195DA7E5E' Size='463360' Version='3.14.0.1703' />",
                    @"  <Payload Name='signed_cab1.cab' DownloadUrl='http://wixtoolset.org/{0}' CertificatePublicKey='BBD1B48A37503767C71F455624967D406A5D66C3' CertificateThumbprint='DE13B4CE635E3F63AA2394E66F95C460267BC82F' Size='1585' />",
                    @"</ExePackage>",
                }, elements);

                elements[0] = elements[0].Replace(">", " Permanent='yes' DetectCondition='test'>");

                var remotePayloadSourceText = "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>" +
                    "  <Fragment>" +
                    "    <PackageGroup Id='BundlePackages'>" +
                    String.Join(Environment.NewLine, elements) +
                    "    </PackageGroup>" +
                    "  </Fragment>" +
                    "</Wix>";

                File.WriteAllText(remotePayloadSourceFile, remotePayloadSourceText);

                result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    remotePayloadSourceFile,
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                     "-o", bundleFile
                });

                result.AssertSuccess();
            }
        }

        [Fact]
        public void CanGetRemotePayloadWithoutCertificate()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var outputFolder = fs.GetFolder();
                var outFile = Path.Combine(outputFolder, "out.xml");

                var result = WixRunner.Execute(new[]
                {
                    "burn", "remotepayload",
                    Path.Combine(folder, ".Data", "burn.exe"),
                    Path.Combine(folder, ".Data", "signed_cab1.cab"),
                    "-o", outFile
                });

                result.AssertSuccess();

                var elements = File.ReadAllLines(outFile);
                elements = elements.Select(s => s.Replace("\"", "'")).ToArray();

                WixAssert.CompareLineByLine(new[]
                {
                    @"<ExePackage>",
                    @"  <ExePackagePayload Name='burn.exe' ProductName='Windows Installer XML Toolset' Description='WiX Toolset Bootstrapper' Hash='F6E722518AC3AB7E31C70099368D5770788C179AA23226110DCF07319B1E1964E246A1E8AE72E2CF23E0138AFC281BAFDE45969204405E114EB20C8195DA7E5E' Size='463360' Version='3.14.0.1703' />",
                    @"  <Payload Name='signed_cab1.cab' Hash='D8D3842403710E1F6036A62543224855CADF546853933C2B17BA99D789D4347B36717687C022678A9D3DE749DFC1482DAAB92B997B62BB32A8A6828B9D04C414' Size='1585' />",
                    @"</ExePackage>",
                }, elements);
            }
        }

        [Fact]
        public void CanGetRemotePayloadsRecursive()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var outputFolder = fs.GetFolder();
                var outFile = Path.Combine(outputFolder, "out.xml");

                var result = WixRunner.Execute(new[]
                {
                    "burn", "remotepayload",
                    "-recurse",
                    "-du", "https://www.example.com/files/{0}",
                    Path.Combine(folder, ".Data", "burn.exe"),
                    Path.Combine(folder, "RemotePayload", "recurse", "*"),
                    "-basepath", Path.Combine(folder, "RemotePayload", "recurse"),
                    "-basepath", folder,
                    "-bp", Path.Combine(folder, ".Data"),
                    "-o", outFile
                });

                result.AssertSuccess();

                var elements = File.ReadAllLines(outFile);
                elements = elements.Select(s => s.Replace("\"", "'")).ToArray();

                WixAssert.CompareLineByLine(new[]
                {
                    @"<ExePackage>",
                    @"  <ExePackagePayload Name='burn.exe' ProductName='Windows Installer XML Toolset' Description='WiX Toolset Bootstrapper' DownloadUrl='https://www.example.com/files/{0}' Hash='F6E722518AC3AB7E31C70099368D5770788C179AA23226110DCF07319B1E1964E246A1E8AE72E2CF23E0138AFC281BAFDE45969204405E114EB20C8195DA7E5E' Size='463360' Version='3.14.0.1703' />",
                    @"  <Payload Name='a.dat' DownloadUrl='https://www.example.com/files/{0}' Hash='D13926E5CBE5ED8B46133F9199FAF2FF25B25981C67A31AE2BC3F6C20390FACBFADCD89BD22D3445D95B989C8EACFB1E68DB634BECB5C9624865BA453BCE362A' Size='16' />",
                    @"  <Payload Name='subfolder\b.dat' DownloadUrl='https://www.example.com/files/{0}' Hash='5F94707BC29ADFE3B9615E6753388707FD0B8F5FD9EEEC2B17E21E72F1635FF7D7A101E7D14F614E111F263CB9AC4D0940BE1247881A7844F226D6C400293D8E' Size='37' />",
                    @"  <Payload Name='subfolder\c.dat' DownloadUrl='https://www.example.com/files/{0}' Hash='97D6209A5571E05E4F72F9C6BF0987651FA03E63F971F9B53C2B3D798A666D9864F232D4E2D6442E47D9D72B282309B6EEFF4EE017B43B706FA92A0F5EF74734' Size='42' />",
                    @"</ExePackage>"
                }, elements);
            }
        }
    }
}
