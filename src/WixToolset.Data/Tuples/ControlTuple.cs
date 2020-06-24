// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Control = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Control,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ControlSymbolFields.DialogRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlSymbolFields.Control), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlSymbolFields.Type), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlSymbolFields.X), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ControlSymbolFields.Y), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ControlSymbolFields.Width), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ControlSymbolFields.Height), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ControlSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ControlSymbolFields.Enabled), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ControlSymbolFields.Indirect), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ControlSymbolFields.Integer), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ControlSymbolFields.LeftScroll), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ControlSymbolFields.RightAligned), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ControlSymbolFields.RightToLeft), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ControlSymbolFields.Sunken), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ControlSymbolFields.Visible), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ControlSymbolFields.Property), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlSymbolFields.Text), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlSymbolFields.NextControlRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlSymbolFields.Help), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlSymbolFields.TrackDiskSpace), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ControlSymbolFields.SourceFile), IntermediateFieldType.Path),
            },
            typeof(ControlSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ControlSymbolFields
    {
        DialogRef,
        Control,
        Type,
        X,
        Y,
        Width,
        Height,
        Attributes,
        Enabled,
        Indirect,
        Integer,
        LeftScroll,
        RightAligned,
        RightToLeft,
        Sunken,
        Visible,
        Property,
        Text,
        NextControlRef,
        Help,
        TrackDiskSpace,
        SourceFile,
    }

    public class ControlSymbol : IntermediateSymbol
    {
        public ControlSymbol() : base(SymbolDefinitions.Control, null, null)
        {
        }

        public ControlSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Control, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ControlSymbolFields index] => this.Fields[(int)index];

        public string DialogRef
        {
            get => (string)this.Fields[(int)ControlSymbolFields.DialogRef];
            set => this.Set((int)ControlSymbolFields.DialogRef, value);
        }

        public string Control
        {
            get => (string)this.Fields[(int)ControlSymbolFields.Control];
            set => this.Set((int)ControlSymbolFields.Control, value);
        }

        public string Type
        {
            get => (string)this.Fields[(int)ControlSymbolFields.Type];
            set => this.Set((int)ControlSymbolFields.Type, value);
        }

        public int X
        {
            get => (int)this.Fields[(int)ControlSymbolFields.X];
            set => this.Set((int)ControlSymbolFields.X, value);
        }

        public int Y
        {
            get => (int)this.Fields[(int)ControlSymbolFields.Y];
            set => this.Set((int)ControlSymbolFields.Y, value);
        }

        public int Width
        {
            get => (int)this.Fields[(int)ControlSymbolFields.Width];
            set => this.Set((int)ControlSymbolFields.Width, value);
        }

        public int Height
        {
            get => (int)this.Fields[(int)ControlSymbolFields.Height];
            set => this.Set((int)ControlSymbolFields.Height, value);
        }

        public int? Attributes
        {
            get => (int?)this.Fields[(int)ControlSymbolFields.Attributes];
            set => this.Set((int)ControlSymbolFields.Attributes, value);
        }

        public bool Enabled
        {
            get => this.Fields[(int)ControlSymbolFields.Enabled].AsBool();
            set => this.Set((int)ControlSymbolFields.Enabled, value);
        }

        public bool Indirect
        {
            get => this.Fields[(int)ControlSymbolFields.Indirect].AsBool();
            set => this.Set((int)ControlSymbolFields.Indirect, value);
        }

        public bool Integer
        {
            get => this.Fields[(int)ControlSymbolFields.Integer].AsBool();
            set => this.Set((int)ControlSymbolFields.Integer, value);
        }
        /*
        /// <summary>PictureButton control</summary>
        public bool Bitmap
        {
            get => this.Fields[(int)ControlSymbolFields.Bitmap].AsBool();
            set => this.Set((int)ControlSymbolFields.Bitmap, value);
        }

        /// <summary>RadioButton control</summary>
        public bool Border
        {
            get => this.Fields[(int)ControlSymbolFields.Border].AsBool();
            set => this.Set((int)ControlSymbolFields.Border, value);
        }

        /// <summary>ListBox and ComboBox control</summary>
        public bool ComboList
        {
            get => this.Fields[(int)ControlSymbolFields.ComboList].AsBool();
            set => this.Set((int)ControlSymbolFields.ComboList, value);
        }

        /// <summary>PushButton control</summary>
        public bool ElevationShield
        {
            get => this.Fields[(int)ControlSymbolFields.ElevationShield].AsBool();
            set => this.Set((int)ControlSymbolFields.ElevationShield, value);
        }

        /// <summary>PictureButton control</summary>
        public bool FixedSize
        {
            get => this.Fields[(int)ControlSymbolFields.FixedSize].AsBool();
            set => this.Set((int)ControlSymbolFields.FixedSize, value);
        }

        /// <summary>PictureButton control</summary>
        public bool Icon
        {
            get => this.Fields[(int)ControlSymbolFields.Icon].AsBool();
            set => this.Set((int)ControlSymbolFields.Icon, value);
        }

        /// <summary>PictureButton control</summary>
        public bool Icon16
        {
            get => this.Fields[(int)ControlSymbolFields.Icon16].AsBool();
            set => this.Set((int)ControlSymbolFields.Icon16, value);
        }

        /// <summary>PictureButton control</summary>
        public bool Icon32
        {
            get => this.Fields[(int)ControlSymbolFields.Icon32].AsBool();
            set => this.Set((int)ControlSymbolFields.Icon32, value);
        }

        /// <summary>PictureButton control</summary>
        public bool Icon48
        {
            get => this.Fields[(int)ControlSymbolFields.Icon48].AsBool();
            set => this.Set((int)ControlSymbolFields.Icon48, value);
        }
        */
        public bool LeftScroll
        {
            get => this.Fields[(int)ControlSymbolFields.LeftScroll].AsBool();
            set => this.Set((int)ControlSymbolFields.LeftScroll, value);
        }
        /*
        /// <summary>PictureButton control</summary>
        public bool PushLike
        {
            get => this.Fields[(int)ControlSymbolFields.PushLike].AsBool();
            set => this.Set((int)ControlSymbolFields.PushLike, value);
        }

        /// <summary>Edit control</summary>
        public bool Mulitline
        {
            get => this.Fields[(int)ControlSymbolFields.Mulitline].AsBool();
            set => this.Set((int)ControlSymbolFields.Mulitline, value);
        }
        */
        public bool RightAligned
        {
            get => this.Fields[(int)ControlSymbolFields.RightAligned].AsBool();
            set => this.Set((int)ControlSymbolFields.RightAligned, value);
        }

        public bool RightToLeft
        {
            get => this.Fields[(int)ControlSymbolFields.RightToLeft].AsBool();
            set => this.Set((int)ControlSymbolFields.RightToLeft, value);
        }
        /*
        /// <summary>VolumeCostList control</summary>
        public bool ShowRollbackCost
        {
            get => this.Fields[(int)ControlSymbolFields.ShowRollbackCost].AsBool();
            set => this.Set((int)ControlSymbolFields.ShowRollbackCost, value);
        }

        /// <summary>ListBox and ComboBox control</summary>
        public bool Sorted
        {
            get => this.Fields[(int)ControlSymbolFields.Sorted].AsBool();
            set => this.Set((int)ControlSymbolFields.Sorted, value);
        }
        */
        public bool Sunken
        {
            get => this.Fields[(int)ControlSymbolFields.Sunken].AsBool();
            set => this.Set((int)ControlSymbolFields.Sunken, value);
        }

        public bool Visible
        {
            get => this.Fields[(int)ControlSymbolFields.Visible].AsBool();
            set => this.Set((int)ControlSymbolFields.Visible, value);
        }

        public string Property
        {
            get => (string)this.Fields[(int)ControlSymbolFields.Property];
            set => this.Set((int)ControlSymbolFields.Property, value);
        }

        public string Text
        {
            get => (string)this.Fields[(int)ControlSymbolFields.Text];
            set => this.Set((int)ControlSymbolFields.Text, value);
        }

        public string NextControlRef
        {
            get => (string)this.Fields[(int)ControlSymbolFields.NextControlRef];
            set => this.Set((int)ControlSymbolFields.NextControlRef, value);
        }

        public string Help
        {
            get => (string)this.Fields[(int)ControlSymbolFields.Help];
            set => this.Set((int)ControlSymbolFields.Help, value);
        }

        public bool TrackDiskSpace
        {
            get => this.Fields[(int)ControlSymbolFields.TrackDiskSpace].AsBool();
            set => this.Set((int)ControlSymbolFields.TrackDiskSpace, value);
        }

        public IntermediateFieldPathValue SourceFile
        {
            get => this.Fields[(int)ControlSymbolFields.SourceFile].AsPath();
            set => this.Set((int)ControlSymbolFields.SourceFile, value);
        }
    }
}