// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition BBControl = new IntermediateSymbolDefinition(
            SymbolDefinitionType.BBControl,
            new[]
            {
                new IntermediateFieldDefinition(nameof(BBControlSymbolFields.BillboardRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(BBControlSymbolFields.BBControl), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(BBControlSymbolFields.Type), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(BBControlSymbolFields.X), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(BBControlSymbolFields.Y), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(BBControlSymbolFields.Width), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(BBControlSymbolFields.Height), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(BBControlSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(BBControlSymbolFields.Enabled), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(BBControlSymbolFields.Indirect), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(BBControlSymbolFields.Integer), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(BBControlSymbolFields.LeftScroll), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(BBControlSymbolFields.RightAligned), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(BBControlSymbolFields.RightToLeft), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(BBControlSymbolFields.Sunken), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(BBControlSymbolFields.Visible), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(BBControlSymbolFields.Text), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(BBControlSymbolFields.SourceFile), IntermediateFieldType.Path),
            },
            typeof(BBControlSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum BBControlSymbolFields
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

    public class BBControlSymbol : IntermediateSymbol
    {
        public BBControlSymbol() : base(SymbolDefinitions.BBControl, null, null)
        {
        }

        public BBControlSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.BBControl, sourceLineNumber, id)
        {
        }

        public IntermediateField this[BBControlSymbolFields index] => this.Fields[(int)index];

        public string BillboardRef
        {
            get => (string)this.Fields[(int)BBControlSymbolFields.BillboardRef];
            set => this.Set((int)BBControlSymbolFields.BillboardRef, value);
        }

        public string BBControl
        {
            get => (string)this.Fields[(int)BBControlSymbolFields.BBControl];
            set => this.Set((int)BBControlSymbolFields.BBControl, value);
        }

        public string Type
        {
            get => (string)this.Fields[(int)BBControlSymbolFields.Type];
            set => this.Set((int)BBControlSymbolFields.Type, value);
        }

        public int X
        {
            get => (int)this.Fields[(int)BBControlSymbolFields.X];
            set => this.Set((int)BBControlSymbolFields.X, value);
        }

        public int Y
        {
            get => (int)this.Fields[(int)BBControlSymbolFields.Y];
            set => this.Set((int)BBControlSymbolFields.Y, value);
        }

        public int Width
        {
            get => (int)this.Fields[(int)BBControlSymbolFields.Width].AsNumber();
            set => this.Set((int)BBControlSymbolFields.Width, value);
        }

        public int Height
        {
            get => (int)this.Fields[(int)BBControlSymbolFields.Height];
            set => this.Set((int)BBControlSymbolFields.Height, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)BBControlSymbolFields.Attributes].AsNumber();
            set => this.Set((int)BBControlSymbolFields.Attributes, value);
        }

        public bool Enabled
        {
            get => this.Fields[(int)BBControlSymbolFields.Enabled].AsBool();
            set => this.Set((int)BBControlSymbolFields.Enabled, value);
        }

        public bool Indirect
        {
            get => this.Fields[(int)BBControlSymbolFields.Indirect].AsBool();
            set => this.Set((int)BBControlSymbolFields.Indirect, value);
        }

        public bool Integer
        {
            get => this.Fields[(int)BBControlSymbolFields.Integer].AsBool();
            set => this.Set((int)BBControlSymbolFields.Integer, value);
        }

        public bool LeftScroll
        {
            get => this.Fields[(int)BBControlSymbolFields.LeftScroll].AsBool();
            set => this.Set((int)BBControlSymbolFields.LeftScroll, value);
        }

        public bool RightAligned
        {
            get => this.Fields[(int)BBControlSymbolFields.RightAligned].AsBool();
            set => this.Set((int)BBControlSymbolFields.RightAligned, value);
        }

        public bool RightToLeft
        {
            get => this.Fields[(int)BBControlSymbolFields.RightToLeft].AsBool();
            set => this.Set((int)BBControlSymbolFields.RightToLeft, value);
        }

        public bool Sunken
        {
            get => this.Fields[(int)BBControlSymbolFields.Sunken].AsBool();
            set => this.Set((int)BBControlSymbolFields.Sunken, value);
        }

        public bool Visible
        {
            get => this.Fields[(int)BBControlSymbolFields.Visible].AsBool();
            set => this.Set((int)BBControlSymbolFields.Visible, value);
        }

        public string Text
        {
            get => (string)this.Fields[(int)BBControlSymbolFields.Text];
            set => this.Set((int)BBControlSymbolFields.Text, value);
        }

        public IntermediateFieldPathValue SourceFile
        {
            get => this.Fields[(int)BBControlSymbolFields.SourceFile].AsPath();
            set => this.Set((int)BBControlSymbolFields.SourceFile, value);
        }
    }
}