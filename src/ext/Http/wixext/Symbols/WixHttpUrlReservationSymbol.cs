// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Http
{
    using WixToolset.Data;
    using WixToolset.Http.Symbols;

    public static partial class HttpSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixHttpUrlReservation = new IntermediateSymbolDefinition(
            HttpSymbolDefinitionType.WixHttpUrlReservation.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixHttpUrlReservationSymbolFields.HandleExisting), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixHttpUrlReservationSymbolFields.Sddl), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixHttpUrlReservationSymbolFields.Url), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixHttpUrlReservationSymbolFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(WixHttpUrlReservationSymbol));
    }
}

namespace WixToolset.Http.Symbols
{
    using WixToolset.Data;

    public enum WixHttpUrlReservationSymbolFields
    {
        HandleExisting,
        Sddl,
        Url,
        ComponentRef,
    }

    public class WixHttpUrlReservationSymbol : IntermediateSymbol
    {
        public WixHttpUrlReservationSymbol() : base(HttpSymbolDefinitions.WixHttpUrlReservation, null, null)
        {
        }

        public WixHttpUrlReservationSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(HttpSymbolDefinitions.WixHttpUrlReservation, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixHttpUrlReservationSymbolFields index] => this.Fields[(int)index];

        public HandleExisting HandleExisting
        {
            get => (HandleExisting)this.Fields[(int)WixHttpUrlReservationSymbolFields.HandleExisting].AsNumber();
            set => this.Set((int)WixHttpUrlReservationSymbolFields.HandleExisting, (int)value);
        }

        public string Sddl
        {
            get => this.Fields[(int)WixHttpUrlReservationSymbolFields.Sddl].AsString();
            set => this.Set((int)WixHttpUrlReservationSymbolFields.Sddl, value);
        }

        public string Url
        {
            get => this.Fields[(int)WixHttpUrlReservationSymbolFields.Url].AsString();
            set => this.Set((int)WixHttpUrlReservationSymbolFields.Url, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)WixHttpUrlReservationSymbolFields.ComponentRef].AsString();
            set => this.Set((int)WixHttpUrlReservationSymbolFields.ComponentRef, value);
        }
    }
}