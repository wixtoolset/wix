// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Tuples;

    public static partial class IisTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition IIsWebVirtualDir = new IntermediateTupleDefinition(
            IisTupleDefinitionType.IIsWebVirtualDir.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebVirtualDirTupleFields.VirtualDir), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebVirtualDirTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebVirtualDirTupleFields.Web_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebVirtualDirTupleFields.Alias), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebVirtualDirTupleFields.Directory_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebVirtualDirTupleFields.DirProperties_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebVirtualDirTupleFields.Application_), IntermediateFieldType.String),
            },
            typeof(IIsWebVirtualDirTuple));
    }
}

namespace WixToolset.Iis.Tuples
{
    using WixToolset.Data;

    public enum IIsWebVirtualDirTupleFields
    {
        VirtualDir,
        Component_,
        Web_,
        Alias,
        Directory_,
        DirProperties_,
        Application_,
    }

    public class IIsWebVirtualDirTuple : IntermediateTuple
    {
        public IIsWebVirtualDirTuple() : base(IisTupleDefinitions.IIsWebVirtualDir, null, null)
        {
        }

        public IIsWebVirtualDirTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisTupleDefinitions.IIsWebVirtualDir, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebVirtualDirTupleFields index] => this.Fields[(int)index];

        public string VirtualDir
        {
            get => this.Fields[(int)IIsWebVirtualDirTupleFields.VirtualDir].AsString();
            set => this.Set((int)IIsWebVirtualDirTupleFields.VirtualDir, value);
        }

        public string Component_
        {
            get => this.Fields[(int)IIsWebVirtualDirTupleFields.Component_].AsString();
            set => this.Set((int)IIsWebVirtualDirTupleFields.Component_, value);
        }

        public string Web_
        {
            get => this.Fields[(int)IIsWebVirtualDirTupleFields.Web_].AsString();
            set => this.Set((int)IIsWebVirtualDirTupleFields.Web_, value);
        }

        public string Alias
        {
            get => this.Fields[(int)IIsWebVirtualDirTupleFields.Alias].AsString();
            set => this.Set((int)IIsWebVirtualDirTupleFields.Alias, value);
        }

        public string Directory_
        {
            get => this.Fields[(int)IIsWebVirtualDirTupleFields.Directory_].AsString();
            set => this.Set((int)IIsWebVirtualDirTupleFields.Directory_, value);
        }

        public string DirProperties_
        {
            get => this.Fields[(int)IIsWebVirtualDirTupleFields.DirProperties_].AsString();
            set => this.Set((int)IIsWebVirtualDirTupleFields.DirProperties_, value);
        }

        public string Application_
        {
            get => this.Fields[(int)IIsWebVirtualDirTupleFields.Application_].AsString();
            set => this.Set((int)IIsWebVirtualDirTupleFields.Application_, value);
        }
    }
}