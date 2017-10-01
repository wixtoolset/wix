// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Rows;
    using WixToolset.Core.Native;

    /// <summary>
    /// Parses localization files and localizes database values.
    /// </summary>
    public sealed class Localizer
    {
        public static readonly XNamespace WxlNamespace = "http://wixtoolset.org/schemas/v4/wxl";
        private static string XmlElementName = "WixLocalization";

        private Dictionary<string, WixVariableRow> variables;
        private Dictionary<string, LocalizedControl> localizedControls;

        /// <summary>
        /// Instantiate a new Localizer.
        /// </summary>
        public Localizer(IEnumerable<Localization> localizations)
        {
            this.Codepage = -1;
            this.variables = new Dictionary<string, WixVariableRow>();
            this.localizedControls = new Dictionary<string, LocalizedControl>();

            foreach (var localization in localizations)
            {
                if (-1 == this.Codepage)
                {
                    this.Codepage = localization.Codepage;
                }

                foreach (WixVariableRow wixVariableRow in localization.Variables)
                {
                    Localizer.AddWixVariable(this.variables, wixVariableRow);
                }

                foreach (KeyValuePair<string, LocalizedControl> localizedControl in localization.LocalizedControls)
                {
                    if (!this.localizedControls.ContainsKey(localizedControl.Key))
                    {
                        this.localizedControls.Add(localizedControl.Key, localizedControl.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the codepage.
        /// </summary>
        /// <value>The codepage.</value>
        public int Codepage { get; private set; }

        /// <summary>
        /// Loads a localization file from a path on disk.
        /// </summary>
        /// <param name="path">Path to library file saved on disk.</param>
        /// <param name="tableDefinitions">Collection containing TableDefinitions to use when loading the localization file.</param>
        /// <param name="suppressSchema">Suppress xml schema validation while loading.</param>
        /// <returns>Returns the loaded localization file.</returns>
        public static Localization ParseLocalizationFile(string path, TableDefinitionCollection tableDefinitions)
        {
            XElement root = XDocument.Load(path).Root;
            Localization localization = null;

            SourceLineNumber sourceLineNumbers = SourceLineNumber.CreateFromXObject(root);
            if (Localizer.XmlElementName == root.Name.LocalName)
            {
                if (Localizer.WxlNamespace == root.Name.Namespace)
                {
                    localization = ParseWixLocalizationElement(root, tableDefinitions);
                }
                else // invalid or missing namespace
                {
                    if (null == root.Name.Namespace)
                    {
                        Messaging.Instance.OnMessage(WixErrors.InvalidWixXmlNamespace(sourceLineNumbers, Localizer.XmlElementName, Localizer.WxlNamespace.NamespaceName));
                    }
                    else
                    {
                        Messaging.Instance.OnMessage(WixErrors.InvalidWixXmlNamespace(sourceLineNumbers, Localizer.XmlElementName, root.Name.LocalName, Localizer.WxlNamespace.NamespaceName));
                    }
                }
            }
            else
            {
                Messaging.Instance.OnMessage(WixErrors.InvalidDocumentElement(sourceLineNumbers, root.Name.LocalName, "localization", Localizer.XmlElementName));
            }

            return localization;
        }

        /// <summary>
        /// Get a localized data value.
        /// </summary>
        /// <param name="id">The name of the localization variable.</param>
        /// <returns>The localized data value or null if it wasn't found.</returns>
        public string GetLocalizedValue(string id)
        {
            return this.variables.TryGetValue(id, out var wixVariableRow) ? wixVariableRow.Value : null;
        }

        /// <summary>
        /// Get a localized control.
        /// </summary>
        /// <param name="dialog">The optional id of the control's dialog.</param>
        /// <param name="control">The id of the control.</param>
        /// <returns>The localized control or null if it wasn't found.</returns>
        public LocalizedControl GetLocalizedControl(string dialog, string control)
        {
            LocalizedControl localizedControl;
            return this.localizedControls.TryGetValue(LocalizedControl.GetKey(dialog, control), out localizedControl) ? localizedControl : null;
        }

        /// <summary>
        /// Adds a WixVariableRow to a dictionary while performing the expected override checks.
        /// </summary>
        /// <param name="variables">Dictionary of variable rows.</param>
        /// <param name="wixVariableRow">Row to add to the variables dictionary.</param>
        private static void AddWixVariable(IDictionary<string, WixVariableRow> variables, WixVariableRow wixVariableRow)
        {
            WixVariableRow existingWixVariableRow;
            if (!variables.TryGetValue(wixVariableRow.Id, out existingWixVariableRow) || (existingWixVariableRow.Overridable && !wixVariableRow.Overridable))
            {
                variables[wixVariableRow.Id] = wixVariableRow;
            }
            else if (!wixVariableRow.Overridable)
            {
                Messaging.Instance.OnMessage(WixErrors.DuplicateLocalizationIdentifier(wixVariableRow.SourceLineNumbers, wixVariableRow.Id));
            }
        }

        /// <summary>
        /// Parses the WixLocalization element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private static Localization ParseWixLocalizationElement(XElement node, TableDefinitionCollection tableDefinitions)
        {
            int codepage = -1;
            string culture = null;
            SourceLineNumber sourceLineNumbers = SourceLineNumber.CreateFromXObject(node);

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || Localizer.WxlNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Codepage":
                            codepage = Common.GetValidCodePage(attrib.Value, true, false, sourceLineNumbers);
                            break;
                        case "Culture":
                            culture = attrib.Value;
                            break;
                        case "Language":
                            // do nothing; @Language is used for locutil which can't convert Culture to lcid
                            break;
                        default:
                            Common.UnexpectedAttribute(sourceLineNumbers, attrib);
                            break;
                    }
                }
                else
                {
                    Common.UnexpectedAttribute(sourceLineNumbers, attrib);
                }
            }

            Dictionary<string, WixVariableRow> variables = new Dictionary<string,WixVariableRow>();
            Dictionary<string, LocalizedControl> localizedControls = new Dictionary<string, LocalizedControl>();

            foreach (XElement child in node.Elements())
            {
                if (Localizer.WxlNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "String":
                            Localizer.ParseString(child, variables, tableDefinitions);
                            break;

                        case "UI":
                            Localizer.ParseUI(child, localizedControls);
                            break;

                        default:
                            Messaging.Instance.OnMessage(WixErrors.UnexpectedElement(sourceLineNumbers, node.Name.ToString(), child.Name.ToString()));
                            break;
                    }
                }
                else
                {
                    Messaging.Instance.OnMessage(WixErrors.UnsupportedExtensionElement(sourceLineNumbers, node.Name.ToString(), child.Name.ToString()));
                }
            }

            return Messaging.Instance.EncounteredError ? null : new Localization(codepage, culture, variables, localizedControls);
        }

        /// <summary>
        /// Parse a localization string into a WixVariableRow.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private static void ParseString(XElement node, IDictionary<string, WixVariableRow> variables, TableDefinitionCollection tableDefinitions)
        {
            string id = null;
            bool overridable = false;
            SourceLineNumber sourceLineNumbers = SourceLineNumber.CreateFromXObject(node);

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || Localizer.WxlNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = Common.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Overridable":
                            overridable = YesNoType.Yes == Common.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Localizable":
                            ; // do nothing
                            break;
                        default:
                            Messaging.Instance.OnMessage(WixErrors.UnexpectedAttribute(sourceLineNumbers, attrib.Parent.Name.ToString(), attrib.Name.ToString()));
                            break;
                    }
                }
                else
                {
                    Messaging.Instance.OnMessage(WixErrors.UnsupportedExtensionAttribute(sourceLineNumbers, attrib.Parent.Name.ToString(), attrib.Name.ToString()));
                }
            }

            string value = Common.GetInnerText(node);

            if (null == id)
            {
                Messaging.Instance.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, "String", "Id"));
            }
            else if (0 == id.Length)
            {
                Messaging.Instance.OnMessage(WixErrors.IllegalIdentifier(sourceLineNumbers, "String", "Id", 0));
            }

            if (!Messaging.Instance.EncounteredError)
            {
                WixVariableRow wixVariableRow = new WixVariableRow(sourceLineNumbers, tableDefinitions["WixVariable"]);
                wixVariableRow.Id = id;
                wixVariableRow.Overridable = overridable;
                wixVariableRow.Value = value;

                Localizer.AddWixVariable(variables, wixVariableRow);
            }
        }

        /// <summary>
        /// Parse a localized control.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="localizedControls">Dictionary of localized controls.</param>
        private static void ParseUI(XElement node, IDictionary<string, LocalizedControl> localizedControls)
        {
            string dialog = null;
            string control = null;
            int x = CompilerConstants.IntegerNotSet;
            int y = CompilerConstants.IntegerNotSet;
            int width = CompilerConstants.IntegerNotSet;
            int height = CompilerConstants.IntegerNotSet;
            int attribs = 0;
            string text = null;
            SourceLineNumber sourceLineNumbers = SourceLineNumber.CreateFromXObject(node);

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || Localizer.WxlNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Dialog":
                            dialog = Common.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Control":
                            control = Common.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "X":
                            x = Common.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Y":
                            y = Common.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Width":
                            width = Common.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Height":
                            height = Common.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "RightToLeft":
                            if (YesNoType.Yes == Common.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attribs |= MsiInterop.MsidbControlAttributesRTLRO;
                            }
                            break;
                        case "RightAligned":
                            if (YesNoType.Yes == Common.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attribs |= MsiInterop.MsidbControlAttributesRightAligned;
                            }
                            break;
                        case "LeftScroll":
                            if (YesNoType.Yes == Common.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attribs |= MsiInterop.MsidbControlAttributesLeftScroll;
                            }
                            break;
                        default:
                            Common.UnexpectedAttribute(sourceLineNumbers, attrib);
                            break;
                    }
                }
                else
                {
                    Common.UnexpectedAttribute(sourceLineNumbers, attrib);
                }
            }

            text = Common.GetInnerText(node);

            if (String.IsNullOrEmpty(control) && 0 < attribs)
            {
                if (MsiInterop.MsidbControlAttributesRTLRO == (attribs & MsiInterop.MsidbControlAttributesRTLRO))
                {
                    Messaging.Instance.OnMessage(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.ToString(), "RightToLeft", "Control"));
                }
                else if (MsiInterop.MsidbControlAttributesRightAligned == (attribs & MsiInterop.MsidbControlAttributesRightAligned))
                {
                    Messaging.Instance.OnMessage(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.ToString(), "RightAligned", "Control"));
                }
                else if (MsiInterop.MsidbControlAttributesLeftScroll == (attribs & MsiInterop.MsidbControlAttributesLeftScroll))
                {
                    Messaging.Instance.OnMessage(WixErrors.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.ToString(), "LeftScroll", "Control"));
                }
            }

            if (String.IsNullOrEmpty(control) && String.IsNullOrEmpty(dialog))
            {
                Messaging.Instance.OnMessage(WixErrors.ExpectedAttributesWithOtherAttribute(sourceLineNumbers, node.Name.ToString(), "Dialog", "Control"));
            }

            if (!Messaging.Instance.EncounteredError)
            {
                LocalizedControl localizedControl = new LocalizedControl(dialog, control, x, y, width, height, attribs, text);
                string key = localizedControl.GetKey();
                if (localizedControls.ContainsKey(key))
                {
                    if (String.IsNullOrEmpty(localizedControl.Control))
                    {
                        Messaging.Instance.OnMessage(WixErrors.DuplicatedUiLocalization(sourceLineNumbers, localizedControl.Dialog));
                    }
                    else
                    {
                        Messaging.Instance.OnMessage(WixErrors.DuplicatedUiLocalization(sourceLineNumbers, localizedControl.Dialog, localizedControl.Control));
                    }
                }
                else
                {
                    localizedControls.Add(key, localizedControl);
                }
            }
        }
    }
}
