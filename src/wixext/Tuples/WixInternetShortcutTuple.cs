// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Tuples;

    public static partial class UtilTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixInternetShortcut = new IntermediateTupleDefinition(
            UtilTupleDefinitionType.WixInternetShortcut.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixInternetShortcutTupleFields.WixInternetShortcut), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixInternetShortcutTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixInternetShortcutTupleFields.Directory_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixInternetShortcutTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixInternetShortcutTupleFields.Target), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixInternetShortcutTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixInternetShortcutTupleFields.IconFile), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixInternetShortcutTupleFields.IconIndex), IntermediateFieldType.Number),
            },
            typeof(WixInternetShortcutTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public enum WixInternetShortcutTupleFields
    {
        WixInternetShortcut,
        Component_,
        Directory_,
        Name,
        Target,
        Attributes,
        IconFile,
        IconIndex,
    }

    public class WixInternetShortcutTuple : IntermediateTuple
    {
        public WixInternetShortcutTuple() : base(UtilTupleDefinitions.WixInternetShortcut, null, null)
        {
        }

        public WixInternetShortcutTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilTupleDefinitions.WixInternetShortcut, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixInternetShortcutTupleFields index] => this.Fields[(int)index];

        public string WixInternetShortcut
        {
            get => this.Fields[(int)WixInternetShortcutTupleFields.WixInternetShortcut].AsString();
            set => this.Set((int)WixInternetShortcutTupleFields.WixInternetShortcut, value);
        }

        public string Component_
        {
            get => this.Fields[(int)WixInternetShortcutTupleFields.Component_].AsString();
            set => this.Set((int)WixInternetShortcutTupleFields.Component_, value);
        }

        public string Directory_
        {
            get => this.Fields[(int)WixInternetShortcutTupleFields.Directory_].AsString();
            set => this.Set((int)WixInternetShortcutTupleFields.Directory_, value);
        }

        public string Name
        {
            get => this.Fields[(int)WixInternetShortcutTupleFields.Name].AsString();
            set => this.Set((int)WixInternetShortcutTupleFields.Name, value);
        }

        public string Target
        {
            get => this.Fields[(int)WixInternetShortcutTupleFields.Target].AsString();
            set => this.Set((int)WixInternetShortcutTupleFields.Target, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)WixInternetShortcutTupleFields.Attributes].AsNumber();
            set => this.Set((int)WixInternetShortcutTupleFields.Attributes, value);
        }

        public string IconFile
        {
            get => this.Fields[(int)WixInternetShortcutTupleFields.IconFile].AsString();
            set => this.Set((int)WixInternetShortcutTupleFields.IconFile, value);
        }

        public int IconIndex
        {
            get => this.Fields[(int)WixInternetShortcutTupleFields.IconIndex].AsNumber();
            set => this.Set((int)WixInternetShortcutTupleFields.IconIndex, value);
        }
    }
}