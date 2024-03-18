// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleContainer = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleContainer,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleContainerSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleContainerSymbolFields.Type), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleContainerSymbolFields.DownloadUrl), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleContainerSymbolFields.Size), IntermediateFieldType.LargeNumber),
                new IntermediateFieldDefinition(nameof(WixBundleContainerSymbolFields.Hash), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleContainerSymbolFields.AttachedContainerIndex), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleContainerSymbolFields.WorkingPath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleContainerSymbolFields.BundleExtensionRef), IntermediateFieldType.String),
            },
            typeof(WixBundleContainerSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixBundleContainerSymbolFields
    {
        Name,
        Type,
        DownloadUrl,
        Size,
        Hash,
        AttachedContainerIndex,
        WorkingPath,
        BundleExtensionRef,
    }

    /// <summary>
    /// Types of bundle packages.
    /// </summary>
    public enum ContainerType
    {
        Attached,
        Detached,
    }

    public class WixBundleContainerSymbol : IntermediateSymbol
    {
        public WixBundleContainerSymbol() : base(SymbolDefinitions.WixBundleContainer, null, null)
        {
        }

        public WixBundleContainerSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleContainer, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleContainerSymbolFields index] => this.Fields[(int)index];

        public string Name
        {
            get => (string)this.Fields[(int)WixBundleContainerSymbolFields.Name];
            set => this.Set((int)WixBundleContainerSymbolFields.Name, value);
        }

        public ContainerType Type
        {
            get => (ContainerType)this.Fields[(int)WixBundleContainerSymbolFields.Type].AsNumber();
            set => this.Set((int)WixBundleContainerSymbolFields.Type, (int)value);
        }

        public string DownloadUrl
        {
            get => (string)this.Fields[(int)WixBundleContainerSymbolFields.DownloadUrl];
            set => this.Set((int)WixBundleContainerSymbolFields.DownloadUrl, value);
        }

        public long? Size
        {
            get => (long?)this.Fields[(int)WixBundleContainerSymbolFields.Size];
            set => this.Set((int)WixBundleContainerSymbolFields.Size, value);
        }

        public string Hash
        {
            get => (string)this.Fields[(int)WixBundleContainerSymbolFields.Hash];
            set => this.Set((int)WixBundleContainerSymbolFields.Hash, value);
        }

        public int? AttachedContainerIndex
        {
            get => (int?)this.Fields[(int)WixBundleContainerSymbolFields.AttachedContainerIndex];
            set => this.Set((int)WixBundleContainerSymbolFields.AttachedContainerIndex, value);
        }

        public string WorkingPath
        {
            get => (string)this.Fields[(int)WixBundleContainerSymbolFields.WorkingPath];
            set => this.Set((int)WixBundleContainerSymbolFields.WorkingPath, value);
        }

        public string BundleExtensionRef
        {
            get => (string)this.Fields[(int)WixBundleContainerSymbolFields.BundleExtensionRef];
            set => this.Set((int)WixBundleContainerSymbolFields.BundleExtensionRef, value);
        }
    }
}
