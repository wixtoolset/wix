// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml;

    /// <summary>
    /// Container class for an intermediate object.
    /// </summary>
    public sealed class Intermediate
    {
        public const string XmlNamespaceUri = "http://wixtoolset.org/schemas/v4/wixobj";
        private static readonly Version CurrentVersion = new Version("4.0.0.0");

        private string id;
        private List<Section> sections;

        /// <summary>
        /// Instantiate a new Intermediate.
        /// </summary>
        public Intermediate()
        {
            this.id = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=').Replace('+', '.').Replace('/', '_');
            this.sections = new List<Section>();
        }

        /// <summary>
        /// Get the sections contained in this intermediate.
        /// </summary>
        /// <value>Sections contained in this intermediate.</value>
        public IEnumerable<Section> Sections { get { return this.sections; } }

        /// <summary>
        /// Adds a section to the intermediate.
        /// </summary>
        /// <param name="section">Section to add to the intermediate.</param>
        public void AddSection(Section section)
        {
            section.IntermediateId = this.id;
            this.sections.Add(section);
        }

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

            using (FileStream stream = File.Create(path))
            using (FileStructure fs = FileStructure.Create(stream, FileFormat.Wixobj, null))
            using (XmlWriter writer = XmlWriter.Create(fs.GetDataStream()))
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
    }
}
