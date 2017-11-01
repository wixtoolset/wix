// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Dialog = new IntermediateTupleDefinition(
            TupleDefinitionType.Dialog,
            new[]
            {
                new IntermediateFieldDefinition(nameof(DialogTupleFields.Dialog), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.HCentering), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.VCentering), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.Width), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.Height), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.Title), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.Control_First), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.Control_Default), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.Control_Cancel), IntermediateFieldType.String),
            },
            typeof(DialogTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum DialogTupleFields
    {
        Dialog,
        HCentering,
        VCentering,
        Width,
        Height,
        Attributes,
        Title,
        Control_First,
        Control_Default,
        Control_Cancel,
    }

    public class DialogTuple : IntermediateTuple
    {
        public DialogTuple() : base(TupleDefinitions.Dialog, null, null)
        {
        }

        public DialogTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Dialog, sourceLineNumber, id)
        {
        }

        public IntermediateField this[DialogTupleFields index] => this.Fields[(int)index];

        public string Dialog
        {
            get => (string)this.Fields[(int)DialogTupleFields.Dialog]?.Value;
            set => this.Set((int)DialogTupleFields.Dialog, value);
        }

        public int HCentering
        {
            get => (int)this.Fields[(int)DialogTupleFields.HCentering]?.Value;
            set => this.Set((int)DialogTupleFields.HCentering, value);
        }

        public int VCentering
        {
            get => (int)this.Fields[(int)DialogTupleFields.VCentering]?.Value;
            set => this.Set((int)DialogTupleFields.VCentering, value);
        }

        public int Width
        {
            get => (int)this.Fields[(int)DialogTupleFields.Width]?.Value;
            set => this.Set((int)DialogTupleFields.Width, value);
        }

        public int Height
        {
            get => (int)this.Fields[(int)DialogTupleFields.Height]?.Value;
            set => this.Set((int)DialogTupleFields.Height, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)DialogTupleFields.Attributes]?.Value;
            set => this.Set((int)DialogTupleFields.Attributes, value);
        }

        public string Title
        {
            get => (string)this.Fields[(int)DialogTupleFields.Title]?.Value;
            set => this.Set((int)DialogTupleFields.Title, value);
        }

        public string Control_First
        {
            get => (string)this.Fields[(int)DialogTupleFields.Control_First]?.Value;
            set => this.Set((int)DialogTupleFields.Control_First, value);
        }

        public string Control_Default
        {
            get => (string)this.Fields[(int)DialogTupleFields.Control_Default]?.Value;
            set => this.Set((int)DialogTupleFields.Control_Default, value);
        }

        public string Control_Cancel
        {
            get => (string)this.Fields[(int)DialogTupleFields.Control_Cancel]?.Value;
            set => this.Set((int)DialogTupleFields.Control_Cancel, value);
        }
    }
}