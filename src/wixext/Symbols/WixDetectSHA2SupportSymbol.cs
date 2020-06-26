// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Symbols;

    public static partial class UtilSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixDetectSHA2Support = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.WixDetectSHA2Support.ToString(),
            new IntermediateFieldDefinition[0],
            typeof(WixDetectSHA2SupportSymbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using WixToolset.Data;

    public class WixDetectSHA2SupportSymbol : IntermediateSymbol
    {
        public WixDetectSHA2SupportSymbol() : base(UtilSymbolDefinitions.WixDetectSHA2Support, null, null)
        {
        }

        public WixDetectSHA2SupportSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.WixDetectSHA2Support, sourceLineNumber, id)
        {
        }

        public IntermediateField this[GroupSymbolFields index] => this.Fields[(int)index];
    }
}
