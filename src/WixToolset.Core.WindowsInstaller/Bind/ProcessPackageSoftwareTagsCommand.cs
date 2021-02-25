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

    internal class ProcessPackageSoftwareTagsCommand
    {
        public ProcessPackageSoftwareTagsCommand(IntermediateSection section, IEnumerable<WixProductTagSymbol> softwareTags, string intermediateFolder)
        {
            this.Section = section;
            this.SoftwareTags = softwareTags;
            this.IntermediateFolder = intermediateFolder;
        }

        private string IntermediateFolder { get; }

        private IntermediateSection Section { get; }

        private IEnumerable<WixProductTagSymbol> SoftwareTags { get; }

        public void Execute()
        {
            string productName = null;
            string productVersion = null;
            string manufacturer = null;
            string upgradeCode = null;

            var summaryInfo = this.Section.Symbols.OfType<SummaryInformationSymbol>().FirstOrDefault(s => s.PropertyId == SummaryInformationType.PackageCode);
            var packageCode = NormalizeGuid(summaryInfo?.Value);

            foreach (var property in this.Section.Symbols.OfType<PropertySymbol>())
            {
                switch (property.Id.Id)
                {
                    case "ProductName":
                        productName = property.Value;
                        break;
                    case "ProductVersion":
                        productVersion = property.Value;
                        break;
                    case "Manufacturer":
                        manufacturer = property.Value;
                        break;
                    case "UpgradeCode":
                        upgradeCode = NormalizeGuid(property.Value);
                        break;
                }
            }

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

                    using (var fs = new FileStream(fileSymbol.Source.Path, FileMode.Create))
                    {
                        CreateTagFile(fs, uniqueId, productName, productVersion, tagRow.Regid, manufacturer, persistentId);
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
            var versionScheme = Version.TryParse(version, out _) ? "multipartnumeric" : "alphanumeric";

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
