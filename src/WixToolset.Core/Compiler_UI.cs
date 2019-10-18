// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;

    /// <summary>
    /// Compiler of the WiX toolset.
    /// </summary>
    internal partial class Compiler : ICompiler
    {
        // NameToBit arrays
        private static readonly string[] TextControlAttributes = { "Transparent", "NoPrefix", "NoWrap", "FormatSize", "UserLanguage" };
        private static readonly string[] HyperlinkControlAttributes = { "Transparent" };
        private static readonly string[] EditControlAttributes = { "Multiline", null, null, null, null, "Password" };
        private static readonly string[] ProgressControlAttributes = { "ProgressBlocks" };
        private static readonly string[] VolumeControlAttributes = { "Removable", "Fixed", "Remote", "CDROM", "RAMDisk", "Floppy", "ShowRollbackCost" };
        private static readonly string[] ListboxControlAttributes = { "Sorted", null, null, null, "UserLanguage" };
        private static readonly string[] ListviewControlAttributes = { "Sorted", null, null, null, "FixedSize", "Icon16", "Icon32" };
        private static readonly string[] ComboboxControlAttributes = { "Sorted", "ComboList", null, null, "UserLanguage" };
        private static readonly string[] RadioControlAttributes = { "Image", "PushLike", "Bitmap", "Icon", "FixedSize", "Icon16", "Icon32", null, "HasBorder" };
        private static readonly string[] ButtonControlAttributes = { "Image", null, "Bitmap", "Icon", "FixedSize", "Icon16", "Icon32", "ElevationShield" };
        private static readonly string[] IconControlAttributes = { "Image", null, null, null, "FixedSize", "Icon16", "Icon32" };
        private static readonly string[] BitmapControlAttributes = { "Image", null, null, null, "FixedSize" };
        private static readonly string[] CheckboxControlAttributes = { null, "PushLike", "Bitmap", "Icon", "FixedSize", "Icon16", "Icon32" };

        /// <summary>
        /// Parses UI elements.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseUIElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            var embeddedUICount = 0;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(node, attrib);
                        break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "BillboardAction":
                        this.ParseBillboardActionElement(child);
                        break;
                    case "ComboBox":
                        this.ParseControlGroupElement(child, TupleDefinitionType.ComboBox, "ListItem");
                        break;
                    case "Dialog":
                        this.ParseDialogElement(child);
                        break;
                    case "DialogRef":
                        this.ParseSimpleRefElement(child, "Dialog");
                        break;
                    case "EmbeddedUI":
                        if (0 < embeddedUICount) // there can be only one embedded UI
                        {
                            var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                            this.Core.Write(ErrorMessages.TooManyChildren(childSourceLineNumbers, node.Name.LocalName, child.Name.LocalName));
                        }
                        this.ParseEmbeddedUIElement(child);
                        ++embeddedUICount;
                        break;
                    case "Error":
                        this.ParseErrorElement(child);
                        break;
                    case "ListBox":
                        this.ParseControlGroupElement(child, TupleDefinitionType.ListBox, "ListItem");
                        break;
                    case "ListView":
                        this.ParseControlGroupElement(child, TupleDefinitionType.ListView, "ListItem");
                        break;
                    case "ProgressText":
                        this.ParseActionTextElement(child);
                        break;
                    case "Publish":
                        var order = 0;
                        this.ParsePublishElement(child, null, null, ref order);
                        break;
                    case "RadioButtonGroup":
                        var radioButtonType = this.ParseRadioButtonGroupElement(child, null, RadioButtonType.NotSet);
                        if (RadioButtonType.Bitmap == radioButtonType || RadioButtonType.Icon == radioButtonType)
                        {
                            var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                            this.Core.Write(ErrorMessages.RadioButtonBitmapAndIconDisallowed(childSourceLineNumbers));
                        }
                        break;
                    case "TextStyle":
                        this.ParseTextStyleElement(child);
                        break;
                    case "UIText":
                        this.ParseUITextElement(child);
                        break;

                    // the following are available indentically under the UI and Product elements for document organization use only
                    case "AdminUISequence":
                        this.ParseSequenceElement(child, SequenceTable.AdminUISequence);
                        break;
                    case "InstallUISequence":
                        this.ParseSequenceElement(child, SequenceTable.InstallUISequence);
                        break;
                    case "Binary":
                        this.ParseBinaryElement(child);
                        break;
                    case "Property":
                        this.ParsePropertyElement(child);
                        break;
                    case "PropertyRef":
                        this.ParseSimpleRefElement(child, "Property");
                        break;
                    case "UIRef":
                        this.ParseSimpleRefElement(child, "WixUI");
                        break;

                    default:
                        this.Core.UnexpectedElement(node, child);
                        break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (null != id && !this.Core.EncounteredError)
            {
                var tuple = new WixUITuple(sourceLineNumbers, id);
                this.Core.AddTuple(tuple);
            }
        }

        /// <summary>
        /// Parses a list item element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="table">Table to add row to.</param>
        /// <param name="property">Identifier of property referred to by list item.</param>
        /// <param name="order">Relative order of list items.</param>
        private void ParseListItemElement(XElement node, TupleDefinitionType tupleType, string property, ref int order)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string icon = null;
            string text = null;
            string value = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Icon":
                        if (TupleDefinitionType.ListView == tupleType)
                        {
                            icon = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Binary", icon);
                        }
                        else
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeExceptOnElement(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "ListView"));
                        }
                        break;
                    case "Text":
                        text = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Value":
                        value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(node, attrib);
                        break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == value)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var tuple = this.Core.CreateTuple(sourceLineNumbers, tupleType);
                tuple.Set(0, property);
                tuple.Set(1, ++order);
                tuple.Set(2, value);
                tuple.Set(3, text);
                if (null != icon)
                {
                    tuple.Set(4, icon);
                }
            }
        }

        /// <summary>
        /// Parses a radio button element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="property">Identifier of property referred to by radio button.</param>
        /// <param name="order">Relative order of radio buttons.</param>
        /// <returns>Type of this radio button.</returns>
        private RadioButtonType ParseRadioButtonElement(XElement node, string property, ref int order)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var type = RadioButtonType.NotSet;
            string value = null;
            string x = null;
            string y = null;
            string width = null;
            string height = null;
            string text = null;
            string tooltip = null;
            string help = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Bitmap":
                        if (RadioButtonType.NotSet != type)
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Icon", "Text"));
                        }
                        text = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        this.Core.CreateSimpleReference(sourceLineNumbers, "Binary", text);
                        type = RadioButtonType.Bitmap;
                        break;
                    case "Height":
                        height = this.Core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
                        break;
                    case "Help":
                        help = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Icon":
                        if (RadioButtonType.NotSet != type)
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttributes(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Bitmap", "Text"));
                        }
                        text = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        this.Core.CreateSimpleReference(sourceLineNumbers, "Binary", text);
                        type = RadioButtonType.Icon;
                        break;
                    case "Text":
                        if (RadioButtonType.NotSet != type)
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, "Bitmap", "Icon"));
                        }
                        text = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        type = RadioButtonType.Text;
                        break;
                    case "ToolTip":
                        tooltip = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Value":
                        value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Width":
                        width = this.Core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
                        break;
                    case "X":
                        x = this.Core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
                        break;
                    case "Y":
                        y = this.Core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(node, attrib);
                        break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == value)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value"));
            }

            if (null == x)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "X"));
            }

            if (null == y)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Y"));
            }

            if (null == width)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Width"));
            }

            if (null == height)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Height"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var tuple = new RadioButtonTuple(sourceLineNumbers)
                {
                    Property = property,
                    Order = ++order,
                    Value = value,
                    Text = text,
                    Help = (null != tooltip || null != help) ? String.Concat(tooltip, "|", help) : null
                };

                tuple.Set((int)RadioButtonTupleFields.X, x);
                tuple.Set((int)RadioButtonTupleFields.Y, y);
                tuple.Set((int)RadioButtonTupleFields.Width, width);
                tuple.Set((int)RadioButtonTupleFields.Height, height);

                this.Core.AddTuple(tuple);
            }

            return type;
        }

        /// <summary>
        /// Parses a billboard element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseBillboardActionElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string action = null;
            var order = 0;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        action = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        this.Core.CreateSimpleReference(sourceLineNumbers, "WixAction", "InstallExecuteSequence", action);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(node, attrib);
                        break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == action)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "Billboard":
                        order = order + 1;
                        this.ParseBillboardElement(child, action, order);
                        break;
                    default:
                        this.Core.UnexpectedElement(node, child);
                        break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }
        }

        /// <summary>
        /// Parses a billboard element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="action">Action for the billboard.</param>
        /// <param name="order">Order of the billboard.</param>
        private void ParseBillboardElement(XElement node, string action, int order)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string feature = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                        break;
                    case "Feature":
                        feature = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        this.Core.CreateSimpleReference(sourceLineNumbers, "Feature", feature);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(node, attrib);
                        break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                id = this.Core.CreateIdentifier("bil", action, order.ToString(), feature);
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "Control":
                        // These are all thrown away.
                        IntermediateTuple lastTabRow = null;
                        string firstControl = null;
                        string defaultControl = null;
                        string cancelControl = null;

                        this.ParseControlElement(child, id.Id, TupleDefinitionType.BBControl, ref lastTabRow, ref firstControl, ref defaultControl, ref cancelControl, false);
                        break;
                    default:
                        this.Core.UnexpectedElement(node, child);
                        break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }


            if (!this.Core.EncounteredError)
            {
                var tuple = new BillboardTuple(sourceLineNumbers, id)
                {
                    FeatureRef = feature,
                    Action = action,
                    Ordering = order
                };

                this.Core.AddTuple(tuple);
            }
        }

        /// <summary>
        /// Parses a control group element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="table">Table referred to by control group.</param>
        /// <param name="childTag">Expected child elements.</param>
        private void ParseControlGroupElement(XElement node, TupleDefinitionType tupleType, string childTag)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var order = 0;
            string property = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Property":
                        property = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(node, attrib);
                        break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == property)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Property"));
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    if (childTag != child.Name.LocalName)
                    {
                        this.Core.UnexpectedElement(node, child);
                    }

                    switch (child.Name.LocalName)
                    {
                    case "ListItem":
                        this.ParseListItemElement(child, tupleType, property, ref order);
                        break;
                    case "Property":
                        this.ParsePropertyElement(child);
                        break;
                    default:
                        this.Core.UnexpectedElement(node, child);
                        break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

        }

        /// <summary>
        /// Parses a radio button control group element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="property">Property associated with this radio button group.</param>
        /// <param name="groupType">Specifies the current type of radio buttons in the group.</param>
        /// <returns>The current type of radio buttons in the group.</returns>
        private RadioButtonType ParseRadioButtonGroupElement(XElement node, string property, RadioButtonType groupType)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var order = 0;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Property":
                        property = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        this.Core.CreateSimpleReference(sourceLineNumbers, "Property", property);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(node, attrib);
                        break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == property)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Property"));
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "RadioButton":
                        var type = this.ParseRadioButtonElement(child, property, ref order);
                        if (RadioButtonType.NotSet == groupType)
                        {
                            groupType = type;
                        }
                        else if (groupType != type)
                        {
                            var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                            this.Core.Write(ErrorMessages.RadioButtonTypeInconsistent(childSourceLineNumbers));
                        }
                        break;
                    default:
                        this.Core.UnexpectedElement(node, child);
                        break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }


            return groupType;
        }

        /// <summary>
        /// Parses an action text element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseActionTextElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string action = null;
            string template = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Action":
                        action = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Template":
                        template = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(node, attrib);
                        break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == action)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Action"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var tuple = new ActionTextTuple(sourceLineNumbers)
                {
                    Action = action,
                    Description = Common.GetInnerText(node),
                    Template = template
                };

                this.Core.AddTuple(tuple);
            }
        }

        /// <summary>
        /// Parses an ui text element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseUITextElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string text = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(node, attrib);
                        break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            text = Common.GetInnerText(node);

            if (null == id)
            {
                id = this.Core.CreateIdentifier("txt", text);
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var tuple = new UITextTuple(sourceLineNumbers, id)
                {
                    Text = text,
                };

                this.Core.AddTuple(tuple);
            }
        }

        /// <summary>
        /// Parses a text style element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseTextStyleElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            int? red = null;
            int? green = null;
            int? blue = null;
            var bold = false;
            var italic = false;
            var strike = false;
            var underline = false;
            string faceName = null;
            var size = "0";

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                        break;

                    // RGB Values
                    case "Red":
                        var redColor = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Byte.MaxValue);
                        if (CompilerConstants.IllegalInteger != redColor)
                        {
                            red = redColor;
                        }
                        break;
                    case "Green":
                        var greenColor = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Byte.MaxValue);
                        if (CompilerConstants.IllegalInteger != greenColor)
                        {
                            green = greenColor;
                        }
                        break;
                    case "Blue":
                        var blueColor = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Byte.MaxValue);
                        if (CompilerConstants.IllegalInteger != blueColor)
                        {
                            blue = blueColor;
                        }
                        break;

                    // Style values
                    case "Bold":
                        bold = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Italic":
                        italic = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Strike":
                        strike = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Underline":
                        underline = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;

                    // Font values
                    case "FaceName":
                        faceName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Size":
                        size = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;

                    default:
                        this.Core.UnexpectedAttribute(node, attrib);
                        break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.CreateIdentifier("txs", faceName, size.ToString(), (red ?? 0).ToString(), (green ?? 0).ToString(), (blue ?? 0).ToString(), bold.ToString(), italic.ToString(), strike.ToString(), underline.ToString());
            }

            if (null == faceName)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "FaceName"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var tuple = new TextStyleTuple(sourceLineNumbers, id)
                {
                    FaceName = faceName,
                    Red = red,
                    Green = green,
                    Blue = blue,
                    Bold = bold,
                    Italic = italic,
                    Strike = strike,
                    Underline = underline,
                };

                tuple.Set((int)TextStyleTupleFields.Size, size);

                this.Core.AddTuple(tuple);
            }
        }

        /// <summary>
        /// Parses a dialog element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseDialogElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            var hidden = false;
            var modal = true;
            var minimize = true;
            var customPalette = false;
            var errorDialog = false;
            var keepModeless = false;
            var height = 0;
            string title = null;
            var leftScroll = false;
            var rightAligned = false;
            var rightToLeft = false;
            var systemModal = false;
            var trackDiskSpace = false;
            var width = 0;
            var x = 50;
            var y = 50;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                        break;
                    case "Height":
                        height = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
                        break;
                    case "Title":
                        title = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Width":
                        width = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
                        break;
                    case "X":
                        x = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, 100);
                        break;
                    case "Y":
                        y = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, 100);
                        break;
                    case "CustomPalette":
                        customPalette = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "ErrorDialog":
                        errorDialog = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Hidden":
                        hidden = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "KeepModeless":
                        keepModeless = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "LeftScroll":
                        leftScroll = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Modeless":
                        modal = YesNoType.Yes != this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "NoMinimize":
                        minimize = YesNoType.Yes != this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "RightAligned":
                        rightAligned = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "RightToLeft":
                        rightToLeft = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "SystemModal":
                        systemModal = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "TrackDiskSpace":
                        trackDiskSpace = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;

                    default:
                        this.Core.UnexpectedAttribute(node, attrib);
                        break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
                id = Identifier.Invalid;
            }

            IntermediateTuple lastTabRow = null;
            string cancelControl = null;
            string defaultControl = null;
            string firstControl = null;

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                    case "Control":
                        this.ParseControlElement(child, id.Id, TupleDefinitionType.Control, ref lastTabRow, ref firstControl, ref defaultControl, ref cancelControl, trackDiskSpace);
                        break;
                    default:
                        this.Core.UnexpectedElement(node, child);
                        break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (null != lastTabRow && null != lastTabRow[1])
            {
                if (firstControl != lastTabRow[1].ToString())
                {
                    lastTabRow.Set(10, firstControl);
                }
            }

            if (null == firstControl)
            {
                this.Core.Write(ErrorMessages.NoFirstControlSpecified(sourceLineNumbers, id.Id));
            }

            if (!this.Core.EncounteredError)
            {
                var tuple = new DialogTuple(sourceLineNumbers, id)
                {
                    HCentering = x,
                    VCentering = y,
                    Width = width,
                    Height = height,
                    CustomPalette = customPalette,
                    ErrorDialog = errorDialog,
                    Visible = !hidden,
                    Modal = modal,
                    KeepModeless = keepModeless,
                    LeftScroll = leftScroll,
                    Minimize = minimize,
                    RightAligned = rightAligned,
                    RightToLeft = rightToLeft,
                    SystemModal = systemModal,
                    TrackDiskSpace = trackDiskSpace,
                    Title = title,
                    FirstControlRef = firstControl,
                    DefaultControlRef = defaultControl,
                    CancelControlRef = cancelControl,
                };

                this.Core.AddTuple(tuple);
            }
        }

        /// <summary>
        /// Parses a control element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="dialog">Identifier for parent dialog.</param>
        /// <param name="table">Table control belongs in.</param>
        /// <param name="lastTabTuple">Last row in the tab order.</param>
        /// <param name="firstControl">Name of the first control in the tab order.</param>
        /// <param name="defaultControl">Name of the default control.</param>
        /// <param name="cancelControl">Name of the candle control.</param>
        /// <param name="trackDiskSpace">True if the containing dialog tracks disk space.</param>
        private void ParseControlElement(XElement node, string dialog, TupleDefinitionType tupleType, ref IntermediateTuple lastTabTuple, ref string firstControl, ref string defaultControl, ref string cancelControl, bool trackDiskSpace)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier controlId = null;
            var bits = new BitArray(32);
            string checkBoxPropertyRef = null;
            string checkboxValue = null;
            string controlType = null;
            var disabled = false;
            string height = null;
            string help = null;
            var isCancel = false;
            var isDefault = false;
            var notTabbable = false;
            string property = null;
            var publishOrder = 0;
            string sourceFile = null;
            string text = null;
            string tooltip = null;
            var radioButtonsType = RadioButtonType.NotSet;
            string width = null;
            string x = null;
            string y = null;

            var hidden = false;
            var sunken = false;
            var indirect = false;
            var integer = false;
            var rightToLeft = false;
            var rightAligned = false;
            var leftScroll = false;

            // The rest of the method relies on the control's Type, so we have to get that first.
            var typeAttribute = node.Attribute("Type");
            if (null == typeAttribute)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Type"));
            }
            else
            {
                controlType = this.Core.GetAttributeValue(sourceLineNumbers, typeAttribute);
            }

            string[] specialAttributes;
            switch (controlType)
            {
            case "Billboard":
                specialAttributes = null;
                notTabbable = true;
                disabled = true;

                this.Core.EnsureTable(sourceLineNumbers, "Billboard");
                break;
            case "Bitmap":
                specialAttributes = BitmapControlAttributes;
                notTabbable = true;
                disabled = true;
                break;
            case "CheckBox":
                specialAttributes = CheckboxControlAttributes;
                break;
            case "ComboBox":
                specialAttributes = ComboboxControlAttributes;
                break;
            case "DirectoryCombo":
                specialAttributes = VolumeControlAttributes;
                break;
            case "DirectoryList":
                specialAttributes = null;
                break;
            case "Edit":
                specialAttributes = EditControlAttributes;
                break;
            case "GroupBox":
                specialAttributes = null;
                notTabbable = true;
                break;
            case "Hyperlink":
                specialAttributes = HyperlinkControlAttributes;
                break;
            case "Icon":
                specialAttributes = IconControlAttributes;
                notTabbable = true;
                disabled = true;
                break;
            case "Line":
                specialAttributes = null;
                notTabbable = true;
                disabled = true;
                break;
            case "ListBox":
                specialAttributes = ListboxControlAttributes;
                break;
            case "ListView":
                specialAttributes = ListviewControlAttributes;
                break;
            case "MaskedEdit":
                specialAttributes = EditControlAttributes;
                break;
            case "PathEdit":
                specialAttributes = EditControlAttributes;
                break;
            case "ProgressBar":
                specialAttributes = ProgressControlAttributes;
                notTabbable = true;
                disabled = true;
                break;
            case "PushButton":
                specialAttributes = ButtonControlAttributes;
                break;
            case "RadioButtonGroup":
                specialAttributes = RadioControlAttributes;
                break;
            case "ScrollableText":
                specialAttributes = null;
                break;
            case "SelectionTree":
                specialAttributes = null;
                break;
            case "Text":
                specialAttributes = TextControlAttributes;
                notTabbable = true;
                break;
            case "VolumeCostList":
                specialAttributes = VolumeControlAttributes;
                notTabbable = true;
                break;
            case "VolumeSelectCombo":
                specialAttributes = VolumeControlAttributes;
                break;
            default:
                specialAttributes = null;
                notTabbable = true;
                break;
            }

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        controlId = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                        break;
                    case "Type": // already processed
                        break;
                    case "Cancel":
                        isCancel = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "CheckBoxPropertyRef":
                        checkBoxPropertyRef = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "CheckBoxValue":
                        checkboxValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Default":
                        isDefault = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Height":
                        height = this.Core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
                        break;
                    case "Help":
                        help = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "IconSize":
                        var iconSizeValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        if (null != specialAttributes)
                        {
                            switch (iconSizeValue)
                            {
                            case "16":
                                this.Core.TrySetBitFromName(specialAttributes, "Icon16", YesNoType.Yes, bits, 16);
                                break;
                            case "32":
                                this.Core.TrySetBitFromName(specialAttributes, "Icon32", YesNoType.Yes, bits, 16);
                                break;
                            case "48":
                                this.Core.TrySetBitFromName(specialAttributes, "Icon16", YesNoType.Yes, bits, 16);
                                this.Core.TrySetBitFromName(specialAttributes, "Icon32", YesNoType.Yes, bits, 16);
                                break;
                            case "":
                                break;
                            default:
                                this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, iconSizeValue, "16", "32", "48"));
                                break;
                            }
                            //if (0 < iconSizeValue.Length)
                            //{
                            //    var iconsSizeType = Wix.Control.ParseIconSizeType(iconSizeValue);
                            //    switch (iconsSizeType)
                            //    {
                            //    case Wix.Control.IconSizeType.Item16:
                            //        this.Core.TrySetBitFromName(specialAttributes, "Icon16", YesNoType.Yes, bits, 16);
                            //        break;
                            //    case Wix.Control.IconSizeType.Item32:
                            //        this.Core.TrySetBitFromName(specialAttributes, "Icon32", YesNoType.Yes, bits, 16);
                            //        break;
                            //    case Wix.Control.IconSizeType.Item48:
                            //        this.Core.TrySetBitFromName(specialAttributes, "Icon16", YesNoType.Yes, bits, 16);
                            //        this.Core.TrySetBitFromName(specialAttributes, "Icon32", YesNoType.Yes, bits, 16);
                            //        break;
                            //    default:
                            //        this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, iconSizeValue, "16", "32", "48"));
                            //        break;
                            //    }
                            //}
                        }
                        else
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeValueWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, iconSizeValue, "Type"));
                        }
                        break;
                    case "Property":
                        property = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "TabSkip":
                        notTabbable = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Text":
                        text = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "ToolTip":
                        tooltip = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Width":
                        width = this.Core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
                        break;
                    case "X":
                        x = this.Core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
                        break;
                    case "Y":
                        y = this.Core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
                        break;
                    case "Disabled":
                        disabled = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Hidden":
                        hidden = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Sunken":
                        sunken = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Indirect":
                        indirect = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Integer":
                        integer = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "RightToLeft":
                        rightToLeft = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "RightAligned":
                        rightAligned = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "LeftScroll":
                        leftScroll = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    default:
                        var attribValue = this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        if (null == specialAttributes || !this.Core.TrySetBitFromName(specialAttributes, attrib.Name.LocalName, attribValue, bits, 16))
                        {
                            this.Core.UnexpectedAttribute(node, attrib);
                        }
                        break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            var attributes = this.Core.CreateIntegerFromBitArray(bits);

            //if (disabled)
            //{
            //    attributes |= WindowsInstallerConstants.MsidbControlAttributesEnabled; // bit will be inverted when stored
            //}

            if (null == height)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Height"));
            }

            if (null == width)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Width"));
            }

            if (null == x)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "X"));
            }

            if (null == y)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Y"));
            }

            if (null == controlId)
            {
                controlId = this.Core.CreateIdentifier("ctl", dialog, x, y, height, width);
            }

            if (isCancel)
            {
                cancelControl = controlId.Id;
            }

            if (isDefault)
            {
                defaultControl = controlId.Id;
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                    switch (child.Name.LocalName)
                    {
                    case "Binary":
                        this.ParseBinaryElement(child);
                        break;
                    case "ComboBox":
                        this.ParseControlGroupElement(child, TupleDefinitionType.ComboBox, "ListItem");
                        break;
                    case "Condition":
                        this.ParseConditionElement(child, node.Name.LocalName, controlId.Id, dialog);
                        break;
                    case "ListBox":
                        this.ParseControlGroupElement(child, TupleDefinitionType.ListBox, "ListItem");
                        break;
                    case "ListView":
                        this.ParseControlGroupElement(child, TupleDefinitionType.ListView, "ListItem");
                        break;
                    case "Property":
                        this.ParsePropertyElement(child);
                        break;
                    case "Publish":
                        this.ParsePublishElement(child, dialog ?? String.Empty, controlId.Id, ref publishOrder);
                        break;
                    case "RadioButtonGroup":
                        radioButtonsType = this.ParseRadioButtonGroupElement(child, property, radioButtonsType);
                        break;
                    case "Subscribe":
                        this.ParseSubscribeElement(child, dialog, controlId.Id);
                        break;
                    case "Text":
                        foreach (var attrib in child.Attributes())
                        {
                            if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                            {
                                switch (attrib.Name.LocalName)
                                {
                                case "SourceFile":
                                    sourceFile = this.Core.GetAttributeValue(childSourceLineNumbers, attrib);
                                    break;
                                default:
                                    this.Core.UnexpectedAttribute(child, attrib);
                                    break;
                                }
                            }
                            else
                            {
                                this.Core.ParseExtensionAttribute(child, attrib);
                            }
                        }

                        text = Common.GetInnerText(child);
                        if (!String.IsNullOrEmpty(text) && null != sourceFile)
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeWithInnerText(childSourceLineNumbers, child.Name.LocalName, "SourceFile"));
                        }
                        break;
                    default:
                        this.Core.UnexpectedElement(node, child);
                        break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            // If the radio buttons have icons, then we need to add the icon attribute.
            switch (radioButtonsType)
            {
            case RadioButtonType.Bitmap:
                attributes |= WindowsInstallerConstants.MsidbControlAttributesBitmap;
                break;
            case RadioButtonType.Icon:
                attributes |= WindowsInstallerConstants.MsidbControlAttributesIcon;
                break;
            case RadioButtonType.Text:
                // Text is the default so nothing needs to be added bits
                break;
            }

            // the logic for creating control rows is a little tricky because of the way tabable controls are set
            IntermediateTuple tuple = null;
            if (!this.Core.EncounteredError)
            {
                if ("CheckBox" == controlType)
                {
                    if (String.IsNullOrEmpty(property) && String.IsNullOrEmpty(checkBoxPropertyRef))
                    {
                        this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Property", "CheckBoxPropertyRef", true));
                    }
                    else if (!String.IsNullOrEmpty(property) && !String.IsNullOrEmpty(checkBoxPropertyRef))
                    {
                        this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Property", "CheckBoxPropertyRef"));
                    }
                    else if (!String.IsNullOrEmpty(property))
                    {
                        var checkBoxTuple = new CheckBoxTuple(sourceLineNumbers)
                        {
                            Property = property,
                            Value = checkboxValue
                        };

                        this.Core.AddTuple(checkBoxTuple);
                    }
                    else
                    {
                        this.Core.CreateSimpleReference(sourceLineNumbers, "CheckBox", checkBoxPropertyRef);
                    }
                }

                var id = new Identifier(controlId.Access, dialog, controlId.Id);

                if (TupleDefinitionType.BBControl == tupleType)
                {
                    var bbTuple = new BBControlTuple(sourceLineNumbers, id)
                    {
                        BillboardRef = dialog,
                        BBControl = controlId.Id,
                        Type = controlType,
                        Attributes = attributes,
                        Enabled = !disabled,
                        Indirect = indirect,
                        Integer = integer,
                        LeftScroll = leftScroll,
                        RightAligned = rightAligned,
                        RightToLeft = rightToLeft,
                        Sunken = sunken,
                        Visible = !hidden,
                        Text = text,
                        SourceFile = sourceFile
                    };

                    bbTuple.Set((int)BBControlTupleFields.X, x);
                    bbTuple.Set((int)BBControlTupleFields.Y, y);
                    bbTuple.Set((int)BBControlTupleFields.Width, width);
                    bbTuple.Set((int)BBControlTupleFields.Height, height);

                    this.Core.AddTuple(bbTuple);

                    tuple = bbTuple;
                }
                else
                {
                    var controlTuple = new ControlTuple(sourceLineNumbers, id)
                    {
                        DialogRef = dialog,
                        Control = controlId.Id,
                        Type = controlType,
                        Attributes = attributes,
                        Enabled = !disabled,
                        Indirect = indirect,
                        Integer = integer,
                        LeftScroll = leftScroll,
                        RightAligned = rightAligned,
                        RightToLeft = rightToLeft,
                        Sunken = sunken,
                        Visible = !hidden,
                        Property = !String.IsNullOrEmpty(property) ? property : checkBoxPropertyRef,
                        Text = text,
                        Help = (null == tooltip && null == help) ? null : String.Concat(tooltip, "|", help), // Separator is required, even if only one is non-null.};
                        SourceFile = sourceFile
                    };

                    controlTuple.Set((int)BBControlTupleFields.X, x);
                    controlTuple.Set((int)BBControlTupleFields.Y, y);
                    controlTuple.Set((int)BBControlTupleFields.Width, width);
                    controlTuple.Set((int)BBControlTupleFields.Height, height);

                    this.Core.AddTuple(controlTuple);

                    tuple = controlTuple;
                }
            }

            if (!notTabbable)
            {
                if (TupleDefinitionType.BBControl == tupleType)
                {
                    this.Core.Write(ErrorMessages.TabbableControlNotAllowedInBillboard(sourceLineNumbers, node.Name.LocalName, controlType));
                }

                if (null == firstControl)
                {
                    firstControl = controlId.Id;
                }

                if (null != lastTabTuple)
                {
                    lastTabTuple.Set(10, controlId.Id);
                }
                lastTabTuple = tuple;
            }

            // bitmap and icon controls contain a foreign key into the binary table in the text column;
            // add a reference if the identifier of the binary entry is known during compilation
            if (("Bitmap" == controlType || "Icon" == controlType) && Common.IsIdentifier(text))
            {
                this.Core.CreateSimpleReference(sourceLineNumbers, "Binary", text);
            }
        }

        /// <summary>
        /// Parses a publish control event element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="dialog">Identifier of parent dialog.</param>
        /// <param name="control">Identifier of parent control.</param>
        /// <param name="order">Relative order of controls.</param>
        private void ParsePublishElement(XElement node, string dialog, string control, ref int order)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string argument = null;
            string condition = null;
            string controlEvent = null;
            string property = null;

            // give this control event a unique ordering
            order++;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Control":
                        if (null != control)
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                        }
                        control = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "Dialog":
                        if (null != dialog)
                        {
                            this.Core.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                        }
                        dialog = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        this.Core.CreateSimpleReference(sourceLineNumbers, "Dialog", dialog);
                        break;
                    case "Event":
                        controlEvent = Compiler.UppercaseFirstChar(this.Core.GetAttributeValue(sourceLineNumbers, attrib));
                        break;
                    case "Order":
                        order = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, 2147483647);
                        break;
                    case "Property":
                        property = String.Concat("[", this.Core.GetAttributeValue(sourceLineNumbers, attrib), "]");
                        break;
                    case "Value":
                        argument = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(node, attrib);
                        break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            condition = this.Core.GetConditionInnerText(node);

            if (null == control)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Control"));
            }

            if (null == dialog)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Dialog"));
            }

            if (null == controlEvent && null == property) // need to specify at least one
            {
                this.Core.Write(ErrorMessages.ExpectedAttributes(sourceLineNumbers, node.Name.LocalName, "Event", "Property"));
            }
            else if (null != controlEvent && null != property) // cannot specify both
            {
                this.Core.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, node.Name.LocalName, "Event", "Property"));
            }

            if (null == argument)
            {
                if (null != controlEvent)
                {
                    this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Value", "Event"));
                }
                else if (null != property)
                {
                    // if this is setting a property to null, put a special value in the argument column
                    argument = "{}";
                }
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var tuple = new ControlEventTuple(sourceLineNumbers)
                {
                    DialogRef = dialog,
                    ControlRef = control,
                    Event = controlEvent ?? property,
                    Argument = argument,
                    Condition = condition,
                    Ordering = order
                };

                this.Core.AddTuple(tuple);
            }

            if ("DoAction" == controlEvent && null != argument)
            {
                // if we're not looking at a standard action or a formatted string then create a reference 
                // to the custom action.
                if (!WindowsInstallerStandard.IsStandardAction(argument) && !Common.ContainsProperty(argument))
                {
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", argument);
                }
            }

            // if we're referring to a dialog but not through a property, add it to the references
            if (("NewDialog" == controlEvent || "SpawnDialog" == controlEvent || "SpawnWaitDialog" == controlEvent || "SelectionBrowse" == controlEvent) && Common.IsIdentifier(argument))
            {
                this.Core.CreateSimpleReference(sourceLineNumbers, "Dialog", argument);
            }
        }

        /// <summary>
        /// Parses a control subscription element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="dialog">Identifier of dialog.</param>
        /// <param name="control">Identifier of control.</param>
        private void ParseSubscribeElement(XElement node, string dialog, string control)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string controlAttribute = null;
            string eventMapping = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Attribute":
                        controlAttribute = Compiler.UppercaseFirstChar(this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib));
                        break;
                    case "Event":
                        eventMapping = Compiler.UppercaseFirstChar(this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib));
                        break;
                    default:
                        this.Core.UnexpectedAttribute(node, attrib);
                        break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var tuple = new EventMappingTuple(sourceLineNumbers)
                {
                    DialogRef = dialog,
                    ControlRef = control,
                    Event = eventMapping,
                    Attribute = controlAttribute
                }; ;

                this.Core.AddTuple(tuple);
            }
        }
    }
}
