// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Diagnostics;
    using SimpleJson;

    [DebuggerDisplay("{Data}")]
    public class IntermediateFieldValue
    {
        public string Context { get; internal set; }

        internal object Data { get; set; }

        public IntermediateFieldValue PreviousValue { get; internal set; }

        public static explicit operator bool(IntermediateFieldValue value)
        {
            return value.AsBool();
        }

        public static explicit operator bool? (IntermediateFieldValue value)
        {
            return value.AsNullableBool();
        }

        public static explicit operator int(IntermediateFieldValue value)
        {
            return value.AsNumber();
        }

        public static explicit operator int? (IntermediateFieldValue value)
        {
            return value.AsNullableNumber();
        }

        public static explicit operator IntermediateFieldPathValue(IntermediateFieldValue value)
        {
            return value.AsPath();
        }

        public static explicit operator string(IntermediateFieldValue value)
        {
            return value.AsString();
        }

        internal static IntermediateFieldValue Deserialize(JsonObject jsonObject)
        {
            var context = jsonObject.GetValueOrDefault<string>("context");
            if (!jsonObject.TryGetValue("data", out var data))
            {
                throw new ArgumentException();
            }

            var value = data;

            if (data is JsonObject jsonData)
            {
                Uri baseUri = null;

                if (jsonData.TryGetValue("baseUri", out var baseUriValue) && baseUriValue is string)
                {
                    baseUri = new Uri((string)baseUriValue);
                }
                jsonData.TryGetValue("embeddedIndex", out var embeddedIndex);

                value = new IntermediateFieldPathValue
                {
                    BaseUri = baseUri,
                    EmbeddedFileIndex = (int?)embeddedIndex,
                    Path = jsonData.GetValueOrDefault<string>("path"),
                };
            }

            var previousValueJson = jsonObject.GetValueOrDefault<JsonObject>("prev");
            var previousValue = (previousValueJson == null) ? null : IntermediateFieldValue.Deserialize(previousValueJson);

            return new IntermediateFieldValue
            {
                Context = context,
                Data = value,
                PreviousValue = previousValue
            };
        }

        internal JsonObject Serialize()
        {
            var jsonObject = new JsonObject();

            if (!String.IsNullOrEmpty(this.Context))
            {
                jsonObject.Add("context", this.Context);
            }

            if (this.Data is IntermediateFieldPathValue pathField)
            {
                var jsonData = new JsonObject();

                if (pathField.BaseUri != null)
                {
                    jsonData.Add("baseUri", pathField.BaseUri.AbsoluteUri);
                }

                if (pathField.EmbeddedFileIndex.HasValue)
                {
                    jsonData.Add("embeddedIndex", pathField.EmbeddedFileIndex.Value);
                }

                if (!String.IsNullOrEmpty(pathField.Path))
                {
                    jsonData.Add("path", pathField.Path);
                }

                jsonObject.Add("data", jsonData);
            }
            else
            {
                jsonObject.Add("data", this.Data);
            }

            if (this.PreviousValue != null)
            {
                var previousValueJson = this.PreviousValue.Serialize();
                jsonObject.Add("prev", previousValueJson);
            }

            return jsonObject;
        }
    }
}
