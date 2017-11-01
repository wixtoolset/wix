// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ModuleSignature = new IntermediateTupleDefinition(
            TupleDefinitionType.ModuleSignature,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ModuleSignatureTupleFields.ModuleID), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleSignatureTupleFields.Language), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ModuleSignatureTupleFields.Version), IntermediateFieldType.String),
            },
            typeof(ModuleSignatureTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ModuleSignatureTupleFields
    {
        ModuleID,
        Language,
        Version,
    }

    public class ModuleSignatureTuple : IntermediateTuple
    {
        public ModuleSignatureTuple() : base(TupleDefinitions.ModuleSignature, null, null)
        {
        }

        public ModuleSignatureTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ModuleSignature, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ModuleSignatureTupleFields index] => this.Fields[(int)index];

        public string ModuleID
        {
            get => (string)this.Fields[(int)ModuleSignatureTupleFields.ModuleID]?.Value;
            set => this.Set((int)ModuleSignatureTupleFields.ModuleID, value);
        }

        public int Language
        {
            get => (int)this.Fields[(int)ModuleSignatureTupleFields.Language]?.Value;
            set => this.Set((int)ModuleSignatureTupleFields.Language, value);
        }

        public string Version
        {
            get => (string)this.Fields[(int)ModuleSignatureTupleFields.Version]?.Value;
            set => this.Set((int)ModuleSignatureTupleFields.Version, value);
        }
    }
}