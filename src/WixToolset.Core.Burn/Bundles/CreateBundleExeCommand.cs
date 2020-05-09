// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Tuples;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class CreateBundleExeCommand
    {
        public CreateBundleExeCommand(IMessaging messaging, IBackendHelper backendHelper, string intermediateFolder, string outputPath, WixBundleTuple bundleTuple, WixBundleContainerTuple uxContainer, IEnumerable<WixBundleContainerTuple> containers)
        {
            this.Messaging = messaging;
            this.BackendHelper = backendHelper;
            this.IntermediateFolder = intermediateFolder;
            this.OutputPath = outputPath;
            this.BundleTuple = bundleTuple;
            this.UXContainer = uxContainer;
            this.Containers = containers;
        }

        public IFileTransfer Transfer { get; private set; }

        private IMessaging Messaging { get; }

        private IBackendHelper BackendHelper { get; }

        private string IntermediateFolder { get; }

        private string OutputPath { get; }

        private WixBundleTuple BundleTuple { get; }

        private WixBundleContainerTuple UXContainer { get; }

        private IEnumerable<WixBundleContainerTuple> Containers { get; }

        public void Execute()
        {
            var bundleFilename = Path.GetFileName(this.OutputPath);

            // Copy the burn.exe to a writable location then mark it to be moved to its final build location.

            var stubPlatform = this.BundleTuple.Platform.ToString();
            var stubFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), stubPlatform, "burn.exe");

            var bundleTempPath = Path.Combine(this.IntermediateFolder, bundleFilename);

            this.Messaging.Write(VerboseMessages.GeneratingBundle(bundleTempPath, stubFile));

            if ("setup.exe".Equals(bundleFilename, StringComparison.OrdinalIgnoreCase))
            {
                this.Messaging.Write(ErrorMessages.InsecureBundleFilename(bundleFilename));
            }

            this.Transfer = this.BackendHelper.CreateFileTransfer(bundleTempPath, this.OutputPath, true, this.BundleTuple.SourceLineNumbers);

            File.Copy(stubFile, bundleTempPath, true);
            File.SetAttributes(bundleTempPath, FileAttributes.Normal);

            this.UpdateBurnResources(bundleTempPath, this.OutputPath, this.BundleTuple);

            // Update the .wixburn section to point to at the UX and attached container(s) then attach the containers
            // if they should be attached.
            using (var writer = BurnWriter.Open(this.Messaging, bundleTempPath))
            {
                var burnStubFile = new FileInfo(bundleTempPath);
                writer.InitializeBundleSectionData(burnStubFile.Length, this.BundleTuple.BundleId);

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

        private void UpdateBurnResources(string bundleTempPath, string outputPath, WixBundleTuple bundleInfo)
        {
            var resources = new Dtf.Resources.ResourceCollection();
            var version = new Dtf.Resources.VersionResource("#1", 1033);

            version.Load(bundleTempPath);
            resources.Add(version);

            // Ensure the bundle info provides a full four part version.
            var fourPartVersion = new Version(bundleInfo.Version);
            var major = (fourPartVersion.Major < 0) ? 0 : fourPartVersion.Major;
            var minor = (fourPartVersion.Minor < 0) ? 0 : fourPartVersion.Minor;
            var build = (fourPartVersion.Build < 0) ? 0 : fourPartVersion.Build;
            var revision = (fourPartVersion.Revision < 0) ? 0 : fourPartVersion.Revision;

            if (UInt16.MaxValue < major || UInt16.MaxValue < minor || UInt16.MaxValue < build || UInt16.MaxValue < revision)
            {
                throw new WixException(ErrorMessages.InvalidModuleOrBundleVersion(bundleInfo.SourceLineNumbers, "Bundle", bundleInfo.Version));
            }

            fourPartVersion = new Version(major, minor, build, revision);
            version.FileVersion = fourPartVersion;
            version.ProductVersion = fourPartVersion;

            var strings = version[1033] ?? version.Add(1033);
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
                var iconGroup = new Dtf.Resources.GroupIconResource("#1", 1033);
                iconGroup.ReadFromFile(bundleInfo.IconSourceFile);
                resources.Add(iconGroup);

                foreach (var icon in iconGroup.Icons)
                {
                    resources.Add(icon);
                }
            }

            if (!String.IsNullOrEmpty(bundleInfo.SplashScreenSourceFile))
            {
                var bitmap = new Dtf.Resources.BitmapResource("#1", 1033);
                bitmap.ReadFromFile(bundleInfo.SplashScreenSourceFile);
                resources.Add(bitmap);
            }

            resources.Save(bundleTempPath);
        }
    }
}
