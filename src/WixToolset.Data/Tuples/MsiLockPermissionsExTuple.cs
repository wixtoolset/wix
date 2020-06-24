// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MsiLockPermissionsEx = new IntermediateSymbolDefinition(
            SymbolDefinitionType.MsiLockPermissionsEx,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiLockPermissionsExSymbolFields.LockObject), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiLockPermissionsExSymbolFields.Table), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiLockPermissionsExSymbolFields.SDDLText), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiLockPermissionsExSymbolFields.Condition), IntermediateFieldType.String),
            },
            typeof(MsiLockPermissionsExSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum MsiLockPermissionsExSymbolFields
    {
        LockObject,
        Table,
        SDDLText,
        Condition,
    }

    public class MsiLockPermissionsExSymbol : IntermediateSymbol
    {
        public MsiLockPermissionsExSymbol() : base(SymbolDefinitions.MsiLockPermissionsEx, null, null)
        {
        }

        public MsiLockPermissionsExSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.MsiLockPermissionsEx, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiLockPermissionsExSymbolFields index] => this.Fields[(int)index];

        public string LockObject
        {
            get => (string)this.Fields[(int)MsiLockPermissionsExSymbolFields.LockObject];
            set => this.Set((int)MsiLockPermissionsExSymbolFields.LockObject, value);
        }

        public string Table
        {
            get => (string)this.Fields[(int)MsiLockPermissionsExSymbolFields.Table];
            set => this.Set((int)MsiLockPermissionsExSymbolFields.Table, value);
        }

        public string SDDLText
        {
            get => (string)this.Fields[(int)MsiLockPermissionsExSymbolFields.SDDLText];
            set => this.Set((int)MsiLockPermissionsExSymbolFields.SDDLText, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)MsiLockPermissionsExSymbolFields.Condition];
            set => this.Set((int)MsiLockPermissionsExSymbolFields.Condition, value);
        }
    }
}