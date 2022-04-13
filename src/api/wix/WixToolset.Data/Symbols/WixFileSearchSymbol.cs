// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixFileSearch = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixFileSearch,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixFileSearchSymbolFields.Path), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFileSearchSymbolFields.MinVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFileSearchSymbolFields.MaxVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFileSearchSymbolFields.MinSize), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixFileSearchSymbolFields.MaxSize), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixFileSearchSymbolFields.MinDate), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixFileSearchSymbolFields.MaxDate), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixFileSearchSymbolFields.Languages), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFileSearchSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixFileSearchSymbolFields.Type), IntermediateFieldType.Number),
            },
            typeof(WixFileSearchSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixFileSearchSymbolFields
    {
        Path,
        MinVersion,
        MaxVersion,
        MinSize,
        MaxSize,
        MinDate,
        MaxDate,
        Languages,
        Attributes,
        Type,
    }

    [Flags]
    public enum WixFileSearchAttributes
    {
        None = 0x000,
        IsDirectory = 0x001,
        MinVersionInclusive = 0x002,
        MaxVersionInclusive = 0x004,
        MinSizeInclusive = 0x008,
        MaxSizeInclusive = 0x010,
        MinDateInclusive = 0x020,
        MaxDateInclusive = 0x040,
    }

    public enum WixFileSearchType
    {
        Path,
        Version,
        Exists,
    }

    public class WixFileSearchSymbol : IntermediateSymbol
    {
        public WixFileSearchSymbol() : base(SymbolDefinitions.WixFileSearch, null, null)
        {
        }

        public WixFileSearchSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixFileSearch, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixFileSearchSymbolFields index] => this.Fields[(int)index];

        public string Path
        {
            get => (string)this.Fields[(int)WixFileSearchSymbolFields.Path];
            set => this.Set((int)WixFileSearchSymbolFields.Path, value);
        }

        public string MinVersion
        {
            get => (string)this.Fields[(int)WixFileSearchSymbolFields.MinVersion];
            set => this.Set((int)WixFileSearchSymbolFields.MinVersion, value);
        }

        public string MaxVersion
        {
            get => (string)this.Fields[(int)WixFileSearchSymbolFields.MaxVersion];
            set => this.Set((int)WixFileSearchSymbolFields.MaxVersion, value);
        }

        public int? MinSize
        {
            get => (int?)this.Fields[(int)WixFileSearchSymbolFields.MinSize];
            set => this.Set((int)WixFileSearchSymbolFields.MinSize, value);
        }

        public int? MaxSize
        {
            get => (int?)this.Fields[(int)WixFileSearchSymbolFields.MaxSize];
            set => this.Set((int)WixFileSearchSymbolFields.MaxSize, value);
        }

        public int? MinDate
        {
            get => (int?)this.Fields[(int)WixFileSearchSymbolFields.MinDate];
            set => this.Set((int)WixFileSearchSymbolFields.MinDate, value);
        }

        public int? MaxDate
        {
            get => (int?)this.Fields[(int)WixFileSearchSymbolFields.MaxDate];
            set => this.Set((int)WixFileSearchSymbolFields.MaxDate, value);
        }

        public string Languages
        {
            get => (string)this.Fields[(int)WixFileSearchSymbolFields.Languages];
            set => this.Set((int)WixFileSearchSymbolFields.Languages, value);
        }

        public WixFileSearchAttributes Attributes
        {
            get => (WixFileSearchAttributes)this.Fields[(int)WixFileSearchSymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixFileSearchSymbolFields.Attributes, (int)value);
        }

        public WixFileSearchType Type
        {
            get => (WixFileSearchType)this.Fields[(int)WixFileSearchSymbolFields.Type].AsNumber();
            set => this.Set((int)WixFileSearchSymbolFields.Type, (int)value);
        }

        public bool IsDirectory
        {
            get { return this.Attributes.HasFlag(WixFileSearchAttributes.IsDirectory); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixFileSearchAttributes.IsDirectory;
                }
                else
                {
                    this.Attributes &= ~WixFileSearchAttributes.IsDirectory;
                }
            }
        }

        public bool MinVersionInclusive
        {
            get { return this.Attributes.HasFlag(WixFileSearchAttributes.MinVersionInclusive); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixFileSearchAttributes.MinVersionInclusive;
                }
                else
                {
                    this.Attributes &= ~WixFileSearchAttributes.MinVersionInclusive;
                }
            }
        }

        public bool MaxVersionInclusive
        {
            get { return this.Attributes.HasFlag(WixFileSearchAttributes.MaxVersionInclusive); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixFileSearchAttributes.MaxVersionInclusive;
                }
                else
                {
                    this.Attributes &= ~WixFileSearchAttributes.MaxVersionInclusive;
                }
            }
        }

        public bool MinSizeInclusive
        {
            get { return this.Attributes.HasFlag(WixFileSearchAttributes.MinSizeInclusive); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixFileSearchAttributes.MinSizeInclusive;
                }
                else
                {
                    this.Attributes &= ~WixFileSearchAttributes.MinSizeInclusive;
                }
            }
        }

        public bool MaxSizeInclusive
        {
            get { return this.Attributes.HasFlag(WixFileSearchAttributes.MaxSizeInclusive); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixFileSearchAttributes.MaxSizeInclusive;
                }
                else
                {
                    this.Attributes &= ~WixFileSearchAttributes.MaxSizeInclusive;
                }
            }
        }

        public bool MinDateInclusive
        {
            get { return this.Attributes.HasFlag(WixFileSearchAttributes.MinDateInclusive); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixFileSearchAttributes.MinDateInclusive;
                }
                else
                {
                    this.Attributes &= ~WixFileSearchAttributes.MinDateInclusive;
                }
            }
        }

        public bool MaxDateInclusive
        {
            get { return this.Attributes.HasFlag(WixFileSearchAttributes.MaxDateInclusive); }
            set
            {
                if (value)
                {
                    this.Attributes |= WixFileSearchAttributes.MaxDateInclusive;
                }
                else
                {
                    this.Attributes &= ~WixFileSearchAttributes.MaxDateInclusive;
                }
            }
        }
    }
}
