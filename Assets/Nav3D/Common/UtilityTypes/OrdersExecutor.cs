using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nav3D.Common
{
    public class OrdersExecutor<R, O> : IDisposable where O : IExecutable
    {
        #region Constants

        //ms
        const int    TASK_LIFE_TIME     = 500;
        const string LOG_ORDER_ENQUEUED = "{0} instance ({1}) has enqueued";
        
        #endregion
        
        #region Attributes
        
        volatile bool m_Alive;
        readonly int  m_MaxAliveOrders;
        volatile int  m_CurrentAliveOrders;
        
        volatile Task m_QueueUpdateTask;

        readonly ConcurrentQueue<R> m_Requesters = new ConcurrentQueue<R>();
        
        readonly ConcurrentDictionary<R, O> m_OrdersPending = new ConcurrentDictionary<R, O>();
        readonly ConcurrentDictionary<R, O> m_OrdersRunning  = new ConcurrentDictionary<R, O>();
        
        readonly object m_LockObject = new object();
        
        #endregion
        
        #region Properties

        public int MaxAliveOrders     => m_MaxAliveOrders;
        public int CurrentAliveOrders => m_CurrentAliveOrders;
        public int PendingOrders      => m_OrdersPending.Count;

        #endregion
        
        #region Constructors

        public OrdersExecutor(int _MaxAliveOrders)
        {
            m_MaxAliveOrders = _MaxAliveOrders;
            m_Alive          = true;
        }

        #endregion
        
        #region Public methods
        
        public void EnqueueOrder(R _Requester, O _Order, Log _Log = null)
        {
            _Log?.WriteFormat(LOG_ORDER_ENQUEUED, _Order.GetType().Name, _Order.GetHashCode());

            lock (m_LockObject)
            {
                if (m_OrdersPending.TryGetValue(_Requester, out O _))
                {
                    m_OrdersPending[_Requester] = _Order;
                }
                else
                {
                    m_Requesters.Enqueue(_Requester);
                    m_OrdersPending.TryAdd(_Requester, _Order);
                }
            }

            CheckQueueUpdateTask();
        }

        public bool TryRemoveOrder(R _Requester)
        {
            lock (m_LockObject)
            {
                return m_OrdersPending.TryRemove(_Requester, out O _);
            }
        }

        public string GetOrderStatus(R _Requester)
        {
            const string ORDER_IS_PENDING = "Order is pending";
            const string ORDER_IS_RUNNING = "Order is running, status: {0}";
            const string ORDER_NOT_FOUND  = "No orders found";

            StringBuilder stringBuilder = new StringBuilder();
            
            bool hasResult = false;
            
            lock (m_LockObject)
            {
                if (m_OrdersPending.TryGetValue(_Requester, out O _))
                {
                    stringBuilder.AppendLine(ORDER_IS_PENDING);
                    hasResult = true;
                }
            }

            if (m_OrdersRunning.TryGetValue(_Requester, out O order))
            {
                stringBuilder.AppendLine(string.Format(ORDER_IS_RUNNING, order.GetExecutingStatus()));
                hasResult = true;
            }

            if (!hasResult)
                stringBuilder.AppendLine(ORDER_NOT_FOUND);
            
            return stringBuilder.ToString();
        }

        public string GetRunningOrdersInfo()
        {
            return string.Join("\n", m_OrdersRunning.Select(_KVP => _KVP.Value.GetExecutingStatus()));
        }
        
        #endregion
        
        #region IDisposable

        public void Dispose()
        {
            m_Alive = false;
        }

        #endregion
        
        #region Service methods
        
        void CheckQueueUpdateTask()
        {
            if (m_QueueUpdateTask is { IsCompleted: false })
                return;
            
            DateTime taskFireTime = DateTime.UtcNow;

            m_QueueUpdateTask?.Dispose();

            m_QueueUpdateTask = Task.Factory.StartNew(
                () =>
                {
                    while (m_Alive && (m_Requesters.Count > 0 || (DateTime.UtcNow - taskFireTime).TotalMilliseconds > TASK_LIFE_TIME))
                    {
                        if (m_Requesters.Count < 0 || m_CurrentAliveOrders >= m_MaxAliveOrders)
                        {
                            Thread.Sleep(100);
                            continue;
                        }

                        UpdateQueue();
                    }
                },
                TaskCreationOptions.LongRunning
            );
        }
        
        void UpdateQueue()
        {
            //get requester
            if (!m_Requesters.TryPeek(out R requester))
                return;

            lock (m_LockObject)
            {
                //remove order
                if (!m_OrdersPending.TryRemove(requester, out O order))
                {
                    //if there is no order, then remove corresponding requester
                    m_Requesters.TryDequeue(out _);
                    return;
                }

                void MarkOrderAsRunning()
                {
                    Interlocked.Increment(ref m_CurrentAliveOrders);
                
                    m_OrdersRunning.TryAdd(requester, order);
                }

                void UnmarkOrderAsRunning()
                {
                    Interlocked.Decrement(ref m_CurrentAliveOrders);

                    m_OrdersRunning.TryRemove(requester, out _);
                }

                MarkOrderAsRunning();
                //execute removed order
                order.Execute(UnmarkOrderAsRunning);
            }

            Thread.Sleep(2);

            //if corresponding order is not exist then delete requester
            if (!m_OrdersPending.ContainsKey(requester))
                m_Requesters.TryDequeue(out _);
        }
        
        #endregion
        
    }
}