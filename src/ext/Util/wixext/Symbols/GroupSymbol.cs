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

        public static readonly IntermediateSymbolDefinition Group6 = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.Group6.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(Group6SymbolFields.GroupRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(Group6SymbolFields.Comment), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(Group6SymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(Group6Symbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using System;
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

    [Flags]
    public enum Group6SymbolAttributes
    {
        None = 0x00000000,
        FailIfExists = 0x00000010,
        UpdateIfExists = 0x00000020,
        DontRemoveOnUninstall = 0x00000100,
        DontCreateGroup = 0x00000200,
        NonVital = 0x00000400,
        RemoveComment = 0x00000800,
    }

    public enum Group6SymbolFields
    {
        GroupRef,
        Comment,
        Attributes,
    }

    public class Group6Symbol : IntermediateSymbol
    {
        public Group6Symbol() : base(UtilSymbolDefinitions.Group6, null, null)
        {
        }

        public Group6Symbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.Group6, sourceLineNumber, id)
        {
        }

        public IntermediateField this[Group6SymbolFields index] => this.Fields[(int)index];

        public string GroupRef
        {
            get => this.Fields[(int)Group6SymbolFields.GroupRef].AsString();
            set => this.Set((int)Group6SymbolFields.GroupRef, value);
        }

        public string Comment
        {
            get => this.Fields[(int)Group6SymbolFields.Comment].AsString();
            set => this.Set((int)Group6SymbolFields.Comment, value);
        }

        public Group6SymbolAttributes Attributes
        {
            get => (Group6SymbolAttributes)this.Fields[(int)Group6SymbolFields.Attributes].AsNumber();
            set => this.Set((int)Group6SymbolFields.Attributes, (int)value);
        }
    }
}
