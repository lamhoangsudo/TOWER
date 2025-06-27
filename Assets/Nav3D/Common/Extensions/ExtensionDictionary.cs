using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Nav3D.Common
{
    public static class ExtensionDictionary
    {
        #region Public methods

        /// <summary>
        /// Alternative to default [] operator with both existance and null check.
        /// Does not proper for performance-sensitive code sections.
        /// </summary>
        /// <typeparam name="TKey">Key type</typeparam>
        /// <typeparam name="TValue">Default-constructible type</typeparam>
        /// <param name="_Dictionary">Dictionary</param>
        /// <param name="_Key">Key</param>
        /// <returns></returns>
        public static TValue GetOrAdd<TKey, TValue>(this Dictionary<TKey, TValue> _Dictionary, TKey _Key) where TValue : new()
        {
            if (_Dictionary.TryGetValue(_Key, out TValue value))
            {
                return value;
            }
            else
            {
                value = new TValue();
                _Dictionary.Add(_Key, value);

                return value;
            }
        }

        /// <summary>
        /// Alternative to default [] operator with both existance and null check.
        /// Does not proper for performance-sensitive code sections.
        /// </summary>
        /// <typeparam name="TKey">Key type</typeparam>
        /// <typeparam name="TValue">Default-constructible type</typeparam>
        /// <param name="_Dictionary">ConcurrentDictionary</param>
        /// <param name="_Key">Key</param>
        /// <returns></returns>
        public static TValue GetOrAdd<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> _Dictionary, TKey _Key) where TValue : new()
        {
            if (_Dictionary.TryGetValue(_Key, out TValue value))
            {
                return value;
            }
            else
            {
                value = new TValue();
                _Dictionary.TryAdd(_Key, value);

                return value;
            }
        }

        #endregion
    }
}