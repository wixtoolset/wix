// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition UpgradedFilesToIgnore = new IntermediateTupleDefinition(
            TupleDefinitionType.UpgradedFilesToIgnore,
            new[]
            {
                new IntermediateFieldDefinition(nameof(UpgradedFilesToIgnoreTupleFields.Upgraded), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradedFilesToIgnoreTupleFields.FTK), IntermediateFieldType.String),
            },
            typeof(UpgradedFilesToIgnoreTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum UpgradedFilesToIgnoreTupleFields
    {
        Upgraded,
        FTK,
    }

    public class UpgradedFilesToIgnoreTuple : IntermediateTuple
    {
        public UpgradedFilesToIgnoreTuple() : base(TupleDefinitions.UpgradedFilesToIgnore, null, null)
        {
        }

        public UpgradedFilesToIgnoreTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.UpgradedFilesToIgnore, sourceLineNumber, id)
        {
        }

        public IntermediateField this[UpgradedFilesToIgnoreTupleFields index] => this.Fields[(int)index];

        public string Upgraded
        {
            get => (string)this.Fields[(int)UpgradedFilesToIgnoreTupleFields.Upgraded]?.Value;
            set => this.Set((int)UpgradedFilesToIgnoreTupleFields.Upgraded, value);
        }

        public string FTK
        {
            get => (string)this.Fields[(int)UpgradedFilesToIgnoreTupleFields.FTK]?.Value;
            set => this.Set((int)UpgradedFilesToIgnoreTupleFields.FTK, value);
        }
    }
}