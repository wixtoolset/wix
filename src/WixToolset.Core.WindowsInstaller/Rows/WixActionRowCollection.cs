// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Rows
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    using System.Xml;
    using WixToolset.Data.Tuples;
    using WixToolset.Data.WindowsInstaller.Rows;

    /// <summary>
    /// A collection of action rows sorted by their sequence table and action name.
    /// </summary>
    // TODO: Remove this
    internal sealed class WixActionRowCollection : ICollection
    {
        private readonly SortedList collection;

        /// <summary>
        /// Creates a new action table object.
        /// </summary>
        public WixActionRowCollection()
        {
            this.collection = new SortedList();
        }

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        /// <value>Number of items in collection.</value>
        public int Count
        {
            get { return this.collection.Count; }
        }

        /// <summary>
        /// Gets if the collection has been synchronized.
        /// </summary>
        /// <value>True if the collection has been synchronized.</value>
        public bool IsSynchronized
        {
            get { return this.collection.IsSynchronized; }
        }

        /// <summary>
        /// Gets the object used to synchronize the collection.
        /// </summary>
        /// <value>Oject used the synchronize the collection.</value>
        public object SyncRoot
        {
            get { return this; }
        }

        /// <summary>
        /// Get an ActionRow by its sequence table and action name.
        /// </summary>
        /// <param name="sequenceTable">The sequence table of the ActionRow.</param>
        /// <param name="action">The action name of the ActionRow.</param>
        public WixActionRow this[SequenceTable sequenceTable, string action]
        {
            get { return (WixActionRow)this.collection[GetKey(sequenceTable, action)]; }
        }

        /// <summary>
        /// Add an ActionRow to the collection.
        /// </summary>
        /// <param name="actionRow">The ActionRow to add.</param>
        /// <param name="overwrite">true to overwrite an existing ActionRow; false otherwise.</param>
        public void Add(WixActionRow actionRow, bool overwrite)
        {
            string key = GetKey(actionRow.SequenceTable, actionRow.Action);

            if (overwrite)
            {
                this.collection[key] = actionRow;
            }
            else
            {
                this.collection.Add(key, actionRow);
            }
        }

        /// <summary>
        /// Add an ActionRow to the collection.
        /// </summary>
        /// <param name="actionRow">The ActionRow to add.</param>
        public void Add(WixActionRow actionRow)
        {
            this.Add(actionRow, false);
        }

        /// <summary>
        /// Determines if the collection contains an ActionRow with a specific sequence table and name.
        /// </summary>
        /// <param name="sequenceTable">The sequence table of the ActionRow.</param>
        /// <param name="action">The action name of the ActionRow.</param>
        /// <returns>true if the ActionRow was found; false otherwise.</returns>
        public bool Contains(SequenceTable sequenceTable, string action)
        {
            return this.collection.Contains(GetKey(sequenceTable, action));
        }

        /// <summary>
        /// Copies the collection into an array.
        /// </summary>
        /// <param name="array">Array to copy the collection into.</param>
        /// <param name="index">Index to start copying from.</param>
        public void CopyTo(System.Array array, int index)
        {
            this.collection.Values.CopyTo(array, index);
        }

        /// <summary>
        /// Gets the enumerator for the collection.
        /// </summary>
        /// <returns>The enumerator for the collection.</returns>
        public IEnumerator GetEnumerator()
        {
            return this.collection.Values.GetEnumerator();
        }

        /// <summary>
        /// Remove an ActionRow from the collection.
        /// </summary>
        /// <param name="sequenceTable">The sequence table of the ActionRow.</param>
        /// <param name="action">The action name of the ActionRow.</param>
        public void Remove(SequenceTable sequenceTable, string action)
        {
            this.collection.Remove(GetKey(sequenceTable, action));
        }

        /// <summary>
        /// Load an action table from an XmlReader.
        /// </summary>
        /// <param name="reader">Reader to get data from.</param>
        /// <returns>The ActionRowCollection represented by the xml.</returns>
        internal static WixActionRowCollection Load(XmlReader reader)
        {
            reader.MoveToContent();

            return Parse(reader);
        }

        /// <summary>
        /// Creates a new action table object and populates it from an Xml reader.
        /// </summary>
        /// <param name="reader">Reader to get data from.</param>
        /// <returns>The parsed ActionTable.</returns>
        private static WixActionRowCollection Parse(XmlReader reader)
        {
            if (!reader.LocalName.Equals("actions"))
            {
                throw new XmlException();
            }

            WixActionRowCollection actionRows = new WixActionRowCollection();
            bool empty = reader.IsEmptyElement;

            while (reader.MoveToNextAttribute())
            {
            }

            if (!empty)
            {
                bool done = false;

                // loop through all the fields in a row
                while (!done && reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                        switch (reader.LocalName)
                        {
                            case "action":
                                WixActionRow[] parsedActionRows = ParseActions(reader);

                                foreach (WixActionRow actionRow in parsedActionRows)
                                {
                                    actionRows.Add(actionRow);
                                }
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

            return actionRows;
        }

        /// <summary>
        /// Get the key for storing an ActionRow.
        /// </summary>
        /// <param name="sequenceTable">The sequence table of the ActionRow.</param>
        /// <param name="action">The action name of the ActionRow.</param>
        /// <returns>The string key.</returns>
        private static string GetKey(SequenceTable sequenceTable, string action)
        {
            return GetKey(sequenceTable.ToString(), action);
        }

        /// <summary>
        /// Get the key for storing an ActionRow.
        /// </summary>
        /// <param name="sequenceTable">The sequence table of the ActionRow.</param>
        /// <param name="action">The action name of the ActionRow.</param>
        /// <returns>The string key.</returns>
        private static string GetKey(string sequenceTable, string action)
        {
            return String.Concat(sequenceTable, '/', action);
        }

        /// <summary>
        /// Parses ActionRows from the Xml reader.
        /// </summary>
        /// <param name="reader">Xml reader that contains serialized ActionRows.</param>
        /// <returns>The parsed ActionRows.</returns>
        internal static WixActionRow[] ParseActions(XmlReader reader)
        {
            Debug.Assert("action" == reader.LocalName);

            string id = null;
            string condition = null;
            bool empty = reader.IsEmptyElement;
            int sequence = Int32.MinValue;
            int sequenceCount = 0;
            SequenceTable[] sequenceTables = new SequenceTable[Enum.GetValues(typeof(SequenceTable)).Length];

            while (reader.MoveToNextAttribute())
            {
                switch (reader.Name)
                {
                    case "name":
                        id = reader.Value;
                        break;
                    case "AdminExecuteSequence":
                        if (reader.Value.Equals("yes"))
                        {
                            sequenceTables[sequenceCount] = SequenceTable.AdminExecuteSequence;
                            ++sequenceCount;
                        }
                        break;
                    case "AdminUISequence":
                        if (reader.Value.Equals("yes"))
                        {
                            sequenceTables[sequenceCount] = SequenceTable.AdminUISequence;
                            ++sequenceCount;
                        }
                        break;
                    case "AdvtExecuteSequence":
                        if (reader.Value.Equals("yes"))
                        {
                            sequenceTables[sequenceCount] = SequenceTable.AdvertiseExecuteSequence;
                            ++sequenceCount;
                        }
                        break;
                    case "condition":
                        condition = reader.Value;
                        break;
                    case "InstallExecuteSequence":
                        if (reader.Value.Equals("yes"))
                        {
                            sequenceTables[sequenceCount] = SequenceTable.InstallExecuteSequence;
                            ++sequenceCount;
                        }
                        break;
                    case "InstallUISequence":
                        if (reader.Value.Equals("yes"))
                        {
                            sequenceTables[sequenceCount] = SequenceTable.InstallUISequence;
                            ++sequenceCount;
                        }
                        break;
                    case "sequence":
                        sequence = Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture);
                        break;
                }
            }

            if (null == id)
            {
                throw new XmlException();
            }

            if (Int32.MinValue == sequence)
            {
                throw new XmlException();
            }
            else if (1 > sequence)
            {
                throw new XmlException();
            }

            if (0 == sequenceCount)
            {
                throw new XmlException();
            }

            if (!empty && reader.Read() && XmlNodeType.EndElement != reader.MoveToContent())
            {
                throw new XmlException();
            }

            // create the actions
            WixActionRow[] actionRows = new WixActionRow[sequenceCount];
            for (var i = 0; i < sequenceCount; i++)
            {
                //WixActionRow actionRow = new WixActionRow(sequenceTables[i], id, condition, sequence);
                //actionRows[i] = actionRow;
                throw new NotImplementedException();
            }

            return actionRows;
        }
    }
}
