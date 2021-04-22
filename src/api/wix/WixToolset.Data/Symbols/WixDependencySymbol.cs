// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixDependency = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixDependency,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixDependencySymbolFields.ProviderKey), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDependencySymbolFields.MinVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDependencySymbolFields.MaxVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDependencySymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixDependencySymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixDependencySymbolFields
    {
        ProviderKey,
        MinVersion,
        MaxVersion,
        Attributes,
    }

    [Flags]
    public enum WixDependencySymbolAttributes : int
    {
        None = 0x0,
        RequiresAttributesMinVersionInclusive = 0x100,
        RequiresAttributesMaxVersionInclusive = 0x200,
    }

    public class WixDependencySymbol : IntermediateSymbol
    {
        public WixDependencySymbol() : base(SymbolDefinitions.WixDependency, null, null)
        {
        }

        public WixDependencySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixDependency, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixDependencySymbolFields index] => this.Fields[(int)index];

        public string ProviderKey
        {
            get => this.Fields[(int)WixDependencySymbolFields.ProviderKey].AsString();
            set => this.Set((int)WixDependencySymbolFields.ProviderKey, value);
        }

        public string MinVersion
        {
            get => this.Fields[(int)WixDependencySymbolFields.MinVersion].AsString();
            set => this.Set((int)WixDependencySymbolFields.MinVersion, value);
        }

        public string MaxVersion
        {
            get => this.Fields[(int)WixDependencySymbolFields.MaxVersion].AsString();
            set => this.Set((int)WixDependencySymbolFields.MaxVersion, value);
        }

        public WixDependencySymbolAttributes Attributes
        {
            get => (WixDependencySymbolAttributes)this.Fields[(int)WixDependencySymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixDependencySymbolFields.Attributes, (int)value);
        }

        public bool RequiresAttributesMinVersionInclusive => (this.Attributes & WixDependencySymbolAttributes.RequiresAttributesMinVersionInclusive) == WixDependencySymbolAttributes.RequiresAttributesMinVersionInclusive;

        public bool RequiresAttributesMaxVersionInclusive => (this.Attributes & WixDependencySymbolAttributes.RequiresAttributesMaxVersionInclusive) == WixDependencySymbolAttributes.RequiresAttributesMaxVersionInclusive;
    }
}
