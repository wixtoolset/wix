// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition BBControl = new IntermediateTupleDefinition(
            TupleDefinitionType.BBControl,
            new[]
            {
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.BillboardRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.BBControl), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.Type), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.X), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.Y), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.Width), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.Height), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.Enabled), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.Indirect), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.Integer), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.LeftScroll), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.RightAligned), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.RightToLeft), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.Sunken), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.Visible), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.Text), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(BBControlTupleFields.SourceFile), IntermediateFieldType.Path),
            },
            typeof(BBControlTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum BBControlTupleFields
    {
        BillboardRef,
        BBControl,
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
        Text,
        SourceFile
    }

    public class BBControlTuple : IntermediateTuple
    {
        public BBControlTuple() : base(TupleDefinitions.BBControl, null, null)
        {
        }

        public BBControlTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.BBControl, sourceLineNumber, id)
        {
        }

        public IntermediateField this[BBControlTupleFields index] => this.Fields[(int)index];

        public string BillboardRef
        {
            get => (string)this.Fields[(int)BBControlTupleFields.BillboardRef];
            set => this.Set((int)BBControlTupleFields.BillboardRef, value);
        }

        public string BBControl
        {
            get => (string)this.Fields[(int)BBControlTupleFields.BBControl];
            set => this.Set((int)BBControlTupleFields.BBControl, value);
        }

        public string Type
        {
            get => (string)this.Fields[(int)BBControlTupleFields.Type];
            set => this.Set((int)BBControlTupleFields.Type, value);
        }

        public int X
        {
            get => (int)this.Fields[(int)BBControlTupleFields.X];
            set => this.Set((int)BBControlTupleFields.X, value);
        }

        public int Y
        {
            get => (int)this.Fields[(int)BBControlTupleFields.Y];
            set => this.Set((int)BBControlTupleFields.Y, value);
        }

        public int Width
        {
            get => (int)this.Fields[(int)BBControlTupleFields.Width].AsNumber();
            set => this.Set((int)BBControlTupleFields.Width, value);
        }

        public int Height
        {
            get => (int)this.Fields[(int)BBControlTupleFields.Height];
            set => this.Set((int)BBControlTupleFields.Height, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)BBControlTupleFields.Attributes].AsNumber();
            set => this.Set((int)BBControlTupleFields.Attributes, value);
        }

        public bool Enabled
        {
            get => this.Fields[(int)BBControlTupleFields.Enabled].AsBool();
            set => this.Set((int)BBControlTupleFields.Enabled, value);
        }

        public bool Indirect
        {
            get => this.Fields[(int)BBControlTupleFields.Indirect].AsBool();
            set => this.Set((int)BBControlTupleFields.Indirect, value);
        }

        public bool Integer
        {
            get => this.Fields[(int)BBControlTupleFields.Integer].AsBool();
            set => this.Set((int)BBControlTupleFields.Integer, value);
        }

        public bool LeftScroll
        {
            get => this.Fields[(int)BBControlTupleFields.LeftScroll].AsBool();
            set => this.Set((int)BBControlTupleFields.LeftScroll, value);
        }

        public bool RightAligned
        {
            get => this.Fields[(int)BBControlTupleFields.RightAligned].AsBool();
            set => this.Set((int)BBControlTupleFields.RightAligned, value);
        }

        public bool RightToLeft
        {
            get => this.Fields[(int)BBControlTupleFields.RightToLeft].AsBool();
            set => this.Set((int)BBControlTupleFields.RightToLeft, value);
        }

        public bool Sunken
        {
            get => this.Fields[(int)BBControlTupleFields.Sunken].AsBool();
            set => this.Set((int)BBControlTupleFields.Sunken, value);
        }

        public bool Visible
        {
            get => this.Fields[(int)BBControlTupleFields.Visible].AsBool();
            set => this.Set((int)BBControlTupleFields.Visible, value);
        }

        public string Text
        {
            get => (string)this.Fields[(int)BBControlTupleFields.Text];
            set => this.Set((int)BBControlTupleFields.Text, value);
        }

        public string SourceFile
        {
            get => (string)this.Fields[(int)BBControlTupleFields.SourceFile];
            set => this.Set((int)BBControlTupleFields.SourceFile, value);
        }
    }
}