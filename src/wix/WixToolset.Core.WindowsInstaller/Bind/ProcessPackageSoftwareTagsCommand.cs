// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class ProcessPackageSoftwareTagsCommand
    {
        public ProcessPackageSoftwareTagsCommand(IntermediateSection section, IBackendHelper backendHelper, IEnumerable<WixPackageTagSymbol> softwareTags, string intermediateFolder)
        {
            this.Section = section;
            this.BackendHelper = backendHelper;
            this.SoftwareTags = softwareTags;
            this.IntermediateFolder = intermediateFolder;
        }

        private string IntermediateFolder { get; }

        private IntermediateSection Section { get; }

        private IBackendHelper BackendHelper { get; }

        private IEnumerable<WixPackageTagSymbol> SoftwareTags { get; }

        public IReadOnlyCollection<ITrackedFile> TrackedFiles { get; private set; }

        public void Execute()
        {
            var trackedFiles = new List<ITrackedFile>();

            var summaryInfo = this.Section.Symbols.OfType<SummaryInformationSymbol>().FirstOrDefault(s => s.PropertyId == SummaryInformationType.PackageCode);
            var packageCode = NormalizeGuid(summaryInfo?.Value);

            var packageSymbol = this.Section.Symbols.OfType<WixPackageSymbol>().First();
            var upgradeCode = NormalizeGuid(packageSymbol.UpgradeCode);

            var fileSymbolsById = this.Section.Symbols.OfType<FileSymbol>().Where(f => f.Id != null).ToDictionary(f => f.Id.Id);

            var workingFolder = Path.Combine(this.IntermediateFolder, "_swidtag");

            Directory.CreateDirectory(workingFolder);

            foreach (var tagRow in this.SoftwareTags)
            {
                if (fileSymbolsById.TryGetValue(tagRow.FileRef, out var fileSymbol))
                {
                    var uniqueId = String.Concat("msi:package/", packageCode);
                    var persistentId = String.IsNullOrEmpty(upgradeCode) ? null : String.Concat("msi:upgrade/", upgradeCode);

                    // Write the tag file.
                    fileSymbol.Source = new IntermediateFieldPathValue { Path = Path.Combine(workingFolder, fileSymbol.Name) };

                    trackedFiles.Add(this.BackendHelper.TrackFile(fileSymbol.Source.Path, TrackedFileType.Intermediate, tagRow.SourceLineNumbers));

                    using (var fs = new FileStream(fileSymbol.Source.Path, FileMode.Create))
                    {
                        CreateTagFile(fs, uniqueId, packageSymbol.Name, packageSymbol.Version, tagRow.Regid, packageSymbol.Manufacturer, persistentId);
                    }

                    // Ensure the matching "SoftwareIdentificationTag" row exists and
                    // is populated correctly.
                    this.Section.AddSymbol(new SoftwareIdentificationTagSymbol(tagRow.SourceLineNumbers, tagRow.Id)
                    {
                        FileRef = fileSymbol.Id.Id,
                        Regid = tagRow.Regid,
                        TagId = uniqueId,
                        PersistentId = persistentId
                    });
                }
            }

            this.TrackedFiles = trackedFiles;
        }

        private static string NormalizeGuid(string guidString)
        {
            if (Guid.TryParse(guidString, out var guid))
            {
                return guid.ToString("D").ToUpperInvariant();
            }

            return guidString;
        }

        private static void CreateTagFile(Stream stream, string uniqueId, string name, string version, string regid, string manufacturer, string persistendId)
        {
            var versionScheme = "alphanumeric";

            if (WixVersion.TryParse(version, out var parsedVersion))
            {
                if (parsedVersion.Prefix.HasValue)
                {
                    version = version.Substring(1);
                }

                if (Version.TryParse(version, out _))
                {
                    versionScheme = "multipartnumeric";
                }
                else if (!parsedVersion.HasRevision)
                {
                    versionScheme = "semver";
                }
                else
                {
                    versionScheme = "multipartnumeric+suffix";
                }
            }

            using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true }))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("SoftwareIdentity", "http://standards.iso.org/iso/19770/-2/2015/schema.xsd");
                writer.WriteAttributeString("tagId", uniqueId);
                writer.WriteAttributeString("name", name);
                writer.WriteAttributeString("version", version);
                writer.WriteAttributeString("versionScheme", versionScheme);

                writer.WriteStartElement("Entity");
                writer.WriteAttributeString("name", manufacturer);
                writer.WriteAttributeString("regid", regid);
                writer.WriteAttributeString("role", "softwareCreator tagCreator");
                writer.WriteEndElement(); // </Entity>

                if (!String.IsNullOrEmpty(persistendId))
                {
                    writer.WriteStartElement("Meta");
                    writer.WriteAttributeString("persistentId", persistendId);
                    writer.WriteEndElement(); // </Meta>
                }

                writer.WriteEndElement(); // </SoftwareIdentity>
            }
        }
    }
}
