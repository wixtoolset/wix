// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusAssemblyDependency = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusAssemblyDependency.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusAssemblyDependencyTupleFields.AssemblyRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusAssemblyDependencyTupleFields.RequiredAssemblyRef), IntermediateFieldType.String),
            },
            typeof(ComPlusAssemblyDependencyTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusAssemblyDependencyTupleFields
    {
        AssemblyRef,
        RequiredAssemblyRef,
    }

    public class ComPlusAssemblyDependencyTuple : IntermediateTuple
    {
        public ComPlusAssemblyDependencyTuple() : base(ComPlusTupleDefinitions.ComPlusAssemblyDependency, null, null)
        {
        }

        public ComPlusAssemblyDependencyTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusAssemblyDependency, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusAssemblyDependencyTupleFields index] => this.Fields[(int)index];

        public string AssemblyRef
        {
            get => this.Fields[(int)ComPlusAssemblyDependencyTupleFields.AssemblyRef].AsString();
            set => this.Set((int)ComPlusAssemblyDependencyTupleFields.AssemblyRef, value);
        }

        public string RequiredAssemblyRef
        {
            get => this.Fields[(int)ComPlusAssemblyDependencyTupleFields.RequiredAssemblyRef].AsString();
            set => this.Set((int)ComPlusAssemblyDependencyTupleFields.RequiredAssemblyRef, value);
        }
    }
}