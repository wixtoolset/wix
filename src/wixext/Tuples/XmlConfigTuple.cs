// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Tuples;

    public static partial class UtilTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition XmlConfig = new IntermediateTupleDefinition(
            UtilTupleDefinitionType.XmlConfig.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(XmlConfigTupleFields.File), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlConfigTupleFields.ElementPath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlConfigTupleFields.VerifyPath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlConfigTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlConfigTupleFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlConfigTupleFields.Flags), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(XmlConfigTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlConfigTupleFields.Sequence), IntermediateFieldType.Number),
            },
            typeof(XmlConfigTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public enum XmlConfigTupleFields
    {
        File,
        ElementPath,
        VerifyPath,
        Name,
        Value,
        Flags,
        ComponentRef,
        Sequence,
    }

    public class XmlConfigTuple : IntermediateTuple
    {
        public XmlConfigTuple() : base(UtilTupleDefinitions.XmlConfig, null, null)
        {
        }

        public XmlConfigTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilTupleDefinitions.XmlConfig, sourceLineNumber, id)
        {
        }

        public IntermediateField this[XmlConfigTupleFields index] => this.Fields[(int)index];

        public string File
        {
            get => this.Fields[(int)XmlConfigTupleFields.File].AsString();
            set => this.Set((int)XmlConfigTupleFields.File, value);
        }

        public string ElementPath
        {
            get => this.Fields[(int)XmlConfigTupleFields.ElementPath].AsString();
            set => this.Set((int)XmlConfigTupleFields.ElementPath, value);
        }

        public string VerifyPath
        {
            get => this.Fields[(int)XmlConfigTupleFields.VerifyPath].AsString();
            set => this.Set((int)XmlConfigTupleFields.VerifyPath, value);
        }

        public string Name
        {
            get => this.Fields[(int)XmlConfigTupleFields.Name].AsString();
            set => this.Set((int)XmlConfigTupleFields.Name, value);
        }

        public string Value
        {
            get => this.Fields[(int)XmlConfigTupleFields.Value].AsString();
            set => this.Set((int)XmlConfigTupleFields.Value, value);
        }

        public int Flags
        {
            get => this.Fields[(int)XmlConfigTupleFields.Flags].AsNumber();
            set => this.Set((int)XmlConfigTupleFields.Flags, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)XmlConfigTupleFields.ComponentRef].AsString();
            set => this.Set((int)XmlConfigTupleFields.ComponentRef, value);
        }

        public int Sequence
        {
            get => this.Fields[(int)XmlConfigTupleFields.Sequence].AsNumber();
            set => this.Set((int)XmlConfigTupleFields.Sequence, value);
        }
    }
}