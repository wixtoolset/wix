// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Symbols;

    public static partial class UtilSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition UserGroup = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.UserGroup.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(UserGroupSymbolFields.UserRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UserGroupSymbolFields.GroupRef), IntermediateFieldType.String),
            },
            typeof(UserGroupSymbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using WixToolset.Data;

    public enum UserGroupSymbolFields
    {
        UserRef,
        GroupRef,
    }

    public class UserGroupSymbol : IntermediateSymbol
    {
        public UserGroupSymbol() : base(UtilSymbolDefinitions.UserGroup, null, null)
        {
        }

        public UserGroupSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.UserGroup, sourceLineNumber, id)
        {
        }

        public IntermediateField this[UserGroupSymbolFields index] => this.Fields[(int)index];

        public string UserRef
        {
            get => this.Fields[(int)UserGroupSymbolFields.UserRef].AsString();
            set => this.Set((int)UserGroupSymbolFields.UserRef, value);
        }

        public string GroupRef
        {
            get => this.Fields[(int)UserGroupSymbolFields.GroupRef].AsString();
            set => this.Set((int)UserGroupSymbolFields.GroupRef, value);
        }
    }
}