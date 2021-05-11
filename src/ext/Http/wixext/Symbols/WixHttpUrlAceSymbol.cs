// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Http
{
    using WixToolset.Data;
    using WixToolset.Http.Symbols;

    public static partial class HttpSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixHttpUrlAce = new IntermediateSymbolDefinition(
            HttpSymbolDefinitionType.WixHttpUrlAce.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixHttpUrlAceSymbolFields.WixHttpUrlReservationRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixHttpUrlAceSymbolFields.SecurityPrincipal), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixHttpUrlAceSymbolFields.Rights), IntermediateFieldType.Number),
            },
            typeof(WixHttpUrlAceSymbol));
    }
}

namespace WixToolset.Http.Symbols
{
    using WixToolset.Data;

    public enum WixHttpUrlAceSymbolFields
    {
        WixHttpUrlReservationRef,
        SecurityPrincipal,
        Rights,
    }

    public class WixHttpUrlAceSymbol : IntermediateSymbol
    {
        public WixHttpUrlAceSymbol() : base(HttpSymbolDefinitions.WixHttpUrlAce, null, null)
        {
        }

        public WixHttpUrlAceSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(HttpSymbolDefinitions.WixHttpUrlAce, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixHttpUrlAceSymbolFields index] => this.Fields[(int)index];

        public string WixHttpUrlReservationRef
        {
            get => this.Fields[(int)WixHttpUrlAceSymbolFields.WixHttpUrlReservationRef].AsString();
            set => this.Set((int)WixHttpUrlAceSymbolFields.WixHttpUrlReservationRef, value);
        }

        public string SecurityPrincipal
        {
            get => this.Fields[(int)WixHttpUrlAceSymbolFields.SecurityPrincipal].AsString();
            set => this.Set((int)WixHttpUrlAceSymbolFields.SecurityPrincipal, value);
        }

        public int Rights
        {
            get => this.Fields[(int)WixHttpUrlAceSymbolFields.Rights].AsNumber();
            set => this.Set((int)WixHttpUrlAceSymbolFields.Rights, value);
        }
    }
}