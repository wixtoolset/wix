// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Tuples;

    public static partial class IisTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition IIsWebDir = new IntermediateTupleDefinition(
            IisTupleDefinitionType.IIsWebDir.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebDirTupleFields.WebDir), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebDirTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebDirTupleFields.Web_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebDirTupleFields.Path), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebDirTupleFields.DirProperties_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebDirTupleFields.Application_), IntermediateFieldType.String),
            },
            typeof(IIsWebDirTuple));
    }
}

namespace WixToolset.Iis.Tuples
{
    using WixToolset.Data;

    public enum IIsWebDirTupleFields
    {
        WebDir,
        Component_,
        Web_,
        Path,
        DirProperties_,
        Application_,
    }

    public class IIsWebDirTuple : IntermediateTuple
    {
        public IIsWebDirTuple() : base(IisTupleDefinitions.IIsWebDir, null, null)
        {
        }

        public IIsWebDirTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisTupleDefinitions.IIsWebDir, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebDirTupleFields index] => this.Fields[(int)index];

        public string WebDir
        {
            get => this.Fields[(int)IIsWebDirTupleFields.WebDir].AsString();
            set => this.Set((int)IIsWebDirTupleFields.WebDir, value);
        }

        public string Component_
        {
            get => this.Fields[(int)IIsWebDirTupleFields.Component_].AsString();
            set => this.Set((int)IIsWebDirTupleFields.Component_, value);
        }

        public string Web_
        {
            get => this.Fields[(int)IIsWebDirTupleFields.Web_].AsString();
            set => this.Set((int)IIsWebDirTupleFields.Web_, value);
        }

        public string Path
        {
            get => this.Fields[(int)IIsWebDirTupleFields.Path].AsString();
            set => this.Set((int)IIsWebDirTupleFields.Path, value);
        }

        public string DirProperties_
        {
            get => this.Fields[(int)IIsWebDirTupleFields.DirProperties_].AsString();
            set => this.Set((int)IIsWebDirTupleFields.DirProperties_, value);
        }

        public string Application_
        {
            get => this.Fields[(int)IIsWebDirTupleFields.Application_].AsString();
            set => this.Set((int)IIsWebDirTupleFields.Application_, value);
        }
    }
}