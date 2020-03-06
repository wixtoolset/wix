// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Tuples;

    public static partial class UtilTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition XmlFile = new IntermediateTupleDefinition(
            UtilTupleDefinitionType.XmlFile.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(XmlFileTupleFields.File), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlFileTupleFields.ElementPath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlFileTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlFileTupleFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlFileTupleFields.Flags), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(XmlFileTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlFileTupleFields.Sequence), IntermediateFieldType.Number),
            },
            typeof(XmlFileTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public enum XmlFileTupleFields
    {
        File,
        ElementPath,
        Name,
        Value,
        Flags,
        ComponentRef,
        Sequence,
    }

    public class XmlFileTuple : IntermediateTuple
    {
        public XmlFileTuple() : base(UtilTupleDefinitions.XmlFile, null, null)
        {
        }

        public XmlFileTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilTupleDefinitions.XmlFile, sourceLineNumber, id)
        {
        }

        public IntermediateField this[XmlFileTupleFields index] => this.Fields[(int)index];

        public string File
        {
            get => this.Fields[(int)XmlFileTupleFields.File].AsString();
            set => this.Set((int)XmlFileTupleFields.File, value);
        }

        public string ElementPath
        {
            get => this.Fields[(int)XmlFileTupleFields.ElementPath].AsString();
            set => this.Set((int)XmlFileTupleFields.ElementPath, value);
        }

        public string Name
        {
            get => this.Fields[(int)XmlFileTupleFields.Name].AsString();
            set => this.Set((int)XmlFileTupleFields.Name, value);
        }

        public string Value
        {
            get => this.Fields[(int)XmlFileTupleFields.Value].AsString();
            set => this.Set((int)XmlFileTupleFields.Value, value);
        }

        public int Flags
        {
            get => this.Fields[(int)XmlFileTupleFields.Flags].AsNumber();
            set => this.Set((int)XmlFileTupleFields.Flags, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)XmlFileTupleFields.ComponentRef].AsString();
            set => this.Set((int)XmlFileTupleFields.ComponentRef, value);
        }

        public int Sequence
        {
            get => this.Fields[(int)XmlFileTupleFields.Sequence].AsNumber();
            set => this.Set((int)XmlFileTupleFields.Sequence, value);
        }
    }
}