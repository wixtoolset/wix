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
                new IntermediateFieldDefinition(nameof(WixFormatFilesTupleFields.Binary_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFormatFilesTupleFields.File_), IntermediateFieldType.String),
            },
            typeof(WixFormatFilesTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public enum WixFormatFilesTupleFields
    {
        Binary_,
        File_,
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

        public string Binary_
        {
            get => this.Fields[(int)WixFormatFilesTupleFields.Binary_].AsString();
            set => this.Set((int)WixFormatFilesTupleFields.Binary_, value);
        }

        public string File_
        {
            get => this.Fields[(int)WixFormatFilesTupleFields.File_].AsString();
            set => this.Set((int)WixFormatFilesTupleFields.File_, value);
        }
    }
}