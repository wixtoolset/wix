// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Diagnostics;
    using SimpleJson;

    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class IntermediateTuple
    {
        private object tags;

        public IntermediateTuple(IntermediateTupleDefinition definition) : this(definition, null, null)
        {
        }

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

        private string DebuggerDisplay => $"{this.Definition?.Name} {this.Id?.Id}";

        public bool AddTag(string add)
        {
            if (this.tags == null)
            {
                this.tags = add;
            }
            else if (this.tags is string tag)
            {
                if (tag == add)
                {
                    return false;
                }

                this.tags = new[] { tag, add };
            }
            else
            {
                var tagsArray = (string[])this.tags;
                var array = new string[tagsArray.Length + 1];

                for (var i = 0; i < tagsArray.Length; ++i)
                {
                    if (tagsArray[i] == add)
                    {
                        return false;
                    }

                    array[i] = tagsArray[i];
                }

                array[tagsArray.Length] = add;

                this.tags = array;
            }

            return true;
        }

        public bool HasTag(string has)
        {
            if (this.tags == null)
            {
                return false;
            }
            else if (this.tags is string tag)
            {
                return tag == has;
            }
            else
            {
                foreach (var element in (string[])this.tags)
                {
                    if (element == has)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public bool RemoveTag(string remove)
        {
            if (this.tags is string tag)
            {
                if (tag == remove)
                {
                    this.tags = null;
                    return true;
                }
            }
            else if (this.tags is string[] tagsArray)
            {
                if (tagsArray.Length == 2)
                {
                    if (tagsArray[0] == remove)
                    {
                        this.tags = tagsArray[1];
                        return true;
                    }
                    else if (tagsArray[1] == remove)
                    {
                        this.tags = tagsArray[0];
                        return true;
                    }
                }
                else
                {
                    var array = new string[tagsArray.Length - 1];
                    var arrayIndex = 0;
                    var found = false;

                    for (var i = 0; i < tagsArray.Length; ++i)
                    {
                        if (tagsArray[i] == remove)
                        {
                            found = true;
                            continue;
                        }
                        else if (arrayIndex == array.Length)
                        {
                            break;
                        }

                        array[arrayIndex++] = tagsArray[i];
                    }

                    if (found)
                    {
                        this.tags = array;
                        return true;
                    }
                }
            }

            return false;
        }

        internal static IntermediateTuple Deserialize(ITupleDefinitionCreator creator, Uri baseUri, JsonObject jsonObject)
        {
            var definitionName = jsonObject.GetValueOrDefault<string>("type");
            var idJson = jsonObject.GetValueOrDefault<JsonObject>("id");
            var sourceLineNumbersJson = jsonObject.GetValueOrDefault<JsonObject>("ln");
            var fieldsJson = jsonObject.GetValueOrDefault<JsonArray>("fields");
            var tagsJson = jsonObject.GetValueOrDefault<JsonArray>("tags");

            var id = (idJson == null) ? null : Identifier.Deserialize(idJson);
            var sourceLineNumbers = (sourceLineNumbersJson == null) ? null : SourceLineNumber.Deserialize(sourceLineNumbersJson);

            creator.TryGetTupleDefinitionByName(definitionName, out var definition); // TODO: this isn't sufficient.
            var tuple = definition.CreateTuple(sourceLineNumbers, id);

            for (var i = 0; i < fieldsJson.Count && i < tuple.Fields.Length; ++i)
            {
                if (fieldsJson[i] is JsonObject fieldJson)
                {
                    tuple.Fields[i] = IntermediateField.Deserialize(tuple.Definition.FieldDefinitions[i], baseUri, fieldJson);
                }
            }

            if (tagsJson == null || tagsJson.Count == 0)
            {
            }
            else if (tagsJson.Count == 1)
            {
                tuple.tags = (string)tagsJson[0];
            }
            else
            {
                var tags = new string[tagsJson.Count];

                for (var i = 0; i < tagsJson.Count; ++i)
                {
                    tags[i] = (string)tagsJson[i];
                }

                tuple.tags = tags;
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

            if (this.tags is string || this.tags is string[])
            {
                JsonArray tagsJson;

                if (this.tags is string tag)
                {
                    tagsJson = new JsonArray(1) { tag };
                }
                else
                {
                    var array = (string[])this.tags;
                    tagsJson = new JsonArray(array.Length);
                    tagsJson.AddRange(array);
                }

                jsonObject.Add("tags", tagsJson);
            }

            return jsonObject;
        }
    }
}
