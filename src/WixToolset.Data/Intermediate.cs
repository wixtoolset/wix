// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using SimpleJson;

    /// <summary>
    /// Container class for an intermediate object.
    /// </summary>
    public sealed class Intermediate
    {
        public const string XmlNamespaceUri = "http://wixtoolset.org/schemas/v4/wixobj";
        private static readonly Version CurrentVersion = new Version("4.0.0.0");

        private Dictionary<string, Localization> localizationsByCulture;

        /// <summary>
        /// Instantiate a new Intermediate.
        /// </summary>
        public Intermediate()
        {
            this.Id = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=').Replace('+', '.').Replace('/', '_');
            this.EmbedFilePaths = new List<string>();
            this.localizationsByCulture = new Dictionary<string, Localization>(StringComparer.OrdinalIgnoreCase);
            this.Sections = new List<IntermediateSection>();
        }

        public Intermediate(string id, IEnumerable<IntermediateSection> sections, IDictionary<string, Localization> localizationsByCulture, IEnumerable<string> embedFilePaths)
        {
            this.Id = id;
            this.EmbedFilePaths = (embedFilePaths != null) ? new List<string>(embedFilePaths) : new List<string>();
            this.localizationsByCulture = (localizationsByCulture != null) ? new Dictionary<string, Localization>(localizationsByCulture, StringComparer.OrdinalIgnoreCase) : new Dictionary<string, Localization>(StringComparer.OrdinalIgnoreCase);
            this.Sections = (sections != null) ? new List<IntermediateSection>(sections) : new List<IntermediateSection>();
        }

        /// <summary>
        /// Get the id for the intermediate.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Get the embed file paths in this intermediate.
        /// </summary>
        public IList<string> EmbedFilePaths { get; }

        /// <summary>
        /// Get the localizations contained in this intermediate.
        /// </summary>
        public IEnumerable<Localization> Localizations => this.localizationsByCulture.Values;

        /// <summary>
        /// Get the sections contained in this intermediate.
        /// </summary>
        public IList<IntermediateSection> Sections { get; }

        /// <summary>
        /// Adds a localization to the intermediate.
        /// </summary>
        /// <param name="localization">Localization to add to the intermediate.</param>
        public void AddLocalization(Localization localization)
        {
            if (this.localizationsByCulture.TryGetValue(localization.Culture, out var existingCulture))
            {
                existingCulture.Merge(localization);
            }
            else
            {
                this.localizationsByCulture.Add(localization.Culture, localization);
            }
        }

        /// <summary>
        /// Gets localization files from this library that match the cultures passed in, in the order of the array of cultures.
        /// </summary>
        /// <param name="cultures">The list of cultures to get localizations for.</param>
        /// <returns>All localizations contained in this library that match the set of cultures provided, in the same order.</returns>
        public IEnumerable<Localization> GetLocalizationsForCultures(IEnumerable<string> cultures)
        {
            foreach (string culture in cultures ?? Array.Empty<string>())
            {
                if (this.localizationsByCulture.TryGetValue(culture, out var localization))
                {
                    yield return localization;
                }
            }
        }

        /// <summary>
        /// Loads an intermediate from a path on disk.
        /// </summary>
        /// <param name="path">Path to intermediate file saved on disk.</param>
        /// <param name="suppressVersionCheck">Suppress checking for wix.dll version mismatches.</param>
        /// <returns>Returns the loaded intermediate.</returns>
        public static Intermediate Load(string path, bool suppressVersionCheck = false)
        {
            var creator = new SimpleTupleDefinitionCreator();
            return Intermediate.Load(path, creator, suppressVersionCheck);
        }

        /// <summary>
        /// Loads an intermediate from a path on disk.
        /// </summary>
        /// <param name="path">Path to intermediate file saved on disk.</param>
        /// <param name="creator">ITupleDefinitionCreator to use when reconstituting the intermediate.</param>
        /// <param name="suppressVersionCheck">Suppress checking for wix.dll version mismatches.</param>
        /// <returns>Returns the loaded intermediate.</returns>
        public static Intermediate Load(string path, ITupleDefinitionCreator creator, bool suppressVersionCheck = false)
        {
            JsonObject jsonObject;

            using (FileStream stream = File.OpenRead(path))
            using (FileStructure fs = FileStructure.Read(stream))
            {
                if (FileFormat.WixIR != fs.FileFormat)
                {
                    throw new WixUnexpectedFileFormatException(path, FileFormat.WixIR, fs.FileFormat);
                }

                var json = fs.GetData();
                jsonObject = SimpleJson.DeserializeObject(json) as JsonObject;
            }

            if (!suppressVersionCheck)
            {
                var versionJson = jsonObject.GetValueOrDefault<string>("version");

                if (!Version.TryParse(versionJson, out var version) || !Intermediate.CurrentVersion.Equals(version))
                {
                    throw new WixException(WixDataErrors.VersionMismatch(SourceLineNumber.CreateFromUri(path), "intermediate", versionJson, Intermediate.CurrentVersion.ToString()));
                }
            }

            var id = jsonObject.GetValueOrDefault<string>("id");

            var sections = new List<IntermediateSection>();

            var sectionsJson = jsonObject.GetValueOrDefault<JsonArray>("sections");
            foreach (JsonObject sectionJson in sectionsJson)
            {
                var section = IntermediateSection.Deserialize(creator, sectionJson);
                sections.Add(section);
            }

            var localizations = new Dictionary<string, Localization>(StringComparer.OrdinalIgnoreCase);

            //var localizationsJson = jsonObject.GetValueOrDefault<JsonArray>("localizations") ?? new JsonArray();
            //foreach (JsonObject localizationJson in localizationsJson)
            //{
            //    var localization = Localization.Deserialize(localizationJson);
            //    localizations.Add(localization.Culture, localization);
            //}

            return new Intermediate(id, sections, localizations, null);
        }

        /// <summary>
        /// Saves an intermediate to a path on disk.
        /// </summary>
        /// <param name="path">Path to save intermediate file to disk.</param>
        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));

            using (var stream = File.Create(path))
            using (var fs = FileStructure.Create(stream, FileFormat.WixIR, this.EmbedFilePaths))
            using (var writer = new StreamWriter(fs.GetDataStream()))
            {
                var jsonObject = new JsonObject
                {
                    { "id", this.Id },
                    { "version", Intermediate.CurrentVersion.ToString() }
                };

                var sectionsJson = new JsonArray(this.Sections.Count);
                foreach (var section in this.Sections)
                {
                    var sectionJson = section.Serialize();
                    sectionsJson.Add(sectionJson);
                }

                jsonObject.Add("sections", sectionsJson);

                //if (this.Localizations.Any())
                //{
                //    var localizationsJson = new JsonArray();
                //    foreach (var localization in this.Localizations)
                //    {
                //        var localizationJson = localization.Serialize();
                //        localizationsJson.Add(localizationJson);
                //    }

                //    jsonObject.Add("localizations", localizationsJson);
                //}

                var json = SimpleJson.SerializeObject(jsonObject);
                writer.Write(json);
            }
        }

