// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Assembly = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Assembly,
            new[]
            {
                new IntermediateFieldDefinition(nameof(AssemblySymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(AssemblySymbolFields.FeatureRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(AssemblySymbolFields.ManifestFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(AssemblySymbolFields.ApplicationFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(AssemblySymbolFields.Type), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(AssemblySymbolFields.ProcessorArchitecture), IntermediateFieldType.String),
            },
            typeof(AssemblySymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum AssemblySymbolFields
    {
        ComponentRef,
        FeatureRef,
        ManifestFileRef,
        ApplicationFileRef,
        Type,
        ProcessorArchitecture,
    }

    public enum AssemblyType
    {
        /// <summary>File is not an assembly.</summary>
        NotAnAssembly,

        /// <summary>File is a Common Language Runtime Assembly.</summary>
        DotNetAssembly,

        /// <summary>File is Win32 SxS assembly.</summary>
        Win32Assembly,
    }

    public class AssemblySymbol : IntermediateSymbol
    {
        public AssemblySymbol() : base(SymbolDefinitions.Assembly, null, null)
        {
        }

        public AssemblySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Assembly, sourceLineNumber, id)
        {
        }

        public IntermediateField this[AssemblySymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => (string)this.Fields[(int)AssemblySymbolFields.ComponentRef];
            set => this.Set((int)AssemblySymbolFields.ComponentRef, value);
        }

        public string FeatureRef
        {
            get => (string)this.Fields[(int)AssemblySymbolFields.FeatureRef];
            set => this.Set((int)AssemblySymbolFields.FeatureRef, value);
        }

        public string ManifestFileRef
        {
            get => (string)this.Fields[(int)AssemblySymbolFields.ManifestFileRef];
            set => this.Set((int)AssemblySymbolFields.ManifestFileRef, value);
        }

        public string ApplicationFileRef
        {
            get => (string)this.Fields[(int)AssemblySymbolFields.ApplicationFileRef];
            set => this.Set((int)AssemblySymbolFields.ApplicationFileRef, value);
        }

        public AssemblyType Type
        {
            get => (AssemblyType)this.Fields[(int)AssemblySymbolFields.Type].AsNumber();
            set => this.Set((int)AssemblySymbolFields.Type, (int)value);
        }

        public string ProcessorArchitecture
        {
            get => (string)this.Fields[(int)AssemblySymbolFields.ProcessorArchitecture];
            set => this.Set((int)AssemblySymbolFields.ProcessorArchitecture, value);
        }
    }
}
