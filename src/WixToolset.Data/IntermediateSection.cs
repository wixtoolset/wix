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
        /// <summary>
        /// Creates a new section as part of an intermediate.
        /// </summary>
        /// <param name="id">Identifier for section.</param>
        /// <param name="type">Type of section.</param>
        /// <param name="codepage">Codepage for resulting database.</param>
        public IntermediateSection(string id, SectionType type, int codepage)
        {
            this.Id = id;
            this.Type = type;
            this.Codepage = codepage;
            this.Tuples = new List<IntermediateTuple>();
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
        /// Gets the codepage for the section.
        /// </summary>
        /// <value>Codepage for the section.</value>
        public int Codepage { get; set; }

        /// <summary>
        /// Gets and sets the identifier of the compilation of the source file containing the section.
        /// </summary>
        public string CompilationId { get; set; }

        /// <summary>
        /// Gets and sets the identifier of the library that combined the section.
        /// </summary>
        public string LibraryId { get; set; }

        /// <summary>
        /// Tuples in the section.
        /// </summary>
        public IList<IntermediateTuple> Tuples { get; }

        /// <summary>
        /// Parse a section from the JSON data.
        /// </summary>
        internal static IntermediateSection Deserialize(ITupleDefinitionCreator creator, JsonObject jsonObject)
        {
            var codepage = jsonObject.GetValueOrDefault("codepage", 0);
            var id = jsonObject.GetValueOrDefault<string>("id");
            var type = jsonObject.GetEnumOrDefault("type", SectionType.Unknown);
            var tuplesJson = jsonObject.GetValueOrDefault<JsonArray>("tuples");

            if (null == id && (SectionType.Unknown != type && SectionType.Fragment != type))
            {
                throw new ArgumentException("JSON object is not a valid section");
            }

            if (SectionType.Unknown == type)
            {
                throw new ArgumentException("JSON object is not a valid section", nameof(type));
            }

            var section = new IntermediateSection(id, type, codepage);

            foreach (JsonObject tupleJson in tuplesJson)
            {
                var tuple = IntermediateTuple.Deserialize(creator, tupleJson);
                section.Tuples.Add(tuple);
            }

            return section;
        }

        internal JsonObject Serialize()
        {
            var jsonObject = new JsonObject
            {
                { "type", this.Type.ToString().ToLowerInvariant() },
                { "codepage", this.Codepage }
            };

            if (!String.IsNullOrEmpty(this.Id))
            {
                jsonObject.Add("id", this.Id);
            }

            var tuplesJson = new JsonArray(this.Tuples.Count);

            foreach (var tuple in this.Tuples)
            {
                var tupleJson = tuple.Serialize();
                tuplesJson.Add(tupleJson);
            }

            jsonObject.Add("tuples", tuplesJson);

            return jsonObject;
        }
    }
}
