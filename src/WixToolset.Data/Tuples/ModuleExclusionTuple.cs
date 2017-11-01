// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ModuleExclusion = new IntermediateTupleDefinition(
            TupleDefinitionType.ModuleExclusion,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ModuleExclusionTupleFields.ModuleID), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleExclusionTupleFields.ModuleLanguage), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleExclusionTupleFields.ExcludedID), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleExclusionTupleFields.ExcludedLanguage), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleExclusionTupleFields.ExcludedMinVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleExclusionTupleFields.ExcludedMaxVersion), IntermediateFieldType.String),
            },
            typeof(ModuleExclusionTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ModuleExclusionTupleFields
    {
        ModuleID,
        ModuleLanguage,
        ExcludedID,
        ExcludedLanguage,
        ExcludedMinVersion,
        ExcludedMaxVersion,
    }

    public class ModuleExclusionTuple : IntermediateTuple
    {
        public ModuleExclusionTuple() : base(TupleDefinitions.ModuleExclusion, null, null)
        {
        }

        public ModuleExclusionTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ModuleExclusion, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ModuleExclusionTupleFields index] => this.Fields[(int)index];

        public string ModuleID
        {
            get => (string)this.Fields[(int)ModuleExclusionTupleFields.ModuleID]?.Value;
            set => this.Set((int)ModuleExclusionTupleFields.ModuleID, value);
        }

        public int ModuleLanguage
        {
            get => (int)this.Fields[(int)ModuleExclusionTupleFields.ModuleLanguage]?.Value;
            set => this.Set((int)ModuleExclusionTupleFields.ModuleLanguage, value);
        }

        public string ExcludedID
        {
            get => (string)this.Fields[(int)ModuleExclusionTupleFields.ExcludedID]?.Value;
            set => this.Set((int)ModuleExclusionTupleFields.ExcludedID, value);
        }

        public int ExcludedLanguage
        {
            get => (int)this.Fields[(int)ModuleExclusionTupleFields.ExcludedLanguage]?.Value;
            set => this.Set((int)ModuleExclusionTupleFields.ExcludedLanguage, value);
        }

        public string ExcludedMinVersion
        {
            get => (string)this.Fields[(int)ModuleExclusionTupleFields.ExcludedMinVersion]?.Value;
            set => this.Set((int)ModuleExclusionTupleFields.ExcludedMinVersion, value);
        }

        public string ExcludedMaxVersion
        {
            get => (string)this.Fields[(int)ModuleExclusionTupleFields.ExcludedMaxVersion]?.Value;
            set => this.Set((int)ModuleExclusionTupleFields.ExcludedMaxVersion, value);
        }
    }
}