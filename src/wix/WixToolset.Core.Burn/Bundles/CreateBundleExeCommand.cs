// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Symbols;
    using WixToolset.Dtf.Resources;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;
    using WixToolset.Versioning;

    internal class CreateBundleExeCommand
    {
        public CreateBundleExeCommand(IMessaging messaging, IFileSystem fileSystem, IBackendHelper backendHelper, string intermediateFolder, string outputPath, WixBundleSymbol bundleSymbol, WixBundleContainerSymbol uxContainer, IEnumerable<WixBundleContainerSymbol> containers)
        {
            this.Messaging = messaging;
            this.FileSystem = fileSystem;
            this.BackendHelper = backendHelper;
            this.IntermediateFolder = intermediateFolder;
            this.OutputPath = outputPath;
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

        private WixBundleSymbol BundleSymbol { get; }

        private WixBundleContainerSymbol UXContainer { get; }

        private IEnumerable<WixBundleContainerSymbol> Containers { get; }

        public void Execute()
        {
            var bundleFilename = Path.GetFileName(this.OutputPath);

            // Copy the burn.exe to a writable location then mark it to be moved to its final build location.

            var stubPlatform = this.BundleSymbol.Platform.ToString();
            var stubFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), stubPlatform, "burn.exe");
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

            this.UpdateBurnResources(bundleTempPath, this.OutputPath, this.BundleSymbol, fourPartVersion);

            // Update the .wixburn section to point to at the UX and attached container(s) then attach the containers
            // if they should be attached.
            using (var writer = BurnWriter.Open(this.Messaging, this.FileSystem, bundleTempPath))
            {
                var burnStubFile = new FileInfo(bundleTempPath);
                writer.InitializeBundleSectionData(burnStubFile.Length, this.BundleSymbol.BundleCode);

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

        private void UpdateBurnResources(string bundleTempPath, string outputPath, WixBundleSymbol bundleInfo, Version fourPartVersion)
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
