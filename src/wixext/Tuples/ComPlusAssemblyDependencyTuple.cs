// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusAssemblyDependency = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusAssemblyDependency.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusAssemblyDependencySymbolFields.AssemblyRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusAssemblyDependencySymbolFields.RequiredAssemblyRef), IntermediateFieldType.String),
            },
            typeof(ComPlusAssemblyDependencySymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusAssemblyDependencySymbolFields
    {
        AssemblyRef,
        RequiredAssemblyRef,
    }

    public class ComPlusAssemblyDependencySymbol : IntermediateSymbol
    {
        public ComPlusAssemblyDependencySymbol() : base(ComPlusSymbolDefinitions.ComPlusAssemblyDependency, null, null)
        {
        }

        public ComPlusAssemblyDependencySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusAssemblyDependency, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusAssemblyDependencySymbolFields index] => this.Fields[(int)index];

        public string AssemblyRef
        {
            get => this.Fields[(int)ComPlusAssemblyDependencySymbolFields.AssemblyRef].AsString();
            set => this.Set((int)ComPlusAssemblyDependencySymbolFields.AssemblyRef, value);
        }

        public string RequiredAssemblyRef
        {
            get => this.Fields[(int)ComPlusAssemblyDependencySymbolFields.RequiredAssemblyRef].AsString();
            set => this.Set((int)ComPlusAssemblyDependencySymbolFields.RequiredAssemblyRef, value);
        }
    }
}