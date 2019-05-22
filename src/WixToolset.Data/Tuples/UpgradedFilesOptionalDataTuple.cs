// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition UpgradedFilesOptionalData = new IntermediateTupleDefinition(
            TupleDefinitionType.UpgradedFilesOptionalData,
            new[]
            {
                new IntermediateFieldDefinition(nameof(UpgradedFilesOptionalDataTupleFields.Upgraded), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradedFilesOptionalDataTupleFields.FTK), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradedFilesOptionalDataTupleFields.SymbolPaths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradedFilesOptionalDataTupleFields.AllowIgnoreOnPatchError), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(UpgradedFilesOptionalDataTupleFields.IncludeWholeFile), IntermediateFieldType.Bool),
            },
            typeof(UpgradedFilesOptionalDataTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum UpgradedFilesOptionalDataTupleFields
    {
        Upgraded,
        FTK,
        SymbolPaths,
        AllowIgnoreOnPatchError,
        IncludeWholeFile,
    }

    public class UpgradedFilesOptionalDataTuple : IntermediateTuple
    {
        public UpgradedFilesOptionalDataTuple() : base(TupleDefinitions.UpgradedFilesOptionalData, null, null)
        {
        }

        public UpgradedFilesOptionalDataTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.UpgradedFilesOptionalData, sourceLineNumber, id)
        {
        }

        public IntermediateField this[UpgradedFilesOptionalDataTupleFields index] => this.Fields[(int)index];

        public string Upgraded
        {
            get => (string)this.Fields[(int)UpgradedFilesOptionalDataTupleFields.Upgraded];
            set => this.Set((int)UpgradedFilesOptionalDataTupleFields.Upgraded, value);
        }

        public string FTK
        {
            get => (string)this.Fields[(int)UpgradedFilesOptionalDataTupleFields.FTK];
            set => this.Set((int)UpgradedFilesOptionalDataTupleFields.FTK, value);
        }

        public string SymbolPaths
        {
            get => (string)this.Fields[(int)UpgradedFilesOptionalDataTupleFields.SymbolPaths];
            set => this.Set((int)UpgradedFilesOptionalDataTupleFields.SymbolPaths, value);
        }

        public bool AllowIgnoreOnPatchError
        {
            get => (bool)this.Fields[(int)UpgradedFilesOptionalDataTupleFields.AllowIgnoreOnPatchError];
            set => this.Set((int)UpgradedFilesOptionalDataTupleFields.AllowIgnoreOnPatchError, value);
        }

        public bool IncludeWholeFile
        {
            get => (bool)this.Fields[(int)UpgradedFilesOptionalDataTupleFields.IncludeWholeFile];
            set => this.Set((int)UpgradedFilesOptionalDataTupleFields.IncludeWholeFile, value);
        }
    }
}
