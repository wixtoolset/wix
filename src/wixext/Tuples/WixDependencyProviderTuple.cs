// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Dependency
{
    using WixToolset.Data;
    using WixToolset.Dependency.Tuples;

    public static partial class DependencyTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixDependencyProvider = new IntermediateTupleDefinition(
            DependencyTupleDefinitionType.WixDependencyProvider.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixDependencyProviderTupleFields.WixDependencyProvider), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDependencyProviderTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDependencyProviderTupleFields.ProviderKey), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDependencyProviderTupleFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDependencyProviderTupleFields.DisplayName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDependencyProviderTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixDependencyProviderTuple));
    }
}

namespace WixToolset.Dependency.Tuples
{
    using WixToolset.Data;

    public enum WixDependencyProviderTupleFields
    {
        WixDependencyProvider,
        Component_,
        ProviderKey,
        Version,
        DisplayName,
        Attributes,
    }

    public class WixDependencyProviderTuple : IntermediateTuple
    {
        public WixDependencyProviderTuple() : base(DependencyTupleDefinitions.WixDependencyProvider, null, null)
        {
        }

        public WixDependencyProviderTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(DependencyTupleDefinitions.WixDependencyProvider, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixDependencyProviderTupleFields index] => this.Fields[(int)index];

        public string WixDependencyProvider
        {
            get => this.Fields[(int)WixDependencyProviderTupleFields.WixDependencyProvider].AsString();
            set => this.Set((int)WixDependencyProviderTupleFields.WixDependencyProvider, value);
        }

        public string Component_
        {
            get => this.Fields[(int)WixDependencyProviderTupleFields.Component_].AsString();
            set => this.Set((int)WixDependencyProviderTupleFields.Component_, value);
        }

        public string ProviderKey
        {
            get => this.Fields[(int)WixDependencyProviderTupleFields.ProviderKey].AsString();
            set => this.Set((int)WixDependencyProviderTupleFields.ProviderKey, value);
        }

        public string Version
        {
            get => this.Fields[(int)WixDependencyProviderTupleFields.Version].AsString();
            set => this.Set((int)WixDependencyProviderTupleFields.Version, value);
        }

        public string DisplayName
        {
            get => this.Fields[(int)WixDependencyProviderTupleFields.DisplayName].AsString();
            set => this.Set((int)WixDependencyProviderTupleFields.DisplayName, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)WixDependencyProviderTupleFields.Attributes].AsNumber();
            set => this.Set((int)WixDependencyProviderTupleFields.Attributes, value);
        }
    }
}