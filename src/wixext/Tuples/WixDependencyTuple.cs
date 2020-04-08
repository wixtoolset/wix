// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Dependency
{
    using WixToolset.Data;
    using WixToolset.Dependency.Tuples;

    public static partial class DependencyTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixDependency = new IntermediateTupleDefinition(
            DependencyTupleDefinitionType.WixDependency.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixDependencyTupleFields.ProviderKey), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDependencyTupleFields.MinVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDependencyTupleFields.MaxVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDependencyTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixDependencyTuple));
    }
}

namespace WixToolset.Dependency.Tuples
{
    using WixToolset.Data;

    public enum WixDependencyTupleFields
    {
        ProviderKey,
        MinVersion,
        MaxVersion,
        Attributes,
    }

    public class WixDependencyTuple : IntermediateTuple
    {
        public WixDependencyTuple() : base(DependencyTupleDefinitions.WixDependency, null, null)
        {
        }

        public WixDependencyTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(DependencyTupleDefinitions.WixDependency, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixDependencyTupleFields index] => this.Fields[(int)index];

        public string ProviderKey
        {
            get => this.Fields[(int)WixDependencyTupleFields.ProviderKey].AsString();
            set => this.Set((int)WixDependencyTupleFields.ProviderKey, value);
        }

        public string MinVersion
        {
            get => this.Fields[(int)WixDependencyTupleFields.MinVersion].AsString();
            set => this.Set((int)WixDependencyTupleFields.MinVersion, value);
        }

        public string MaxVersion
        {
            get => this.Fields[(int)WixDependencyTupleFields.MaxVersion].AsString();
            set => this.Set((int)WixDependencyTupleFields.MaxVersion, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)WixDependencyTupleFields.Attributes].AsNumber();
            set => this.Set((int)WixDependencyTupleFields.Attributes, value);
        }
    }
}