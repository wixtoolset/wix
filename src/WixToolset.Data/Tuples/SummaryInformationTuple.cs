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

    public enum SummaryInformationType
    {
        Codepage = 1,
        Title,
        Subject,
        PatchPackageName = 3, //used by patches
        Author,
        Keywords,
        Comments,
        PlatformAndLanguage,
        PatchProductCodes = 7, // used by patches
        TransformPlatformAndLanguageOrStorageNames,
        TransformNames = 8, // used by patches
        PackageCode,
        PatchCode = 9, // used by patches
        TransformProductCodes = 9, // used by transforms
        Reserved11 = 11, // reserved by patches
        Created,
        LastSaved,
        WindowsInstallerVersion,
        Reserved14 = 14, // reserved by patches
        WordCount,
        PatchInstallerRequirement = 15, // used by patches
        Reserved16, // reserved by patches
        TransformValidationFlags = 16, // used by transforms
        CreatingApplication = 18,
        Security
    }

    /// <summary>
    /// Summary information values for the PachInstallerRequirement property.
    /// </summary>
    public enum PatchInstallerRequirement
    {
        /// <summary>Any version of the installer will do</summary>
        Version10 = 1,

        /// <summary>At least 1.2</summary>
        Version12 = 2,

        /// <summary>At least 2.0</summary>
        Version20 = 3,

        /// <summary>At least 3.0</summary>
        Version30 = 4,

        /// <summary>At least 3.1</summary>
        Version31 = 5,
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

        public SummaryInformationType PropertyId
        {
            get => (SummaryInformationType)this.Fields[(int)SummaryInformationTupleFields.PropertyId].AsNumber();
            set => this.Set((int)SummaryInformationTupleFields.PropertyId, (int)value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)SummaryInformationTupleFields.Value];
            set => this.Set((int)SummaryInformationTupleFields.Value, value);
        }
    }
}
