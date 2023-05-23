// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Symbols;
    using WixToolset.Dtf.Resources;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;
    using WixToolset.Versioning;

    internal class CreateBundleExeCommand
    {
        public CreateBundleExeCommand(IMessaging messaging, IFileSystem fileSystem, IBackendHelper backendHelper, string intermediateFolder, string outputPath, WixBootstrapperApplicationDllSymbol bootstrapperApplicationDllSymbol, WixBundleSymbol bundleSymbol, WixBundleContainerSymbol uxContainer, IEnumerable<WixBundleContainerSymbol> containers)
        {
            this.Messaging = messaging;
            this.FileSystem = fileSystem;
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

        private IFileSystem FileSystem { get; }

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
            var stubFile = this.BundleSymbol.BurnStubPath?.Path;
            if (string.IsNullOrEmpty(stubFile))
            {
                stubFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), stubPlatform, "burn.exe");
            }
            var bundleTempPath = Path.Combine(this.IntermediateFolder, bundleFilename);

            this.Messaging.Write(VerboseMessages.GeneratingBundle(bundleTempPath, stubFile));

            if ("setup.exe".Equals(bundleFilename, StringComparison.OrdinalIgnoreCase))
            {
                this.Messaging.Write(ErrorMessages.InsecureBundleFilename(bundleFilename));
            }

            this.Transfer = this.BackendHelper.CreateFileTransfer(bundleTempPath, this.OutputPath, true, this.BundleSymbol.SourceLineNumbers);

            this.FileSystem.CopyFile(this.BundleSymbol.SourceLineNumbers, stubFile, bundleTempPath, allowHardlink: false);
            File.SetAttributes(bundleTempPath, FileAttributes.Normal);

            var fourPartVersion = this.GetFourPartVersion(this.BundleSymbol);

            var applicationManifestData = GenerateApplicationManifest(this.BundleSymbol, this.BootstrapperApplicationDllSymbol, this.OutputPath, fourPartVersion);

            this.UpdateBurnResources(bundleTempPath, this.OutputPath, this.BundleSymbol, fourPartVersion, applicationManifestData);

            // Update the .wixburn section to point to at the UX and attached container(s) then attach the containers
            // if they should be attached.
            using (var writer = BurnWriter.Open(this.Messaging, this.FileSystem, bundleTempPath))
            {
                var burnStubFile = new FileInfo(bundleTempPath);
                writer.InitializeBundleSectionData(burnStubFile.Length, this.BundleSymbol.BundleId);

                // Always attach the UX container first
                writer.AppendContainer(this.UXContainer.SourceLineNumbers, this.UXContainer.WorkingPath, BurnWriter.Container.UX);

                // Now append all other attached containers
                foreach (var container in this.Containers)
                {
                    if (ContainerType.Attached == container.Type)
                    {
                        // The container was only created if it had payloads.
                        if (!String.IsNullOrEmpty(container.WorkingPath) && BurnConstants.BurnUXContainerName != container.Id.Id)
                        {
                            writer.AppendContainer(container.SourceLineNumbers, container.WorkingPath, BurnWriter.Container.Attached);
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

                    switch (bootstrapperApplicationSymbol.DpiAwareness)
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

                    writer.WriteStartElement("longPathAware", ws2016Namespace);
                    writer.WriteString("true");
                    writer.WriteEndElement();

                    writer.WriteEndElement(); // </windowSettings>
                    writer.WriteEndElement(); // </application>
                }

                writer.WriteEndDocument(); // </assembly>
                writer.Close();

                return memoryStream.ToArray();
            }
        }

        private Version GetFourPartVersion(WixBundleSymbol bundleSymbol)
        {
            // Ensure the bundle info provides a full four-part version.

            if (!WixVersion.TryParse(bundleSymbol.Version, out var wixVersion))
            {
                // Display an error message indicating that we will require a four-part version number
                // not just a WixVersion.
                this.Messaging.Write(ErrorMessages.IllegalVersionValue(bundleSymbol.SourceLineNumbers, "Bundle", "Version", bundleSymbol.Version));
                return new Version(0, 0);
            }

            var major = wixVersion.Major;
            var minor = wixVersion.Minor;
            var build = wixVersion.Patch;
            var revision = wixVersion.Revision;

            if (UInt16.MaxValue < major || UInt16.MaxValue < minor || UInt16.MaxValue < build || UInt16.MaxValue < revision)
            {
                major = Math.Max(major, UInt16.MaxValue);
                minor = Math.Max(minor, UInt16.MaxValue);
                build = Math.Max(build, UInt16.MaxValue);
                revision = Math.Max(revision, UInt16.MaxValue);

                this.Messaging.Write(BurnBackendWarnings.CannotParseBundleVersionAsFourPartVersion(bundleSymbol.SourceLineNumbers, bundleSymbol.Version));
            }

            return new Version((int)major, (int)minor, (int)build, (int)revision);
        }

        private void UpdateBurnResources(string bundleTempPath, string outputPath, WixBundleSymbol bundleInfo, Version fourPartVersion, byte[] applicationManifestData)
        {
            const int burnLocale = 1033;
            var resources = new ResourceCollection();
            var version = new VersionResource("#1", burnLocale);

            version.Load(bundleTempPath);
            resources.Add(version);

            version.FileVersion = fourPartVersion;
            version.ProductVersion = fourPartVersion;

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

            if (bundleInfo.IconSourceFile != null)
            {
                var iconGroup = new GroupIconResource("#1", burnLocale);
                iconGroup.ReadFromFile(bundleInfo.IconSourceFile.Path);
                resources.Add(iconGroup);

                foreach (var icon in iconGroup.Icons)
                {
                    resources.Add(icon);
                }
            }

            var splashScreenType = BURN_SPLASH_SCREEN_TYPE.BURN_SPLASH_SCREEN_TYPE_NONE;

            if (bundleInfo.SplashScreenSourceFile != null)
            {
                var bitmap = new BitmapResource("#1", burnLocale);
                bitmap.ReadFromFile(bundleInfo.SplashScreenSourceFile.Path);
                resources.Add(bitmap);

                splashScreenType = BURN_SPLASH_SCREEN_TYPE.BURN_SPLASH_SCREEN_TYPE_BITMAP_RESOURCE;
            }

            var splashScreenConfig = new BURN_SPLASH_SCREEN_CONFIGURATION
            {
                Type = splashScreenType,
                ResourceId = 1,
            };

            var splashScreenConfigResource = new Resource(ResourceType.RCData, "#1", burnLocale, splashScreenConfig.ToBytes());
            resources.Add(splashScreenConfigResource);

            var manifestResource = new Resource(ResourceType.Manifest, "#1", burnLocale, applicationManifestData);
            resources.Add(manifestResource);

            try
            {
                this.FileSystem.ExecuteWithRetries(() => resources.Save(bundleTempPath));
            }
            catch (IOException e)
            {
                this.Messaging.Write(BurnBackendErrors.FailedToUpdateBundleResources(bundleInfo.SourceLineNumbers, bundleInfo.IconSourceFile?.Path, bundleInfo.SplashScreenSourceFile?.Path, e.Message));
            }
        }

        enum BURN_SPLASH_SCREEN_TYPE
        {
            BURN_SPLASH_SCREEN_TYPE_NONE,
            BURN_SPLASH_SCREEN_TYPE_BITMAP_RESOURCE,
        }

        [StructLayout(LayoutKind.Sequential)]
        struct BURN_SPLASH_SCREEN_CONFIGURATION
        {
            [MarshalAs(UnmanagedType.I4)]
            public BURN_SPLASH_SCREEN_TYPE Type;

            [MarshalAs(UnmanagedType.U2)]
            public ushort ResourceId;

            public byte[] ToBytes()
            {
                var cb = Marshal.SizeOf(this);
                var data = new byte[cb];
                var pBuffer = Marshal.AllocHGlobal(cb);

                try
                {
                    Marshal.StructureToPtr(this, pBuffer, true);
                    Marshal.Copy(pBuffer, data, 0, cb);
                    return data;
                }
                finally
                {
                    Marshal.FreeHGlobal(pBuffer);
                }
            }
        }
    }
}
