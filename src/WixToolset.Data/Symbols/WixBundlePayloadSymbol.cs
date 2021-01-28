// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundlePayload = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundlePayload,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundlePayloadSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadSymbolFields.SourceFile), IntermediateFieldType.Path),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadSymbolFields.DownloadUrl), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadSymbolFields.Compressed), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadSymbolFields.UnresolvedSourceFile), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadSymbolFields.DisplayName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadSymbolFields.FileSize), IntermediateFieldType.LargeNumber),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadSymbolFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadSymbolFields.Hash), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadSymbolFields.ContainerRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadSymbolFields.PackageRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadSymbolFields.ContentFile), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadSymbolFields.EmbeddedId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadSymbolFields.LayoutOnly), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadSymbolFields.Packaging), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadSymbolFields.ParentPackagePayloadRef), IntermediateFieldType.String),
            },
            typeof(WixBundlePayloadSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixBundlePayloadSymbolFields
    {
        Name,
        SourceFile,
        DownloadUrl,
        Compressed,
        UnresolvedSourceFile,
        DisplayName,
        Description,
        FileSize,
        Version,
        Hash,
        ContainerRef,
        PackageRef,
        ContentFile,
        EmbeddedId,
        LayoutOnly,
        Packaging,
        ParentPackagePayloadRef,
    }

    public class WixBundlePayloadSymbol : IntermediateSymbol
    {
        public WixBundlePayloadSymbol() : base(SymbolDefinitions.WixBundlePayload, null, null)
        {
        }

        public WixBundlePayloadSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundlePayload, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundlePayloadSymbolFields index] => this.Fields[(int)index];

        public string Name
        {
            get => (string)this.Fields[(int)WixBundlePayloadSymbolFields.Name];
            set => this.Set((int)WixBundlePayloadSymbolFields.Name, value);
        }

        public IntermediateFieldPathValue SourceFile
        {
            get => this.Fields[(int)WixBundlePayloadSymbolFields.SourceFile].AsPath();
            set => this.Set((int)WixBundlePayloadSymbolFields.SourceFile, value);
        }

        public string DownloadUrl
        {
            get => (string)this.Fields[(int)WixBundlePayloadSymbolFields.DownloadUrl];
            set => this.Set((int)WixBundlePayloadSymbolFields.DownloadUrl, value);
        }

        public bool? Compressed
        {
            get => (bool?)this.Fields[(int)WixBundlePayloadSymbolFields.Compressed];
            set => this.Set((int)WixBundlePayloadSymbolFields.Compressed, value);
        }

        public string UnresolvedSourceFile
        {
            get => (string)this.Fields[(int)WixBundlePayloadSymbolFields.UnresolvedSourceFile];
            set => this.Set((int)WixBundlePayloadSymbolFields.UnresolvedSourceFile, value);
        }

        public string DisplayName
        {
            get => (string)this.Fields[(int)WixBundlePayloadSymbolFields.DisplayName];
            set => this.Set((int)WixBundlePayloadSymbolFields.DisplayName, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)WixBundlePayloadSymbolFields.Description];
            set => this.Set((int)WixBundlePayloadSymbolFields.Description, value);
        }

        public long? FileSize
        {
            get => (long?)this.Fields[(int)WixBundlePayloadSymbolFields.FileSize];
            set => this.Set((int)WixBundlePayloadSymbolFields.FileSize, value);
        }

        public string Version
        {
            get => (string)this.Fields[(int)WixBundlePayloadSymbolFields.Version];
            set => this.Set((int)WixBundlePayloadSymbolFields.Version, value);
        }

        public string Hash
        {
            get => (string)this.Fields[(int)WixBundlePayloadSymbolFields.Hash];
            set => this.Set((int)WixBundlePayloadSymbolFields.Hash, value);
        }

        public string ContainerRef
        {
            get => (string)this.Fields[(int)WixBundlePayloadSymbolFields.ContainerRef];
            set => this.Set((int)WixBundlePayloadSymbolFields.ContainerRef, value);
        }

        public string PackageRef
        {
            get => (string)this.Fields[(int)WixBundlePayloadSymbolFields.PackageRef];
            set => this.Set((int)WixBundlePayloadSymbolFields.PackageRef, value);
        }

        public bool ContentFile
        {
            get => (bool)this.Fields[(int)WixBundlePayloadSymbolFields.ContentFile];
            set => this.Set((int)WixBundlePayloadSymbolFields.ContentFile, value);
        }

        public string EmbeddedId
        {
            get => (string)this.Fields[(int)WixBundlePayloadSymbolFields.EmbeddedId];
            set => this.Set((int)WixBundlePayloadSymbolFields.EmbeddedId, value);
        }

        public bool LayoutOnly
        {
            get => (bool)this.Fields[(int)WixBundlePayloadSymbolFields.LayoutOnly];
            set => this.Set((int)WixBundlePayloadSymbolFields.LayoutOnly, value);
        }

        public PackagingType? Packaging
        {
            get => (PackagingType?)this.Fields[(int)WixBundlePayloadSymbolFields.Packaging].AsNumber();
            set => this.Set((int)WixBundlePayloadSymbolFields.Packaging, (int?)value);
        }

        public string ParentPackagePayloadRef
        {
            get => (string)this.Fields[(int)WixBundlePayloadSymbolFields.ParentPackagePayloadRef];
            set => this.Set((int)WixBundlePayloadSymbolFields.ParentPackagePayloadRef, value);
        }
    }
}
