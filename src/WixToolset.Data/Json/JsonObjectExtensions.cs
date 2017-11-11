// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using SimpleJson;

    internal static class JsonObjectExtensions
    {
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
            return jsonObject.TryGetValue(key, out var value) ? value as T: defaultValue;
        }

        public static T GetEnumOrDefault<T>(this JsonObject jsonObject, string key, T defaultValue) where T : struct
        {
#if DEBUG
            if (!typeof(T).IsEnum) throw new ArgumentException("This method is designed to only only support enums.", nameof(T));
#endif
            var value = jsonObject.GetValueOrDefault<string>(key);
            return Enum.TryParse(value, true, out T e) ? e : defaultValue;
        }
    }
}
