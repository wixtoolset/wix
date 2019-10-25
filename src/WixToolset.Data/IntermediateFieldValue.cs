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

        public static explicit operator bool(IntermediateFieldValue value) => value.AsBool();

        public static explicit operator bool? (IntermediateFieldValue value) => value.AsNullableBool();

        public static explicit operator int(IntermediateFieldValue value) => value.AsNumber();

        public static explicit operator int? (IntermediateFieldValue value) => value.AsNullableNumber();

        public static explicit operator IntermediateFieldPathValue(IntermediateFieldValue value) => value.AsPath();

        public static explicit operator string(IntermediateFieldValue value) => value.AsString();

        internal static IntermediateFieldValue Deserialize(JsonObject jsonObject, Uri baseUri, IntermediateFieldType type)
        {
            var context = jsonObject.GetValueOrDefault<string>("context");
            if (!jsonObject.TryGetValue("data", out var data))
            {
                throw new ArgumentException();
            }

            var value = data;

            switch (value)
            {
            case int intData:
                switch (type)
                {
                case IntermediateFieldType.Bool:
                    value = intData != 0;
                    break;

                case IntermediateFieldType.LargeNumber:
                    value = Convert.ToInt64(data);
                    break;

                case IntermediateFieldType.Path:
                case IntermediateFieldType.String:
                    value = intData.ToString();
                    break;
                }
                break;

            case long longData:
                switch (type)
                {
                case IntermediateFieldType.Bool:
                    value = longData != 0;
                    break;

                case IntermediateFieldType.Number:
                    value = Convert.ToInt32(longData);
                    break;

                case IntermediateFieldType.Path:
                case IntermediateFieldType.String:
                    value = longData.ToString();
                    break;
                }
                break;

            case JsonObject jsonData:
                jsonData.TryGetValue("embed", out var embed);

                value = new IntermediateFieldPathValue
                {
                    BaseUri = (embed != null) ? baseUri : null,
                    Embed = embed != null,
                    Path = jsonData.GetValueOrDefault<string>("path"),
                };
                break;

            // Nothing to do for this case, so leave it out.
            // case string stringData:
            //     break;
            }

            var previousValueJson = jsonObject.GetValueOrDefault<JsonObject>("prev");
            var previousValue = (previousValueJson == null) ? null : IntermediateFieldValue.Deserialize(previousValueJson, baseUri, type);

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

                // pathField.BaseUri is set during load, not saved.

                if (pathField.Embed)
                {
                    jsonData.Add("embed", "true");
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
