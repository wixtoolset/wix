// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Tuples;

    public static partial class UtilTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixFormatFiles = new IntermediateTupleDefinition(
            UtilTupleDefinitionType.WixFormatFiles.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixFormatFilesTupleFields.BinaryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFormatFilesTupleFields.FileRef), IntermediateFieldType.String),
            },
            typeof(WixFormatFilesTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public enum WixFormatFilesTupleFields
    {
        BinaryRef,
        FileRef,
    }

    public class WixFormatFilesTuple : IntermediateTuple
    {
        public WixFormatFilesTuple() : base(UtilTupleDefinitions.WixFormatFiles, null, null)
        {
        }

        public WixFormatFilesTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilTupleDefinitions.WixFormatFiles, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixFormatFilesTupleFields index] => this.Fields[(int)index];

        public string BinaryRef
        {
            get => this.Fields[(int)WixFormatFilesTupleFields.BinaryRef].AsString();
            set => this.Set((int)WixFormatFilesTupleFields.BinaryRef, value);
        }

        public string FileRef
        {
            get => this.Fields[(int)WixFormatFilesTupleFields.FileRef].AsString();
            set => this.Set((int)WixFormatFilesTupleFields.FileRef, value);
        }
    }
}