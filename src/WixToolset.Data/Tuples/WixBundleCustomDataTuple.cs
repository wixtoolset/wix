// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundleCustomData = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundleCustomData,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleCustomDataTupleFields.AttributeNames), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleCustomDataTupleFields.Type), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleCustomDataTupleFields.BundleExtensionRef), IntermediateFieldType.String),
            },
            typeof(WixBundleCustomDataTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBundleCustomDataTupleFields
    {
        AttributeNames,
        Type,
        BundleExtensionRef,
    }

    public enum WixBundleCustomDataType
    {
        Unknown,
        BootstrapperApplication,
        BundleExtension,
    }

    public class WixBundleCustomDataTuple : IntermediateTuple
    {
        public const char AttributeNamesSeparator = '\x85';

        public WixBundleCustomDataTuple() : base(TupleDefinitions.WixBundleCustomData, null, null)
        {
        }

        public WixBundleCustomDataTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundleCustomData, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleCustomDataTupleFields index] => this.Fields[(int)index];

        public string AttributeNames
        {
            get => (string)this.Fields[(int)WixBundleCustomDataTupleFields.AttributeNames];
            set => this.Set((int)WixBundleCustomDataTupleFields.AttributeNames, value);
        }

        public WixBundleCustomDataType Type
        {
            get => (WixBundleCustomDataType)this.Fields[(int)WixBundleCustomDataTupleFields.Type].AsNumber();
            set => this.Set((int)WixBundleCustomDataTupleFields.Type, (int)value);
        }

        public string BundleExtensionRef
        {
            get => (string)this.Fields[(int)WixBundleCustomDataTupleFields.BundleExtensionRef];
            set => this.Set((int)WixBundleCustomDataTupleFields.BundleExtensionRef, value);
        }

        public string[] AttributeNamesSeparated => this.AttributeNames.Split(AttributeNamesSeparator);
    }
}
