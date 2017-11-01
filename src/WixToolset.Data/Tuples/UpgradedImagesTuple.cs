// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition UpgradedImages = new IntermediateTupleDefinition(
            TupleDefinitionType.UpgradedImages,
            new[]
            {
                new IntermediateFieldDefinition(nameof(UpgradedImagesTupleFields.Upgraded), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradedImagesTupleFields.MsiPath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradedImagesTupleFields.PatchMsiPath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradedImagesTupleFields.SymbolPaths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradedImagesTupleFields.Family), IntermediateFieldType.String),
            },
            typeof(UpgradedImagesTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum UpgradedImagesTupleFields
    {
        Upgraded,
        MsiPath,
        PatchMsiPath,
        SymbolPaths,
        Family,
    }

    public class UpgradedImagesTuple : IntermediateTuple
    {
        public UpgradedImagesTuple() : base(TupleDefinitions.UpgradedImages, null, null)
        {
        }

        public UpgradedImagesTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.UpgradedImages, sourceLineNumber, id)
        {
        }

        public IntermediateField this[UpgradedImagesTupleFields index] => this.Fields[(int)index];

        public string Upgraded
        {
            get => (string)this.Fields[(int)UpgradedImagesTupleFields.Upgraded]?.Value;
            set => this.Set((int)UpgradedImagesTupleFields.Upgraded, value);
        }

        public string MsiPath
        {
            get => (string)this.Fields[(int)UpgradedImagesTupleFields.MsiPath]?.Value;
            set => this.Set((int)UpgradedImagesTupleFields.MsiPath, value);
        }

        public string PatchMsiPath
        {
            get => (string)this.Fields[(int)UpgradedImagesTupleFields.PatchMsiPath]?.Value;
            set => this.Set((int)UpgradedImagesTupleFields.PatchMsiPath, value);
        }

        public string SymbolPaths
        {
            get => (string)this.Fields[(int)UpgradedImagesTupleFields.SymbolPaths]?.Value;
            set => this.Set((int)UpgradedImagesTupleFields.SymbolPaths, value);
        }

        public string Family
        {
            get => (string)this.Fields[(int)UpgradedImagesTupleFields.Family]?.Value;
            set => this.Set((int)UpgradedImagesTupleFields.Family, value);
        }
    }
}