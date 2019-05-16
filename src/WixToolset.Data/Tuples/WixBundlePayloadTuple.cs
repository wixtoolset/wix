// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundlePayload = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundlePayload,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundlePayloadTupleFields.WixBundlePayload), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadTupleFields.SourceFile), IntermediateFieldType.Path),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadTupleFields.DownloadUrl), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadTupleFields.Compressed), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadTupleFields.UnresolvedSourceFile), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadTupleFields.DisplayName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadTupleFields.EnableSignatureValidation), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadTupleFields.FileSize), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadTupleFields.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadTupleFields.Hash), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadTupleFields.PublicKey), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadTupleFields.Thumbprint), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadTupleFields.Catalog_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadTupleFields.Container_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadTupleFields.Package), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadTupleFields.ContentFile), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadTupleFields.EmbeddedId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadTupleFields.LayoutOnly), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadTupleFields.Packaging), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundlePayloadTupleFields.ParentPackagePayload_), IntermediateFieldType.String),
            },
            typeof(WixBundlePayloadTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum WixBundlePayloadTupleFields
    {
        WixBundlePayload,
        Name,
        SourceFile,
        DownloadUrl,
        Compressed,
        UnresolvedSourceFile,
        DisplayName,
        Description,
        EnableSignatureValidation,
        FileSize,
        Version,
        Hash,
        PublicKey,
        Thumbprint,
        Catalog_,
        Container_,
        Package,
        ContentFile,
        EmbeddedId,
        LayoutOnly,
        Packaging,
        ParentPackagePayload_,
    }

    public class WixBundlePayloadTuple : IntermediateTuple
    {
        public WixBundlePayloadTuple() : base(TupleDefinitions.WixBundlePayload, null, null)
        {
        }

        public WixBundlePayloadTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundlePayload, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundlePayloadTupleFields index] => this.Fields[(int)index];

        public string WixBundlePayload
        {
            get => (string)this.Fields[(int)WixBundlePayloadTupleFields.WixBundlePayload];
            set => this.Set((int)WixBundlePayloadTupleFields.WixBundlePayload, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)WixBundlePayloadTupleFields.Name];
            set => this.Set((int)WixBundlePayloadTupleFields.Name, value);
        }

        public string SourceFile
        {
            get => (string)this.Fields[(int)WixBundlePayloadTupleFields.SourceFile];
            set => this.Set((int)WixBundlePayloadTupleFields.SourceFile, value);
        }

        public string DownloadUrl
        {
            get => (string)this.Fields[(int)WixBundlePayloadTupleFields.DownloadUrl];
            set => this.Set((int)WixBundlePayloadTupleFields.DownloadUrl, value);
        }

        public YesNoDefaultType Compressed
        {
            get => Enum.TryParse((string)this.Fields[(int)WixBundlePayloadTupleFields.Compressed], true, out YesNoDefaultType value) ? value : YesNoDefaultType.NotSet;
            set => this.Set((int)WixBundlePayloadTupleFields.Compressed, value.ToString().ToLowerInvariant());
        }

        public string UnresolvedSourceFile
        {
            get => (string)this.Fields[(int)WixBundlePayloadTupleFields.UnresolvedSourceFile];
            set => this.Set((int)WixBundlePayloadTupleFields.UnresolvedSourceFile, value);
        }

        public string DisplayName
        {
            get => (string)this.Fields[(int)WixBundlePayloadTupleFields.DisplayName];
            set => this.Set((int)WixBundlePayloadTupleFields.DisplayName, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)WixBundlePayloadTupleFields.Description];
            set => this.Set((int)WixBundlePayloadTupleFields.Description, value);
        }

        public bool EnableSignatureValidation
        {
            get => (bool)this.Fields[(int)WixBundlePayloadTupleFields.EnableSignatureValidation];
            set => this.Set((int)WixBundlePayloadTupleFields.EnableSignatureValidation, value);
        }

        public int FileSize
        {
            get => (int)this.Fields[(int)WixBundlePayloadTupleFields.FileSize];
            set => this.Set((int)WixBundlePayloadTupleFields.FileSize, value);
        }

        public string Version
        {
            get => (string)this.Fields[(int)WixBundlePayloadTupleFields.Version];
            set => this.Set((int)WixBundlePayloadTupleFields.Version, value);
        }

        public string Hash
        {
            get => (string)this.Fields[(int)WixBundlePayloadTupleFields.Hash];
            set => this.Set((int)WixBundlePayloadTupleFields.Hash, value);
        }

        public string PublicKey
        {
            get => (string)this.Fields[(int)WixBundlePayloadTupleFields.PublicKey];
            set => this.Set((int)WixBundlePayloadTupleFields.PublicKey, value);
        }

        public string Thumbprint
        {
            get => (string)this.Fields[(int)WixBundlePayloadTupleFields.Thumbprint];
            set => this.Set((int)WixBundlePayloadTupleFields.Thumbprint, value);
        }

        public string Catalog_
        {
            get => (string)this.Fields[(int)WixBundlePayloadTupleFields.Catalog_];
            set => this.Set((int)WixBundlePayloadTupleFields.Catalog_, value);
        }

        public string Container_
        {
            get => (string)this.Fields[(int)WixBundlePayloadTupleFields.Container_];
            set => this.Set((int)WixBundlePayloadTupleFields.Container_, value);
        }

        public string Package
        {
            get => (string)this.Fields[(int)WixBundlePayloadTupleFields.Package];
            set => this.Set((int)WixBundlePayloadTupleFields.Package, value);
        }

        public bool ContentFile
        {
            get => (bool)this.Fields[(int)WixBundlePayloadTupleFields.ContentFile];
            set => this.Set((int)WixBundlePayloadTupleFields.ContentFile, value);
        }

        public string EmbeddedId
        {
            get => (string)this.Fields[(int)WixBundlePayloadTupleFields.EmbeddedId];
            set => this.Set((int)WixBundlePayloadTupleFields.EmbeddedId, value);
        }

        public int LayoutOnly
        {
            get => (int)this.Fields[(int)WixBundlePayloadTupleFields.LayoutOnly];
            set => this.Set((int)WixBundlePayloadTupleFields.LayoutOnly, value);
        }

        public int Packaging
        {
            get => (int)this.Fields[(int)WixBundlePayloadTupleFields.Packaging];
            set => this.Set((int)WixBundlePayloadTupleFields.Packaging, value);
        }

        public string ParentPackagePayload_
        {
            get => (string)this.Fields[(int)WixBundlePayloadTupleFields.ParentPackagePayload_];
            set => this.Set((int)WixBundlePayloadTupleFields.ParentPackagePayload_, value);
        }
    }
}