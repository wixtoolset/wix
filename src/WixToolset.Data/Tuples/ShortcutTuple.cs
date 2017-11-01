// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Shortcut = new IntermediateTupleDefinition(
            TupleDefinitionType.Shortcut,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ShortcutTupleFields.Shortcut), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ShortcutTupleFields.Directory_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ShortcutTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ShortcutTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ShortcutTupleFields.Target), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ShortcutTupleFields.Arguments), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ShortcutTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ShortcutTupleFields.Hotkey), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ShortcutTupleFields.Icon_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ShortcutTupleFields.IconIndex), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ShortcutTupleFields.ShowCmd), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ShortcutTupleFields.WkDir), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ShortcutTupleFields.DisplayResourceDLL), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ShortcutTupleFields.DisplayResourceId), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ShortcutTupleFields.DescriptionResourceDLL), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ShortcutTupleFields.DescriptionResourceId), IntermediateFieldType.Number),
            },
            typeof(ShortcutTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ShortcutTupleFields
    {
        Shortcut,
        Directory_,
        Name,
        Component_,
        Target,
        Arguments,
        Description,
        Hotkey,
        Icon_,
        IconIndex,
        ShowCmd,
        WkDir,
        DisplayResourceDLL,
        DisplayResourceId,
        DescriptionResourceDLL,
        DescriptionResourceId,
    }

    public class ShortcutTuple : IntermediateTuple
    {
        public ShortcutTuple() : base(TupleDefinitions.Shortcut, null, null)
        {
        }

        public ShortcutTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Shortcut, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ShortcutTupleFields index] => this.Fields[(int)index];

        public string Shortcut
        {
            get => (string)this.Fields[(int)ShortcutTupleFields.Shortcut]?.Value;
            set => this.Set((int)ShortcutTupleFields.Shortcut, value);
        }

        public string Directory_
        {
            get => (string)this.Fields[(int)ShortcutTupleFields.Directory_]?.Value;
            set => this.Set((int)ShortcutTupleFields.Directory_, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)ShortcutTupleFields.Name]?.Value;
            set => this.Set((int)ShortcutTupleFields.Name, value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)ShortcutTupleFields.Component_]?.Value;
            set => this.Set((int)ShortcutTupleFields.Component_, value);
        }

        public string Target
        {
            get => (string)this.Fields[(int)ShortcutTupleFields.Target]?.Value;
            set => this.Set((int)ShortcutTupleFields.Target, value);
        }

        public string Arguments
        {
            get => (string)this.Fields[(int)ShortcutTupleFields.Arguments]?.Value;
            set => this.Set((int)ShortcutTupleFields.Arguments, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ShortcutTupleFields.Description]?.Value;
            set => this.Set((int)ShortcutTupleFields.Description, value);
        }

        public int Hotkey
        {
            get => (int)this.Fields[(int)ShortcutTupleFields.Hotkey]?.Value;
            set => this.Set((int)ShortcutTupleFields.Hotkey, value);
        }

        public string Icon_
        {
            get => (string)this.Fields[(int)ShortcutTupleFields.Icon_]?.Value;
            set => this.Set((int)ShortcutTupleFields.Icon_, value);
        }

        public int IconIndex
        {
            get => (int)this.Fields[(int)ShortcutTupleFields.IconIndex]?.Value;
            set => this.Set((int)ShortcutTupleFields.IconIndex, value);
        }

        public int ShowCmd
        {
            get => (int)this.Fields[(int)ShortcutTupleFields.ShowCmd]?.Value;
            set => this.Set((int)ShortcutTupleFields.ShowCmd, value);
        }

        public string WkDir
        {
            get => (string)this.Fields[(int)ShortcutTupleFields.WkDir]?.Value;
            set => this.Set((int)ShortcutTupleFields.WkDir, value);
        }

        public string DisplayResourceDLL
        {
            get => (string)this.Fields[(int)ShortcutTupleFields.DisplayResourceDLL]?.Value;
            set => this.Set((int)ShortcutTupleFields.DisplayResourceDLL, value);
        }

        public int DisplayResourceId
        {
            get => (int)this.Fields[(int)ShortcutTupleFields.DisplayResourceId]?.Value;
            set => this.Set((int)ShortcutTupleFields.DisplayResourceId, value);
        }

        public string DescriptionResourceDLL
        {
            get => (string)this.Fields[(int)ShortcutTupleFields.DescriptionResourceDLL]?.Value;
            set => this.Set((int)ShortcutTupleFields.DescriptionResourceDLL, value);
        }

        public int DescriptionResourceId
        {
            get => (int)this.Fields[(int)ShortcutTupleFields.DescriptionResourceId]?.Value;
            set => this.Set((int)ShortcutTupleFields.DescriptionResourceId, value);
        }
    }
}