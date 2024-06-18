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
                new IntermediateFieldDefinition(nameof(GroupSymbol.SymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(GroupSymbol.SymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(GroupSymbol.SymbolFields.Domain), IntermediateFieldType.String),
            },
            typeof(GroupSymbol));

        public static readonly IntermediateSymbolDefinition Group6 = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.Group6.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(Group6Symbol.SymbolFields.GroupRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(Group6Symbol.SymbolFields.Comment), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(Group6Symbol.SymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(Group6Symbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using System;
    using WixToolset.Data;

    public class GroupSymbol : IntermediateSymbol
    {
        public enum SymbolFields
        {
            ComponentRef,
            Name,
            Domain,
        }

        public GroupSymbol() : base(UtilSymbolDefinitions.Group, null, null)
        {
        }

        public GroupSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.Group, sourceLineNumber, id)
        {
        }

        public IntermediateField this[GroupSymbol.SymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)GroupSymbol.SymbolFields.ComponentRef].AsString();
            set => this.Set((int)GroupSymbol.SymbolFields.ComponentRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)GroupSymbol.SymbolFields.Name].AsString();
            set => this.Set((int)GroupSymbol.SymbolFields.Name, value);
        }

        public string Domain
        {
            get => this.Fields[(int)GroupSymbol.SymbolFields.Domain].AsString();
            set => this.Set((int)GroupSymbol.SymbolFields.Domain, value);
        }
    }

    public class Group6Symbol : IntermediateSymbol
    {
        [Flags]
        public enum SymbolAttributes
        {
            None = 0x00000000,
            FailIfExists = 0x00000001,
            UpdateIfExists = 0x00000002,
            DontRemoveOnUninstall = 0x00000004,
            DontCreateGroup = 0x00000008,
            NonVital = 0x00000010,
            RemoveComment = 0x00000020,
        }

        public enum SymbolFields
        {
            GroupRef,
            Comment,
            Attributes,
        }

        public Group6Symbol() : base(UtilSymbolDefinitions.Group6, null, null)
        {
        }

        public Group6Symbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.Group6, sourceLineNumber, id)
        {
        }

        public IntermediateField this[Group6Symbol.SymbolFields index] => this.Fields[(int)index];

        public string GroupRef
        {
            get => this.Fields[(int)Group6Symbol.SymbolFields.GroupRef].AsString();
            set => this.Set((int)Group6Symbol.SymbolFields.GroupRef, value);
        }

        public string Comment
        {
            get => this.Fields[(int)Group6Symbol.SymbolFields.Comment].AsString();
            set => this.Set((int)Group6Symbol.SymbolFields.Comment, value);
        }

        public SymbolAttributes Attributes
        {
            get => (SymbolAttributes)this.Fields[(int)Group6Symbol.SymbolFields.Attributes].AsNumber();
            set => this.Set((int)Group6Symbol.SymbolFields.Attributes, (int)value);
        }
    }
}
