// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Symbols;

    public static partial class UtilSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition GroupGroup = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.GroupGroup.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(GroupGroupSymbolFields.ParentGroupRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(GroupGroupSymbolFields.ChildGroupRef), IntermediateFieldType.String),
            },
            typeof(UserGroupSymbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using WixToolset.Data;

    public enum GroupGroupSymbolFields
    {
        ParentGroupRef,
        ChildGroupRef,
    }

    public class GroupGroupSymbol : IntermediateSymbol
    {
        public GroupGroupSymbol() : base(UtilSymbolDefinitions.GroupGroup, null, null)
        {
        }

        public GroupGroupSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.GroupGroup, sourceLineNumber, id)
        {
        }

        public IntermediateField this[GroupGroupSymbolFields index] => this.Fields[(int)index];

        public string ParentGroupRef
        {
            get => this.Fields[(int)GroupGroupSymbolFields.ParentGroupRef].AsString();
            set => this.Set((int)GroupGroupSymbolFields.ParentGroupRef, value);
        }

        public string ChildGroupRef
        {
            get => this.Fields[(int)GroupGroupSymbolFields.ChildGroupRef].AsString();
            set => this.Set((int)GroupGroupSymbolFields.ChildGroupRef, value);
        }

    }
}
