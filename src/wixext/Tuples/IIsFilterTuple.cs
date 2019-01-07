// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Tuples;

    public static partial class IisTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition IIsFilter = new IntermediateTupleDefinition(
            IisTupleDefinitionType.IIsFilter.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsFilterTupleFields.Filter), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsFilterTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsFilterTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsFilterTupleFields.Path), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsFilterTupleFields.Web_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsFilterTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsFilterTupleFields.Flags), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsFilterTupleFields.LoadOrder), IntermediateFieldType.Number),
            },
            typeof(IIsFilterTuple));
    }
}

namespace WixToolset.Iis.Tuples
{
    using WixToolset.Data;

    public enum IIsFilterTupleFields
    {
        Filter,
        Name,
        Component_,
        Path,
        Web_,
        Description,
        Flags,
        LoadOrder,
    }

    public class IIsFilterTuple : IntermediateTuple
    {
        public IIsFilterTuple() : base(IisTupleDefinitions.IIsFilter, null, null)
        {
        }

        public IIsFilterTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisTupleDefinitions.IIsFilter, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsFilterTupleFields index] => this.Fields[(int)index];

        public string Filter
        {
            get => this.Fields[(int)IIsFilterTupleFields.Filter].AsString();
            set => this.Set((int)IIsFilterTupleFields.Filter, value);
        }

        public string Name
        {
            get => this.Fields[(int)IIsFilterTupleFields.Name].AsString();
            set => this.Set((int)IIsFilterTupleFields.Name, value);
        }

        public string Component_
        {
            get => this.Fields[(int)IIsFilterTupleFields.Component_].AsString();
            set => this.Set((int)IIsFilterTupleFields.Component_, value);
        }

        public string Path
        {
            get => this.Fields[(int)IIsFilterTupleFields.Path].AsString();
            set => this.Set((int)IIsFilterTupleFields.Path, value);
        }

        public string Web_
        {
            get => this.Fields[(int)IIsFilterTupleFields.Web_].AsString();
            set => this.Set((int)IIsFilterTupleFields.Web_, value);
        }

        public string Description
        {
            get => this.Fields[(int)IIsFilterTupleFields.Description].AsString();
            set => this.Set((int)IIsFilterTupleFields.Description, value);
        }

        public int Flags
        {
            get => this.Fields[(int)IIsFilterTupleFields.Flags].AsNumber();
            set => this.Set((int)IIsFilterTupleFields.Flags, value);
        }

        public int LoadOrder
        {
            get => this.Fields[(int)IIsFilterTupleFields.LoadOrder].AsNumber();
            set => this.Set((int)IIsFilterTupleFields.LoadOrder, value);
        }
    }
}