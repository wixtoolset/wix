// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System.Collections.Generic;

    internal static class DictionaryExtensions
    {
        public static V GetValueOrDefault<K, V>(this IDictionary<K, V> dictionary, K key, V defaultValue = default(V))
        {
            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }
    }
}
