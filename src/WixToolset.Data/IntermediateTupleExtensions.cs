// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    public static class IntermediateTupleExtensions
    {
        public static IntermediateField Set(this IntermediateTuple tuple, int index, object value)
        {
            var definition = tuple.Definition.FieldDefinitions[index];

            var field = tuple.Fields[index].Set(definition, value); ;

            return tuple.Fields[index] = field;
        }

        public static bool AsBool(this IntermediateTuple tuple, int index)
        {
            return tuple?.Fields[index].AsBool() ?? false;
        }

        public static bool? AsNullableBool(this IntermediateTuple tuple, int index)
        {
            return tuple?.Fields[index].AsNullableBool();
        }

        public static int AsNumber(this IntermediateTuple tuple, int index)
        {
            return tuple?.Fields[index].AsNumber() ?? 0;
        }

        public static int? AsNullableNumber(this IntermediateTuple tuple, int index)
        {
            return tuple?.Fields[index].AsNullableNumber();
        }

        public static string AsString(this IntermediateTuple tuple, int index)
        {
            return tuple?.Fields[index].AsString();
        }
    }
}
