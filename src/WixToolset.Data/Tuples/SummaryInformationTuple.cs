// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition SummaryInformation = new IntermediateTupleDefinition(
            TupleDefinitionType.SummaryInformation,
            new[]
            {
                new IntermediateFieldDefinition(nameof(SummaryInformationTupleFields.PropertyId), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(SummaryInformationTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(SummaryInformationTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum SummaryInformationTupleFields
    {
        PropertyId,
        Value,
    }

    public enum SumaryInformationType
    {
        Codepage = 1,
        Title,
        Subject,
        Author,
        Keywords,
        Comments,
        PlatformAndLanguage,
        TransformPlatformAndLanguageOrStorageNames,
        PackageCode,
        Created = 12,
        LastSaved,
        WindowsInstallerVersion,
        WordCount,
        CreatingApplication = 18,
        Security
    }

    public class SummaryInformationTuple : IntermediateTuple
    {
        public SummaryInformationTuple() : base(TupleDefinitions.SummaryInformation, null, null)
        {
        }

        public SummaryInformationTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.SummaryInformation, sourceLineNumber, id)
        {
        }

        public IntermediateField this[SummaryInformationTupleFields index] => this.Fields[(int)index];

        public SumaryInformationType PropertyId
        {
            get => (SumaryInformationType)this.Fields[(int)SummaryInformationTupleFields.PropertyId].AsNumber();
            set => this.Set((int)SummaryInformationTupleFields.PropertyId, (int)value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)SummaryInformationTupleFields.Value];
            set => this.Set((int)SummaryInformationTupleFields.Value, value);
        }
    }
}
