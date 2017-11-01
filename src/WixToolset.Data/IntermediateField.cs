// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System.Diagnostics;
    using SimpleJson;

    [DebuggerDisplay("Name={Name,nq} Type={Type} Value={Value.AsString()}")]
    public class IntermediateField
    {
        public IntermediateField(IntermediateFieldDefinition definition)
        {
            this.Definition = definition;
        }

        public IntermediateFieldDefinition Definition { get; }

        public string Name => this.Definition.Name;

        public IntermediateFieldType Type => this.Definition.Type;

        public string Context => this.Value?.Context;

        public IntermediateFieldValue PreviousValue => this.Value?.PreviousValue;

        internal IntermediateFieldValue Value { get; set; }

        public static explicit operator bool(IntermediateField field)
        {
            return field.AsBool();
        }

        public static explicit operator bool? (IntermediateField field)
        {
            return field.AsNullableBool();
        }

        public static explicit operator int(IntermediateField field)
        {
            return field.AsNumber();
        }

        public static explicit operator int? (IntermediateField field)
        {
            return field.AsNullableNumber();
        }

        public static explicit operator string(IntermediateField field)
        {
            return field.AsString();
        }

        internal static IntermediateField Deserialize(IntermediateFieldDefinition definition, JsonObject jsonObject)
        {
            var field = new IntermediateField(definition);

            if (jsonObject != null)
            {
                field.Value = IntermediateFieldValue.Deserialize(jsonObject);
            }

            return field;
        }

        internal JsonObject Serialize()
        {
            return this.Value?.Serialize();
        }
    }
}
