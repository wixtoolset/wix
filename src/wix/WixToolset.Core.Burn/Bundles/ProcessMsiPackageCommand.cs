// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Data;
    using WixToolset.Core.Native.Msi;

    /// <summary>
    /// Initializes package state from the MSI contents.
    /// </summary>
    internal class ProcessMsiPackageCommand
    {
        private const string PropertySqlQuery = "SELECT `Value` FROM `Property` WHERE `Property` = ?";

        public ProcessMsiPackageCommand(IServiceProvider serviceProvider, IEnumerable<IBurnBackendBinderExtension> backendExtensions, IntermediateSection section, PackageFacade facade, Dictionary<string, WixBundlePayloadSymbol> packagePayloads)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
            this.BackendHelper = serviceProvider.GetService<IBackendHelper>();
            this.PathResolver = serviceProvider.GetService<IPathResolver>();

            this.BackendExtensions = backendExtensions;

            this.PackagePayloads = packagePayloads;
            this.Section = section;

            this.ChainPackage = facade.PackageSymbol;
            this.MsiPackage = (WixBundleMsiPackageSymbol)facade.SpecificPackageSymbol;
            this.PackagePayload = packagePayloads[this.ChainPackage.PayloadRef];
        }

        private IMessaging Messaging { get; }

        private IBackendHelper BackendHelper { get; }

        private IPathResolver PathResolver { get; }

        private IEnumerable<IBurnBackendBinderExtension> BackendExtensions { get; }

        private Dictionary<string, WixBundlePayloadSymbol> PackagePayloads { get; }

        private WixBundlePackageSymbol ChainPackage { get; }

        private WixBundleMsiPackageSymbol MsiPackage { get; }

        private string PackageId => this.ChainPackage.Id.Id;

        private WixBundlePayloadSymbol PackagePayload { get; }

        private IntermediateSection Section { get; }

        /// <summary>
        /// Processes the MSI packages to add properties and payloads from the MSI packages.
        /// </summary>
        public void Execute()
        {
            var harvestedMsiPackage = this.Section.Symbols.OfType<WixBundleHarvestedMsiPackageSymbol>()
                                                          .Where(h => h.Id.Id == this.PackagePayload.Id.Id)
                                                          .SingleOrDefault();

            if (harvestedMsiPackage == null)
            {
                harvestedMsiPackage = this.HarvestPackage();

                if (harvestedMsiPackage == null)
                {
                    return;
                }
            }

            foreach (var childPayload in this.Section.Symbols.OfType<WixBundlePayloadSymbol>().Where(p => p.ParentPackagePayloadRef == this.PackagePayload.Id.Id).ToList())
            {
                this.Section.AddSymbol(new WixGroupSymbol(childPayload.SourceLineNumbers)
                {
                    ParentType = ComplexReferenceParentType.Package,
                    ParentId = this.PackageId,
                    ChildType = ComplexReferenceChildType.Payload,
                    ChildId = childPayload.Id.Id,
                });
            }

            this.ChainPackage.PerMachine = harvestedMsiPackage.PerMachine;
            this.ChainPackage.Win64 = harvestedMsiPackage.Win64;

            this.MsiPackage.ProductCode = harvestedMsiPackage.ProductCode;
            this.MsiPackage.UpgradeCode = harvestedMsiPackage.UpgradeCode;
            this.MsiPackage.Manufacturer = harvestedMsiPackage.Manufacturer;
            this.MsiPackage.ProductLanguage = Convert.ToInt32(harvestedMsiPackage.ProductLanguage, CultureInfo.InvariantCulture);
            this.MsiPackage.ProductVersion = harvestedMsiPackage.ProductVersion;

            if (String.IsNullOrEmpty(this.ChainPackage.CacheId))
            {
                this.ChainPackage.CacheId = CacheIdGenerator.GenerateLocalCacheId(this.Messaging, harvestedMsiPackage, this.PackagePayload, this.MsiPackage.SourceLineNumbers, "MsiPackage");
            }

            if (String.IsNullOrEmpty(this.ChainPackage.DisplayName))
            {
                this.ChainPackage.DisplayName = harvestedMsiPackage.ProductName;
            }

            if (String.IsNullOrEmpty(this.ChainPackage.Description))
            {
                this.ChainPackage.Description = harvestedMsiPackage.ArpComments;
            }

            if (String.IsNullOrEmpty(this.ChainPackage.Version))
            {
                this.ChainPackage.Version = this.MsiPackage.ProductVersion;
            }

            if (!this.BackendHelper.IsValidMsiProductVersion(this.MsiPackage.ProductVersion))
            {
                this.Messaging.Write(WarningMessages.InvalidMsiProductVersion(this.PackagePayload.SourceLineNumbers, this.MsiPackage.ProductVersion, this.PackageId));
            }

            this.SetPerMachineAppropriately(harvestedMsiPackage.AllUsers);

            var msiPropertyNames = this.GetMsiPropertyNames();

            // Ensure the MSI package is appropriately marked visible or not.
            this.SetPackageVisibility(harvestedMsiPackage.ArpSystemComponent, msiPropertyNames);

            // Unless the MSI or setup code overrides the default, set MSIFASTINSTALL for best performance.
            if (String.IsNullOrEmpty(harvestedMsiPackage.MsiFastInstall) && !msiPropertyNames.Contains("MSIFASTINSTALL"))
            {
                this.AddMsiProperty("MSIFASTINSTALL", "7");
            }

            this.ChainPackage.InstallSize = harvestedMsiPackage.InstallSize;
        }

        public WixBundleHarvestedMsiPackageSymbol HarvestPackage()
        {
            bool perMachine;
            bool win64;
            string productName;
            string arpComments;
            string allUsers;
            string msiFastInstall;
            string arpSystemComponent;
            string productCode;
            string upgradeCode;
            string manufacturer;
            string productLanguage;
            string productVersion;
            long installSize;

            var sourcePath = this.PackagePayload.SourceFile.Path;

            try
            {
                var longNamesInImage = false;
                var compressed = false;

                this.CheckIfWindowsInstallerFileTooLarge(this.PackagePayload.SourceLineNumbers, sourcePath, "MSI");

                using (var db = new Database(sourcePath, OpenDatabase.ReadOnly))
                {
                    // Read data out of the msi database...
                    using (var sumInfo = new SummaryInformation(db))
                    {
                        var fileAndElevateFlags = sumInfo.GetNumericProperty(SummaryInformation.Package.FileAndElevatedFlags);
                        var platformsAndLanguages = sumInfo.GetProperty(SummaryInformation.Package.PlatformsAndLanguages);

                        // 1 is the Word Count summary information stream bit that means
                        // the MSI uses short file names when set. We care about long file
                        // names so check when the bit is not set.

                        longNamesInImage = 0 == (fileAndElevateFlags & 1);

                        // 2 is the Word Count summary information stream bit that means
                        // files are compressed in the MSI by default when the bit is set.
                        compressed = 2 == (fileAndElevateFlags & 2);

                        // 8 is the Word Count summary information stream bit that means
                        // "Elevated privileges are not required to install this package."
                        // in MSI 4.5 and below, if this bit is 0, elevation is required.
                        perMachine = (0 == (fileAndElevateFlags & 8));
                        win64 = this.IsWin64(sourcePath, platformsAndLanguages);
                    }

                    using (var view = db.OpenView(PropertySqlQuery))
                    {
                        productName = ProcessMsiPackageCommand.GetProperty(view, "ProductName");
                        arpComments = ProcessMsiPackageCommand.GetProperty(view, "ARPCOMMENTS");
                        allUsers = ProcessMsiPackageCommand.GetProperty(view, "ALLUSERS");
                        msiFastInstall = ProcessMsiPackageCommand.GetProperty(view, "MSIFASTINSTALL");
                        arpSystemComponent = ProcessMsiPackageCommand.GetProperty(view, "ARPSYSTEMCOMPONENT");

                        productCode = ProcessMsiPackageCommand.GetProperty(view, "ProductCode");
                        upgradeCode = ProcessMsiPackageCommand.GetProperty(view, "UpgradeCode");
                        manufacturer = ProcessMsiPackageCommand.GetProperty(view, "Manufacturer");
                        productLanguage = ProcessMsiPackageCommand.GetProperty(view, "ProductLanguage");
                        productVersion = ProcessMsiPackageCommand.GetProperty(view, "ProductVersion");
                    }

                    var payloadNames = this.GetPayloadTargetNames();

                    this.CreateRelatedPackages(db);

                    this.CreateMsiFeatures(db);

                    // Add all external cabinets as package payloads.
                    this.ImportExternalCabinetAsPayloads(db, payloadNames);

                    // Add all external files as package payloads and calculate the total install size as the rollup of
                    // File table's sizes.
                    installSize = this.ImportExternalFileAsPayloadsAndReturnInstallSize(db, longNamesInImage, compressed, payloadNames);

                    // Add all dependency providers from the MSI.
                    this.ImportDependencyProviders(db);
                }
            }
            catch (MsiException e)
            {
                this.Messaging.Write(ErrorMessages.UnableToReadPackageInformation(this.PackagePayload.SourceLineNumbers, sourcePath, e.Message));
                return null;
            }

            return this.Section.AddSymbol(new WixBundleHarvestedMsiPackageSymbol(this.PackagePayload.SourceLineNumbers, this.PackagePayload.Id)
            {
                PerMachine = perMachine,
                Win64 = win64,
                ProductName = productName,
                ArpComments = arpComments,
                AllUsers = allUsers,
                MsiFastInstall = msiFastInstall,
                ArpSystemComponent = arpSystemComponent,
                ProductCode = productCode,
                UpgradeCode = upgradeCode,
                Manufacturer = manufacturer,
                ProductLanguage = productLanguage,
                ProductVersion = productVersion,
                InstallSize = installSize,
            });
        }

        private ISet<string> GetPayloadTargetNames()
        {
            var payloadNames = this.PackagePayloads.Values.Select(p => p.Name);

            return new HashSet<string>(payloadNames, StringComparer.OrdinalIgnoreCase);
        }

        private ISet<string> GetMsiPropertyNames()
        {
            var properties = this.Section.Symbols.OfType<WixBundleMsiPropertySymbol>()
                .Where(p => p.PackageRef == this.PackageId)
                .Select(p => p.Name);

            return new HashSet<string>(properties, StringComparer.Ordinal);
        }

        // https://docs.microsoft.com/en-us/windows/win32/msi/template-summary
        private bool IsWin64(string sourcePath, string platformsAndLanguages)
        {
            var separatorIndex = platformsAndLanguages.IndexOf(';');
            var platformValue = separatorIndex > 0 ? platformsAndLanguages.Substring(0, separatorIndex) : platformsAndLanguages;

            switch (platformValue)
            {
                case "Arm64":
                case "Intel64":
                case "x64":
                    return true;

                case "Arm":
                case "Intel":
                    return false;

                default:
                    this.Messaging.Write(BurnBackendWarnings.UnknownMsiPackagePlatform(this.PackagePayload.SourceLineNumbers, sourcePath, platformValue));
                    return true;
            }
        }

        private void SetPerMachineAppropriately(string allusers)
        {
            Debug.Assert(this.ChainPackage.PerMachine.HasValue);
            var perMachine = this.ChainPackage.PerMachine.Value;

            // Can ignore ALLUSERS from MsiProperties because it is not allowed there.
            if (this.MsiPackage.ForcePerMachine)
            {
                if (!perMachine)
                {
                    this.Messaging.Write(WarningMessages.PerUserButForcingPerMachine(this.PackagePayload.SourceLineNumbers, this.PackageId));
                    this.ChainPackage.PerMachine = true; // ensure that we think the package is per-machine.
                }

                // Force ALLUSERS=1 via the MSI command-line.
                this.AddMsiProperty("ALLUSERS", "1");
            }
            else
            {
                if (String.IsNullOrEmpty(allusers))
                {
                    // Not forced per-machine and no ALLUSERS property, flip back to per-user.
                    if (perMachine)
                    {
                        this.Messaging.Write(WarningMessages.ImplicitlyPerUser(this.ChainPackage.SourceLineNumbers, this.PackageId));
                        this.ChainPackage.PerMachine = false;
                    }
                }
                else if (allusers.Equals("1", StringComparison.Ordinal))
                {
                    if (!perMachine)
                    {
                        this.Messaging.Write(ErrorMessages.PerUserButAllUsersEquals1(this.ChainPackage.SourceLineNumbers, this.PackageId));
                    }
                }
                else if (allusers.Equals("2", StringComparison.Ordinal))
                {
                    this.Messaging.Write(WarningMessages.DiscouragedAllUsersValue(this.ChainPackage.SourceLineNumbers, this.PackageId, perMachine ? "machine" : "user"));
                }
                else
                {
                    this.Messaging.Write(ErrorMessages.UnsupportedAllUsersValue(this.ChainPackage.SourceLineNumbers, this.PackageId, allusers));
                }
            }
        }

        private void SetPackageVisibility(string systemComponent, ISet<string> msiPropertyNames)
        {
            // If the authoring specifically added "ARPSYSTEMCOMPONENT", don't do it again.
            if (!msiPropertyNames.Contains("ARPSYSTEMCOMPONENT"))
            {
                var alreadyVisible = String.IsNullOrEmpty(systemComponent);
                var visible = this.ChainPackage.Visible;

                // If not already set to the correct visibility.
                if (alreadyVisible != visible)
                {
                    this.AddMsiProperty("ARPSYSTEMCOMPONENT", visible ? String.Empty : "1");
                }
            }
        }

        private void CreateRelatedPackages(Database db)
        {
            // Represent the Upgrade table as related packages.
            if (db.TableExists("Upgrade"))
            {
                using (var view = db.OpenExecuteView("SELECT `UpgradeCode`, `VersionMin`, `VersionMax`, `Language`, `Attributes` FROM `Upgrade`"))
                {
                    foreach (var record in view.Records)
                    {
                        var recordAttributes = record.GetInteger(5);

                        var attributes = WixBundleRelatedPackageAttributes.None;
                        attributes |= (recordAttributes & WindowsInstallerConstants.MsidbUpgradeAttributesOnlyDetect) == WindowsInstallerConstants.MsidbUpgradeAttributesOnlyDetect ? WixBundleRelatedPackageAttributes.OnlyDetect : 0;
                        attributes |= (recordAttributes & WindowsInstallerConstants.MsidbUpgradeAttributesVersionMinInclusive) == WindowsInstallerConstants.MsidbUpgradeAttributesVersionMinInclusive ? WixBundleRelatedPackageAttributes.MinInclusive : 0;
                        attributes |= (recordAttributes & WindowsInstallerConstants.MsidbUpgradeAttributesVersionMaxInclusive) == WindowsInstallerConstants.MsidbUpgradeAttributesVersionMaxInclusive ? WixBundleRelatedPackageAttributes.MaxInclusive : 0;
                        attributes |= (recordAttributes & WindowsInstallerConstants.MsidbUpgradeAttributesLanguagesExclusive) == WindowsInstallerConstants.MsidbUpgradeAttributesLanguagesExclusive ? 0 : WixBundleRelatedPackageAttributes.LangInclusive;

                        this.Section.AddSymbol(new WixBundleRelatedPackageSymbol(this.PackagePayload.SourceLineNumbers)
                        {
                            PackagePayloadRef = this.PackagePayload.Id.Id,
                            RelatedId = record.GetString(1),
                            MinVersion = record.GetString(2),
                            MaxVersion = record.GetString(3),
                            Languages = record.GetString(4),
                            Attributes = attributes,
                        });
                    }
                }
            }
        }

        private void CreateMsiFeatures(Database db)
        {
            if (db.TableExists("Feature") && db.TableExists("FeatureComponents"))
            {
                using (var allFeaturesView = db.OpenExecuteView("SELECT * FROM `Feature`"))
                using (var featureView = db.OpenView("SELECT `Component_` FROM `FeatureComponents` WHERE `Feature_` = ?"))
                using (var componentView = db.OpenView("SELECT `FileSize` FROM `File` WHERE `Component_` = ?"))
                {
                    using (var featureRecord = new Record(1))
                    using (var componentRecord = new Record(1))
                    {
                        foreach (var allFeaturesResultRecord in allFeaturesView.Records)
                        {
                            var featureName = allFeaturesResultRecord.GetString(1);

                            // Calculate the Feature size.
                            featureRecord.SetString(1, featureName);
                            featureView.Execute(featureRecord);

                            // Loop over all the components for the feature to calculate the size of the feature.
                            long size = 0;
                            foreach (var componentResultRecord in featureView.Records)
                            {
                                var component = componentResultRecord.GetString(1);
                                componentRecord.SetString(1, component);
                                componentView.Execute(componentRecord);

                                foreach (var fileResultRecord in componentView.Records)
                                {
                                    var fileSize = fileResultRecord.GetString(1);
                                    size += Convert.ToInt32(fileSize, CultureInfo.InvariantCulture.NumberFormat);
                                }
                            }

                            this.Section.AddSymbol(new WixBundleMsiFeatureSymbol(this.PackagePayload.SourceLineNumbers, new Identifier(AccessModifier.Section, this.PackagePayload.Id.Id, featureName))
                            {
                                PackagePayloadRef = this.PackagePayload.Id.Id,
                                Name = featureName,
                                Parent = allFeaturesResultRecord.GetString(2),
                                Title = allFeaturesResultRecord.GetString(3),
                                Description = allFeaturesResultRecord.GetString(4),
                                Display = allFeaturesResultRecord.GetInteger(5),
                                Level = allFeaturesResultRecord.GetInteger(6),
                                Directory = allFeaturesResultRecord.GetString(7),
                                Attributes = allFeaturesResultRecord.GetInteger(8),
                                Size = size
                            });
                        }
                    }
                }
            }
        }

        private void ImportExternalCabinetAsPayloads(Database db, ISet<string> payloadNames)
        {
            if (db.TableExists("Media"))
            {
                using (var view = db.OpenExecuteView("SELECT `Cabinet` FROM `Media`"))
                {
                    var sourceLineNumbers = this.PackagePayload.SourceLineNumbers;

                    foreach (var cabinetRecord in view.Records)
                    {
                        var cabinet = cabinetRecord.GetString(1);

                        if (!String.IsNullOrEmpty(cabinet) && !cabinet.StartsWith("#", StringComparison.Ordinal))
                        {
                            // If we didn't find the Payload as an existing child of the package, we need to
                            // add it.  We expect the file to exist on-disk in the same relative location as
                            // the MSI expects to find it...
                            var cabinetName = Path.Combine(Path.GetDirectoryName(this.PackagePayload.Name), cabinet);

                            if (!payloadNames.Contains(cabinetName))
                            {
                                var generatedId = this.BackendHelper.GenerateIdentifier("cab", this.PackagePayload.Id.Id, cabinet);
                                var payloadSourceFile = this.ResolveRelatedFile(this.PackagePayload.SourceFile.Path, this.PackagePayload.UnresolvedSourceFile, cabinet, "cabinet", sourceLineNumbers);

                                this.Section.AddSymbol(new WixBundlePayloadSymbol(sourceLineNumbers, new Identifier(AccessModifier.Section, generatedId))
                                {
                                    Name = cabinetName,
                                    SourceFile = new IntermediateFieldPathValue { Path = payloadSourceFile },
                                    Compressed = this.PackagePayload.Compressed,
                                    UnresolvedSourceFile = cabinetName,
                                    ContainerRef = this.PackagePayload.ContainerRef,
                                    DownloadUrl = this.PackagePayload.DownloadUrl,
                                    Packaging = this.PackagePayload.Packaging,
                                    ParentPackagePayloadRef = this.PackagePayload.Id.Id,
                                });

                                this.CheckIfWindowsInstallerFileTooLarge(sourceLineNumbers, payloadSourceFile, "cabinet");
                            }
                        }
                    }
                }
            }
        }

        private long ImportExternalFileAsPayloadsAndReturnInstallSize(Database db, bool longNamesInImage, bool compressed, ISet<string> payloadNames)
        {
            long size = 0;

            if (db.TableExists("Component") && db.TableExists("Directory") && db.TableExists("File"))
            {
                var directories = new Dictionary<string, IResolvedDirectory>();

                // Load up the directory hash table so we will be able to resolve source paths
                // for files in the MSI database.
                using (var view = db.OpenExecuteView("SELECT `Directory`, `Directory_Parent`, `DefaultDir` FROM `Directory`"))
                {
                    foreach (var record in view.Records)
                    {
                        var sourceName = this.BackendHelper.GetMsiFileName(record.GetString(3), true, longNamesInImage);

                        var resolvedDirectory = this.BackendHelper.CreateResolvedDirectory(record.GetString(2), sourceName);

                        directories.Add(record.GetString(1), resolvedDirectory);
                    }
                }

                // Resolve the source paths to external files and add each file size to the total
                // install size of the package.
                using (var view = db.OpenExecuteView("SELECT `Directory_`, `File`, `FileName`, `File`.`Attributes`, `FileSize` FROM `Component`, `File` WHERE `Component`.`Component`=`File`.`Component_`"))
                {
                    var sourceLineNumbers = this.PackagePayload.SourceLineNumbers;

                    foreach (var record in view.Records)
                    {
                        // If the file is explicitly uncompressed or the MSI is uncompressed and the file is not
                        // explicitly marked compressed then this is an external file.
                        var compressionBit = record.GetInteger(4);
                        if (WindowsInstallerConstants.MsidbFileAttributesNoncompressed == (compressionBit & WindowsInstallerConstants.MsidbFileAttributesNoncompressed) ||
                            (!compressed && 0 == (compressionBit & WindowsInstallerConstants.MsidbFileAttributesCompressed)))
                        {
                            var fileSourcePath = this.PathResolver.GetFileSourcePath(directories, record.GetString(1), record.GetString(3), compressed, longNamesInImage);
                            var name = Path.Combine(Path.GetDirectoryName(this.PackagePayload.Name), fileSourcePath);

                            if (!payloadNames.Contains(name))
                            {
                                var generatedId = this.BackendHelper.GenerateIdentifier("f", this.PackagePayload.Id.Id, record.GetString(2));
                                var payloadSourceFile = this.ResolveRelatedFile(this.PackagePayload.SourceFile.Path, this.PackagePayload.UnresolvedSourceFile, fileSourcePath, "payload", sourceLineNumbers);

                                this.Section.AddSymbol(new WixBundlePayloadSymbol(sourceLineNumbers, new Identifier(AccessModifier.Section, generatedId))
                                {
                                    Name = name,
                                    SourceFile = new IntermediateFieldPathValue { Path = payloadSourceFile },
                                    Compressed = this.PackagePayload.Compressed,
                                    UnresolvedSourceFile = name,
                                    ContainerRef = this.PackagePayload.ContainerRef,
                                    DownloadUrl = this.PackagePayload.DownloadUrl,
                                    Packaging = this.PackagePayload.Packaging,
                                    ParentPackagePayloadRef = this.PackagePayload.Id.Id,
                                });
                            }
                        }

                        size += record.GetInteger(5);
                    }
                }
            }

            return size;
        }

        private void AddMsiProperty(string name, string value)
        {
            this.Section.AddSymbol(new WixBundleMsiPropertySymbol(this.PackagePayload.SourceLineNumbers, new Identifier(AccessModifier.Section, this.PackageId, name))
            {
                PackageRef = this.PackageId,
                Name = name,
                Value = value,
            });
        }

        private void ImportDependencyProviders(Database db)
        {
            this.ImportDependencyProvidersFromTable(db, "WixDependencyProvider");
            this.ImportDependencyProvidersFromTable(db, "Wix4DependencyProvider");
        }

        private void ImportDependencyProvidersFromTable(Database db, string tableName)
        {
            if (db.TableExists(tableName))
            {
                using (var view = db.OpenExecuteView($"SELECT `WixDependencyProvider`, `ProviderKey`, `Version`, `DisplayName`, `Attributes` FROM `{tableName}`"))
                {
                    foreach (var record in view.Records)
                    {
                        var id = new Identifier(AccessModifier.Section, this.BackendHelper.GenerateIdentifier("dep", this.PackagePayload.Id.Id, record.GetString(1)));

                        // Import the provider key and attributes.
                        this.Section.AddSymbol(new WixBundleHarvestedDependencyProviderSymbol(this.PackagePayload.SourceLineNumbers, id)
                        {
                            PackagePayloadRef = this.PackagePayload.Id.Id,
                            ProviderKey = record.GetString(2),
                            Version = record.GetString(3) ?? this.MsiPackage.ProductVersion,
                            DisplayName = record.GetString(4) ?? this.ChainPackage.DisplayName,
                            ProviderAttributes = record.GetInteger(5),
                        });
                    }
                }
            }
        }

        private string ResolveRelatedFile(string resolvedSource, string unresolvedSource, string relatedSource, string type, SourceLineNumber sourceLineNumbers)
        {
            var checkedPaths = new List<string>();

            foreach (var extension in this.BackendExtensions)
            {
                var resolved = extension.ResolveRelatedFile(unresolvedSource, relatedSource, type, sourceLineNumbers);

                if (resolved?.CheckedPaths != null)
                {
                    checkedPaths.AddRange(resolved.CheckedPaths);
                }

                if (!String.IsNullOrEmpty(resolved?.Path))
                {
                    return resolved?.Path;
                }
            }

            var resolvedPath = Path.Combine(Path.GetDirectoryName(resolvedSource), relatedSource);

            if (!File.Exists(resolvedPath))
            {
                checkedPaths.Add(resolvedPath);
                this.Messaging.Write(ErrorMessages.FileNotFound(sourceLineNumbers, resolvedPath, type, checkedPaths));
            }

            return resolvedPath;
        }

        private void CheckIfWindowsInstallerFileTooLarge(SourceLineNumber sourceLineNumber, string path, string description)
        {
            // Best effort check to see if the file is too large for the Windows Installer.
            try
            {
                var fi = new FileInfo(path);
                if (fi.Length > Int32.MaxValue)
                {
                    this.Messaging.Write(WarningMessages.WindowsInstallerFileTooLarge(sourceLineNumber, path, description));
                }
            }
            catch
            {
            }
        }

        private static string GetProperty(View view, string property)
        {
            using (var queryRecord = new Record(1))
            {
                queryRecord[1] = property;

                view.Execute(queryRecord);

                using (var record = view.Fetch())
                {
                    return record?.GetString(1);
                }
            }
        }
    }
}
