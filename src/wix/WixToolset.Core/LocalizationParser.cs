// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Bind;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;

    internal class LocalizationParser : ILocalizationParser
    {
        public static readonly XNamespace WxlNamespace = "http://wixtoolset.org/schemas/v4/wxl";
        private const string XmlElementName = "WixLocalization";

        internal LocalizationParser(IServiceProvider serviceProvider)
        {
            this.Messaging = serviceProvider.GetService<IMessaging>();
        }

        private IMessaging Messaging { get; }

        public Localization ParseLocalization(string path)
        {
            var document = XDocument.Load(path);
            return this.ParseLocalization(document);
        }

        public Localization ParseLocalization(XDocument document)
        {
            var root = document.Root;
            Localization localization = null;

            var sourceLineNumbers = SourceLineNumber.CreateFromXObject(root);
            if (LocalizationParser.XmlElementName == root.Name.LocalName)
            {
                if (LocalizationParser.WxlNamespace == root.Name.Namespace)
                {
                    localization = ParseWixLocalizationElement(this.Messaging, root);
                }
                else // invalid or missing namespace
                {
                    if (null == root.Name.Namespace)
                    {
                        this.Messaging.Write(ErrorMessages.InvalidWixXmlNamespace(sourceLineNumbers, LocalizationParser.XmlElementName, LocalizationParser.WxlNamespace.NamespaceName));
                    }
                    else
                    {
                        this.Messaging.Write(ErrorMessages.InvalidWixXmlNamespace(sourceLineNumbers, LocalizationParser.XmlElementName, root.Name.LocalName, LocalizationParser.WxlNamespace.NamespaceName));
                    }
                }
            }
            else
            {
                this.Messaging.Write(ErrorMessages.InvalidDocumentElement(sourceLineNumbers, root.Name.LocalName, "localization", LocalizationParser.XmlElementName));
            }

            return localization;
        }

        /// <summary>
        /// Adds a WixVariableRow to a dictionary while performing the expected override checks.
        /// </summary>
        /// <param name="messaging"></param>
        /// <param name="variables">Dictionary of variable rows.</param>
        /// <param name="wixVariableRow">Row to add to the variables dictionary.</param>
        private static void AddWixVariable(IMessaging messaging, IDictionary<string, BindVariable> variables, BindVariable wixVariableRow)
        {
            if (!variables.TryGetValue(wixVariableRow.Id, out var existingWixVariableRow) || (existingWixVariableRow.Overridable && !wixVariableRow.Overridable))
            {
                variables[wixVariableRow.Id] = wixVariableRow;
            }
            else if (!wixVariableRow.Overridable)
            {
                messaging.Write(ErrorMessages.DuplicateLocalizationIdentifier(wixVariableRow.SourceLineNumbers, wixVariableRow.Id));
            }
        }

        /// <summary>
        /// Parses the WixLocalization element.
        /// </summary>
        /// <param name="messaging"></param>
        /// <param name="node">Element to parse.</param>
        private static Localization ParseWixLocalizationElement(IMessaging messaging, XElement node)
        {
            var sourceLineNumbers = SourceLineNumber.CreateFromXObject(node);
            int? codepage = null;
            int? summaryInformationCodepage = null;
            string culture = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || LocalizationParser.WxlNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Codepage":
                            codepage = Common.GetValidCodePage(attrib.Value, allowNoChange: true, onlyAnsi: false, sourceLineNumbers);
                            break;
                        case "SummaryInformationCodepage":
                            summaryInformationCodepage = Common.GetValidCodePage(attrib.Value, allowNoChange: true, onlyAnsi: false, sourceLineNumbers);
                            break;
                        case "Culture":
                            culture = attrib.Value;
                            break;
                        case "Language":
                            // do nothing; @Language is used for locutil which can't convert Culture to lcid
                            break;
                        default:
                            Common.UnexpectedAttribute(messaging, sourceLineNumbers, attrib);
                            break;
                    }
                }
                else
                {
                    Common.UnexpectedAttribute(messaging, sourceLineNumbers, attrib);
                }
            }

            var variables = new Dictionary<string, BindVariable>();
            var localizedControls = new Dictionary<string, LocalizedControl>();

            foreach (var child in node.Elements())
            {
                if (LocalizationParser.WxlNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "String":
                            LocalizationParser.ParseString(messaging, child, variables);
                            break;

                        case "UI":
                            LocalizationParser.ParseUI(messaging, child, localizedControls);
                            break;

                        default:
                            messaging.Write(ErrorMessages.UnexpectedElement(sourceLineNumbers, node.Name.ToString(), child.Name.ToString()));
                            break;
                    }
                }
                else
                {
                    messaging.Write(ErrorMessages.UnsupportedExtensionElement(sourceLineNumbers, node.Name.ToString(), child.Name.ToString()));
                }
            }

            return messaging.EncounteredError ? null : new Localization(codepage, summaryInformationCodepage, culture, variables, localizedControls);
        }

        /// <summary>
        /// Parse a localization string into a WixVariableRow.
        /// </summary>
        /// <param name="messaging"></param>
        /// <param name="node">Element to parse.</param>
        /// <param name="variables"></param>
        private static void ParseString(IMessaging messaging, XElement node, IDictionary<string, BindVariable> variables)
        {
            string id = null;
            var overridable = false;
            string value = null;
            var sourceLineNumbers = SourceLineNumber.CreateFromXObject(node);

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || LocalizationParser.WxlNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = Common.GetAttributeIdentifierValue(messaging, sourceLineNumbers, attrib);
                            break;
                        case "Overridable":
                            overridable = YesNoType.Yes == Common.GetAttributeYesNoValue(messaging, sourceLineNumbers, attrib);
                            break;
                        case "Localizable":
                            ; // do nothing
                            break;
                        case "Value":
                            value = Common.GetAttributeValue(messaging, sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        default:
                            messaging.Write(ErrorMessages.UnexpectedAttribute(sourceLineNumbers, attrib.Parent.Name.ToString(), attrib.Name.ToString()));
                            break;
                    }
                }
                else
                {
                    messaging.Write(ErrorMessages.UnsupportedExtensionAttribute(sourceLineNumbers, attrib.Parent.Name.ToString(), attrib.Name.ToString()));
                }
            }

            if (null == id)
            {
                messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, "String", "Id"));
            }
            else if (0 == id.Length)
            {
                messaging.Write(ErrorMessages.IllegalIdentifier(sourceLineNumbers, "String", "Id", 0));
            }

            Common.InnerTextDisallowed(messaging, node, "Value");

            if (!messaging.EncounteredError)
            {
                var variable = new BindVariable
                {
                    SourceLineNumbers = sourceLineNumbers,
                    Id = id,
                    Overridable = overridable,
                    Value = value,
                };

                LocalizationParser.AddWixVariable(messaging, variables, variable);
            }
        }

        /// <summary>
        /// Parse a localized control.
        /// </summary>
        /// <param name="messaging"></param>
        /// <param name="node">Element to parse.</param>
        /// <param name="localizedControls">Dictionary of localized controls.</param>
        private static void ParseUI(IMessaging messaging, XElement node, IDictionary<string, LocalizedControl> localizedControls)
        {
            string dialog = null;
            string control = null;
            string text = null;
            var x = CompilerConstants.IntegerNotSet;
            var y = CompilerConstants.IntegerNotSet;
            var width = CompilerConstants.IntegerNotSet;
            var height = CompilerConstants.IntegerNotSet;
            var sourceLineNumbers = SourceLineNumber.CreateFromXObject(node);
            var rightToLeft = false;
            var rightAligned = false;
            var leftScroll = false;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || LocalizationParser.WxlNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Dialog":
                            dialog = Common.GetAttributeIdentifierValue(messaging, sourceLineNumbers, attrib);
                            break;
                        case "Control":
                            control = Common.GetAttributeIdentifierValue(messaging, sourceLineNumbers, attrib);
                            break;
                        case "X":
                            x = Common.GetAttributeIntegerValue(messaging, sourceLineNumbers, attrib, 0, Int16.MaxValue);
                            break;
                        case "Y":
                            y = Common.GetAttributeIntegerValue(messaging, sourceLineNumbers, attrib, 0, Int16.MaxValue);
                            break;
                        case "Width":
                            width = Common.GetAttributeIntegerValue(messaging, sourceLineNumbers, attrib, 0, Int16.MaxValue);
                            break;
                        case "Height":
                            height = Common.GetAttributeIntegerValue(messaging, sourceLineNumbers, attrib, 0, Int16.MaxValue);
                            break;
                        case "RightToLeft":
                            rightToLeft = YesNoType.Yes == Common.GetAttributeYesNoValue(messaging, sourceLineNumbers, attrib);
                            break;
                        case "RightAligned":
                            rightAligned = YesNoType.Yes == Common.GetAttributeYesNoValue(messaging, sourceLineNumbers, attrib);
                            break;
                        case "LeftScroll":
                            leftScroll = YesNoType.Yes == Common.GetAttributeYesNoValue(messaging, sourceLineNumbers, attrib);
                            break;
                        case "Text":
                            text = Common.GetAttributeValue(messaging, sourceLineNumbers, attrib, EmptyRule.CanBeEmpty);
                            break;
                        default:
                            Common.UnexpectedAttribute(messaging, sourceLineNumbers, attrib);
                            break;
                    }
                }
                else
                {
                    Common.UnexpectedAttribute(messaging, sourceLineNumbers, attrib);
                }
            }

            if (String.IsNullOrEmpty(control) && (rightToLeft || rightAligned || leftScroll))
            {
                if (rightToLeft)
                {
                    messaging.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.ToString(), "RightToLeft", "Control"));
                }

                if (rightAligned)
                {
                    messaging.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.ToString(), "RightAligned", "Control"));
                }

                if (leftScroll)
                {
                    messaging.Write(ErrorMessages.IllegalAttributeWithoutOtherAttributes(sourceLineNumbers, node.Name.ToString(), "LeftScroll", "Control"));
                }
            }

            if (String.IsNullOrEmpty(control) && String.IsNullOrEmpty(dialog))
            {
                messaging.Write(ErrorMessages.ExpectedAttributesWithOtherAttribute(sourceLineNumbers, node.Name.ToString(), "Dialog", "Control"));
            }

            Common.InnerTextDisallowed(messaging, node, "Text");

            if (!messaging.EncounteredError)
            {
                var localizedControl = new LocalizedControl(dialog, control, x, y, width, height, rightToLeft, rightAligned, leftScroll, text);
                var key = localizedControl.GetKey();
                if (localizedControls.ContainsKey(key))
                {
                    if (String.IsNullOrEmpty(localizedControl.Control))
                    {
                        messaging.Write(ErrorMessages.DuplicatedUiLocalization(sourceLineNumbers, localizedControl.Dialog));
                    }
                    else
                    {
                        messaging.Write(ErrorMessages.DuplicatedUiLocalization(sourceLineNumbers, localizedControl.Dialog, localizedControl.Control));
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
