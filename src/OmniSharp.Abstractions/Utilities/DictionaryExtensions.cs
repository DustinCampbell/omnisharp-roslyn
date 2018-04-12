using System;
using System.Collections.Generic;

namespace OmniSharp.Utilities
{
    internal static class DictionaryExtensions
    {
        public static TValue GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> valueGetter)
        {
            if (!dictionary.TryGetValue(key, out var value))
            {
                value = valueGetter(key);
                dictionary.Add(key, value);
            }

            return value;
        }

        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> keyValuePair, out TKey key, out TValue value)
            => (key, value) = (keyValuePair.Key, keyValuePair.Value);
    }
}
