// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Class = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Class,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ClassSymbolFields.CLSID), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ClassSymbolFields.Context), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ClassSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ClassSymbolFields.DefaultProgIdRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ClassSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ClassSymbolFields.AppIdRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ClassSymbolFields.FileTypeMask), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ClassSymbolFields.IconRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ClassSymbolFields.IconIndex), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ClassSymbolFields.DefInprocHandler), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ClassSymbolFields.Argument), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ClassSymbolFields.FeatureRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ClassSymbolFields.RelativePath), IntermediateFieldType.Bool),
            },
            typeof(ClassSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ClassSymbolFields
    {
        CLSID,
        Context,
        ComponentRef,
        DefaultProgIdRef,
        Description,
        AppIdRef,
        FileTypeMask,
        IconRef,
        IconIndex,
        DefInprocHandler,
        Argument,
        FeatureRef,
        RelativePath,
    }

    public class ClassSymbol : IntermediateSymbol
    {
        public ClassSymbol() : base(SymbolDefinitions.Class, null, null)
        {
        }

        public ClassSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Class, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ClassSymbolFields index] => this.Fields[(int)index];

        public string CLSID
        {
            get => (string)this.Fields[(int)ClassSymbolFields.CLSID];
            set => this.Set((int)ClassSymbolFields.CLSID, value);
        }

        public string Context
        {
            get => (string)this.Fields[(int)ClassSymbolFields.Context];
            set => this.Set((int)ClassSymbolFields.Context, value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)ClassSymbolFields.ComponentRef];
            set => this.Set((int)ClassSymbolFields.ComponentRef, value);
        }

        public string DefaultProgIdRef
        {
            get => (string)this.Fields[(int)ClassSymbolFields.DefaultProgIdRef];
            set => this.Set((int)ClassSymbolFields.DefaultProgIdRef, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ClassSymbolFields.Description];
            set => this.Set((int)ClassSymbolFields.Description, value);
        }

        public string AppIdRef
        {
            get => (string)this.Fields[(int)ClassSymbolFields.AppIdRef];
            set => this.Set((int)ClassSymbolFields.AppIdRef, value);
        }

        public string FileTypeMask
        {
            get => (string)this.Fields[(int)ClassSymbolFields.FileTypeMask];
            set => this.Set((int)ClassSymbolFields.FileTypeMask, value);
        }

        public string IconRef
        {
            get => (string)this.Fields[(int)ClassSymbolFields.IconRef];
            set => this.Set((int)ClassSymbolFields.IconRef, value);
        }

        public int? IconIndex
        {
            get => (int?)this.Fields[(int)ClassSymbolFields.IconIndex];
            set => this.Set((int)ClassSymbolFields.IconIndex, value);
        }

        public string DefInprocHandler
        {
            get => (string)this.Fields[(int)ClassSymbolFields.DefInprocHandler];
            set => this.Set((int)ClassSymbolFields.DefInprocHandler, value);
        }

        public string Argument
        {
            get => (string)this.Fields[(int)ClassSymbolFields.Argument];
            set => this.Set((int)ClassSymbolFields.Argument, value);
        }

        public string FeatureRef
        {
            get => (string)this.Fields[(int)ClassSymbolFields.FeatureRef];
            set => this.Set((int)ClassSymbolFields.FeatureRef, value);
        }

        public bool RelativePath
        {
            get => this.Fields[(int)ClassSymbolFields.RelativePath].AsBool();
            set => this.Set((int)ClassSymbolFields.RelativePath, value);
        }
    }
}