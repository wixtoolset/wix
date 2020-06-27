// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Netfx.Symbols
{
    using WixToolset.Data;

    public enum NetFxNativeImageSymbolFields
    {
        FileRef,
        Priority,
        Attributes,
        ApplicationFileRef,
        ApplicationBaseDirectoryRef,
    }

    public class NetFxNativeImageSymbol : IntermediateSymbol
    {
        public NetFxNativeImageSymbol() : base(NetfxSymbolDefinitions.NetFxNativeImage, null, null)
        {
        }

        public NetFxNativeImageSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(NetfxSymbolDefinitions.NetFxNativeImage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[NetFxNativeImageSymbolFields index] => this.Fields[(int)index];

        public string FileRef
        {
            get => this.Fields[(int)NetFxNativeImageSymbolFields.FileRef].AsString();
            set => this.Set((int)NetFxNativeImageSymbolFields.FileRef, value);
        }

        public int Priority
        {
            get => this.Fields[(int)NetFxNativeImageSymbolFields.Priority].AsNumber();
            set => this.Set((int)NetFxNativeImageSymbolFields.Priority, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)NetFxNativeImageSymbolFields.Attributes].AsNumber();
            set => this.Set((int)NetFxNativeImageSymbolFields.Attributes, value);
        }

        public string ApplicationFileRef
        {
            get => this.Fields[(int)NetFxNativeImageSymbolFields.ApplicationFileRef].AsString();
            set => this.Set((int)NetFxNativeImageSymbolFields.ApplicationFileRef, value);
        }

        public string ApplicationBaseDirectoryRef
        {
            get => this.Fields[(int)NetFxNativeImageSymbolFields.ApplicationBaseDirectoryRef].AsString();
            set => this.Set((int)NetFxNativeImageSymbolFields.ApplicationBaseDirectoryRef, value);
        }
    }
}