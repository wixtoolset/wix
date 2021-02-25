// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.Burn.Bind
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Dtf.WindowsInstaller;

    internal class ProcessBundleSoftwareTagsCommand
    {
        public ProcessBundleSoftwareTagsCommand(IntermediateSection section, IEnumerable<WixBundleTagSymbol> softwareTags)
        {
            this.Section = section;
            this.SoftwareTags = softwareTags;
        }

        private IntermediateSection Section { get; }

        private IEnumerable<WixBundleTagSymbol> SoftwareTags { get; }

        public void Execute()
        {
            var bundleInfo = this.Section.Symbols.OfType<WixBundleSymbol>().FirstOrDefault();
            var bundleId = NormalizeGuid(bundleInfo.BundleId);
            var upgradeCode = NormalizeGuid(bundleInfo.UpgradeCode);

            var uniqueId = String.Concat("wix:bundle/", bundleId);
            var persistentId = String.Concat("wix:bundle.upgrade/", upgradeCode);

            // Try to collect all the software id tags from all the child packages.
            var containedTags = CollectPackageTags(this.Section);

            foreach (var bundleTag in this.SoftwareTags)
            {
                using (var ms = new MemoryStream())
                {
                    CreateTagFile(ms, uniqueId, bundleInfo.Name, bundleInfo.Version, bundleTag.Regid, bundleInfo.Manufacturer, persistentId, containedTags);
                    bundleTag.Xml = Encoding.UTF8.GetString(ms.ToArray());
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

        private static IEnumerable<SoftwareTag> CollectPackageTags(IntermediateSection section)
        {
            var tags = new List<SoftwareTag>();

            var msiPackages = section.Symbols.OfType<WixBundlePackageSymbol>().Where(s => s.Type == WixBundlePackageType.Msi).ToList();
            if (msiPackages.Any())
            {
                var payloadSymbolsById = section.Symbols.OfType<WixBundlePayloadSymbol>().ToDictionary(s => s.Id.Id);

                foreach (var msiPackage in msiPackages)
                {
                    var payload = payloadSymbolsById[msiPackage.PayloadRef];

                    using (var db = new Database(payload.SourceFile.Path))
                    {
                        using (var view = db.OpenView("SELECT `Regid`, `TagId` FROM `SoftwareIdentificationTag`"))
                        {
                            view.Execute();
                            while (true)
                            {
                                using (var record = view.Fetch())
                                {
                                    if (null == record)
                                    {
                                        break;
                                    }

                                    tags.Add(new SoftwareTag { Regid = record.GetString(1), Id = record.GetString(2) });
                                }
                            }
                        }
                    }
                }
            }

            return tags;
        }

        private static void CreateTagFile(Stream stream, string uniqueId, string name, string version, string regid, string manufacturer, string persistendId, IEnumerable<SoftwareTag> containedTags)
        {
            var versionScheme = Version.TryParse(version, out _) ? "multipartnumeric" : "alphanumeric";

            using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true}))
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

                foreach (var containedTag in containedTags)
                {
                    writer.WriteStartElement("Link");
                    writer.WriteAttributeString("rel", "component");
                    writer.WriteAttributeString("href", String.Concat("swid:", containedTag.Id));
                    writer.WriteEndElement(); // </Link>
                }

                writer.WriteEndElement(); // </SoftwareIdentity>
            }
        }

        private class SoftwareTag
        {
            public string Regid { get; set; }

            public string Id { get; set; }
        }
    }
}
