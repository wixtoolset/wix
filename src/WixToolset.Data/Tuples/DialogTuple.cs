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
                new IntermediateFieldDefinition(nameof(DialogTupleFields.HCentering), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.VCentering), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.Width), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.Height), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.CustomPalette), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.ErrorDialog), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.Visible), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.Modal), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.KeepModeless), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.LeftScroll), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.Minimize), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.RightAligned), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.RightToLeft), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.SystemModal), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(DialogTupleFields.TrackDiskSpace), IntermediateFieldType.Bool),
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
        HCentering,
        VCentering,
        Width,
        Height,
        CustomPalette,
        ErrorDialog,
        Visible,
        Modal,
        KeepModeless,
        LeftScroll,
        Minimize,
        RightAligned,
        RightToLeft,
        SystemModal,
        TrackDiskSpace,
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

        public bool CustomPalette
        {
            get => this.Fields[(int)DialogTupleFields.CustomPalette].AsBool();
            set => this.Set((int)DialogTupleFields.CustomPalette, value);
        }

        public bool ErrorDialog
        {
            get => this.Fields[(int)DialogTupleFields.ErrorDialog].AsBool();
            set => this.Set((int)DialogTupleFields.ErrorDialog, value);
        }

        public bool Visible
        {
            get => this.Fields[(int)DialogTupleFields.Visible].AsBool();
            set => this.Set((int)DialogTupleFields.Visible, value);
        }

        public bool Modal
        {
            get => this.Fields[(int)DialogTupleFields.Modal].AsBool();
            set => this.Set((int)DialogTupleFields.Modal, value);
        }

        public bool KeepModeless
        {
            get => this.Fields[(int)DialogTupleFields.KeepModeless].AsBool();
            set => this.Set((int)DialogTupleFields.KeepModeless, value);
        }

        public bool LeftScroll
        {
            get => this.Fields[(int)DialogTupleFields.LeftScroll].AsBool();
            set => this.Set((int)DialogTupleFields.LeftScroll, value);
        }

        public bool Minimize
        {
            get => this.Fields[(int)DialogTupleFields.Minimize].AsBool();
            set => this.Set((int)DialogTupleFields.Minimize, value);
        }

        public bool RightAligned
        {
            get => this.Fields[(int)DialogTupleFields.RightAligned].AsBool();
            set => this.Set((int)DialogTupleFields.RightAligned, value);
        }

        public bool RightToLeft
        {
            get => this.Fields[(int)DialogTupleFields.RightToLeft].AsBool();
            set => this.Set((int)DialogTupleFields.RightToLeft, value);
        }

        public bool TrackDiskSpace
        {
            get => this.Fields[(int)DialogTupleFields.TrackDiskSpace].AsBool();
            set => this.Set((int)DialogTupleFields.TrackDiskSpace, value);
        }

        public bool SystemModal
        {
            get => this.Fields[(int)DialogTupleFields.SystemModal].AsBool();
            set => this.Set((int)DialogTupleFields.SystemModal, value);
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