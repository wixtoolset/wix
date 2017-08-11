// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System.Xml;

    /// <summary>
    /// Substorage inside an output.
    /// </summary>
    public sealed class SubStorage
    {
        /// <summary>
        /// Instantiate a new substorage.
        /// </summary>
        /// <param name="name">The substorage name.</param>
        /// <param name="data">The substorage data.</param>
        public SubStorage(string name, Output data)
        {
            this.Name = name;
            this.Data = data;
        }

        /// <summary>
        /// Gets the substorage name.
        /// </summary>
        /// <value>The substorage name.</value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the substorage data.
        /// </summary>
        /// <value>The substorage data.</value>
        public Output Data { get; private set; }

        /// <summary>
        /// Creates a SubStorage from the XmlReader.
        /// </summary>
        /// <param name="reader">Reader to get data from.</param>
        /// <returns>New SubStorage object.</returns>
        internal static SubStorage Read(XmlReader reader)
        {
            if (!reader.LocalName.Equals("subStorage" == reader.LocalName))
            {
                throw new XmlException();
            }

            Output data = null;
            bool empty = reader.IsEmptyElement;
            string name = null;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.LocalName)
                {
                    case "name":
                        name = reader.Value;
                        break;
                }
            }

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
                                case "wixOutput":
                                    data = Output.Read(reader, true);
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

            return new SubStorage(name, data);
        }

        /// <summary>
        /// Persists a SubStorage in an XML format.
        /// </summary>
        /// <param name="writer">XmlWriter where the SubStorage should persist itself as XML.</param>
        internal void Write(XmlWriter writer)
        {
            writer.WriteStartElement("subStorage", Output.XmlNamespaceUri);

            writer.WriteAttributeString("name", this.Name);

            this.Data.Write(writer);

            writer.WriteEndElement();
        }
    }
}
