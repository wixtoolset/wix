// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    public static class IntermediateTupleExtensions
    {
        public static bool AsBool(this IntermediateTuple tuple, int index) => tuple?.Fields[index].AsBool() ?? false;

        public static bool? AsNullableBool(this IntermediateTuple tuple, int index) => tuple?.Fields[index].AsNullableBool();

        public static int AsNumber(this IntermediateTuple tuple, int index) => tuple?.Fields[index].AsNumber() ?? 0;

        public static int? AsNullableNumber(this IntermediateTuple tuple, int index) => tuple?.Fields[index].AsNullableNumber();

        public static string AsString(this IntermediateTuple tuple, int index) => tuple?.Fields[index].AsString();

        public static IntermediateField Set(this IntermediateTuple tuple, int index, bool value)
        {
            var definition = tuple.Definition.FieldDefinitions[index];

            var field = tuple.Fields[index].Set(definition, value);

            return tuple.Fields[index] = field;
        }

        public static IntermediateField Set(this IntermediateTuple tuple, int index, bool? value)
        {
            if (value == null && NoFieldMetadata(tuple, index))
            {
                return tuple.Fields[index] = null;
            }

            var definition = tuple.Definition.FieldDefinitions[index];

            var field = tuple.Fields[index].Set(definition, value);

            return tuple.Fields[index] = field;
        }

        public static IntermediateField Set(this IntermediateTuple tuple, int index, long value)
        {
            var definition = tuple.Definition.FieldDefinitions[index];

            var field = tuple.Fields[index].Set(definition, value);

            return tuple.Fields[index] = field;
        }

        public static IntermediateField Set(this IntermediateTuple tuple, int index, long? value)
        {
            if (value == null && NoFieldMetadata(tuple, index))
            {
                return tuple.Fields[index] = null;
            }

            var definition = tuple.Definition.FieldDefinitions[index];

            var field = tuple.Fields[index].Set(definition, value);

            return tuple.Fields[index] = field;
        }

        public static IntermediateField Set(this IntermediateTuple tuple, int index, int value)
        {
            var definition = tuple.Definition.FieldDefinitions[index];

            var field = tuple.Fields[index].Set(definition, value);

            return tuple.Fields[index] = field;
        }

        public static IntermediateField Set(this IntermediateTuple tuple, int index, int? value)
        {
            if (value == null && NoFieldMetadata(tuple, index))
            {
                return tuple.Fields[index] = null;
            }

            var definition = tuple.Definition.FieldDefinitions[index];

            var field = tuple.Fields[index].Set(definition, value);

            return tuple.Fields[index] = field;
        }

        public static IntermediateField Set(this IntermediateTuple tuple, int index, IntermediateFieldPathValue value)
        {
            if (value == null && NoFieldMetadata(tuple, index))
            {
                return tuple.Fields[index] = null;
            }

            var definition = tuple.Definition.FieldDefinitions[index];

            var field = tuple.Fields[index].Set(definition, value);

            return tuple.Fields[index] = field;
        }

        public static IntermediateField Set(this IntermediateTuple tuple, int index, string value)
        {
            if (value == null && NoFieldMetadata(tuple, index))
            {
                return tuple.Fields[index] = null;
            }

            var definition = tuple.Definition.FieldDefinitions[index];

            var field = tuple.Fields[index].Set(definition, value);

            return tuple.Fields[index] = field;
        }

        private static bool NoFieldMetadata(IntermediateTuple tuple, int index)
        {
            var field = tuple?.Fields[index];

            return field?.Context == null && field?.PreviousValue == null;
        }
    }
}
