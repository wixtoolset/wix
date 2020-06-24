// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Dialog = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Dialog,
            new[]
            {
                new IntermediateFieldDefinition(nameof(DialogSymbolFields.HCentering), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(DialogSymbolFields.VCentering), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(DialogSymbolFields.Width), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(DialogSymbolFields.Height), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(DialogSymbolFields.CustomPalette), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(DialogSymbolFields.ErrorDialog), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(DialogSymbolFields.Visible), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(DialogSymbolFields.Modal), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(DialogSymbolFields.KeepModeless), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(DialogSymbolFields.LeftScroll), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(DialogSymbolFields.Minimize), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(DialogSymbolFields.RightAligned), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(DialogSymbolFields.RightToLeft), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(DialogSymbolFields.SystemModal), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(DialogSymbolFields.TrackDiskSpace), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(DialogSymbolFields.Title), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DialogSymbolFields.FirstControlRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DialogSymbolFields.DefaultControlRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DialogSymbolFields.CancelControlRef), IntermediateFieldType.String),
            },
            typeof(DialogSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum DialogSymbolFields
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
        FirstControlRef,
        DefaultControlRef,
        CancelControlRef,
    }

    public class DialogSymbol : IntermediateSymbol
    {
        public DialogSymbol() : base(SymbolDefinitions.Dialog, null, null)
        {
        }

        public DialogSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Dialog, sourceLineNumber, id)
        {
        }

        public IntermediateField this[DialogSymbolFields index] => this.Fields[(int)index];

        public int HCentering
        {
            get => (int)this.Fields[(int)DialogSymbolFields.HCentering];
            set => this.Set((int)DialogSymbolFields.HCentering, value);
        }

        public int VCentering
        {
            get => (int)this.Fields[(int)DialogSymbolFields.VCentering];
            set => this.Set((int)DialogSymbolFields.VCentering, value);
        }

        public int Width
        {
            get => (int)this.Fields[(int)DialogSymbolFields.Width];
            set => this.Set((int)DialogSymbolFields.Width, value);
        }

        public int Height
        {
            get => (int)this.Fields[(int)DialogSymbolFields.Height];
            set => this.Set((int)DialogSymbolFields.Height, value);
        }

        public bool CustomPalette
        {
            get => this.Fields[(int)DialogSymbolFields.CustomPalette].AsBool();
            set => this.Set((int)DialogSymbolFields.CustomPalette, value);
        }

        public bool ErrorDialog
        {
            get => this.Fields[(int)DialogSymbolFields.ErrorDialog].AsBool();
            set => this.Set((int)DialogSymbolFields.ErrorDialog, value);
        }

        public bool Visible
        {
            get => this.Fields[(int)DialogSymbolFields.Visible].AsBool();
            set => this.Set((int)DialogSymbolFields.Visible, value);
        }

        public bool Modal
        {
            get => this.Fields[(int)DialogSymbolFields.Modal].AsBool();
            set => this.Set((int)DialogSymbolFields.Modal, value);
        }

        public bool KeepModeless
        {
            get => this.Fields[(int)DialogSymbolFields.KeepModeless].AsBool();
            set => this.Set((int)DialogSymbolFields.KeepModeless, value);
        }

        public bool LeftScroll
        {
            get => this.Fields[(int)DialogSymbolFields.LeftScroll].AsBool();
            set => this.Set((int)DialogSymbolFields.LeftScroll, value);
        }

        public bool Minimize
        {
            get => this.Fields[(int)DialogSymbolFields.Minimize].AsBool();
            set => this.Set((int)DialogSymbolFields.Minimize, value);
        }

        public bool RightAligned
        {
            get => this.Fields[(int)DialogSymbolFields.RightAligned].AsBool();
            set => this.Set((int)DialogSymbolFields.RightAligned, value);
        }

        public bool RightToLeft
        {
            get => this.Fields[(int)DialogSymbolFields.RightToLeft].AsBool();
            set => this.Set((int)DialogSymbolFields.RightToLeft, value);
        }

        public bool TrackDiskSpace
        {
            get => this.Fields[(int)DialogSymbolFields.TrackDiskSpace].AsBool();
            set => this.Set((int)DialogSymbolFields.TrackDiskSpace, value);
        }

        public bool SystemModal
        {
            get => this.Fields[(int)DialogSymbolFields.SystemModal].AsBool();
            set => this.Set((int)DialogSymbolFields.SystemModal, value);
        }

        public string Title
        {
            get => (string)this.Fields[(int)DialogSymbolFields.Title];
            set => this.Set((int)DialogSymbolFields.Title, value);
        }

        public string FirstControlRef
        {
            get => (string)this.Fields[(int)DialogSymbolFields.FirstControlRef];
            set => this.Set((int)DialogSymbolFields.FirstControlRef, value);
        }

        public string DefaultControlRef
        {
            get => (string)this.Fields[(int)DialogSymbolFields.DefaultControlRef];
            set => this.Set((int)DialogSymbolFields.DefaultControlRef, value);
        }

        public string CancelControlRef
        {
            get => (string)this.Fields[(int)DialogSymbolFields.CancelControlRef];
            set => this.Set((int)DialogSymbolFields.CancelControlRef, value);
        }
    }
}