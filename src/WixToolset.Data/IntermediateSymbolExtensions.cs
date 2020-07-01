// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    public static class IntermediateSymbolExtensions
    {
        public static bool AsBool(this IntermediateSymbol symbol, int index) => symbol?.Fields[index].AsBool() ?? false;

        public static bool? AsNullableBool(this IntermediateSymbol symbol, int index) => symbol?.Fields[index].AsNullableBool();

        public static int AsNumber(this IntermediateSymbol symbol, int index) => symbol?.Fields[index].AsNumber() ?? 0;

        public static int? AsNullableNumber(this IntermediateSymbol symbol, int index) => symbol?.Fields[index].AsNullableNumber();

        public static string AsString(this IntermediateSymbol symbol, int index) => symbol?.Fields[index].AsString();

        public static IntermediateField Set(this IntermediateSymbol symbol, int index, bool value)
        {
            var definition = symbol.Definition.FieldDefinitions[index];

            var field = symbol.Fields[index].Set(definition, value);

            return symbol.Fields[index] = field;
        }

        public static IntermediateField Set(this IntermediateSymbol symbol, int index, bool? value)
        {
            if (value == null && NoFieldMetadata(symbol, index))
            {
                return symbol.Fields[index] = null;
            }

            var definition = symbol.Definition.FieldDefinitions[index];

            var field = symbol.Fields[index].Set(definition, value);

            return symbol.Fields[index] = field;
        }

        public static IntermediateField Set(this IntermediateSymbol symbol, int index, long value)
        {
            var definition = symbol.Definition.FieldDefinitions[index];

            var field = symbol.Fields[index].Set(definition, value);

            return symbol.Fields[index] = field;
        }

        public static IntermediateField Set(this IntermediateSymbol symbol, int index, long? value)
        {
            if (value == null && NoFieldMetadata(symbol, index))
            {
                return symbol.Fields[index] = null;
            }

            var definition = symbol.Definition.FieldDefinitions[index];

            var field = symbol.Fields[index].Set(definition, value);

            return symbol.Fields[index] = field;
        }

        public static IntermediateField Set(this IntermediateSymbol symbol, int index, int value)
        {
            var definition = symbol.Definition.FieldDefinitions[index];

            var field = symbol.Fields[index].Set(definition, value);

            return symbol.Fields[index] = field;
        }

        public static IntermediateField Set(this IntermediateSymbol symbol, int index, int? value)
        {
            if (value == null && NoFieldMetadata(symbol, index))
            {
                return symbol.Fields[index] = null;
            }

            var definition = symbol.Definition.FieldDefinitions[index];

            var field = symbol.Fields[index].Set(definition, value);

            return symbol.Fields[index] = field;
        }

        public static IntermediateField Set(this IntermediateSymbol symbol, int index, IntermediateFieldPathValue value)
        {
            if (value?.Path == null && value?.BaseUri == null && NoFieldMetadata(symbol, index))
            {
                return symbol.Fields[index] = null;
            }

            var definition = symbol.Definition.FieldDefinitions[index];

            var field = symbol.Fields[index].Set(definition, value);

            return symbol.Fields[index] = field;
        }

        public static IntermediateField Set(this IntermediateSymbol symbol, int index, string value)
        {
            if (value == null && NoFieldMetadata(symbol, index))
            {
                return symbol.Fields[index] = null;
            }

            var definition = symbol.Definition.FieldDefinitions[index];

            var field = symbol.Fields[index].Set(definition, value);

            return symbol.Fields[index] = field;
        }

        private static bool NoFieldMetadata(IntermediateSymbol symbol, int index)
        {
            var field = symbol?.Fields[index];

            return field?.Context == null && field?.PreviousValue == null;
        }
    }
}
