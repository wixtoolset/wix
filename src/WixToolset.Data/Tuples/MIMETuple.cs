// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition MIME = new IntermediateTupleDefinition(
            TupleDefinitionType.MIME,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MIMETupleFields.ContentType), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MIMETupleFields.Extension_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MIMETupleFields.CLSID), IntermediateFieldType.String),
            },
            typeof(MIMETuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MIMETupleFields
    {
        ContentType,
        Extension_,
        CLSID,
    }

    public class MIMETuple : IntermediateTuple
    {
        public MIMETuple() : base(TupleDefinitions.MIME, null, null)
        {
        }

        public MIMETuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.MIME, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MIMETupleFields index] => this.Fields[(int)index];

        public string ContentType
        {
            get => (string)this.Fields[(int)MIMETupleFields.ContentType];
            set => this.Set((int)MIMETupleFields.ContentType, value);
        }

        public string Extension_
        {
            get => (string)this.Fields[(int)MIMETupleFields.Extension_];
            set => this.Set((int)MIMETupleFields.Extension_, value);
        }

        public string CLSID
        {
            get => (string)this.Fields[(int)MIMETupleFields.CLSID];
            set => this.Set((int)MIMETupleFields.CLSID, value);
        }
    }
}