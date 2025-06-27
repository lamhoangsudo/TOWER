using System;
using System.Collections.Generic;

namespace Nav3D.Pathfinding
{
    public class MinHeap<T> where T : IComparable<T>
    {
        #region Constants

        const int DEFAULT_HEAP_SIZE = 32;

        #endregion

        #region Attributes

        List<T> m_Data;

        #endregion

        #region Properties

        public int Count => m_Data.Count;

        public int Capacity => m_Data.Capacity;

        #endregion

        #region Constructors

        public MinHeap()
        {
            m_Data = new List<T>(DEFAULT_HEAP_SIZE);
        }

        public MinHeap(int _Capacity)
        {
            if (_Capacity < 1) throw new ArgumentException("Capacity must be greater than zero");
            m_Data = new List<T>(_Capacity);
        }

        #endregion

        #region Public methods

        public void Add(T _Item)
        {
            int index = m_Data.Count;
            var parent = (index - 1) >> 1; ;
            m_Data.Add(_Item);

            while (index > 0 && _Item.CompareTo(m_Data[parent]) < 0)
            {
                // There is no need to do a “swap”,
                // because the value with the number “index”
                // is stored either in one of the child elements
                // or it is an “item”.
                m_Data[index] = m_Data[parent];
                index = parent;
                parent = (index - 1) >> 1; ;
            }

            m_Data[index] = _Item;
        }

        public T GetMin()
        {
            if (m_Data.Count == 0) throw new InvalidOperationException("Cannot get min, heap is empty.");
            return m_Data[0];
        }

        public T PopMin()
        {
            if (m_Data.Count == 0) throw new InvalidOperationException("Cannot pop min, heap is empty.");
            var res = m_Data[0];

            m_Data[0] = m_Data[m_Data.Count - 1];
            var parent = 0;
            var item = m_Data[parent];

            while (true)
            {
                int leftChild = (parent << 1) + 1;
                if (leftChild >= m_Data.Count) break;

                int rightChild = (parent << 1) + 2;
                int minChildIndex;

                if (rightChild >= m_Data.Count ||
                    m_Data[leftChild].CompareTo(m_Data[rightChild]) < 0) minChildIndex = leftChild;
                else minChildIndex = rightChild;

                if (item.CompareTo(m_Data[minChildIndex]) < 0) break;

                // There is no need to do a “swap”,
                // because the value with the number “parent”
                // is stored either in its parent element
                // or it is an removed min element.
                m_Data[parent] = m_Data[minChildIndex];
                parent = minChildIndex;
            }

            m_Data[parent] = item;
            m_Data.RemoveAt(m_Data.Count - 1);

            return res;
        }

        public void Clear()
        {
            m_Data = new List<T>(DEFAULT_HEAP_SIZE);
        }

        public List<T> ToList()
        {
            return m_Data;
        }

        public override string ToString()
        {
            string str = "MinHeap{";
            if (m_Data.Count > 0)
            {
                str += m_Data[0];
                for (var i = 1; i < m_Data.Count; ++i)
                {
                    str += ", " + m_Data[i];
                }
            }

            str += "}";
            return base.ToString();
        }

        public void TestValidity()
        {
            for (var i = 1; i < m_Data.Count; ++i)
            {
                var parent = (i - 1) >> 1;
                if (m_Data[parent].CompareTo(m_Data[i]) <= 0) continue;
                throw new Exception("Parent " + parent + " greater than child " + i + "\n" + ToString());
            }
        }

        #endregion
    }
}
