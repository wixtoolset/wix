// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using WixToolset.Data;
    using WixToolset.Bal.Tuples;

    public static partial class BalTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixStdbaOptions = new IntermediateTupleDefinition(
            BalTupleDefinitionType.WixStdbaOptions.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixStdbaOptionsTupleFields.SuppressOptionsUI), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixStdbaOptionsTupleFields.SuppressDowngradeFailure), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixStdbaOptionsTupleFields.SuppressRepair), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixStdbaOptionsTupleFields.ShowVersion), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixStdbaOptionsTupleFields.SupportCacheOnly), IntermediateFieldType.Number),
            },
            typeof(WixStdbaOptionsTuple));
    }
}

namespace WixToolset.Bal.Tuples
{
    using WixToolset.Data;

    public enum WixStdbaOptionsTupleFields
    {
        SuppressOptionsUI,
        SuppressDowngradeFailure,
        SuppressRepair,
        ShowVersion,
        SupportCacheOnly,
    }

    public class WixStdbaOptionsTuple : IntermediateTuple
    {
        public WixStdbaOptionsTuple() : base(BalTupleDefinitions.WixStdbaOptions, null, null)
        {
        }

        public WixStdbaOptionsTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(BalTupleDefinitions.WixStdbaOptions, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixStdbaOptionsTupleFields index] => this.Fields[(int)index];

        public int SuppressOptionsUI
        {
            get => this.Fields[(int)WixStdbaOptionsTupleFields.SuppressOptionsUI].AsNumber();
            set => this.Set((int)WixStdbaOptionsTupleFields.SuppressOptionsUI, value);
        }

        public int SuppressDowngradeFailure
        {
            get => this.Fields[(int)WixStdbaOptionsTupleFields.SuppressDowngradeFailure].AsNumber();
            set => this.Set((int)WixStdbaOptionsTupleFields.SuppressDowngradeFailure, value);
        }

        public int SuppressRepair
        {
            get => this.Fields[(int)WixStdbaOptionsTupleFields.SuppressRepair].AsNumber();
            set => this.Set((int)WixStdbaOptionsTupleFields.SuppressRepair, value);
        }

        public int ShowVersion
        {
            get => this.Fields[(int)WixStdbaOptionsTupleFields.ShowVersion].AsNumber();
            set => this.Set((int)WixStdbaOptionsTupleFields.ShowVersion, value);
        }

        public int SupportCacheOnly
        {
            get => this.Fields[(int)WixStdbaOptionsTupleFields.SupportCacheOnly].AsNumber();
            set => this.Set((int)WixStdbaOptionsTupleFields.SupportCacheOnly, value);
        }
    }
}