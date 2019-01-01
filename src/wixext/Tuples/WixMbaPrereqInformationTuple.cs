// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using WixToolset.Data;
    using WixToolset.Bal.Tuples;

    public static partial class BalTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixMbaPrereqInformation = new IntermediateTupleDefinition(
            BalTupleDefinitionType.WixMbaPrereqInformation.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixMbaPrereqInformationTupleFields.PackageId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixMbaPrereqInformationTupleFields.LicenseFile), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixMbaPrereqInformationTupleFields.LicenseUrl), IntermediateFieldType.String),
            },
            typeof(WixMbaPrereqInformationTuple));
    }
}

namespace WixToolset.Bal.Tuples
{
    using WixToolset.Data;

    public enum WixMbaPrereqInformationTupleFields
    {
        PackageId,
        LicenseFile,
        LicenseUrl,
    }

    public class WixMbaPrereqInformationTuple : IntermediateTuple
    {
        public WixMbaPrereqInformationTuple() : base(BalTupleDefinitions.WixMbaPrereqInformation, null, null)
        {
        }

        public WixMbaPrereqInformationTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(BalTupleDefinitions.WixMbaPrereqInformation, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixMbaPrereqInformationTupleFields index] => this.Fields[(int)index];

        public string PackageId
        {
            get => this.Fields[(int)WixMbaPrereqInformationTupleFields.PackageId].AsString();
            set => this.Set((int)WixMbaPrereqInformationTupleFields.PackageId, value);
        }

        public string LicenseFile
        {
            get => this.Fields[(int)WixMbaPrereqInformationTupleFields.LicenseFile].AsString();
            set => this.Set((int)WixMbaPrereqInformationTupleFields.LicenseFile, value);
        }

        public string LicenseUrl
        {
            get => this.Fields[(int)WixMbaPrereqInformationTupleFields.LicenseUrl].AsString();
            set => this.Set((int)WixMbaPrereqInformationTupleFields.LicenseUrl, value);
        }
    }
}