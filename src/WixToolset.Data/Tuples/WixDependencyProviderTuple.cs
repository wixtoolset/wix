// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixDependencyProvider = new IntermediateTupleDefinition(
            TupleDefinitionType.WixDependencyProvider.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixDependencyProviderTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDependencyProviderTupleFields.ProviderKey), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDependencyProviderTupleFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDependencyProviderTupleFields.DisplayName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDependencyProviderTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixDependencyProviderTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;
    using WixToolset.Data;

    public enum WixDependencyProviderTupleFields
    {
        ComponentRef,
        ProviderKey,
        Version,
        DisplayName,
        Attributes,
    }

    [Flags]
    public enum WixDependencyProviderAttributes
    {
        ProvidesAttributesBundle = 0x10000
    }

    public class WixDependencyProviderTuple : IntermediateTuple
    {
        public WixDependencyProviderTuple() : base(TupleDefinitions.WixDependencyProvider, null, null)
        {
        }

        public WixDependencyProviderTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixDependencyProvider, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixDependencyProviderTupleFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)WixDependencyProviderTupleFields.ComponentRef].AsString();
            set => this.Set((int)WixDependencyProviderTupleFields.ComponentRef, value);
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

        public WixDependencyProviderAttributes Attributes
        {
            get => (WixDependencyProviderAttributes)(int)this.Fields[(int)WixDependencyProviderTupleFields.Attributes];
            set => this.Set((int)WixDependencyProviderTupleFields.Attributes, (int)value);
        }

        public bool Bundle => (this.Attributes & WixDependencyProviderAttributes.ProvidesAttributesBundle) == WixDependencyProviderAttributes.ProvidesAttributesBundle;
    }
}
