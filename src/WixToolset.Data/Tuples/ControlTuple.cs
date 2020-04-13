// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Control = new IntermediateTupleDefinition(
            TupleDefinitionType.Control,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ControlTupleFields.DialogRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Control), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Type), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.X), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Y), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Width), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Height), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Enabled), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Indirect), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Integer), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.LeftScroll), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.RightAligned), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.RightToLeft), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Sunken), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Visible), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Property), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Text), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.NextControlRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.Help), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.TrackDiskSpace), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(ControlTupleFields.SourceFile), IntermediateFieldType.Path),
            },
            typeof(ControlTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ControlTupleFields
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

    public class ControlTuple : IntermediateTuple
    {
        public ControlTuple() : base(TupleDefinitions.Control, null, null)
        {
        }

        public ControlTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Control, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ControlTupleFields index] => this.Fields[(int)index];

        public string DialogRef
        {
            get => (string)this.Fields[(int)ControlTupleFields.DialogRef];
            set => this.Set((int)ControlTupleFields.DialogRef, value);
        }

        public string Control
        {
            get => (string)this.Fields[(int)ControlTupleFields.Control];
            set => this.Set((int)ControlTupleFields.Control, value);
        }

        public string Type
        {
            get => (string)this.Fields[(int)ControlTupleFields.Type];
            set => this.Set((int)ControlTupleFields.Type, value);
        }

        public int X
        {
            get => (int)this.Fields[(int)ControlTupleFields.X];
            set => this.Set((int)ControlTupleFields.X, value);
        }

        public int Y
        {
            get => (int)this.Fields[(int)ControlTupleFields.Y];
            set => this.Set((int)ControlTupleFields.Y, value);
        }

        public int Width
        {
            get => (int)this.Fields[(int)ControlTupleFields.Width];
            set => this.Set((int)ControlTupleFields.Width, value);
        }

        public int Height
        {
            get => (int)this.Fields[(int)ControlTupleFields.Height];
            set => this.Set((int)ControlTupleFields.Height, value);
        }

        public int? Attributes
        {
            get => (int?)this.Fields[(int)ControlTupleFields.Attributes];
            set => this.Set((int)ControlTupleFields.Attributes, value);
        }

        public bool Enabled
        {
            get => this.Fields[(int)ControlTupleFields.Enabled].AsBool();
            set => this.Set((int)ControlTupleFields.Enabled, value);
        }

        public bool Indirect
        {
            get => this.Fields[(int)ControlTupleFields.Indirect].AsBool();
            set => this.Set((int)ControlTupleFields.Indirect, value);
        }

        public bool Integer
        {
            get => this.Fields[(int)ControlTupleFields.Integer].AsBool();
            set => this.Set((int)ControlTupleFields.Integer, value);
        }
        /*
        /// <summary>PictureButton control</summary>
        public bool Bitmap
        {
            get => this.Fields[(int)ControlTupleFields.Bitmap].AsBool();
            set => this.Set((int)ControlTupleFields.Bitmap, value);
        }

        /// <summary>RadioButton control</summary>
        public bool Border
        {
            get => this.Fields[(int)ControlTupleFields.Border].AsBool();
            set => this.Set((int)ControlTupleFields.Border, value);
        }

        /// <summary>ListBox and ComboBox control</summary>
        public bool ComboList
        {
            get => this.Fields[(int)ControlTupleFields.ComboList].AsBool();
            set => this.Set((int)ControlTupleFields.ComboList, value);
        }

        /// <summary>PushButton control</summary>
        public bool ElevationShield
        {
            get => this.Fields[(int)ControlTupleFields.ElevationShield].AsBool();
            set => this.Set((int)ControlTupleFields.ElevationShield, value);
        }

        /// <summary>PictureButton control</summary>
        public bool FixedSize
        {
            get => this.Fields[(int)ControlTupleFields.FixedSize].AsBool();
            set => this.Set((int)ControlTupleFields.FixedSize, value);
        }

        /// <summary>PictureButton control</summary>
        public bool Icon
        {
            get => this.Fields[(int)ControlTupleFields.Icon].AsBool();
            set => this.Set((int)ControlTupleFields.Icon, value);
        }

        /// <summary>PictureButton control</summary>
        public bool Icon16
        {
            get => this.Fields[(int)ControlTupleFields.Icon16].AsBool();
            set => this.Set((int)ControlTupleFields.Icon16, value);
        }

        /// <summary>PictureButton control</summary>
        public bool Icon32
        {
            get => this.Fields[(int)ControlTupleFields.Icon32].AsBool();
            set => this.Set((int)ControlTupleFields.Icon32, value);
        }

        /// <summary>PictureButton control</summary>
        public bool Icon48
        {
            get => this.Fields[(int)ControlTupleFields.Icon48].AsBool();
            set => this.Set((int)ControlTupleFields.Icon48, value);
        }
        */
        public bool LeftScroll
        {
            get => this.Fields[(int)ControlTupleFields.LeftScroll].AsBool();
            set => this.Set((int)ControlTupleFields.LeftScroll, value);
        }
        /*
        /// <summary>PictureButton control</summary>
        public bool PushLike
        {
            get => this.Fields[(int)ControlTupleFields.PushLike].AsBool();
            set => this.Set((int)ControlTupleFields.PushLike, value);
        }

        /// <summary>Edit control</summary>
        public bool Mulitline
        {
            get => this.Fields[(int)ControlTupleFields.Mulitline].AsBool();
            set => this.Set((int)ControlTupleFields.Mulitline, value);
        }
        */
        public bool RightAligned
        {
            get => this.Fields[(int)ControlTupleFields.RightAligned].AsBool();
            set => this.Set((int)ControlTupleFields.RightAligned, value);
        }

        public bool RightToLeft
        {
            get => this.Fields[(int)ControlTupleFields.RightToLeft].AsBool();
            set => this.Set((int)ControlTupleFields.RightToLeft, value);
        }
        /*
        /// <summary>VolumeCostList control</summary>
        public bool ShowRollbackCost
        {
            get => this.Fields[(int)ControlTupleFields.ShowRollbackCost].AsBool();
            set => this.Set((int)ControlTupleFields.ShowRollbackCost, value);
        }

        /// <summary>ListBox and ComboBox control</summary>
        public bool Sorted
        {
            get => this.Fields[(int)ControlTupleFields.Sorted].AsBool();
            set => this.Set((int)ControlTupleFields.Sorted, value);
        }
        */
        public bool Sunken
        {
            get => this.Fields[(int)ControlTupleFields.Sunken].AsBool();
            set => this.Set((int)ControlTupleFields.Sunken, value);
        }

        public bool Visible
        {
            get => this.Fields[(int)ControlTupleFields.Visible].AsBool();
            set => this.Set((int)ControlTupleFields.Visible, value);
        }

        public string Property
        {
            get => (string)this.Fields[(int)ControlTupleFields.Property];
            set => this.Set((int)ControlTupleFields.Property, value);
        }

        public string Text
        {
            get => (string)this.Fields[(int)ControlTupleFields.Text];
            set => this.Set((int)ControlTupleFields.Text, value);
        }

        public string NextControlRef
        {
            get => (string)this.Fields[(int)ControlTupleFields.NextControlRef];
            set => this.Set((int)ControlTupleFields.NextControlRef, value);
        }

        public string Help
        {
            get => (string)this.Fields[(int)ControlTupleFields.Help];
            set => this.Set((int)ControlTupleFields.Help, value);
        }

        public bool TrackDiskSpace
        {
            get => this.Fields[(int)ControlTupleFields.TrackDiskSpace].AsBool();
            set => this.Set((int)ControlTupleFields.TrackDiskSpace, value);
        }

        public string SourceFile
        {
            get => (string)this.Fields[(int)ControlTupleFields.SourceFile];
            set => this.Set((int)ControlTupleFields.SourceFile, value);
        }
    }
}