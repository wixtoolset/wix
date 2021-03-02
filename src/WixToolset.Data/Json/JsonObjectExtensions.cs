// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using SimpleJson;

    internal static class JsonObjectExtensions
    {
        public static JsonObject AddNonDefaultValue(this JsonObject jsonObject, string key, bool value, bool defaultValue = default(bool))
        {
            if (value != defaultValue)
            {
                jsonObject.Add(key, value);
            }

            return jsonObject;
        }

        public static JsonObject AddNonDefaultValue(this JsonObject jsonObject, string key, int value, int defaultValue = default(int))
        {
            if (value != defaultValue)
            {
                jsonObject.Add(key, value);
            }

            return jsonObject;
        }

        public static JsonObject AddNonDefaultValue(this JsonObject jsonObject, string key, object value, object defaultValue = null)
        {
            if (value != defaultValue)
            {
                jsonObject.Add(key, value);
            }

            return jsonObject;
        }

        public static JsonObject AddIsNotNullOrEmpty(this JsonObject jsonObject, string key, string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                jsonObject.Add(key, value);
            }

            return jsonObject;
        }

        public static bool GetValueOrDefault(this JsonObject jsonObject, string key, bool defaultValue)
        {
            return jsonObject.TryGetValue(key, out var value) ? Convert.ToBoolean(value) : defaultValue;
        }

        public static int GetValueOrDefault(this JsonObject jsonObject, string key, int defaultValue)
        {
            return jsonObject.TryGetValue(key, out var value) ? Convert.ToInt32(value) : defaultValue;
        }

        public static int? GetValueOrDefault(this JsonObject jsonObject, string key, int? defaultValue)
        {
            return jsonObject.TryGetValue(key, out var value) ? Convert.ToInt32(value) : defaultValue;
        }

        public static T GetValueOrDefault<T>(this JsonObject jsonObject, string key, T defaultValue = default(T)) where T : class
        {
            return jsonObject.TryGetValue(key, out var value) ? value as T : defaultValue;
        }

        public static T GetEnumOrDefault<T>(this JsonObject jsonObject, string key, T defaultValue) where T : struct
        {
#if DEBUG
            if (!typeof(T).IsEnum) { throw new ArgumentException("This method only supports enums.", nameof(T)); }
#endif
            var value = jsonObject.GetValueOrDefault<string>(key);
            return Enum.TryParse(value, true, out T e) ? e : defaultValue;
        }
    }
}
