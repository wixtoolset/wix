// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;

    public static class IntermediateFieldExtensions
    {
        [ThreadStatic]
        internal static string valueContext;

        public static IntermediateField Set(this IntermediateField field, object value)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }
            else if (value == null)
            {
                // Null is always allowed.
            }
            else if (field.Type == IntermediateFieldType.Bool && !(value is bool))
            {
                throw new ArgumentException(nameof(value));
            }
            else if (field.Type == IntermediateFieldType.Number && !(value is int))
            {
                throw new ArgumentException(nameof(value));
            }
            else if (field.Type == IntermediateFieldType.String && !(value is string))
            {
                throw new ArgumentException(nameof(value));
            }
            else if (field.Type == IntermediateFieldType.Path && !(value is IntermediateFieldPathValue || value is string))
            {
                throw new ArgumentException(nameof(value));
            }

            if (field.Type == IntermediateFieldType.Path && value != null && value is string)
            {
                value = new IntermediateFieldPathValue { Path = (string)value };
            }

            field.Value = new IntermediateFieldValue
            {
                Context = valueContext,
                Data = value,
                PreviousValue = field.Value
            };

            return field;
        }

        public static IntermediateField Set(this IntermediateField field, IntermediateFieldDefinition definition, object value)
        {
            if (field == null)
            {
                field = new IntermediateField(definition);
            }

            return field.Set(value);
        }

        public static bool AsBool(this IntermediateField field)
        {
            if (field == null || field.Value == null || field.Value.Data == null)
            {
                return false;
            }

            switch (field.Definition.Type)
            {
                case IntermediateFieldType.Bool:
                    return field.Value.AsBool();

                case IntermediateFieldType.Number:
                    return field.Value.AsNumber() != 0;

                case IntermediateFieldType.String:
                    return !String.IsNullOrEmpty(field.Value.AsString());

                case IntermediateFieldType.Path:
                    return !String.IsNullOrEmpty(field.Value.AsPath()?.Path);

                default:
                    throw new InvalidCastException($"Cannot convert field {field.Name} with type {field.Type} to boolean");
            }
        }

        public static bool? AsNullableBool(this IntermediateField field)
        {
            if (field == null || field.Value == null || field.Value.Data == null)
            {
                return null;
            }

            switch (field.Definition.Type)
            {
                case IntermediateFieldType.Bool:
                    return field.Value.AsBool();

                case IntermediateFieldType.Number:
                    return field.Value.AsNumber() != 0;

                case IntermediateFieldType.String:
                    return !System.String.IsNullOrEmpty(field.Value.AsString());

                default:
                    throw new InvalidCastException($"Cannot convert field {field.Name} with type {field.Type} to boolean");
            }
        }


        public static int AsNumber(this IntermediateField field)
        {
            if (field == null || field.Value == null || field.Value.Data == null)
            {
                return 0;
            }

            switch (field.Definition.Type)
            {
                case IntermediateFieldType.Bool:
                    return field.Value.AsBool() ? 1 : 0;

                case IntermediateFieldType.Number:
                    return field.Value.AsNumber();

                case IntermediateFieldType.String:
                    return Convert.ToInt32(field.Value.AsString());

                default:
                    throw new InvalidCastException($"Cannot convert field {field.Name} with type {field.Type} to number");
            }
        }

        public static int? AsNullableNumber(this IntermediateField field)
        {
            if (field == null || field.Value == null || field.Value.Data == null)
            {
                return null;
            }

            switch (field.Definition.Type)
            {
                case IntermediateFieldType.Bool:
                    return field.Value.AsBool() ? 1 : 0;

                case IntermediateFieldType.Number:
                    return field.Value.AsNumber();

                case IntermediateFieldType.String:
                    return Convert.ToInt32(field.Value.AsString());

                default:
                    throw new InvalidCastException($"Cannot convert field {field.Name} with type {field.Type} to number");
            }
        }

        public static IntermediateFieldPathValue AsPath(this IntermediateField field)
        {
            if (field == null || field.Value == null || field.Value.Data == null)
            {
                return null;
            }

            switch (field.Definition.Type)
            {
                case IntermediateFieldType.String:
                    return new IntermediateFieldPathValue { Path = field.Value.AsString() };

                case IntermediateFieldType.Path:
                    return field.Value.AsPath();

                default:
                    throw new InvalidCastException($"Cannot convert field {field.Name} with type {field.Type} to string");
            }
        }

        public static string AsString(this IntermediateField field)
        {
            if (field == null || field.Value == null || field.Value.Data == null)
            {
                return null;
            }

            switch (field.Definition.Type)
            {
                case IntermediateFieldType.Bool:
                    return field.Value.AsBool() ? "true" : "false";

                case IntermediateFieldType.Number:
                    return field.Value.AsNumber().ToString();

                case IntermediateFieldType.String:
                    return field.Value.AsString();

                case IntermediateFieldType.Path:
                    return field.Value.AsPath()?.Path;

                default:
                    throw new InvalidCastException($"Cannot convert field {field.Name} with type {field.Type} to string");
            }
        }
    }
}
