// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Shortcut = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Shortcut,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ShortcutSymbolFields.DirectoryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ShortcutSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ShortcutSymbolFields.ShortName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ShortcutSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ShortcutSymbolFields.Target), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ShortcutSymbolFields.Arguments), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ShortcutSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ShortcutSymbolFields.Hotkey), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ShortcutSymbolFields.IconRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ShortcutSymbolFields.IconIndex), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ShortcutSymbolFields.Show), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ShortcutSymbolFields.WkDir), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ShortcutSymbolFields.DisplayResourceDLL), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ShortcutSymbolFields.DisplayResourceId), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ShortcutSymbolFields.DescriptionResourceDLL), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ShortcutSymbolFields.DescriptionResourceId), IntermediateFieldType.Number),
            },
            typeof(ShortcutSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ShortcutSymbolFields
    {
        DirectoryRef,
        Name,
        ShortName,
        ComponentRef,
        Target,
        Arguments,
        Description,
        Hotkey,
        IconRef,
        IconIndex,
        Show,
        WkDir,
        DisplayResourceDLL,
        DisplayResourceId,
        DescriptionResourceDLL,
        DescriptionResourceId,
    }

    public enum ShortcutShowType
    {
        Normal = 1,
        Maximized = 3,
        Minimized = 7
    }

    public class ShortcutSymbol : IntermediateSymbol
    {
        public ShortcutSymbol() : base(SymbolDefinitions.Shortcut, null, null)
        {
        }

        public ShortcutSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Shortcut, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ShortcutSymbolFields index] => this.Fields[(int)index];

        public string DirectoryRef
        {
            get => (string)this.Fields[(int)ShortcutSymbolFields.DirectoryRef];
            set => this.Set((int)ShortcutSymbolFields.DirectoryRef, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)ShortcutSymbolFields.Name];
            set => this.Set((int)ShortcutSymbolFields.Name, value);
        }

        public string ShortName
        {
            get => (string)this.Fields[(int)ShortcutSymbolFields.ShortName];
            set => this.Set((int)ShortcutSymbolFields.ShortName, value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)ShortcutSymbolFields.ComponentRef];
            set => this.Set((int)ShortcutSymbolFields.ComponentRef, value);
        }

        public string Target
        {
            get => (string)this.Fields[(int)ShortcutSymbolFields.Target];
            set => this.Set((int)ShortcutSymbolFields.Target, value);
        }

        public string Arguments
        {
            get => (string)this.Fields[(int)ShortcutSymbolFields.Arguments];
            set => this.Set((int)ShortcutSymbolFields.Arguments, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ShortcutSymbolFields.Description];
            set => this.Set((int)ShortcutSymbolFields.Description, value);
        }

        public int? Hotkey
        {
            get => this.Fields[(int)ShortcutSymbolFields.Hotkey].AsNullableNumber();
            set => this.Set((int)ShortcutSymbolFields.Hotkey, value);
        }

        public string IconRef
        {
            get => (string)this.Fields[(int)ShortcutSymbolFields.IconRef];
            set => this.Set((int)ShortcutSymbolFields.IconRef, value);
        }

        public int? IconIndex
        {
            get => this.Fields[(int)ShortcutSymbolFields.IconIndex].AsNullableNumber();
            set => this.Set((int)ShortcutSymbolFields.IconIndex, value);
        }

        public ShortcutShowType? Show
        {
            get => (ShortcutShowType?)this.Fields[(int)ShortcutSymbolFields.Show].AsNullableNumber();
            set => this.Set((int)ShortcutSymbolFields.Show, (int?)value);
        }

        public string WorkingDirectory
        {
            get => (string)this.Fields[(int)ShortcutSymbolFields.WkDir];
            set => this.Set((int)ShortcutSymbolFields.WkDir, value);
        }

        public string DisplayResourceDll
        {
            get => (string)this.Fields[(int)ShortcutSymbolFields.DisplayResourceDLL];
            set => this.Set((int)ShortcutSymbolFields.DisplayResourceDLL, value);
        }

        public int? DisplayResourceId
        {
            get => this.Fields[(int)ShortcutSymbolFields.DisplayResourceId].AsNullableNumber();
            set => this.Set((int)ShortcutSymbolFields.DisplayResourceId, value);
        }

        public string DescriptionResourceDll
        {
            get => (string)this.Fields[(int)ShortcutSymbolFields.DescriptionResourceDLL];
            set => this.Set((int)ShortcutSymbolFields.DescriptionResourceDLL, value);
        }

        public int? DescriptionResourceId
        {
            get => this.Fields[(int)ShortcutSymbolFields.DescriptionResourceId].AsNullableNumber();
            set => this.Set((int)ShortcutSymbolFields.DescriptionResourceId, value);
        }
    }
}
