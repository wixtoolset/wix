// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Netfx.Tuples
{
    using WixToolset.Data;

    public enum NetFxNativeImageTupleFields
    {
        NetFxNativeImage,
        File_,
        Priority,
        Attributes,
        File_Application,
        Directory_ApplicationBase,
    }

    public class NetFxNativeImageTuple : IntermediateTuple
    {
        public NetFxNativeImageTuple() : base(NetfxTupleDefinitions.NetFxNativeImage, null, null)
        {
        }

        public NetFxNativeImageTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(NetfxTupleDefinitions.NetFxNativeImage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[NetFxNativeImageTupleFields index] => this.Fields[(int)index];

        public string NetFxNativeImage
        {
            get => this.Fields[(int)NetFxNativeImageTupleFields.NetFxNativeImage].AsString();
            set => this.Set((int)NetFxNativeImageTupleFields.NetFxNativeImage, value);
        }

        public string File_
        {
            get => this.Fields[(int)NetFxNativeImageTupleFields.File_].AsString();
            set => this.Set((int)NetFxNativeImageTupleFields.File_, value);
        }

        public int Priority
        {
            get => this.Fields[(int)NetFxNativeImageTupleFields.Priority].AsNumber();
            set => this.Set((int)NetFxNativeImageTupleFields.Priority, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)NetFxNativeImageTupleFields.Attributes].AsNumber();
            set => this.Set((int)NetFxNativeImageTupleFields.Attributes, value);
        }

        public string File_Application
        {
            get => this.Fields[(int)NetFxNativeImageTupleFields.File_Application].AsString();
            set => this.Set((int)NetFxNativeImageTupleFields.File_Application, value);
        }

        public string Directory_ApplicationBase
        {
            get => this.Fields[(int)NetFxNativeImageTupleFields.Directory_ApplicationBase].AsString();
            set => this.Set((int)NetFxNativeImageTupleFields.Directory_ApplicationBase, value);
        }
    }
}