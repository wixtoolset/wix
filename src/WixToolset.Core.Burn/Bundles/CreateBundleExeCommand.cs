// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Symbols;
    using WixToolset.Dtf.Resources;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class CreateBundleExeCommand
    {
        public CreateBundleExeCommand(IMessaging messaging, IBackendHelper backendHelper, string intermediateFolder, string outputPath, WixBootstrapperApplicationDllSymbol bootstrapperApplicationDllSymbol, WixBundleSymbol bundleSymbol, WixBundleContainerSymbol uxContainer, IEnumerable<WixBundleContainerSymbol> containers)
        {
            this.Messaging = messaging;
            this.BackendHelper = backendHelper;
            this.IntermediateFolder = intermediateFolder;
            this.OutputPath = outputPath;
            this.BootstrapperApplicationDllSymbol = bootstrapperApplicationDllSymbol;
            this.BundleSymbol = bundleSymbol;
            this.UXContainer = uxContainer;
            this.Containers = containers;
        }

        public IFileTransfer Transfer { get; private set; }

        private IMessaging Messaging { get; }

        private IBackendHelper BackendHelper { get; }

        private string IntermediateFolder { get; }

        private string OutputPath { get; }

        private WixBootstrapperApplicationDllSymbol BootstrapperApplicationDllSymbol { get; }

        private WixBundleSymbol BundleSymbol { get; }

        private WixBundleContainerSymbol UXContainer { get; }

        private IEnumerable<WixBundleContainerSymbol> Containers { get; }

        public void Execute()
        {
            var bundleFilename = Path.GetFileName(this.OutputPath);

            // Copy the burn.exe to a writable location then mark it to be moved to its final build location.

            var stubPlatform = this.BundleSymbol.Platform.ToString();
            var stubFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), stubPlatform, "burn.exe");

            if (stubPlatform != "X86")
            {
                this.Messaging.Write(WarningMessages.ExperimentalBundlePlatform(stubPlatform));
            }

            var bundleTempPath = Path.Combine(this.IntermediateFolder, bundleFilename);

            this.Messaging.Write(VerboseMessages.GeneratingBundle(bundleTempPath, stubFile));

            if ("setup.exe".Equals(bundleFilename, StringComparison.OrdinalIgnoreCase))
            {
                this.Messaging.Write(ErrorMessages.InsecureBundleFilename(bundleFilename));
            }

            this.Transfer = this.BackendHelper.CreateFileTransfer(bundleTempPath, this.OutputPath, true, this.BundleSymbol.SourceLineNumbers);

            File.Copy(stubFile, bundleTempPath, true);
            File.SetAttributes(bundleTempPath, FileAttributes.Normal);

            var windowsAssemblyVersion = GetWindowsAssemblyVersion(this.BundleSymbol);

            var applicationManifestData = GenerateApplicationManifest(this.BundleSymbol, this.BootstrapperApplicationDllSymbol, this.OutputPath, windowsAssemblyVersion);

            UpdateBurnResources(bundleTempPath, this.OutputPath, this.BundleSymbol, windowsAssemblyVersion, applicationManifestData);

            // Update the .wixburn section to point to at the UX and attached container(s) then attach the containers
            // if they should be attached.
            using (var writer = BurnWriter.Open(this.Messaging, bundleTempPath))
            {
                var burnStubFile = new FileInfo(bundleTempPath);
                writer.InitializeBundleSectionData(burnStubFile.Length, this.BundleSymbol.BundleId);

                // Always attach the UX container first
                writer.AppendContainer(this.UXContainer.WorkingPath, BurnWriter.Container.UX);

                // Now append all other attached containers
                foreach (var container in this.Containers)
                {
                    if (ContainerType.Attached == container.Type)
                    {
                        // The container was only created if it had payloads.
                        if (!String.IsNullOrEmpty(container.WorkingPath) && BurnConstants.BurnUXContainerName != container.Id.Id)
                        {
                            writer.AppendContainer(container.WorkingPath, BurnWriter.Container.Attached);
                        }
                    }
                }
            }
        }

        private static byte[] GenerateApplicationManifest(WixBundleSymbol bundleSymbol, WixBootstrapperApplicationDllSymbol bootstrapperApplicationSymbol, string outputPath, Version windowsAssemblyVersion)
        {
            const string asmv1Namespace = "urn:schemas-microsoft-com:asm.v1";
            const string asmv3Namespace = "urn:schemas-microsoft-com:asm.v3";
            const string compatv1Namespace = "urn:schemas-microsoft-com:compatibility.v1";
            const string ws2005Namespace = "http://schemas.microsoft.com/SMI/2005/WindowsSettings";
            const string ws2016Namespace = "http://schemas.microsoft.com/SMI/2016/WindowsSettings";
            const string ws2017Namespace = "http://schemas.microsoft.com/SMI/2017/WindowsSettings";

            var bundleFileName = Path.GetFileName(outputPath);
            var bundleAssemblyVersion = windowsAssemblyVersion.ToString();
            var bundlePlatform = bundleSymbol.Platform == Platform.X64 ? "amd64" : bundleSymbol.Platform.ToString().ToLower();
            var bundleDescription = bundleSymbol.Name;

            using (var memoryStream = new MemoryStream())
            using (var writer = new XmlTextWriter(memoryStream, Encoding.UTF8))
            {
                writer.WriteStartDocument();

                writer.WriteStartElement("assembly", asmv1Namespace);
                writer.WriteAttributeString("manifestVersion", "1.0");

                writer.WriteStartElement("assemblyIdentity");
                writer.WriteAttributeString("name", bundleFileName);
                writer.WriteAttributeString("version", bundleAssemblyVersion);
                writer.WriteAttributeString("processorArchitecture", bundlePlatform);
                writer.WriteAttributeString("type", "win32");
                writer.WriteEndElement(); // </assemblyIdentity>

                if (!String.IsNullOrEmpty(bundleDescription))
                {
                    writer.WriteStartElement("description");
                    writer.WriteString(bundleDescription);
                    writer.WriteEndElement();
                }

                writer.WriteStartElement("dependency");
                writer.WriteStartElement("dependentAssembly");
                writer.WriteStartElement("assemblyIdentity");
                writer.WriteAttributeString("name", "Microsoft.Windows.Common-Controls");
                writer.WriteAttributeString("version", "6.0.0.0");
                writer.WriteAttributeString("processorArchitecture", bundlePlatform);
                writer.WriteAttributeString("publicKeyToken", "6595b64144ccf1df");
                writer.WriteAttributeString("language", "*");
                writer.WriteAttributeString("type", "win32");
                writer.WriteEndElement(); // </assemblyIdentity>
                writer.WriteEndElement(); // </dependentAssembly>
                writer.WriteEndElement(); // </dependency>

                writer.WriteStartElement("compatibility", compatv1Namespace);
                writer.WriteStartElement("application");

                writer.WriteStartElement("supportedOS");
                writer.WriteAttributeString("Id", "{e2011457-1546-43c5-a5fe-008deee3d3f0}"); // Windows Vista
                writer.WriteEndElement();
                writer.WriteStartElement("supportedOS");
                writer.WriteAttributeString("Id", "{35138b9a-5d96-4fbd-8e2d-a2440225f93a}"); // Windows 7
                writer.WriteEndElement();
                writer.WriteStartElement("supportedOS");
                writer.WriteAttributeString("Id", "{4a2f28e3-53b9-4441-ba9c-d69d4a4a6e38}"); // Windows 8
                writer.WriteEndElement();
                writer.WriteStartElement("supportedOS");
                writer.WriteAttributeString("Id", "{1f676c76-80e1-4239-95bb-83d0f6d0da78}"); // Windows 8.1
                writer.WriteEndElement();
                writer.WriteStartElement("supportedOS");
                writer.WriteAttributeString("Id", "{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}"); // Windows 10
                writer.WriteEndElement();

                writer.WriteEndElement(); // </application>
                writer.WriteEndElement(); // </compatibility>

                writer.WriteStartElement("trustInfo", asmv3Namespace);
                writer.WriteStartElement("security");
                writer.WriteStartElement("requestedPrivileges");
                writer.WriteStartElement("requestedExecutionLevel");
                writer.WriteAttributeString("level", "asInvoker");
                writer.WriteAttributeString("uiAccess", "false");
                writer.WriteEndElement(); // </requestedExecutionLevel>
                writer.WriteEndElement(); // </requestedPrivileges>
                writer.WriteEndElement(); // </security>
                writer.WriteEndElement(); // </trustInfo>

                if (bootstrapperApplicationSymbol.DpiAwareness != WixBootstrapperApplicationDpiAwarenessType.Unaware)
                {
                    string dpiAwareValue = null;
                    string dpiAwarenessValue = null;
                    string gdiScalingValue = null;

                    switch(bootstrapperApplicationSymbol.DpiAwareness)
                    {
                        case WixBootstrapperApplicationDpiAwarenessType.GdiScaled:
                            gdiScalingValue = "true";
                            break;
                        case WixBootstrapperApplicationDpiAwarenessType.PerMonitor:
                            dpiAwareValue = "true/pm";
                            break;
                        case WixBootstrapperApplicationDpiAwarenessType.PerMonitorV2:
                            dpiAwareValue = "true/pm";
                            dpiAwarenessValue = "PerMonitorV2, PerMonitor";
                            break;
                        case WixBootstrapperApplicationDpiAwarenessType.System:
                            dpiAwareValue = "true";
                            break;
                    }

                    writer.WriteStartElement("application", asmv3Namespace);
                    writer.WriteStartElement("windowsSettings");

                    if (dpiAwareValue != null)
                    {
                        writer.WriteStartElement("dpiAware", ws2005Namespace);
                        writer.WriteString(dpiAwareValue);
                        writer.WriteEndElement();
                    }

                    if (dpiAwarenessValue != null)
                    {
                        writer.WriteStartElement("dpiAwareness", ws2016Namespace);
                        writer.WriteString(dpiAwarenessValue);
                        writer.WriteEndElement();
                    }

                    if (gdiScalingValue != null)
                    {
                        writer.WriteStartElement("gdiScaling", ws2017Namespace);
                        writer.WriteString(gdiScalingValue);
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement(); // </windowSettings>
                    writer.WriteEndElement(); // </application>
                }

                writer.WriteEndDocument(); // </assembly>
                writer.Close();

                return memoryStream.ToArray();
            }
        }

        private static Version GetWindowsAssemblyVersion(WixBundleSymbol bundleSymbol)
        {
            // Ensure the bundle info provides a full four part version.
            var fourPartVersion = new Version(bundleSymbol.Version);
            var major = (fourPartVersion.Major < 0) ? 0 : fourPartVersion.Major;
            var minor = (fourPartVersion.Minor < 0) ? 0 : fourPartVersion.Minor;
            var build = (fourPartVersion.Build < 0) ? 0 : fourPartVersion.Build;
            var revision = (fourPartVersion.Revision < 0) ? 0 : fourPartVersion.Revision;

            if (UInt16.MaxValue < major || UInt16.MaxValue < minor || UInt16.MaxValue < build || UInt16.MaxValue < revision)
            {
                throw new WixException(ErrorMessages.InvalidModuleOrBundleVersion(bundleSymbol.SourceLineNumbers, "Bundle", bundleSymbol.Version));
            }

            return new Version(major, minor, build, revision);
        }

        private static void UpdateBurnResources(string bundleTempPath, string outputPath, WixBundleSymbol bundleInfo, Version windowsAssemblyVersion, byte[] applicationManifestData)
        {
            const int burnLocale = 1033;
            var resources = new Dtf.Resources.ResourceCollection();
            var version = new Dtf.Resources.VersionResource("#1", burnLocale);

            version.Load(bundleTempPath);
            resources.Add(version);

            version.FileVersion = windowsAssemblyVersion;
            version.ProductVersion = windowsAssemblyVersion;

            var strings = version[burnLocale] ?? version.Add(burnLocale);
            strings["LegalCopyright"] = bundleInfo.Copyright;
            strings["OriginalFilename"] = Path.GetFileName(outputPath);
            strings["FileVersion"] = bundleInfo.Version;    // string versions do not have to be four parts.
            strings["ProductVersion"] = bundleInfo.Version; // string versions do not have to be four parts.

            if (!String.IsNullOrEmpty(bundleInfo.Name))
            {
                strings["ProductName"] = bundleInfo.Name;
                strings["FileDescription"] = bundleInfo.Name;
            }

            if (!String.IsNullOrEmpty(bundleInfo.Manufacturer))
            {
                strings["CompanyName"] = bundleInfo.Manufacturer;
            }
            else
            {
                strings["CompanyName"] = String.Empty;
            }

            if (!String.IsNullOrEmpty(bundleInfo.IconSourceFile))
            {
                var iconGroup = new Dtf.Resources.GroupIconResource("#1", burnLocale);
                iconGroup.ReadFromFile(bundleInfo.IconSourceFile);
                resources.Add(iconGroup);

                foreach (var icon in iconGroup.Icons)
                {
                    resources.Add(icon);
                }
            }

            if (!String.IsNullOrEmpty(bundleInfo.SplashScreenSourceFile))
            {
                var bitmap = new Dtf.Resources.BitmapResource("#1", burnLocale);
                bitmap.ReadFromFile(bundleInfo.SplashScreenSourceFile);
                resources.Add(bitmap);
            }

            var manifestResource = new Resource(ResourceType.Manifest, "#1", burnLocale, applicationManifestData);
            resources.Add(manifestResource);

            resources.Save(bundleTempPath);
        }
    }
}
