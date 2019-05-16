// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition UpgradedFiles_OptionalData = new IntermediateTupleDefinition(
            TupleDefinitionType.UpgradedFiles_OptionalData,
            new[]
            {
                new IntermediateFieldDefinition(nameof(UpgradedFiles_OptionalDataTupleFields.Upgraded), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradedFiles_OptionalDataTupleFields.FTK), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradedFiles_OptionalDataTupleFields.SymbolPaths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradedFiles_OptionalDataTupleFields.AllowIgnoreOnPatchError), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(UpgradedFiles_OptionalDataTupleFields.IncludeWholeFile), IntermediateFieldType.Number),
            },
            typeof(UpgradedFiles_OptionalDataTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum UpgradedFiles_OptionalDataTupleFields
    {
        Upgraded,
        FTK,
        SymbolPaths,
        AllowIgnoreOnPatchError,
        IncludeWholeFile,
    }

    public class UpgradedFiles_OptionalDataTuple : IntermediateTuple
    {
        public UpgradedFiles_OptionalDataTuple() : base(TupleDefinitions.UpgradedFiles_OptionalData, null, null)
        {
        }

        public UpgradedFiles_OptionalDataTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.UpgradedFiles_OptionalData, sourceLineNumber, id)
        {
        }

        public IntermediateField this[UpgradedFiles_OptionalDataTupleFields index] => this.Fields[(int)index];

        public string Upgraded
        {
            get => (string)this.Fields[(int)UpgradedFiles_OptionalDataTupleFields.Upgraded];
            set => this.Set((int)UpgradedFiles_OptionalDataTupleFields.Upgraded, value);
        }

        public string FTK
        {
            get => (string)this.Fields[(int)UpgradedFiles_OptionalDataTupleFields.FTK];
            set => this.Set((int)UpgradedFiles_OptionalDataTupleFields.FTK, value);
        }

        public string SymbolPaths
        {
            get => (string)this.Fields[(int)UpgradedFiles_OptionalDataTupleFields.SymbolPaths];
            set => this.Set((int)UpgradedFiles_OptionalDataTupleFields.SymbolPaths, value);
        }

        public int AllowIgnoreOnPatchError
        {
            get => (int)this.Fields[(int)UpgradedFiles_OptionalDataTupleFields.AllowIgnoreOnPatchError];
            set => this.Set((int)UpgradedFiles_OptionalDataTupleFields.AllowIgnoreOnPatchError, value);
        }

        public int IncludeWholeFile
        {
            get => (int)this.Fields[(int)UpgradedFiles_OptionalDataTupleFields.IncludeWholeFile];
            set => this.Set((int)UpgradedFiles_OptionalDataTupleFields.IncludeWholeFile, value);
        }
    }
}