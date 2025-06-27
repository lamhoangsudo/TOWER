using System;
using System.Collections;
using System.Collections.Concurrent;

namespace Nav3D.Common
{
    public class LimitedSizeQueue<T>
    {
        #region Attributes

        object m_LockObject = new object();

        int m_Limit;
        ConcurrentQueue<T> m_Queue;

        #endregion

        #region Properties

        public int Count => m_Queue.Count;
        public int Limit => m_Limit;

        #endregion

        #region Constructors

        public LimitedSizeQueue(int _MaxSize)
        {
            m_Limit = _MaxSize;
            m_Queue = new ConcurrentQueue<T>();

        }

        #endregion

        #region Public methods

        public void Enqueue(T _Value)
        {
            m_Queue.Enqueue(_Value);


            lock (m_LockObject)
            {
                T overflow;
                while (m_Queue.Count > m_Limit && m_Queue.TryDequeue(out overflow)) ;
            }
        }

        public T Dequeue()
        {
            if (m_Queue.Count > 0 && m_Queue.TryDequeue(out T result))
                return result;

            throw new InvalidOperationException("The queue is empty");
        }

        public T Peek()
        {
            if (m_Queue.Count > 0 && m_Queue.TryPeek(out T result))
                return result;

            throw new InvalidOperationException("The queue is empty");
        }

        public void Clear()
        {
            lock (m_LockObject)
                m_Queue = new ConcurrentQueue<T>();
        }

        public IEnumerator GetEnumerator()
        {
            return m_Queue.GetEnumerator();
        }

        #endregion
    }
}
