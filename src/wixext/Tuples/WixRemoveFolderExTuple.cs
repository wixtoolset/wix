// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Tuples;

    public static partial class UtilTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixRemoveFolderEx = new IntermediateTupleDefinition(
            UtilTupleDefinitionType.WixRemoveFolderEx.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixRemoveFolderExTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixRemoveFolderExTupleFields.Property), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixRemoveFolderExTupleFields.InstallMode), IntermediateFieldType.Number),
            },
            typeof(WixRemoveFolderExTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public enum WixRemoveFolderExTupleFields
    {
        ComponentRef,
        Property,
        InstallMode,
    }

    public class WixRemoveFolderExTuple : IntermediateTuple
    {
        public WixRemoveFolderExTuple() : base(UtilTupleDefinitions.WixRemoveFolderEx, null, null)
        {
        }

        public WixRemoveFolderExTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilTupleDefinitions.WixRemoveFolderEx, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixRemoveFolderExTupleFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)WixRemoveFolderExTupleFields.ComponentRef].AsString();
            set => this.Set((int)WixRemoveFolderExTupleFields.ComponentRef, value);
        }

        public string Property
        {
            get => this.Fields[(int)WixRemoveFolderExTupleFields.Property].AsString();
            set => this.Set((int)WixRemoveFolderExTupleFields.Property, value);
        }

        public int InstallMode
        {
            get => this.Fields[(int)WixRemoveFolderExTupleFields.InstallMode].AsNumber();
            set => this.Set((int)WixRemoveFolderExTupleFields.InstallMode, value);
        }
    }
}