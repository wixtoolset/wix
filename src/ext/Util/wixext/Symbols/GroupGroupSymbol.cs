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
                new IntermediateFieldDefinition(nameof(GroupGroupSymbol.SymbolFields.ParentGroupRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(GroupGroupSymbol.SymbolFields.ChildGroupRef), IntermediateFieldType.String),
            },
            typeof(UserGroupSymbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using WixToolset.Data;

    public class GroupGroupSymbol : IntermediateSymbol
    {
        public enum SymbolFields
        {
            ParentGroupRef,
            ChildGroupRef,
        }

        public GroupGroupSymbol() : base(UtilSymbolDefinitions.GroupGroup, null, null)
        {
        }

        public GroupGroupSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.GroupGroup, sourceLineNumber, id)
        {
        }

        public IntermediateField this[GroupGroupSymbol.SymbolFields index] => this.Fields[(int)index];

        public string ParentGroupRef
        {
            get => this.Fields[(int)GroupGroupSymbol.SymbolFields.ParentGroupRef].AsString();
            set => this.Set((int)GroupGroupSymbol.SymbolFields.ParentGroupRef, value);
        }

        public string ChildGroupRef
        {
            get => this.Fields[(int)GroupGroupSymbol.SymbolFields.ChildGroupRef].AsString();
            set => this.Set((int)GroupGroupSymbol.SymbolFields.ChildGroupRef, value);
        }

    }
}
