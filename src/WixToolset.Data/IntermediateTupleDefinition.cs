// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using SimpleJson;

    public class IntermediateTupleDefinition
    {
        private object tags;

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

        internal static IntermediateTupleDefinition Deserialize(JsonObject jsonObject)
        {
            var name = jsonObject.GetValueOrDefault<string>("name");
            var revision = jsonObject.GetValueOrDefault("rev", 0);
            var definitionsJson = jsonObject.GetValueOrDefault<JsonArray>("fields");
            var tagsJson = jsonObject.GetValueOrDefault<JsonArray>("tags");

            var fieldDefinitions = new IntermediateFieldDefinition[definitionsJson.Count];

            for (var i = 0; i < definitionsJson.Count; ++i)
            {
                var definitionJson = (JsonObject)definitionsJson[i];
                var fieldName = definitionJson.GetValueOrDefault<string>("name");
                var fieldType = definitionJson.GetEnumOrDefault("type", IntermediateFieldType.String);
                fieldDefinitions[i] = new IntermediateFieldDefinition(fieldName, fieldType);
            }

            var definition = new IntermediateTupleDefinition(name, revision, fieldDefinitions, null);

            if (tagsJson == null || tagsJson.Count == 0)
            {
            }
            else if (tagsJson.Count == 1)
            {
                definition.tags = (string)tagsJson[0];
            }
            else
            {
                var tags = new string[tagsJson.Count];

                for (var i = 0; i < tagsJson.Count; ++i)
                {
                    tags[i] = (string)tagsJson[i];
                }

                definition.tags = tags;
            }

            return definition;
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
