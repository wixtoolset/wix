// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;

    public static class IntermediateFieldValueExtensions
    {
        public static bool AsBool(this IntermediateFieldValue value)
        {
            if (value.Data is bool b)
            {
                return b;
            }
            else if (value.Data is int n)
            {
                return n != 0;
            }
            else if (value.Data is long l)
            {
                return l != 0;
            }
            else if (value.Data is string s)
            {
                if (s.Equals("yes", StringComparison.OrdinalIgnoreCase) || s.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                else if (s.Equals("no", StringComparison.OrdinalIgnoreCase) || s.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return (bool)value.Data;
        }

        public static bool? AsNullableBool(this IntermediateFieldValue value)
        {
            if (value?.Data == null)
            {
                return null;
            }

            return value.AsBool();
        }

        public static long AsLargeNumber(this IntermediateFieldValue value)
        {
            if (value.Data is long l)
            {
                return l;
            }
            else if (value.Data is int n)
            {
                return n;
            }
            else if (value.Data is bool b)
            {
                return b ? 1 : 0;
            }
            else if (value.Data is string s)
            {
                try
                {
                    return Convert.ToInt32(s);
                }
                catch (FormatException)
                {
                    throw new WixException(ErrorMessages.UnableToConvertFieldToNumber(s));
                }
            }

            return (long)value.Data;
        }

        public static long? AsNullableLargeNumber(this IntermediateFieldValue value)
        {
            if (value?.Data == null)
            {
                return null;
            }

            return value.AsLargeNumber();
        }

        public static int AsNumber(this IntermediateFieldValue value)
        {
            if (value.Data is int n)
            {
                return n;
            }
            else if (value.Data is long l)
            {
                return (int)l;
            }
            else if (value.Data is bool b)
            {
                return b ? 1 : 0;
            }
            else if (value.Data is string s)
            {
                try
                {
                    return Convert.ToInt32(s);
                }
                catch (FormatException)
                {
                    throw new WixException(ErrorMessages.UnableToConvertFieldToNumber(s));
                }
            }

            return (int)value.Data;
        }

        public static int? AsNullableNumber(this IntermediateFieldValue value)
        {
            if (value?.Data == null)
            {
                return null;
            }

            return value.AsNumber();
        }

        public static IntermediateFieldPathValue AsPath(this IntermediateFieldValue value)
        {
            return (IntermediateFieldPathValue)value?.Data;
        }

        public static string AsString(this IntermediateFieldValue value)
        {
            if (value?.Data == null)
            {
                return null;
            }
            else if (value.Data is string s)
            {
                return s;
            }
            else if (value.Data is int n)
            {
                return n.ToString();
            }
            else if (value.Data is long l)
            {
                return l.ToString();
            }
            else if (value.Data is bool b)
            {
                return b ? "true" : "false";
            }
            else if (value.Data is IntermediateFieldPathValue p)
            {
                return p.Path;
            }

            return (string)value.Data;
        }
    }
}
