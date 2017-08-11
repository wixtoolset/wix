// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;

    /// <summary>
    /// Object that represents a library file.
    /// </summary>
    public sealed class Library
    {
        public const string XmlNamespaceUri = "http://wixtoolset.org/schemas/v4/wixlib";
        private static readonly Version CurrentVersion = new Version("4.0.0.0");

        private string id;
        private Dictionary<string, Localization> localizations;
        private List<Section> sections;

        /// <summary>
        /// Instantiates a new empty library which is only useful from static creating methods.
        /// </summary>
        private Library()
        {
            this.localizations = new Dictionary<string, Localization>();
            this.sections = new List<Section>();
        }

        /// <summary>
        /// Instantiate a new library populated with sections.
        /// </summary>
        /// <param name="sections">Sections to add to the library.</param>
        public Library(IEnumerable<Section> sections)
        {
            this.localizations = new Dictionary<string, Localization>();
            this.sections = new List<Section>(sections);

            this.id = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=').Replace('+', '.').Replace('/', '_');
            foreach (Section section in this.sections)
            {
                section.LibraryId = this.id;
            }
        }

        /// <summary>
        /// Get the sections contained in this library.
        /// </summary>
        /// <value>Sections contained in this library.</value>
        public IEnumerable<Section> Sections { get { return this.sections; } }

        /// <summary>
        /// Add a localization file to this library.
        /// </summary>
        /// <param name="localization">The localization file to add.</param>
        public void AddLocalization(Localization localization)
        {
            Localization existingCulture;
            if (this.localizations.TryGetValue(localization.Culture, out existingCulture))
            {
                existingCulture.Merge(localization);
            }
            else
            {
                this.localizations.Add(localization.Culture, localization);
            }
        }

        /// <summary>
        /// Gets localization files from this library that match the cultures passed in, in the order of the array of cultures.
        /// </summary>
        /// <param name="cultures">The list of cultures to get localizations for.</param>
        /// <returns>All localizations contained in this library that match the set of cultures provided, in the same order.</returns>
        public IEnumerable<Localization> GetLocalizations(string[] cultures)
        {
            foreach (string culture in cultures ?? new string[0])
            {
                Localization localization;
                if (this.localizations.TryGetValue(culture, out localization))
                {
                    yield return localization;
                }
            }
        }

        /// <summary>
        /// Loads a library from a path on disk.
        /// </summary>
        /// <param name="path">Path to library file saved on disk.</param>
        /// <param name="tableDefinitions">Collection containing TableDefinitions to use when reconstituting the intermediates.</param>
        /// <param name="suppressVersionCheck">Suppresses wix.dll version mismatch check.</param>
        /// <returns>Returns the loaded library.</returns>
        public static Library Load(string path, TableDefinitionCollection tableDefinitions, bool suppressVersionCheck)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                return Load(stream, new Uri(Path.GetFullPath(path)), tableDefinitions, suppressVersionCheck);
            }
        }

        /// <summary>
        /// Loads a library from a stream.
        /// </summary>
        /// <param name="stream">Stream containing the library file.</param>
        /// <param name="uri">Uri for finding this stream.</param>
        /// <param name="tableDefinitions">Collection containing TableDefinitions to use when reconstituting the intermediates.</param>
        /// <param name="suppressVersionCheck">Suppresses wix.dll version mismatch check.</param>
        /// <returns>Returns the loaded library.</returns>
        public static Library Load(Stream stream, Uri uri, TableDefinitionCollection tableDefinitions, bool suppressVersionCheck)
        {
            using (FileStructure fs = FileStructure.Read(stream))
            {
                if (FileFormat.Wixlib != fs.FileFormat)
                {
                    throw new WixUnexpectedFileFormatException(uri.LocalPath, FileFormat.Wixlib, fs.FileFormat);
                }

                using (XmlReader reader = XmlReader.Create(fs.GetDataStream(), null, uri.AbsoluteUri))
                {
                    try
                    {
                        reader.MoveToContent();
                        return Library.Read(reader, tableDefinitions, suppressVersionCheck);
                    }
                    catch (XmlException xe)
                    {
                        throw new WixCorruptFileException(uri.LocalPath, fs.FileFormat, xe);
                    }
                }
            }
        }

        /// <summary>
        /// Saves a library to a path on disk.
        /// </summary>
        /// <param name="path">Path to save library file to on disk.</param>
        /// <param name="resolver">The WiX path resolver.</param>
        public void Save(string path, ILibraryBinaryFileResolver resolver)
        {
            List<string> embedFilePaths = new List<string>();

            // Resolve paths to files that are to be embedded in the library.
            if (null != resolver)
            {
                foreach (Table table in this.sections.SelectMany(s => s.Tables))
                {
                    foreach (Row row in table.Rows)
                    {
                        foreach (ObjectField objectField in row.Fields.Where(f => f is ObjectField))
                        {
                            if (null != objectField.Data)
                            {
                                string file = resolver.Resolve(row.SourceLineNumbers, table.Name, (string)objectField.Data);
                                if (!String.IsNullOrEmpty(file))
                                {
                                    // File was successfully resolved so track the embedded index as the embedded file index.
                                    objectField.EmbeddedFileIndex = embedFilePaths.Count;
                                    embedFilePaths.Add(file);
                                }
                                else
                                {
                                    Messaging.Instance.OnMessage(WixDataErrors.FileNotFound(row.SourceLineNumbers, (string)objectField.Data, table.Name));
                                }
                            }
                            else // clear out embedded file id in case there was one there before.
                            {
                                objectField.EmbeddedFileIndex = null;
                            }
                        }
                    }
                }
            }

            // Do not save the library if errors were found while resolving object paths.
            if (Messaging.Instance.EncounteredError)
            {
                return;
            }

            // Ensure the location to output the library exists and write it out.
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));

            using (FileStream stream = File.Create(path))
            using (FileStructure fs = FileStructure.Create(stream, FileFormat.Wixlib, embedFilePaths))
            using (XmlWriter writer = XmlWriter.Create(fs.GetDataStream()))
            {
                writer.WriteStartDocument();

                this.Write(writer);

                writer.WriteEndDocument();
            }
        }

        /// <summary>
        /// Parse the root library element.
        /// </summary>
        /// <param name="reader">XmlReader with library persisted as Xml.</param>
        /// <param name="tableDefinitions">Collection containing TableDefinitions to use when reconstituting the intermediates.</param>
        /// <param name="suppressVersionCheck">Suppresses check for wix.dll version mismatch.</param>
        /// <returns>The parsed Library.</returns>
        private static Library Read(XmlReader reader, TableDefinitionCollection tableDefinitions, bool suppressVersionCheck)
        {
            if (!reader.LocalName.Equals("wixLibrary"))
            {
                throw new XmlException();
            }

            bool empty = reader.IsEmptyElement;
            Library library = new Library();
            Version version = null;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.LocalName)
                {
                    case "version":
                        version = new Version(reader.Value);
                        break;
                    case "id":
                        library.id = reader.Value;
                        break;
                }
            }

            if (!suppressVersionCheck && null != version && !Library.CurrentVersion.Equals(version))
            {
                throw new WixException(WixDataErrors.VersionMismatch(SourceLineNumber.CreateFromUri(reader.BaseURI), "library", version.ToString(), Library.CurrentVersion.ToString()));
            }

            if (!empty)
            {
                bool done = false;

                while (!done && (XmlNodeType.Element == reader.NodeType || reader.Read()))
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.LocalName)
                            {
                                case "localization":
                                    Localization localization = Localization.Read(reader, tableDefinitions);
                                    library.localizations.Add(localization.Culture, localization);
                                    break;
                                case "section":
                                    Section section = Section.Read(reader, tableDefinitions);
                                    section.LibraryId = library.id;
                                    library.sections.Add(section);
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

            return library;
        }

        /// <summary>
        /// Persists a library in an XML format.
        /// </summary>
        /// <param name="writer">XmlWriter where the library should persist itself as XML.</param>
        private void Write(XmlWriter writer)
        {
            writer.WriteStartElement("wixLibrary", XmlNamespaceUri);

            writer.WriteAttributeString("version", CurrentVersion.ToString());

            writer.WriteAttributeString("id", this.id);

            foreach (Localization localization in this.localizations.Values)
            {
                localization.Write(writer);
            }

            foreach (Section section in this.sections)
            {
                section.Write(writer);
            }

            writer.WriteEndElement();
        }
    }
}
