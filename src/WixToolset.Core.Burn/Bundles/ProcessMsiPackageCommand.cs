// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bundles
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Core.Native;
    using Dtf = WixToolset.Dtf.WindowsInstaller;
    using WixToolset.Data.Bind;

    /// <summary>
    /// Initializes package state from the MSI contents.
    /// </summary>
    internal class ProcessMsiPackageCommand
    {
#if TODO
        private const string PropertySqlFormat = "SELECT `Value` FROM `Property` WHERE `Property` = '{0}'";

        public RowDictionary<WixBundlePayloadRow> AuthoredPayloads { private get; set; }

        public PackageFacade Facade { private get; set; }

        public IEnumerable<IBurnBackendExtension> BackendExtensions { private get; set; }

        public Table MsiFeatureTable { private get; set; }

        public Table MsiPropertyTable { private get; set; }

        public Table PayloadTable { private get; set; }

        public Table RelatedPackageTable { private get; set; }

        /// <summary>
        /// Processes the MSI packages to add properties and payloads from the MSI packages.
        /// </summary>
        public void Execute()
        {
            WixBundlePayloadRow packagePayload = this.AuthoredPayloads.Get(this.Facade.Package.PackagePayload);

            string sourcePath = packagePayload.FullFileName;
            bool longNamesInImage = false;
            bool compressed = false;
            bool x64 = false;
            try
            {
                // Read data out of the msi database...
                using (Dtf.SummaryInfo sumInfo = new Dtf.SummaryInfo(sourcePath, false))
                {
                    // 1 is the Word Count summary information stream bit that means
                    // the MSI uses short file names when set. We care about long file
                    // names so check when the bit is not set.
                    longNamesInImage = 0 == (sumInfo.WordCount & 1);

                    // 2 is the Word Count summary information stream bit that means
                    // files are compressed in the MSI by default when the bit is set.
                    compressed = 2 == (sumInfo.WordCount & 2);

                    x64 = (sumInfo.Template.Contains("x64") || sumInfo.Template.Contains("Intel64"));

                    // 8 is the Word Count summary information stream bit that means
                    // "Elevated privileges are not required to install this package."
                    // in MSI 4.5 and below, if this bit is 0, elevation is required.
                    this.Facade.Package.PerMachine = (0 == (sumInfo.WordCount & 8)) ? YesNoDefaultType.Yes : YesNoDefaultType.No;
                    this.Facade.Package.x64 = x64 ? YesNoType.Yes : YesNoType.No;
                }

                using (Dtf.Database db = new Dtf.Database(sourcePath))
                {
                    this.Facade.MsiPackage.ProductCode = ProcessMsiPackageCommand.GetProperty(db, "ProductCode");
                    this.Facade.MsiPackage.UpgradeCode = ProcessMsiPackageCommand.GetProperty(db, "UpgradeCode");
                    this.Facade.MsiPackage.Manufacturer = ProcessMsiPackageCommand.GetProperty(db, "Manufacturer");
                    this.Facade.MsiPackage.ProductLanguage = Convert.ToInt32(ProcessMsiPackageCommand.GetProperty(db, "ProductLanguage"), CultureInfo.InvariantCulture);
                    this.Facade.MsiPackage.ProductVersion = ProcessMsiPackageCommand.GetProperty(db, "ProductVersion");

                    if (!Common.IsValidModuleOrBundleVersion(this.Facade.MsiPackage.ProductVersion))
                    {
                        // not a proper .NET version (e.g., five fields); can we get a valid four-part version number?
                        string version = null;
                        string[] versionParts = this.Facade.MsiPackage.ProductVersion.Split('.');
                        int count = versionParts.Length;
                        if (0 < count)
                        {
                            version = versionParts[0];
                            for (int i = 1; i < 4 && i < count; ++i)
                            {
                                version = String.Concat(version, ".", versionParts[i]);
                            }
                        }
                        
                        if (!String.IsNullOrEmpty(version) && Common.IsValidModuleOrBundleVersion(version))
                        {
                            Messaging.Instance.OnMessage(WixWarnings.VersionTruncated(this.Facade.Package.SourceLineNumbers, this.Facade.MsiPackage.ProductVersion, sourcePath, version));
                            this.Facade.MsiPackage.ProductVersion = version;
                        }
                        else
                        {
                            Messaging.Instance.OnMessage(WixErrors.InvalidProductVersion(this.Facade.Package.SourceLineNumbers, this.Facade.MsiPackage.ProductVersion, sourcePath));
                        }
                    }

                    if (String.IsNullOrEmpty(this.Facade.Package.CacheId))
                    {
                        this.Facade.Package.CacheId = String.Format("{0}v{1}", this.Facade.MsiPackage.ProductCode, this.Facade.MsiPackage.ProductVersion);
                    }

                    if (String.IsNullOrEmpty(this.Facade.Package.DisplayName))
                    {
                        this.Facade.Package.DisplayName = ProcessMsiPackageCommand.GetProperty(db, "ProductName");
                    }

                    if (String.IsNullOrEmpty(this.Facade.Package.Description))
                    {
                        this.Facade.Package.Description = ProcessMsiPackageCommand.GetProperty(db, "ARPCOMMENTS");
                    }

                    ISet<string> payloadNames = this.GetPayloadTargetNames();

                    ISet<string> msiPropertyNames = this.GetMsiPropertyNames();

                    this.SetPerMachineAppropriately(db, sourcePath);

                    // Ensure the MSI package is appropriately marked visible or not.
                    this.SetPackageVisibility(db, msiPropertyNames);

                    // Unless the MSI or setup code overrides the default, set MSIFASTINSTALL for best performance.
                    if (!msiPropertyNames.Contains("MSIFASTINSTALL") && !ProcessMsiPackageCommand.HasProperty(db, "MSIFASTINSTALL"))
                    {
                        this.AddMsiProperty("MSIFASTINSTALL", "7");
                    }

                    this.CreateRelatedPackages(db);

                    // If feature selection is enabled, represent the Feature table in the manifest.
                    if (this.Facade.MsiPackage.EnableFeatureSelection)
                    {
                        this.CreateMsiFeatures(db);
                    }

                    // Add all external cabinets as package payloads.
                    this.ImportExternalCabinetAsPayloads(db, packagePayload, payloadNames);

                    // Add all external files as package payloads and calculate the total install size as the rollup of
                    // File table's sizes.
                    this.Facade.Package.InstallSize = this.ImportExternalFileAsPayloadsAndReturnInstallSize(db, packagePayload, longNamesInImage, compressed, payloadNames);

                    // Add all dependency providers from the MSI.
                    this.ImportDependencyProviders(db);
                }
            }
            catch (Dtf.InstallerException e)
            {
                Messaging.Instance.OnMessage(WixErrors.UnableToReadPackageInformation(this.Facade.Package.SourceLineNumbers, sourcePath, e.Message));
            }
        }

        private ISet<string> GetPayloadTargetNames()
        {
            IEnumerable<string> payloadNames = this.PayloadTable.RowsAs<WixBundlePayloadRow>()
                .Where(r => r.Package == this.Facade.Package.WixChainItemId)
                .Select(r => r.Name);

            return new HashSet<string>(payloadNames, StringComparer.OrdinalIgnoreCase);
        }

        private ISet<string> GetMsiPropertyNames()
        {
            IEnumerable<string> properties = this.MsiPropertyTable.RowsAs<WixBundleMsiPropertyRow>()
                .Where(r => r.ChainPackageId == this.Facade.Package.WixChainItemId)
                .Select(r => r.Name);

            return new HashSet<string>(properties, StringComparer.Ordinal);
        }

        private void SetPerMachineAppropriately(Dtf.Database db, string sourcePath)
        {
            if (this.Facade.MsiPackage.ForcePerMachine)
            {
                if (YesNoDefaultType.No == this.Facade.Package.PerMachine)
                {
                    Messaging.Instance.OnMessage(WixWarnings.PerUserButForcingPerMachine(this.Facade.Package.SourceLineNumbers, sourcePath));
                    this.Facade.Package.PerMachine = YesNoDefaultType.Yes; // ensure that we think the package is per-machine.
                }

                // Force ALLUSERS=1 via the MSI command-line.
                this.AddMsiProperty("ALLUSERS", "1");
            }
            else
            {
                string allusers = ProcessMsiPackageCommand.GetProperty(db, "ALLUSERS");

                if (String.IsNullOrEmpty(allusers))
                {
                    // Not forced per-machine and no ALLUSERS property, flip back to per-user.
                    if (YesNoDefaultType.Yes == this.Facade.Package.PerMachine)
                    {
                        Messaging.Instance.OnMessage(WixWarnings.ImplicitlyPerUser(this.Facade.Package.SourceLineNumbers, sourcePath));
                        this.Facade.Package.PerMachine = YesNoDefaultType.No;
                    }
                }
                else if (allusers.Equals("1", StringComparison.Ordinal))
                {
                    if (YesNoDefaultType.No == this.Facade.Package.PerMachine)
                    {
                        Messaging.Instance.OnMessage(WixErrors.PerUserButAllUsersEquals1(this.Facade.Package.SourceLineNumbers, sourcePath));
                    }
                }
                else if (allusers.Equals("2", StringComparison.Ordinal))
                {
                    Messaging.Instance.OnMessage(WixWarnings.DiscouragedAllUsersValue(this.Facade.Package.SourceLineNumbers, sourcePath, (YesNoDefaultType.Yes == this.Facade.Package.PerMachine) ? "machine" : "user"));
                }
                else
                {
                    Messaging.Instance.OnMessage(WixErrors.UnsupportedAllUsersValue(this.Facade.Package.SourceLineNumbers, sourcePath, allusers));
                }
            }
        }

        private void SetPackageVisibility(Dtf.Database db, ISet<string> msiPropertyNames)
        {
            bool alreadyVisible = !ProcessMsiPackageCommand.HasProperty(db, "ARPSYSTEMCOMPONENT");

            if (alreadyVisible != this.Facade.Package.Visible) // if not already set to the correct visibility.
            {
                // If the authoring specifically added "ARPSYSTEMCOMPONENT", don't do it again.
                if (!msiPropertyNames.Contains("ARPSYSTEMCOMPONENT"))
                {
                    this.AddMsiProperty("ARPSYSTEMCOMPONENT", this.Facade.Package.Visible ? String.Empty : "1");
                }
            }
        }

        private void CreateRelatedPackages(Dtf.Database db)
        {
            // Represent the Upgrade table as related packages.
            if (db.Tables.Contains("Upgrade"))
            {
                using (Dtf.View view = db.OpenView("SELECT `UpgradeCode`, `VersionMin`, `VersionMax`, `Language`, `Attributes` FROM `Upgrade`"))
                {
                    view.Execute();
                    while (true)
                    {
                        using (Dtf.Record record = view.Fetch())
                        {
                            if (null == record)
                            {
                                break;
                            }

                            WixBundleRelatedPackageRow related = (WixBundleRelatedPackageRow)this.RelatedPackageTable.CreateRow(this.Facade.Package.SourceLineNumbers);
                            related.ChainPackageId = this.Facade.Package.WixChainItemId;
                            related.Id = record.GetString(1);
                            related.MinVersion = record.GetString(2);
                            related.MaxVersion = record.GetString(3);
                            related.Languages = record.GetString(4);

                            int attributes = record.GetInteger(5);
                            related.OnlyDetect = (attributes & MsiInterop.MsidbUpgradeAttributesOnlyDetect) == MsiInterop.MsidbUpgradeAttributesOnlyDetect;
                            related.MinInclusive = (attributes & MsiInterop.MsidbUpgradeAttributesVersionMinInclusive) == MsiInterop.MsidbUpgradeAttributesVersionMinInclusive;
                            related.MaxInclusive = (attributes & MsiInterop.MsidbUpgradeAttributesVersionMaxInclusive) == MsiInterop.MsidbUpgradeAttributesVersionMaxInclusive;
                            related.LangInclusive = (attributes & MsiInterop.MsidbUpgradeAttributesLanguagesExclusive) == 0;
                        }
                    }
                }
            }
        }

        private void CreateMsiFeatures(Dtf.Database db)
        {
            if (db.Tables.Contains("Feature"))
            {
                using (Dtf.View featureView = db.OpenView("SELECT `Component_` FROM `FeatureComponents` WHERE `Feature_` = ?"))
                using (Dtf.View componentView = db.OpenView("SELECT `FileSize` FROM `File` WHERE `Component_` = ?"))
                {
                    using (Dtf.Record featureRecord = new Dtf.Record(1))
                    using (Dtf.Record componentRecord = new Dtf.Record(1))
                    {
                        using (Dtf.View allFeaturesView = db.OpenView("SELECT * FROM `Feature`"))
                        {
                            allFeaturesView.Execute();

                            while (true)
                            {
                                using (Dtf.Record allFeaturesResultRecord = allFeaturesView.Fetch())
                                {
                                    if (null == allFeaturesResultRecord)
                                    {
                                        break;
                                    }

                                    string featureName = allFeaturesResultRecord.GetString(1);

                                    // Calculate the Feature size.
                                    featureRecord.SetString(1, featureName);
                                    featureView.Execute(featureRecord);

                                    // Loop over all the components for the feature to calculate the size of the feature.
                                    long size = 0;
                                    while (true)
                                    {
                                        using (Dtf.Record componentResultRecord = featureView.Fetch())
                                        {
                                            if (null == componentResultRecord)
                                            {
                                                break;
                                            }
                                            string component = componentResultRecord.GetString(1);
                                            componentRecord.SetString(1, component);
                                            componentView.Execute(componentRecord);

                                            while (true)
                                            {
                                                using (Dtf.Record fileResultRecord = componentView.Fetch())
                                                {
                                                    if (null == fileResultRecord)
                                                    {
                                                        break;
                                                    }

                                                    string fileSize = fileResultRecord.GetString(1);
                                                    size += Convert.ToInt32(fileSize, CultureInfo.InvariantCulture.NumberFormat);
                                                }
                                            }
                                        }
                                    }

                                    WixBundleMsiFeatureRow feature = (WixBundleMsiFeatureRow)this.MsiFeatureTable.CreateRow(this.Facade.Package.SourceLineNumbers);
                                    feature.ChainPackageId = this.Facade.Package.WixChainItemId;
                                    feature.Name = featureName;
                                    feature.Parent = allFeaturesResultRecord.GetString(2);
                                    feature.Title = allFeaturesResultRecord.GetString(3);
                                    feature.Description = allFeaturesResultRecord.GetString(4);
                                    feature.Display = allFeaturesResultRecord.GetInteger(5);
                                    feature.Level = allFeaturesResultRecord.GetInteger(6);
                                    feature.Directory = allFeaturesResultRecord.GetString(7);
                                    feature.Attributes = allFeaturesResultRecord.GetInteger(8);
                                    feature.Size = size;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ImportExternalCabinetAsPayloads(Dtf.Database db, WixBundlePayloadRow packagePayload, ISet<string> payloadNames)
        {
            if (db.Tables.Contains("Media"))
            {
                foreach (string cabinet in db.ExecuteStringQuery("SELECT `Cabinet` FROM `Media`"))
                {
                    if (!String.IsNullOrEmpty(cabinet) && !cabinet.StartsWith("#", StringComparison.Ordinal))
                    {
                        // If we didn't find the Payload as an existing child of the package, we need to
                        // add it.  We expect the file to exist on-disk in the same relative location as
                        // the MSI expects to find it...
                        string cabinetName = Path.Combine(Path.GetDirectoryName(packagePayload.Name), cabinet);

                        if (!payloadNames.Contains(cabinetName))
                        {
                            string generatedId = Common.GenerateIdentifier("cab", packagePayload.Id, cabinet);
                            string payloadSourceFile = this.ResolveRelatedFile(packagePayload.UnresolvedSourceFile, cabinet, "Cabinet", this.Facade.Package.SourceLineNumbers, BindStage.Normal);

                            WixBundlePayloadRow payload = (WixBundlePayloadRow)this.PayloadTable.CreateRow(this.Facade.Package.SourceLineNumbers);
                            payload.Id = generatedId;
                            payload.Name = cabinetName;
                            payload.SourceFile = payloadSourceFile;
                            payload.Compressed = packagePayload.Compressed;
                            payload.UnresolvedSourceFile = cabinetName;
                            payload.Package = packagePayload.Package;
                            payload.Container = packagePayload.Container;
                            payload.ContentFile = true;
                            payload.EnableSignatureValidation = packagePayload.EnableSignatureValidation;
                            payload.Packaging = packagePayload.Packaging;
                            payload.ParentPackagePayload = packagePayload.Id;
                        }
                    }
                }
            }
        }

        private long ImportExternalFileAsPayloadsAndReturnInstallSize(Dtf.Database db, WixBundlePayloadRow packagePayload, bool longNamesInImage, bool compressed, ISet<string> payloadNames)
        {
            long size = 0;

            if (db.Tables.Contains("Component") && db.Tables.Contains("Directory") && db.Tables.Contains("File"))
            {
                Hashtable directories = new Hashtable();

                // Load up the directory hash table so we will be able to resolve source paths
                // for files in the MSI database.
                using (Dtf.View view = db.OpenView("SELECT `Directory`, `Directory_Parent`, `DefaultDir` FROM `Directory`"))
                {
                    view.Execute();
                    while (true)
                    {
                        using (Dtf.Record record = view.Fetch())
                        {
                            if (null == record)
                            {
                                break;
                            }

                            string sourceName = Common.GetName(record.GetString(3), true, longNamesInImage);
                            directories.Add(record.GetString(1), new ResolvedDirectory(record.GetString(2), sourceName));
                        }
                    }
                }

                // Resolve the source paths to external files and add each file size to the total
                // install size of the package.
                using (Dtf.View view = db.OpenView("SELECT `Directory_`, `File`, `FileName`, `File`.`Attributes`, `FileSize` FROM `Component`, `File` WHERE `Component`.`Component`=`File`.`Component_`"))
                {
                    view.Execute();
                    while (true)
                    {
                        using (Dtf.Record record = view.Fetch())
                        {
                            if (null == record)
                            {
                                break;
                            }

                            // Skip adding the loose files as payloads if it was suppressed.
                            if (!this.Facade.MsiPackage.SuppressLooseFilePayloadGeneration)
                            {
                                // If the file is explicitly uncompressed or the MSI is uncompressed and the file is not
                                // explicitly marked compressed then this is an external file.
                                if (MsiInterop.MsidbFileAttributesNoncompressed == (record.GetInteger(4) & MsiInterop.MsidbFileAttributesNoncompressed) ||
                                    (!compressed && 0 == (record.GetInteger(4) & MsiInterop.MsidbFileAttributesCompressed)))
                                {
                                    string fileSourcePath = Binder.GetFileSourcePath(directories, record.GetString(1), record.GetString(3), compressed, longNamesInImage);
                                    string name = Path.Combine(Path.GetDirectoryName(packagePayload.Name), fileSourcePath);

                                    if (!payloadNames.Contains(name))
                                    {
                                        string generatedId = Common.GenerateIdentifier("f", packagePayload.Id, record.GetString(2));
                                        string payloadSourceFile = this.ResolveRelatedFile(packagePayload.UnresolvedSourceFile, fileSourcePath, "File", this.Facade.Package.SourceLineNumbers, BindStage.Normal);

                                        WixBundlePayloadRow payload = (WixBundlePayloadRow)this.PayloadTable.CreateRow(this.Facade.Package.SourceLineNumbers);
                                        payload.Id = generatedId;
                                        payload.Name = name;
                                        payload.SourceFile = payloadSourceFile;
                                        payload.Compressed = packagePayload.Compressed;
                                        payload.UnresolvedSourceFile = name;
                                        payload.Package = packagePayload.Package;
                                        payload.Container = packagePayload.Container;
                                        payload.ContentFile = true;
                                        payload.EnableSignatureValidation = packagePayload.EnableSignatureValidation;
                                        payload.Packaging = packagePayload.Packaging;
                                        payload.ParentPackagePayload = packagePayload.Id;
                                    }
                                }
                            }

                            size += record.GetInteger(5);
                        }
                    }
                }
            }

            return size;
        }

        private void AddMsiProperty(string name, string value)
        {
            WixBundleMsiPropertyRow row = (WixBundleMsiPropertyRow)this.MsiPropertyTable.CreateRow(this.Facade.MsiPackage.SourceLineNumbers);
            row.ChainPackageId = this.Facade.Package.WixChainItemId;
            row.Name = name;
            row.Value = value;
        }

        private void ImportDependencyProviders(Dtf.Database db)
        {
            if (db.Tables.Contains("WixDependencyProvider"))
            {
                string query = "SELECT `ProviderKey`, `Version`, `DisplayName`, `Attributes` FROM `WixDependencyProvider`";

                using (Dtf.View view = db.OpenView(query))
                {
                    view.Execute();
                    while (true)
                    {
                        using (Dtf.Record record = view.Fetch())
                        {
                            if (null == record)
                            {
                                break;
                            }

                            // Import the provider key and attributes.
                            string providerKey = record.GetString(1);
                            string version = record.GetString(2) ?? this.Facade.MsiPackage.ProductVersion;
                            string displayName = record.GetString(3) ?? this.Facade.Package.DisplayName;
                            int attributes = record.GetInteger(4);

                            ProvidesDependency dependency = new ProvidesDependency(providerKey, version, displayName, attributes);
                            dependency.Imported = true;

                            this.Facade.Provides.Add(dependency);
                        }
                    }
                }
            }
        }

        private string ResolveRelatedFile(string sourceFile, string relatedSource, string type, SourceLineNumber sourceLineNumbers, BindStage stage)
        {
            foreach (var extension in this.BackendExtensions)
            {
                var relatedFile = extension.ResolveRelatedFile(sourceFile, relatedSource, type, sourceLineNumbers, stage);

                if (!String.IsNullOrEmpty(relatedFile))
                {
                    return relatedFile;
                }
            }

            return null;
        }

        /// <summary>
        /// Queries a Windows Installer database for a Property value.
        /// </summary>
        /// <param name="db">Database to query.</param>
        /// <param name="property">Property to examine.</param>
        /// <returns>String value for result or null if query doesn't match a single result.</returns>
        private static string GetProperty(Dtf.Database db, string property)
        {
            try
            {
                return db.ExecuteScalar(PropertyQuery(property)).ToString();
            }
            catch (Dtf.InstallerException)
            {
            }

            return null;
        }

        /// <summary>
        /// Queries a Windows Installer database to determine if one or more rows exist in the Property table.
        /// </summary>
        /// <param name="db">Database to query.</param>
        /// <param name="property">Property to examine.</param>
        /// <returns>True if query matches at least one result.</returns>
        private static bool HasProperty(Dtf.Database db, string property)
        {
            try
            {
                return 0 < db.ExecuteQuery(PropertyQuery(property)).Count;
            }
            catch (Dtf.InstallerException)
            {
            }

            return false;
        }

        private static string PropertyQuery(string property)
        {
            // quick sanity check that we'll be creating a valid query...
            // TODO: Are there any other special characters we should be looking for?
            Debug.Assert(!property.Contains("'"));

            return String.Format(CultureInfo.InvariantCulture, ProcessMsiPackageCommand.PropertySqlFormat, property);
        }
#endif
    }
}
