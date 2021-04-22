// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;

    public static class IntermediateFieldExtensions
    {
        [ThreadStatic]
        internal static string valueContext;

        public static bool IsNull(this IntermediateField field) => field?.Value?.Data == null;

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

            case IntermediateFieldType.LargeNumber:
            case IntermediateFieldType.Number:
                return field.Value.AsLargeNumber() != 0;

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

            return field.AsBool();
        }

        public static long AsLargeNumber(this IntermediateField field)
        {
            if (field == null || field.Value == null || field.Value.Data == null)
            {
                return 0;
            }

            switch (field.Definition.Type)
            {
            case IntermediateFieldType.Bool:
                return field.Value.AsBool() ? 1 : 0;

            case IntermediateFieldType.LargeNumber:
            case IntermediateFieldType.Number:
                return field.Value.AsLargeNumber();

            case IntermediateFieldType.String:
                return Convert.ToInt64(field.Value.AsString());

            default:
                throw new InvalidCastException($"Cannot convert field {field.Name} with type {field.Type} to large number");
            }
        }

        public static long? AsNullableLargeNumber(this IntermediateField field)
        {
            if (field == null || field.Value == null || field.Value.Data == null)
            {
                return null;
            }

            return field.AsLargeNumber();
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

            case IntermediateFieldType.LargeNumber:
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

            return field.AsNumber();
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

            case IntermediateFieldType.LargeNumber:
            case IntermediateFieldType.Number:
                return field.Value.AsLargeNumber().ToString();

            case IntermediateFieldType.String:
                return field.Value.AsString();

            case IntermediateFieldType.Path:
                return field.Value.AsPath()?.Path;

            default:
                throw new InvalidCastException($"Cannot convert field {field.Name} with type {field.Type} to string");
            }
        }

        public static object AsObject(this IntermediateField field)
        {
            return field?.Value.Data;
        }

        public static IntermediateField Set(this IntermediateField field, bool value)
        {
            object data;

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            switch (field.Type)
            {
            case IntermediateFieldType.Bool:
                data = value;
                break;

            case IntermediateFieldType.LargeNumber:
                data = value ? (long)1 : (long)0;
                break;

            case IntermediateFieldType.Number:
                data = value ? 1 : 0;
                break;

            case IntermediateFieldType.Path:
                throw new ArgumentException($"Cannot convert bool '{value}' to a 'Path' field type.", nameof(value));

            case IntermediateFieldType.String:
                data = value ? "true" : "false";
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(value), $"Unknown intermediate field type: {value.GetType()}");
            };

            return AssignFieldValue(field, data);
        }

        public static IntermediateField Set(this IntermediateField field, bool? value)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            return value.HasValue ? field.Set(value.Value) : AssignFieldValue(field, null);
        }

        public static IntermediateField Set(this IntermediateField field, long value)
        {
            object data;

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            switch (field.Type)
            {
            case IntermediateFieldType.Bool:
                data = (value != 0);
                break;

            case IntermediateFieldType.LargeNumber:
                data = value;
                break;

            case IntermediateFieldType.Number:
                data = (int)value;
                break;

            case IntermediateFieldType.Path:
                throw new ArgumentException($"Cannot convert large number '{value}' to a 'Path' field type.", nameof(value));

            case IntermediateFieldType.String:
                data = value.ToString();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(value), $"Unknown intermediate field type: {value.GetType()}");
            };

            return AssignFieldValue(field, data);
        }

        public static IntermediateField Set(this IntermediateField field, long? value)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            return value.HasValue ? field.Set(value.Value) : AssignFieldValue(field, null);
        }

        public static IntermediateField Set(this IntermediateField field, int value)
        {
            object data;

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            switch (field.Type)
            {
            case IntermediateFieldType.Bool:
                data = (value != 0);
                break;

            case IntermediateFieldType.LargeNumber:
                data = (long)value;
                break;

            case IntermediateFieldType.Number:
                data = value;
                break;

            case IntermediateFieldType.Path:
                throw new ArgumentException($"Cannot convert number '{value}' to a 'Path' field type.", nameof(value));

            case IntermediateFieldType.String:
                data = value.ToString();
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(value), $"Unknown intermediate field type: {value.GetType()}");
            };

            return AssignFieldValue(field, data);
        }

        public static IntermediateField Set(this IntermediateField field, int? value)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            return value.HasValue ? field.Set(value.Value) : AssignFieldValue(field, null);
        }

        public static IntermediateField Set(this IntermediateField field, IntermediateFieldPathValue value)
        {
            object data;

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }
            else if (value == null) // null is always allowed.
            {
                data = null;
            }
            else
            {
                switch (field.Type)
                {
                case IntermediateFieldType.Bool:
                    throw new ArgumentException($"Cannot convert path '{value.Path}' to a 'bool' field type.", nameof(value));

                case IntermediateFieldType.LargeNumber:
                    throw new ArgumentException($"Cannot convert path '{value.Path}' to a 'large number' field type.", nameof(value));

                case IntermediateFieldType.Number:
                    throw new ArgumentException($"Cannot convert path '{value.Path}' to a 'number' field type.", nameof(value));

                case IntermediateFieldType.Path:
                    data = value;
                    break;

                case IntermediateFieldType.String:
                    data = value.Path;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(value), $"Unknown intermediate field type: {value.GetType()}");
                }
            }

            return AssignFieldValue(field, data);
        }

        public static IntermediateField Set(this IntermediateField field, string value)
        {
            object data;

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }
            else if (value == null) // Null is always allowed.
            {
                data = null;
            }
            else
            {
                switch (field.Type)
                {
                case IntermediateFieldType.Bool:
                    if (value.Equals("yes", StringComparison.OrdinalIgnoreCase) || value.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        data = true;
                    }
                    else if (value.Equals("no", StringComparison.OrdinalIgnoreCase) || value.Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        data = false;
                    }
                    else
                    {
                        throw new ArgumentException($"Cannot convert string '{value}' to a 'bool' field type.", nameof(value));
                    }
                    break;

                case IntermediateFieldType.LargeNumber:
                    if (Int64.TryParse(value, out var largeNumber))
                    {
                        data = largeNumber;
                    }
                    else
                    {
                        throw new ArgumentException($"Cannot convert string '{value}' to a 'large number' field type.", nameof(value));
                    }
                    break;

                case IntermediateFieldType.Number:
                    if (Int32.TryParse(value, out var number))
                    {
                        data = number;
                    }
                    else
                    {
                        throw new ArgumentException($"Cannot convert string '{value}' to a 'number' field type.", field.Name);
                    }
                    break;

                case IntermediateFieldType.Path:
                    data = new IntermediateFieldPathValue { Path = value };
                    break;

                case IntermediateFieldType.String:
                    data = value;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(value), $"Unknown intermediate field type: {value.GetType()}");
                }
            }

            return AssignFieldValue(field, data);
        }

        public static IntermediateField Set(this IntermediateField field, IntermediateFieldDefinition definition, bool value)
        {
            return EnsureField(field, definition).Set(value);
        }

        public static IntermediateField Set(this IntermediateField field, IntermediateFieldDefinition definition, bool? value)
        {
            return EnsureField(field, definition).Set(value);
        }

        public static IntermediateField Set(this IntermediateField field, IntermediateFieldDefinition definition, long value)
        {
            return EnsureField(field, definition).Set(value);
        }

        public static IntermediateField Set(this IntermediateField field, IntermediateFieldDefinition definition, long? value)
        {
            return EnsureField(field, definition).Set(value);
        }

        public static IntermediateField Set(this IntermediateField field, IntermediateFieldDefinition definition, int value)
        {
            return EnsureField(field, definition).Set(value);
        }

        public static IntermediateField Set(this IntermediateField field, IntermediateFieldDefinition definition, int? value)
        {
            return EnsureField(field, definition).Set(value);
        }

        public static IntermediateField Set(this IntermediateField field, IntermediateFieldDefinition definition, IntermediateFieldPathValue value)
        {
            return EnsureField(field, definition).Set(value);
        }

        public static IntermediateField Set(this IntermediateField field, IntermediateFieldDefinition definition, string value)
        {
            return EnsureField(field, definition).Set(value);
        }

        public static void Overwrite(this IntermediateField field, string value) => field.Value.Data = value;

        private static IntermediateField AssignFieldValue(IntermediateField field, object data)
        {
            field.Value = new IntermediateFieldValue
            {
                Context = valueContext,
                Data = data,
                PreviousValue = field.Value
            };

            return field;
        }

        private static IntermediateField EnsureField(IntermediateField field, IntermediateFieldDefinition definition)
        {
            return field ?? new IntermediateField(definition);
        }
    }
}
