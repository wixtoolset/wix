// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Dependency
{
    using WixToolset.Data;
    using WixToolset.Dependency.Tuples;

    public static partial class DependencyTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixDependencyRef = new IntermediateTupleDefinition(
            DependencyTupleDefinitionType.WixDependencyRef.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixDependencyRefTupleFields.WixDependencyProviderRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDependencyRefTupleFields.WixDependencyRef), IntermediateFieldType.String),
            },
            typeof(WixDependencyRefTuple));
    }
}

namespace WixToolset.Dependency.Tuples
{
    using WixToolset.Data;

    public enum WixDependencyRefTupleFields
    {
        WixDependencyProviderRef,
        WixDependencyRef,
    }

    public class WixDependencyRefTuple : IntermediateTuple
    {
        public WixDependencyRefTuple() : base(DependencyTupleDefinitions.WixDependencyRef, null, null)
        {
        }

        public WixDependencyRefTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(DependencyTupleDefinitions.WixDependencyRef, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixDependencyRefTupleFields index] => this.Fields[(int)index];

        public string WixDependencyProviderRef
        {
            get => this.Fields[(int)WixDependencyRefTupleFields.WixDependencyProviderRef].AsString();
            set => this.Set((int)WixDependencyRefTupleFields.WixDependencyProviderRef, value);
        }

        public string WixDependencyRef
        {
            get => this.Fields[(int)WixDependencyRefTupleFields.WixDependencyRef].AsString();
            set => this.Set((int)WixDependencyRefTupleFields.WixDependencyRef, value);
        }
    }
}