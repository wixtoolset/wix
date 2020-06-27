// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Dependency
{
    using WixToolset.Data;
    using WixToolset.Dependency.Symbols;

    public static partial class DependencySymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixDependency = new IntermediateSymbolDefinition(
            DependencySymbolDefinitionType.WixDependency.ToString(),
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

namespace WixToolset.Dependency.Symbols
{
    using WixToolset.Data;

    public enum WixDependencySymbolFields
    {
        ProviderKey,
        MinVersion,
        MaxVersion,
        Attributes,
    }

    public class WixDependencySymbol : IntermediateSymbol
    {
        public WixDependencySymbol() : base(DependencySymbolDefinitions.WixDependency, null, null)
        {
        }

        public WixDependencySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(DependencySymbolDefinitions.WixDependency, sourceLineNumber, id)
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

        public int Attributes
        {
            get => this.Fields[(int)WixDependencySymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixDependencySymbolFields.Attributes, value);
        }
    }
}