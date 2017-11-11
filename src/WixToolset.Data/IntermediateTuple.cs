// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using SimpleJson;

    public class IntermediateTuple
    {
        //public IntermediateTuple(IntermediateTupleDefinition definition) : this(definition, null, null)
        //{
        //}

        public IntermediateTuple(IntermediateTupleDefinition definition, SourceLineNumber sourceLineNumber, Identifier id = null)
        {
            this.Definition = definition;
            this.Fields = new IntermediateField[definition.FieldDefinitions.Length];
            this.SourceLineNumbers = sourceLineNumber;
            this.Id = id;
        }

        public IntermediateTupleDefinition Definition { get; }

        public IntermediateField[] Fields { get; }

        public SourceLineNumber SourceLineNumbers { get; set; }

        public Identifier Id { get; set; }

        public IntermediateField this[int index] => this.Fields[index];

        internal static IntermediateTuple Deserialize(ITupleDefinitionCreator creator, JsonObject jsonObject)
        {
            var definitionName = jsonObject.GetValueOrDefault<string>("type");
            var idJson = jsonObject.GetValueOrDefault<JsonObject>("id");
            var sourceLineNumbersJson = jsonObject.GetValueOrDefault<JsonObject>("ln");
            var fieldsJson = jsonObject.GetValueOrDefault<JsonArray>("fields");

            creator.TryGetTupleDefinitionByName(definitionName, out var definition); // TODO: this isn't sufficient.
            var tuple = definition.CreateTuple();

            tuple.Id = (idJson == null) ? null : Identifier.Deserialize(idJson);
            tuple.SourceLineNumbers = (sourceLineNumbersJson == null) ? null : SourceLineNumber.Deserialize(sourceLineNumbersJson);

            for (var i = 0; i < fieldsJson.Count; ++i)
            {
                if (tuple.Fields.Length > i && fieldsJson[i] is JsonObject fieldJson)
                {
                    tuple.Fields[i] = IntermediateField.Deserialize(tuple.Definition.FieldDefinitions[i], fieldJson);
                }
            }

            return tuple;
        }

        internal JsonObject Serialize()
        {
            var jsonObject = new JsonObject
            {
                { "type", this.Definition.Name }
            };

            var idJson = this.Id?.Serialize();
            if (idJson != null)
            {
                jsonObject.Add("id", idJson);
            }

            var lnJson = this.SourceLineNumbers?.Serialize();
            if (lnJson != null)
            {
                jsonObject.Add("ln", lnJson);
            }

            var fieldsJson = new JsonArray(this.Fields.Length);

            foreach (var field in this.Fields)
            {
                var fieldJson = field?.Serialize();
                fieldsJson.Add(fieldJson);
            }

            jsonObject.Add("fields", fieldsJson);

            return jsonObject;
        }
    }
}
