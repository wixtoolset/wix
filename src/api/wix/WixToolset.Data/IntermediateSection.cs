// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Collections.Generic;
    using SimpleJson;

    /// <summary>
    /// Section in an intermediate file.
    /// </summary>
    public class IntermediateSection
    {
        private readonly List<IntermediateSymbol> symbols;

        /// <summary>
        /// Creates a new section as part of an intermediate.
        /// </summary>
        /// <param name="id">Identifier for section.</param>
        /// <param name="type">Type of section.</param>
        /// <param name="compilationId">Optional compilation identifier</param>
        public IntermediateSection(string id, SectionType type, string compilationId = null)
        {
            this.Id = id;
            this.Type = type;
            this.CompilationId = compilationId;
            this.symbols = new List<IntermediateSymbol>();
        }

        /// <summary>
        /// Gets the identifier for the section.
        /// </summary>
        /// <value>Section identifier.</value>
        public string Id { get; }

        /// <summary>
        /// Gets the type of the section.
        /// </summary>
        /// <value>Type of section.</value>
        public SectionType Type { get; }

        /// <summary>
        /// Gets and sets the identifier of the compilation of the source file containing the section.
        /// </summary>
        public string CompilationId { get; }

        /// <summary>
        /// Gets and sets the identifier of the library that combined the section.
        /// </summary>
        public string LibraryId { get; private set; }

        /// <summary>
        /// Symbols in the section.
        /// </summary>
        public IReadOnlyCollection<IntermediateSymbol> Symbols => this.symbols;

        /// <summary>
        /// Adds a symbol to the section.
        /// </summary>
        /// <typeparam name="T">Type of IntermediateSymbol to add to the section.</typeparam>
        /// <param name="symbol">Symbol to add to the section.</param>
        /// <returns>Symbol added to the section.</returns>
        public T AddSymbol<T>(T symbol) where T : IntermediateSymbol
        {
            this.symbols.Add(symbol);
            return symbol;
        }

        /// <summary>
        /// Assigns the section to a library.
        /// </summary>
        /// <param name="libraryId">Identifier of the library.</param>
        public void AssignToLibrary(string libraryId)
        {
            this.LibraryId = libraryId;
        }

        /// <summary>
        /// Removes a symbol from the section.
        /// </summary>
        /// <param name="symbol">Symbol to remove.</param>
        /// <returns>True if the symbol was removed; otherwise false.</returns>
        public bool RemoveSymbol(IntermediateSymbol symbol)
        {
            return this.symbols.Remove(symbol);
        }

        /// <summary>
        /// Parse a section from the JSON data.
        /// </summary>
        internal static IntermediateSection Deserialize(ISymbolDefinitionCreator creator, Uri baseUri, JsonObject jsonObject)
        {
            var id = jsonObject.GetValueOrDefault<string>("id");
            var type = jsonObject.GetEnumOrDefault("type", SectionType.Unknown);

            if (SectionType.Unknown == type)
            {
                throw new ArgumentException("JSON object is not a valid section, unknown section type", nameof(type));
            }

            var section = new IntermediateSection(id, type);

            var symbolsJson = jsonObject.GetValueOrDefault<JsonArray>("symbols");

            foreach (JsonObject symbolJson in symbolsJson)
            {
                var symbol = IntermediateSymbol.Deserialize(creator, baseUri, symbolJson);
                section.symbols.Add(symbol);
            }

            return section;
        }

        internal JsonObject Serialize()
        {
            var jsonObject = new JsonObject
            {
                { "type", this.Type.ToString().ToLowerInvariant() }
            };

            if (!String.IsNullOrEmpty(this.Id))
            {
                jsonObject.Add("id", this.Id);
            }

            var symbolsJson = new JsonArray(this.Symbols.Count);

            foreach (var symbol in this.Symbols)
            {
                var symbolJson = symbol.Serialize();
                symbolsJson.Add(symbolJson);
            }

            jsonObject.Add("symbols", symbolsJson);

            return jsonObject;
        }
    }
}
