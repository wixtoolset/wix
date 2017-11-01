// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ModuleComponents = new IntermediateTupleDefinition(
            TupleDefinitionType.ModuleComponents,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ModuleComponentsTupleFields.Component), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleComponentsTupleFields.ModuleID), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleComponentsTupleFields.Language), IntermediateFieldType.Number),
            },
            typeof(ModuleComponentsTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ModuleComponentsTupleFields
    {
        Component,
        ModuleID,
        Language,
    }

    public class ModuleComponentsTuple : IntermediateTuple
    {
        public ModuleComponentsTuple() : base(TupleDefinitions.ModuleComponents, null, null)
        {
        }

        public ModuleComponentsTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ModuleComponents, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ModuleComponentsTupleFields index] => this.Fields[(int)index];

        public string Component
        {
            get => (string)this.Fields[(int)ModuleComponentsTupleFields.Component]?.Value;
            set => this.Set((int)ModuleComponentsTupleFields.Component, value);
        }

        public string ModuleID
        {
            get => (string)this.Fields[(int)ModuleComponentsTupleFields.ModuleID]?.Value;
            set => this.Set((int)ModuleComponentsTupleFields.ModuleID, value);
        }

        public int Language
        {
            get => (int)this.Fields[(int)ModuleComponentsTupleFields.Language]?.Value;
            set => this.Set((int)ModuleComponentsTupleFields.Language, value);
        }
    }
}