// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusAssembly = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusAssembly.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusAssemblyTupleFields.Assembly), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusAssemblyTupleFields.Application_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusAssemblyTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusAssemblyTupleFields.AssemblyName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusAssemblyTupleFields.DllPath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusAssemblyTupleFields.TlbPath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusAssemblyTupleFields.PSDllPath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusAssemblyTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(ComPlusAssemblyTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusAssemblyTupleFields
    {
        Assembly,
        Application_,
        Component_,
        AssemblyName,
        DllPath,
        TlbPath,
        PSDllPath,
        Attributes,
    }

    public class ComPlusAssemblyTuple : IntermediateTuple
    {
        public ComPlusAssemblyTuple() : base(ComPlusTupleDefinitions.ComPlusAssembly, null, null)
        {
        }

        public ComPlusAssemblyTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusAssembly, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusAssemblyTupleFields index] => this.Fields[(int)index];

        public string Assembly
        {
            get => this.Fields[(int)ComPlusAssemblyTupleFields.Assembly].AsString();
            set => this.Set((int)ComPlusAssemblyTupleFields.Assembly, value);
        }

        public string Application_
        {
            get => this.Fields[(int)ComPlusAssemblyTupleFields.Application_].AsString();
            set => this.Set((int)ComPlusAssemblyTupleFields.Application_, value);
        }

        public string Component_
        {
            get => this.Fields[(int)ComPlusAssemblyTupleFields.Component_].AsString();
            set => this.Set((int)ComPlusAssemblyTupleFields.Component_, value);
        }

        public string AssemblyName
        {
            get => this.Fields[(int)ComPlusAssemblyTupleFields.AssemblyName].AsString();
            set => this.Set((int)ComPlusAssemblyTupleFields.AssemblyName, value);
        }

        public string DllPath
        {
            get => this.Fields[(int)ComPlusAssemblyTupleFields.DllPath].AsString();
            set => this.Set((int)ComPlusAssemblyTupleFields.DllPath, value);
        }

        public string TlbPath
        {
            get => this.Fields[(int)ComPlusAssemblyTupleFields.TlbPath].AsString();
            set => this.Set((int)ComPlusAssemblyTupleFields.TlbPath, value);
        }

        public string PSDllPath
        {
            get => this.Fields[(int)ComPlusAssemblyTupleFields.PSDllPath].AsString();
            set => this.Set((int)ComPlusAssemblyTupleFields.PSDllPath, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)ComPlusAssemblyTupleFields.Attributes].AsNumber();
            set => this.Set((int)ComPlusAssemblyTupleFields.Attributes, value);
        }
    }
}