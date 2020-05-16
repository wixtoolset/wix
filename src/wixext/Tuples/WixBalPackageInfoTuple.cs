// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using WixToolset.Data;
    using WixToolset.Bal.Tuples;

    public static partial class BalTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBalPackageInfo = new IntermediateTupleDefinition(
            BalTupleDefinitionType.WixBalPackageInfo.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBalPackageInfoTupleFields.PackageId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBalPackageInfoTupleFields.DisplayInternalUICondition), IntermediateFieldType.String),
            },
            typeof(WixBalPackageInfoTuple));
    }
}

namespace WixToolset.Bal.Tuples
{
    using WixToolset.Data;

    public enum WixBalPackageInfoTupleFields
    {
        PackageId,
        DisplayInternalUICondition,
    }

    public class WixBalPackageInfoTuple : IntermediateTuple
    {
        public WixBalPackageInfoTuple() : base(BalTupleDefinitions.WixBalPackageInfo, null, null)
        {
        }

        public WixBalPackageInfoTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(BalTupleDefinitions.WixBalPackageInfo, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBalPackageInfoTupleFields index] => this.Fields[(int)index];

        public string PackageId
        {
            get => this.Fields[(int)WixBalPackageInfoTupleFields.PackageId].AsString();
            set => this.Set((int)WixBalPackageInfoTupleFields.PackageId, value);
        }

        public string DisplayInternalUICondition
        {
            get => this.Fields[(int)WixBalPackageInfoTupleFields.DisplayInternalUICondition].AsString();
            set => this.Set((int)WixBalPackageInfoTupleFields.DisplayInternalUICondition, value);
        }
    }
}
