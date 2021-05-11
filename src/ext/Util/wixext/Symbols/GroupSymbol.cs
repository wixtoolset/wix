// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Symbols;

    public static partial class UtilSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Group = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.Group.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(GroupSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(GroupSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(GroupSymbolFields.Domain), IntermediateFieldType.String),
            },
            typeof(GroupSymbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using WixToolset.Data;

    public enum GroupSymbolFields
    {
        ComponentRef,
        Name,
        Domain,
    }

    public class GroupSymbol : IntermediateSymbol
    {
        public GroupSymbol() : base(UtilSymbolDefinitions.Group, null, null)
        {
        }

        public GroupSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.Group, sourceLineNumber, id)
        {
        }

        public IntermediateField this[GroupSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)GroupSymbolFields.ComponentRef].AsString();
            set => this.Set((int)GroupSymbolFields.ComponentRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)GroupSymbolFields.Name].AsString();
            set => this.Set((int)GroupSymbolFields.Name, value);
        }

        public string Domain
        {
            get => this.Fields[(int)GroupSymbolFields.Domain].AsString();
            set => this.Set((int)GroupSymbolFields.Domain, value);
        }
    }
}