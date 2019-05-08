// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using SimpleJson;

    public class IntermediateTupleDefinition
    {
        public IntermediateTupleDefinition(string name, IntermediateFieldDefinition[] fieldDefinitions, Type strongTupleType)
            : this(TupleDefinitionType.MustBeFromAnExtension, name, 0, fieldDefinitions, strongTupleType)
        {
        }

        public IntermediateTupleDefinition(string name, int revision, IntermediateFieldDefinition[] fieldDefinitions, Type strongTupleType)
            : this(TupleDefinitionType.MustBeFromAnExtension, name, revision, fieldDefinitions, strongTupleType)
        {
        }

        internal IntermediateTupleDefinition(TupleDefinitionType type, IntermediateFieldDefinition[] fieldDefinitions, Type strongTupleType)
            : this(type, type.ToString(), 0, fieldDefinitions, strongTupleType)
        {
        }

        private IntermediateTupleDefinition(TupleDefinitionType type, string name, int revision, IntermediateFieldDefinition[] fieldDefinitions, Type strongTupleType)
        {
            this.Type = type;
            this.Name = name;
            this.Revision = revision;
            this.FieldDefinitions = fieldDefinitions;
            this.StrongTupleType = strongTupleType ?? typeof(IntermediateTuple);
#if DEBUG
            if (this.StrongTupleType != typeof(IntermediateTuple) && !this.StrongTupleType.IsSubclassOf(typeof(IntermediateTuple))) { throw new ArgumentException(nameof(strongTupleType)); }
#endif
        }

        public int Revision { get; }

        public TupleDefinitionType Type { get; }

        public string Name { get; }

        public IntermediateFieldDefinition[] FieldDefinitions { get; }

        private Type StrongTupleType { get; }

        public IntermediateTuple CreateTuple(SourceLineNumber sourceLineNumber = null, Identifier id = null)
        {
            var result = (this.StrongTupleType == typeof(IntermediateTuple)) ? (IntermediateTuple)Activator.CreateInstance(this.StrongTupleType, this) : (IntermediateTuple)Activator.CreateInstance(this.StrongTupleType);
            result.SourceLineNumbers = sourceLineNumber;
            result.Id = id;

            return result;
        }

        internal static IntermediateTupleDefinition Deserialize(JsonObject jsonObject)
        {
            var name = jsonObject.GetValueOrDefault<string>("name");
            var revision = jsonObject.GetValueOrDefault("rev", 0);
            var definitionsJson = jsonObject.GetValueOrDefault<JsonArray>("fields");

            var fieldDefinitions = new IntermediateFieldDefinition[definitionsJson.Count];

            for (var i = 0; i < definitionsJson.Count; ++i)
            {
                var definitionJson = (JsonObject)definitionsJson[i];
                var fieldName = definitionJson.GetValueOrDefault<string>("name");
                var fieldType = definitionJson.GetEnumOrDefault("type", IntermediateFieldType.String);
                fieldDefinitions[i] = new IntermediateFieldDefinition(fieldName, fieldType);
            }

            return new IntermediateTupleDefinition(name, revision, fieldDefinitions, null);
        }

        internal JsonObject Serialize()
        {
            var jsonObject = new JsonObject
            {
                { "name", this.Name }
            };

            if (this.Revision > 0)
            {
                jsonObject.Add("rev", this.Revision);
            }

            var fieldsJson = new JsonArray(this.FieldDefinitions.Length);

            foreach (var fieldDefinition in this.FieldDefinitions)
            {
                var fieldJson = new JsonObject
                {
                    { "name", fieldDefinition.Name },
                };

                if (fieldDefinition.Type != IntermediateFieldType.String)
                {
                    fieldJson.Add("type", fieldDefinition.Type.ToString().ToLowerInvariant());
                }

                fieldsJson.Add(fieldJson);
            }

            jsonObject.Add("fields", fieldsJson);

            return jsonObject;
        }
    }
}
