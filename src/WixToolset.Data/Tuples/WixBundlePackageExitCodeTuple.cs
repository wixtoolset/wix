// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundlePackageExitCode = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundlePackageExitCode,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundlePackageExitCodeTupleFields.ChainPackageId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageExitCodeTupleFields.Code), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundlePackageExitCodeTupleFields.Behavior), IntermediateFieldType.String),
            },
            typeof(WixBundlePackageExitCodeTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum WixBundlePackageExitCodeTupleFields
    {
        ChainPackageId,
        Code,
        Behavior,
    }

    public enum ExitCodeBehaviorType
    {
        NotSet = -1,
        Success,
        Error,
        ScheduleReboot,
        ForceReboot,
    }

    public class WixBundlePackageExitCodeTuple : IntermediateTuple
    {
        public WixBundlePackageExitCodeTuple() : base(TupleDefinitions.WixBundlePackageExitCode, null, null)
        {
        }

        public WixBundlePackageExitCodeTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundlePackageExitCode, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundlePackageExitCodeTupleFields index] => this.Fields[(int)index];

        public string ChainPackageId
        {
            get => (string)this.Fields[(int)WixBundlePackageExitCodeTupleFields.ChainPackageId]?.Value;
            set => this.Set((int)WixBundlePackageExitCodeTupleFields.ChainPackageId, value);
        }

        public int Code
        {
            get => (int)this.Fields[(int)WixBundlePackageExitCodeTupleFields.Code]?.Value;
            set => this.Set((int)WixBundlePackageExitCodeTupleFields.Code, value);
        }

        public ExitCodeBehaviorType Behavior
        {
            get => Enum.TryParse((string)this.Fields[(int)WixBundlePackageExitCodeTupleFields.Behavior]?.Value, true, out ExitCodeBehaviorType value) ? value : ExitCodeBehaviorType.NotSet;
            set => this.Set((int)WixBundlePackageExitCodeTupleFields.Behavior, value.ToString());
        }
    }
}