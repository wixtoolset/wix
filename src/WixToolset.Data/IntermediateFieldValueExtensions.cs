// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    public static class IntermediateFieldValueExtensions
    {
        public static bool AsBool(this IntermediateFieldValue value)
        {
            return value?.Data == null ? false : (bool)value.Data;
        }

        public static bool? AsNullableBool(this IntermediateFieldValue value)
        {
            return (bool?)value?.Data;
        }

        public static int AsNumber(this IntermediateFieldValue value)
        {
            return value?.Data == null ? 0 : (int)value.Data;
        }

        public static int? AsNullableNumber(this IntermediateFieldValue value)
        {
            return (int?)value?.Data;
        }

        public static IntermediateFieldPathValue AsPath(this IntermediateFieldValue value)
        {
            return (IntermediateFieldPathValue)value?.Data;
        }

        public static string AsString(this IntermediateFieldValue value)
        {
            return (string)value?.Data;
        }
    }
}
