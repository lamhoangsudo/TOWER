using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections;

namespace Nav3D.Common
{
    public class ConcurrentHashSet<T> : IEnumerable<T>
    {
        #region Attributes

        readonly ConcurrentDictionary<T, byte> m_Dictionary = new ConcurrentDictionary<T, byte>();

        #endregion

        #region Properties

        public int Count => m_Dictionary.Count;

        #endregion

        #region IEnumerable

        public IEnumerator<T> GetEnumerator()
        {
            return m_Dictionary.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_Dictionary.GetEnumerator();
        }

        #endregion

        #region Public methods

        public bool TryAdd(T _Value)
        {
            return m_Dictionary.TryAdd(_Value, 0);
        }

        public bool TryRemove(T _Value)
        {
            return m_Dictionary.TryRemove(_Value, out _);
        }

        public void Clear()
        {
            m_Dictionary.Clear();
        }

        public HashSet<T> GetHashSetCopy()
        {
            return new HashSet<T>(m_Dictionary.Keys);
        }

        #endregion
    }
}