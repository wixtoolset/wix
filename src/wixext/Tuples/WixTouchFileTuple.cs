// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Tuples;

    public static partial class UtilTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixTouchFile = new IntermediateTupleDefinition(
            UtilTupleDefinitionType.WixTouchFile.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixTouchFileTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixTouchFileTupleFields.Path), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixTouchFileTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixTouchFileTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public enum WixTouchFileTupleFields
    {
        ComponentRef,
        Path,
        Attributes,
    }

    public class WixTouchFileTuple : IntermediateTuple
    {
        public WixTouchFileTuple() : base(UtilTupleDefinitions.WixTouchFile, null, null)
        {
        }

        public WixTouchFileTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilTupleDefinitions.WixTouchFile, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixTouchFileTupleFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)WixTouchFileTupleFields.ComponentRef].AsString();
            set => this.Set((int)WixTouchFileTupleFields.ComponentRef, value);
        }

        public string Path
        {
            get => this.Fields[(int)WixTouchFileTupleFields.Path].AsString();
            set => this.Set((int)WixTouchFileTupleFields.Path, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)WixTouchFileTupleFields.Attributes].AsNumber();
            set => this.Set((int)WixTouchFileTupleFields.Attributes, value);
        }
    }
}