// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.DifxApp
{
    using WixToolset.Data;
    using WixToolset.DifxApp.Symbols;

    public static partial class DifxAppSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MsiDriverPackages = new IntermediateSymbolDefinition(
            DifxAppSymbolDefinitionType.MsiDriverPackages.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiDriverPackagesSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiDriverPackagesSymbolFields.Flags), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MsiDriverPackagesSymbolFields.Sequence), IntermediateFieldType.Number),
            },
            typeof(MsiDriverPackagesSymbol));
    }
}

namespace WixToolset.DifxApp.Symbols
{
    using WixToolset.Data;

    public enum MsiDriverPackagesSymbolFields
    {
        ComponentRef,
        Flags,
        Sequence,
    }

    public class MsiDriverPackagesSymbol : IntermediateSymbol
    {
        public MsiDriverPackagesSymbol() : base(DifxAppSymbolDefinitions.MsiDriverPackages, null, null)
        {
        }

        public MsiDriverPackagesSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(DifxAppSymbolDefinitions.MsiDriverPackages, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiDriverPackagesSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)MsiDriverPackagesSymbolFields.ComponentRef].AsString();
            set => this.Set((int)MsiDriverPackagesSymbolFields.ComponentRef, value);
        }

        public int Flags
        {
            get => this.Fields[(int)MsiDriverPackagesSymbolFields.Flags].AsNumber();
            set => this.Set((int)MsiDriverPackagesSymbolFields.Flags, value);
        }

        public int? Sequence
        {
            get => this.Fields[(int)MsiDriverPackagesSymbolFields.Sequence].AsNullableNumber();
            set => this.Set((int)MsiDriverPackagesSymbolFields.Sequence, value);
        }
    }
}