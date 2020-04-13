// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundleContainer = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundleContainer,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleContainerTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleContainerTupleFields.Type), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleContainerTupleFields.DownloadUrl), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleContainerTupleFields.Size), IntermediateFieldType.LargeNumber),
                new IntermediateFieldDefinition(nameof(WixBundleContainerTupleFields.Hash), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleContainerTupleFields.AttachedContainerIndex), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleContainerTupleFields.WorkingPath), IntermediateFieldType.String),
            },
            typeof(WixBundleContainerTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum WixBundleContainerTupleFields
    {
        Name,
        Type,
        DownloadUrl,
        Size,
        Hash,
        AttachedContainerIndex,
        WorkingPath,
    }

    /// <summary>
    /// Types of bundle packages.
    /// </summary>
    public enum ContainerType
    {
        Attached,
        Detached,
    }

    public class WixBundleContainerTuple : IntermediateTuple
    {
        public WixBundleContainerTuple() : base(TupleDefinitions.WixBundleContainer, null, null)
        {
        }

        public WixBundleContainerTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundleContainer, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleContainerTupleFields index] => this.Fields[(int)index];

        public string Name
        {
            get => (string)this.Fields[(int)WixBundleContainerTupleFields.Name];
            set => this.Set((int)WixBundleContainerTupleFields.Name, value);
        }

        public ContainerType Type
        {
            get => (ContainerType)this.Fields[(int)WixBundleContainerTupleFields.Type].AsNumber();
            set => this.Set((int)WixBundleContainerTupleFields.Type, (int)value);
        }

        public string DownloadUrl
        {
            get => (string)this.Fields[(int)WixBundleContainerTupleFields.DownloadUrl];
            set => this.Set((int)WixBundleContainerTupleFields.DownloadUrl, value);
        }

        public long? Size
        {
            get => (long?)this.Fields[(int)WixBundleContainerTupleFields.Size];
            set => this.Set((int)WixBundleContainerTupleFields.Size, value);
        }

        public string Hash
        {
            get => (string)this.Fields[(int)WixBundleContainerTupleFields.Hash];
            set => this.Set((int)WixBundleContainerTupleFields.Hash, value);
        }

        public int? AttachedContainerIndex
        {
            get => (int?)this.Fields[(int)WixBundleContainerTupleFields.AttachedContainerIndex];
            set => this.Set((int)WixBundleContainerTupleFields.AttachedContainerIndex, value);
        }

        public string WorkingPath
        {
            get => (string)this.Fields[(int)WixBundleContainerTupleFields.WorkingPath];
            set => this.Set((int)WixBundleContainerTupleFields.WorkingPath, value);
        }
    }
}