#if false
        /// <summary>
        /// Loads an intermediate from a path on disk.
        /// </summary>
        /// <param name="path">Path to intermediate file saved on disk.</param>
        /// <param name="tableDefinitions">Collection containing TableDefinitions to use when reconstituting the intermediate.</param>
        /// <param name="suppressVersionCheck">Suppress checking for wix.dll version mismatches.</param>
        /// <returns>Returns the loaded intermediate.</returns>
        public static Intermediate Load(string path, TableDefinitionCollection tableDefinitions, bool suppressVersionCheck)
        {
            using (FileStream stream = File.OpenRead(path))
            using (FileStructure fs = FileStructure.Read(stream))
            {
                if (FileFormat.Wixobj != fs.FileFormat)
                {
                    throw new WixUnexpectedFileFormatException(path, FileFormat.Wixobj, fs.FileFormat);
                }

                Uri uri = new Uri(Path.GetFullPath(path));
                using (XmlReader reader = XmlReader.Create(fs.GetDataStream(), null, uri.AbsoluteUri))
                {
                    try
                    {
                        reader.MoveToContent();
                        return Intermediate.Read(reader, tableDefinitions, suppressVersionCheck);
                    }
                    catch (XmlException xe)
                    {
                        throw new WixCorruptFileException(path, fs.FileFormat, xe);
                    }
                }
            }
        }

        /// <summary>
        /// Saves an intermediate to a path on disk.
        /// </summary>
        /// <param name="path">Path to save intermediate file to disk.</param>
        public void Save(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));

            using (var stream = File.Create(path))
            using (var fs = FileStructure.Create(stream, FileFormat.Wixobj, null))
            using (var writer = XmlWriter.Create(fs.GetDataStream()))
            {
                writer.WriteStartDocument();
                this.Write(writer);
                writer.WriteEndDocument();
            }
        }

        /// <summary>
        /// Parse an intermediate from an XML format.
        /// </summary>
        /// <param name="reader">XmlReader where the intermediate is persisted.</param>
        /// <param name="tableDefinitions">TableDefinitions to use in the intermediate.</param>
        /// <param name="suppressVersionCheck">Suppress checking for wix.dll version mismatch.</param>
        /// <returns>The parsed Intermediate.</returns>
        private static Intermediate Read(XmlReader reader, TableDefinitionCollection tableDefinitions, bool suppressVersionCheck)
        {
            if ("wixObject" != reader.LocalName)
            {
                throw new XmlException();
            }

            bool empty = reader.IsEmptyElement;
            Version objVersion = null;
            string id = null;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.LocalName)
                {
                    case "version":
                        objVersion = new Version(reader.Value);
                        break;
                    case "id":
                        id = reader.Value;
                        break;
                }
            }

            if (!suppressVersionCheck && null != objVersion && !Intermediate.CurrentVersion.Equals(objVersion))
            {
                throw new WixException(WixDataErrors.VersionMismatch(SourceLineNumber.CreateFromUri(reader.BaseURI), "object", objVersion.ToString(), Intermediate.CurrentVersion.ToString()));
            }

            Intermediate intermediate = new Intermediate();
            intermediate.id = id;

            if (!empty)
            {
                bool done = false;

                while (!done && reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.LocalName)
                            {
                                case "section":
                                    intermediate.AddSection(Section.Read(reader, tableDefinitions));
                                    break;
                                default:
                                    throw new XmlException();
                            }
                            break;
                        case XmlNodeType.EndElement:
                            done = true;
                            break;
                    }
                }

                if (!done)
                {
                    throw new XmlException();
                }
            }

            return intermediate;
        }

        /// <summary>
        /// Persists an intermediate in an XML format.
        /// </summary>
        /// <param name="writer">XmlWriter where the Intermediate should persist itself as XML.</param>
        private void Write(XmlWriter writer)
        {
            writer.WriteStartElement("wixObject", XmlNamespaceUri);

            writer.WriteAttributeString("version", Intermediate.CurrentVersion.ToString());

            writer.WriteAttributeString("id", this.id);

            foreach (Section section in this.Sections)
            {
                section.Write(writer);
            }

            writer.WriteEndElement();
        }
#endif
    }
}
