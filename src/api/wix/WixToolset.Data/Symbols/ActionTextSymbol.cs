// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ActionText = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ActionText,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ActionTextSymbolFields.Action), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ActionTextSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ActionTextSymbolFields.Template), IntermediateFieldType.String),
            },
            typeof(ActionTextSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ActionTextSymbolFields
    {
        Action,
        Description,
        Template,
    }

    public class ActionTextSymbol : IntermediateSymbol
    {
        public ActionTextSymbol() : base(SymbolDefinitions.ActionText, null, null)
        {
        }

        public ActionTextSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ActionText, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ActionTextSymbolFields index] => this.Fields[(int)index];

        public string Action
        {
            get => (string)this.Fields[(int)ActionTextSymbolFields.Action];
            set => this.Set((int)ActionTextSymbolFields.Action, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ActionTextSymbolFields.Description];
            set => this.Set((int)ActionTextSymbolFields.Description, value);
        }

        public string Template
        {
            get => (string)this.Fields[(int)ActionTextSymbolFields.Template];
            set => this.Set((int)ActionTextSymbolFields.Template, value);
        }
    }
}