// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Schema;
    using WixToolset.Data.Msi;
    using WixToolset.Data.Rows;

    /// <summary>
    /// Object that represents a localization file.
    /// </summary>
    public sealed class Localization
    {
        private static string XmlElementName = "localization";

        private Dictionary<string, WixVariableRow> variables = new Dictionary<string, WixVariableRow>();
        private Dictionary<string, LocalizedControl> localizedControls = new Dictionary<string, LocalizedControl>();

        /// <summary>
        /// Instantiates a new localization object.
        /// </summary>
        public Localization(int codepage, string culture, IDictionary<string, WixVariableRow> variables, IDictionary<string, LocalizedControl> localizedControls)
        {
            this.Codepage = codepage;
            this.Culture = String.IsNullOrEmpty(culture) ? String.Empty : culture.ToLowerInvariant();
            this.variables = new Dictionary<string, WixVariableRow>(variables);
            this.localizedControls = new Dictionary<string, LocalizedControl>(localizedControls);
        }

        /// <summary>
        /// Gets the codepage.
        /// </summary>
        /// <value>The codepage.</value>
        public int Codepage { get; private set; }

        /// <summary>
        /// Gets the culture.
        /// </summary>
        /// <value>The culture.</value>
        public string Culture { get; private set; }

        /// <summary>
        /// Gets the variables.
        /// </summary>
        /// <value>The variables.</value>
        public ICollection<WixVariableRow> Variables
        {
            get { return this.variables.Values; }
        }

        /// <summary>
        /// Gets the localized controls.
        /// </summary>
        /// <value>The localized controls.</value>
        public ICollection<KeyValuePair<string, LocalizedControl>> LocalizedControls
        {
            get { return this.localizedControls; }
        }

        /// <summary>
        /// Merge the information from another localization object into this one.
        /// </summary>
        /// <param name="localization">The localization object to be merged into this one.</param>
        public void Merge(Localization localization)
        {
            foreach (WixVariableRow wixVariableRow in localization.Variables)
            {
                WixVariableRow existingWixVariableRow;
                if (!this.variables.TryGetValue(wixVariableRow.Id, out existingWixVariableRow) || (existingWixVariableRow.Overridable && !wixVariableRow.Overridable))
                {
                    variables[wixVariableRow.Id] = wixVariableRow;
                }
                else if (!wixVariableRow.Overridable)
                {
                    throw new WixException(WixDataErrors.DuplicateLocalizationIdentifier(wixVariableRow.SourceLineNumbers, wixVariableRow.Id));
                }
            }
        }

        /// <summary>
        /// Loads a localization file from a stream.
        /// </summary>
        /// <param name="reader">XmlReader where the intermediate is persisted.</param>
        /// <param name="tableDefinitions">Collection containing TableDefinitions to use when loading the localization file.</param>
        /// <returns>Returns the loaded localization.</returns>
        internal static Localization Read(XmlReader reader, TableDefinitionCollection tableDefinitions)
        {
            Debug.Assert("localization" == reader.LocalName);

            int codepage = 0;
            string culture = null;
            bool empty = reader.IsEmptyElement;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.Name)
                {
                    case "codepage":
                        codepage = Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture);
                        break;
                    case "culture":
                        culture = reader.Value;
                        break;
                }
            }

            TableDefinition wixVariableTable = tableDefinitions["WixVariable"];
            Dictionary<string, WixVariableRow> variables = new Dictionary<string, WixVariableRow>();
            Dictionary<string, LocalizedControl> localizedControls = new Dictionary<string, LocalizedControl>();

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
                                case "string":
                                    WixVariableRow row = Localization.ReadString(reader, wixVariableTable);
                                    variables.Add(row.Id, row);
                                    break;

                                case "ui":
                                    LocalizedControl ui = Localization.ReadUI(reader);
                                    localizedControls.Add(ui.GetKey(), ui);
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

            return new Localization(codepage, culture, variables, localizedControls);
        }

        /// <summary>
        /// Writes a localization file into an XML format.
        /// </summary>
        /// <param name="writer">XmlWriter where the localization file should persist itself as XML.</param>
        internal void Write(XmlWriter writer)
        {
            writer.WriteStartElement(Localization.XmlElementName, Library.XmlNamespaceUri);

            if (-1 != this.Codepage)
            {
                writer.WriteAttributeString("codepage", this.Codepage.ToString(CultureInfo.InvariantCulture));
            }

            if (!String.IsNullOrEmpty(this.Culture))
            {
                writer.WriteAttributeString("culture", this.Culture);
            }

            foreach (WixVariableRow wixVariableRow in this.variables.Values)
            {
                writer.WriteStartElement("string", Library.XmlNamespaceUri);

                writer.WriteAttributeString("id", wixVariableRow.Id);

                if (wixVariableRow.Overridable)
                {
                    writer.WriteAttributeString("overridable", "yes");
                }

                writer.WriteCData(wixVariableRow.Value);

                writer.WriteEndElement();
            }

            foreach (string controlKey in this.localizedControls.Keys)
            {
                writer.WriteStartElement("ui", Library.XmlNamespaceUri);

                string[] controlKeys = controlKey.Split('/');
                string dialog = controlKeys[0];
                string control = controlKeys[1];

                if (!String.IsNullOrEmpty(dialog))
                {
                    writer.WriteAttributeString("dialog", dialog);
                }

                if (!String.IsNullOrEmpty(control))
                {
                    writer.WriteAttributeString("control", control);
                }

                LocalizedControl localizedControl = this.localizedControls[controlKey];

                if (Common.IntegerNotSet != localizedControl.X)
                {
                    writer.WriteAttributeString("x", localizedControl.X.ToString());
                }

                if (Common.IntegerNotSet != localizedControl.Y)
                {
                    writer.WriteAttributeString("y", localizedControl.Y.ToString());
                }

                if (Common.IntegerNotSet != localizedControl.Width)
                {
                    writer.WriteAttributeString("width", localizedControl.Width.ToString());
                }

                if (Common.IntegerNotSet != localizedControl.Height)
                {
                    writer.WriteAttributeString("height", localizedControl.Height.ToString());
                }

                if (MsiInterop.MsidbControlAttributesRTLRO == (localizedControl.Attributes & MsiInterop.MsidbControlAttributesRTLRO))
                {
                    writer.WriteAttributeString("rightToLeft", "yes");
                }

                if (MsiInterop.MsidbControlAttributesRightAligned == (localizedControl.Attributes & MsiInterop.MsidbControlAttributesRightAligned))
                {
                    writer.WriteAttributeString("rightAligned", "yes");
                }

                if (MsiInterop.MsidbControlAttributesLeftScroll == (localizedControl.Attributes & MsiInterop.MsidbControlAttributesLeftScroll))
                {
                    writer.WriteAttributeString("leftScroll", "yes");
                }

                if (!String.IsNullOrEmpty(localizedControl.Text))
                {
                    writer.WriteCData(localizedControl.Text);
                }

                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Loads a localization file from a stream.
        /// </summary>
        /// <param name="reader">XmlReader where the intermediate is persisted.</param>
        /// <param name="tableDefinitions">Collection containing TableDefinitions to use when loading the localization file.</param>
        /// <returns>Returns the loaded localization.</returns>
        private static WixVariableRow ReadString(XmlReader reader, TableDefinition wixVariableTable)
        {
            Debug.Assert("string" == reader.LocalName);

            string id = null;
            string value = null;
            bool overridable = false;
            bool empty = reader.IsEmptyElement;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.Name)
                {
                    case "id":
                        id = reader.Value;
                        break;
                    case "overridable":
                        overridable = reader.Value.Equals("yes");
                        break;
                }
            }


            if (!empty)
            {
                reader.Read();

                value = reader.Value;

                reader.Read();

                if (XmlNodeType.EndElement != reader.NodeType)
                {
                    throw new XmlException();
                }
            }

            WixVariableRow wixVariableRow = new WixVariableRow(SourceLineNumber.CreateFromUri(reader.BaseURI), wixVariableTable);
            wixVariableRow.Id = id;
            wixVariableRow.Overridable = overridable;
            wixVariableRow.Value = value;

            return wixVariableRow;
        }

        private static LocalizedControl ReadUI(XmlReader reader)
        {
            Debug.Assert("ui" == reader.LocalName);

            string dialog = null;
            string control = null;
            int x = Common.IntegerNotSet;
            int y = Common.IntegerNotSet;
            int width = Common.IntegerNotSet;
            int height = Common.IntegerNotSet;
            int attributes = Common.IntegerNotSet;
            string text = null;
            bool empty = reader.IsEmptyElement;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.Name)
                {
                    case "dialog":
                        dialog = reader.Value;
                        break;
                    case "control":
                        control = reader.Value;
                        break;
                    case "x":
                        x = Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture);
                        break;
                    case "y":
                        y = Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture);
                        break;
                    case "width":
                        width = Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture);
                        break;
                    case "height":
                        height = Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture);
                        break;
                    case "attributes":
                        attributes = Convert.ToInt32(reader.Value, CultureInfo.InvariantCulture);
                        break;
                }
            }

            if (!empty)
            {
                reader.Read();

                text = reader.Value;

                reader.Read();

                if (XmlNodeType.EndElement != reader.NodeType)
                {
                    throw new XmlException();
                }
            }

            return new LocalizedControl(dialog, control, x, y, width, height, attributes, text);
        }
    }
}
