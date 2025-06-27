using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;

namespace Nav3D.Common
{
    public class ThreadDispatcher : MonoBehaviour
    {
        #region Factory

        public static ThreadDispatcher Instance => Singleton<ThreadDispatcher>.Instance;

        #endregion

        #region Attributes

        readonly ConcurrentQueue<Action> m_ActionQueue = new ConcurrentQueue<Action>();

        #if UNITY_EDITOR
        bool                                                   m_VerboseInvocation;
        readonly ConcurrentQueue<KeyValuePair<string, Action>> m_ActionQueueEditor = new ConcurrentQueue<KeyValuePair<string, Action>>();
        #endif

        #endregion

        #region Properties

        public static bool Doomed { get; private set; } = false;

        #endregion

        #region Public methods

        /// <summary>
        /// Must be inited from main thread!
        /// </summary>
        public void Initialize()
        {
        }
        #if UNITY_EDITOR
        public void Initialize(bool _VerboseInvocation = false)
        {
            m_VerboseInvocation = _VerboseInvocation;
        }
        #endif

        public void Uninitialize()
        {
            UtilsCommon.SmartDestroy(this);
        }

        public static void BeginInvoke(Action _Action)
        {
            if (_Action == null)
                return;

            #if UNITY_EDITOR
            Instance.m_ActionQueueEditor.Enqueue(new KeyValuePair<string, Action>(Environment.StackTrace, _Action));
            return;
            #endif
            Instance.m_ActionQueue.Enqueue(_Action);
        }

        public static void BeginInvoke<T>(Action<T> _Action, T _Arg)
        {
            BeginInvoke(() => _Action(_Arg));
        }

        public static void BeginInvoke<T0, T1>(Action<T0, T1> _Action, T0 _Arg0, T1 _Arg1)
        {
            BeginInvoke(() => _Action(_Arg0, _Arg1));
        }

        public static void BeginInvoke<T0, T1, T2>(Action<T0, T1, T2> _Action, T0 _Arg0, T1 _Arg1, T2 _Arg2)
        {
            BeginInvoke(() => _Action(_Arg0, _Arg1, _Arg2));
        }

        #endregion

        #region Engine methods

        void Update()
        {
            #if UNITY_EDITOR
            while (Instance.m_ActionQueueEditor.TryDequeue(out KeyValuePair<string, Action> keyValuePair))
            {
                if (keyValuePair.Value != null)
                {
                    try
                    {
                        if (m_VerboseInvocation)
                            UnityEngine.Debug.Log($"{nameof(ThreadDispatcher)}.QueueUpdate(): StackTrace: {keyValuePair.Key}");

                        keyValuePair.Value.Invoke();
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogErrorFormat("Error on event invocation: {0}", ex);
                    }
                }
            }
            return;
            #endif

            while (Instance.m_ActionQueue.TryDequeue(out Action action))
            {
                if (action != null)
                {
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogErrorFormat("Error on event invocation: {0}", ex);
                    }
                }
            }
        }

        void Awake()
        {
            Doomed = false;
        }

        void OnDestroy()
        {
            Doomed = true;
        }

        #endregion
    }
}