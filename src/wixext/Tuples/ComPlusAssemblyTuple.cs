// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusAssembly = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusAssembly.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusAssemblySymbolFields.ApplicationRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusAssemblySymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusAssemblySymbolFields.AssemblyName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusAssemblySymbolFields.DllPath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusAssemblySymbolFields.TlbPath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusAssemblySymbolFields.PSDllPath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusAssemblySymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(ComPlusAssemblySymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusAssemblySymbolFields
    {
        ApplicationRef,
        ComponentRef,
        AssemblyName,
        DllPath,
        TlbPath,
        PSDllPath,
        Attributes,
    }

    public class ComPlusAssemblySymbol : IntermediateSymbol
    {
        public ComPlusAssemblySymbol() : base(ComPlusSymbolDefinitions.ComPlusAssembly, null, null)
        {
        }

        public ComPlusAssemblySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusAssembly, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusAssemblySymbolFields index] => this.Fields[(int)index];

        public string ApplicationRef
        {
            get => this.Fields[(int)ComPlusAssemblySymbolFields.ApplicationRef].AsString();
            set => this.Set((int)ComPlusAssemblySymbolFields.ApplicationRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusAssemblySymbolFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusAssemblySymbolFields.ComponentRef, value);
        }

        public string AssemblyName
        {
            get => this.Fields[(int)ComPlusAssemblySymbolFields.AssemblyName].AsString();
            set => this.Set((int)ComPlusAssemblySymbolFields.AssemblyName, value);
        }

        public string DllPath
        {
            get => this.Fields[(int)ComPlusAssemblySymbolFields.DllPath].AsString();
            set => this.Set((int)ComPlusAssemblySymbolFields.DllPath, value);
        }

        public string TlbPath
        {
            get => this.Fields[(int)ComPlusAssemblySymbolFields.TlbPath].AsString();
            set => this.Set((int)ComPlusAssemblySymbolFields.TlbPath, value);
        }

        public string PSDllPath
        {
            get => this.Fields[(int)ComPlusAssemblySymbolFields.PSDllPath].AsString();
            set => this.Set((int)ComPlusAssemblySymbolFields.PSDllPath, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)ComPlusAssemblySymbolFields.Attributes].AsNumber();
            set => this.Set((int)ComPlusAssemblySymbolFields.Attributes, value);
        }
    }
}