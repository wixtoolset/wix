// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using System.Diagnostics;
    using SimpleJson;

    [DebuggerDisplay("Name={Name,nq} Type={Type} Value={Value?.AsString()}")]
    public class IntermediateField
    {
        public IntermediateField(IntermediateFieldDefinition definition) => this.Definition = definition;

        public IntermediateFieldDefinition Definition { get; }

        public string Name => this.Definition.Name;

        public IntermediateFieldType Type => this.Definition.Type;

        public string Context => this.Value?.Context;

        public IntermediateFieldValue PreviousValue => this.Value?.PreviousValue;

        internal IntermediateFieldValue Value { get; set; }

        public static explicit operator bool(IntermediateField field) => field.AsBool();

        public static explicit operator bool? (IntermediateField field) => field.AsNullableBool();

        public static explicit operator int(IntermediateField field) => field.AsNumber();

        public static explicit operator int? (IntermediateField field) => field.AsNullableNumber();

        public static explicit operator string(IntermediateField field) => field.AsString();

        internal static IntermediateField Deserialize(IntermediateFieldDefinition definition, Uri baseUri, JsonObject jsonObject)
        {
            IntermediateField field = null;

            if (jsonObject != null)
            {
                field = new IntermediateField(definition);

                field.Value = IntermediateFieldValue.Deserialize(jsonObject, baseUri, definition.Type);
            }

            return field;
        }

        internal JsonObject Serialize() => this.Value?.Serialize();
    }
}
