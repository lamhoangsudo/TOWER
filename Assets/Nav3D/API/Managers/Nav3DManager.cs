using Nav3D.Common;
using Nav3D.Agents;
using Nav3D.LocalAvoidance;
using Nav3D.Obstacles;
using Nav3D.Pathfinding;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Nav3D.API
{
    public static class Nav3DManager
    {
        #region Constants

        //the factor used to compute path storage bucket size relative to min bucket size
        const int PATH_STORAGE_BUCKET_SIZE_FACTOR = 3;

        static readonly string NEED_INIT_ERROR = $"{nameof(Nav3DManager)} needs to be initialized before! " +
                                                 $"All your work with Nav3D entities should begin on the {nameof(Nav3DManager)}.{nameof(OnNav3DInit)} event execution.";

        static readonly string ON_INIT_UNSUBSCRIBE_ERROR = $"There is no need to unsubscribe from the {nameof(OnNav3DInit)} event. " +
                                                           $"All subscriptions will be invoked and unsubscribed after {nameof(Nav3DManager)} is initialized.";

        static readonly string ON_PRE_INIT_UNSUBSCRIBE_ERROR = $"There is no need to unsubscribe from the {nameof(OnNav3DPreInit)} event. " +
                                                               $"All subscriptions will be unsubscribed after {nameof(Nav3DManager)} is initialized.";

        static readonly string NOT_INITIALIZED_WARNING = $"{nameof(Nav3DManager)} is not initialized yet!";

        #endregion

        #region Exceptions

        public class Nav3DManagerNotInitializedException : Exception
        {
            public Nav3DManagerNotInitializedException()
                : base(NEED_INIT_ERROR)
            {
            }
        }

        #endregion

        #region Attributes

        static readonly List<Action> m_OnInitSubscribers         = new List<Action>();
        static readonly List<Action> m_OnInitInternalSubscribers = new List<Action>();
        static readonly List<Action> m_OnPreInitSubscribers      = new List<Action>();

        static event Action m_OnInit;
        static event Action m_OnPreInit;
        static event Action m_OnInitInternal;

        #endregion

        #region Events

        /// <summary>
        /// Lets you know that Nav3DManager initialization has occurred.
        /// </summary>
        public static event Action OnNav3DInit
        {
            add
            {
                if (value == null)
                    return;

                if (Inited)
                {
                    ThreadDispatcher.BeginInvoke(value);

                    return;
                }

                Action subscriber = () => ThreadDispatcher.BeginInvoke(value);

                m_OnInitSubscribers.Add(subscriber);

                m_OnInit += subscriber;
            }
            remove { Debug.LogError(ON_INIT_UNSUBSCRIBE_ERROR); }
        }

        public static event Action OnNav3DInitInternal
        {
            add
            {
                if (value == null)
                    return;

                if (Inited)
                {
                    value.Invoke();
                    return;
                }

                m_OnInitInternalSubscribers.Add(value);
                m_OnInitInternal += value;
            }
            remove
            {
                m_OnInitInternalSubscribers.Remove(value);
                m_OnInitInternal -= value;
            }
        }

        public static event Action OnNav3DPreInit
        {
            add
            {
                if (value == null)
                    return;

                if (PreInited)
                {
                    ThreadDispatcher.BeginInvoke(value);

                    return;
                }

                Action subscriber = () => ThreadDispatcher.BeginInvoke(value);

                m_OnPreInitSubscribers.Add(subscriber);

                m_OnPreInit += subscriber;
            }
            remove { Debug.LogError(ON_PRE_INIT_UNSUBSCRIBE_ERROR); }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Shows if Nav3D is ready to work.
        /// </summary>
        public static bool Inited { get; private set; }

        public static bool PreInited { get; private set; }

        #endregion

        #region Public methods

        /// <summary>
        /// Determines whether point is inside of space cell occupied by any obstacle.
        /// </summary>
        /// <param name="_Point"></param>
        public static bool IsPointInsideOccupiedVolume(Vector3 _Point)
        {
            if (!Inited)
                return false;

            return ObstacleManager.Instance.IsPointInsideObstacle(_Point);
        }

        /// <summary>
        /// Nav3D initialization. Runtime work with Nav3D starts from here.
        /// </summary>
        /// <param name="_MinBucketSize">The size of the smallest pathfinding graph nodes.</param>
        public static void InitNav3DRuntime(float _MinBucketSize)
        {
            if (_MinBucketSize <= 0)
                throw new ArgumentException($"[{nameof(Nav3DManager)}]: Init error. {nameof(_MinBucketSize)} value must be greater than zero.");

            #if UNITY_EDITOR
            ThreadDispatcher.Instance.Initialize(false);
            #elif !UNITY_EDITOR
            ThreadDispatcher.Instance.Initialize();
            #endif
            ObstacleManager.Instance.Initialize(_MinBucketSize);
            PathfindingManager.Instance.Initialize(_MinBucketSize * PATH_STORAGE_BUCKET_SIZE_FACTOR);
            AgentManager.Instance.Initialize(new List<Nav3DAgentMover>());
            ObstacleParticularResolutionManager.Instance.Initialize();

            MarksAsPreInited();

            Nav3DObstacleLoader obstacleLoader = UnityEngine.Object.FindObjectOfType<Nav3DObstacleLoader>();

            if (obstacleLoader != null && obstacleLoader.enabled)
            {
                obstacleLoader.OnLoadingFinished += MarksAsInited;
            }
            else
            {
                MarksAsInited();
            }
        }

        /// <summary>
        /// Nav3D initialization for edit mode purposes.
        /// </summary>
        /// <param name="_MinBucketSize">The size of the smallest pathfinding graph nodes.</param>
        public static void InitNav3DEditMode(float _MinBucketSize)
        {
            if (_MinBucketSize <= 0)
                throw new ArgumentException($"[{nameof(Nav3DManager)}]: Init error. {nameof(_MinBucketSize)} value must be greater than zero.");

            ThreadDispatcher.Instance.Initialize();
            ObstacleManager.Instance.Initialize(_MinBucketSize);
            ObstacleParticularResolutionManager.Instance.Initialize();
            PathfindingManager.Instance.Initialize(_MinBucketSize * PATH_STORAGE_BUCKET_SIZE_FACTOR);
            AgentManager.Instance.Initialize(new List<Nav3DAgentMover>());

            UnityEngine.Object.FindObjectsOfType<Nav3DParticularResolutionRegion>().ForEach(_Region => _Region.InitializeEditMode());
        }

        /// <summary>
        /// Clean all Objects used by Nav3D and stops all running async tasks.
        /// Call before loading a new scene.
        /// </summary>
        public static void Dispose3DNav()
        {
            Inited    = false;
            PreInited = false;

            UnsubscribeOnInitSubscribers();
            UnsubscribeOnInitInternalSubscribers();
            UnsubscribeOnPreInitSubscribers();

            m_OnInit         = null;
            m_OnInitInternal = null;
            m_OnPreInit      = null;

            if (!ThreadDispatcher.Doomed)
                ThreadDispatcher.Instance.Uninitialize();

            if (!AgentManager.Doomed)
                AgentManager.Instance.Uninitialize();

            if (!ObstacleManager.Doomed)
                ObstacleManager.Instance.Uninitialize();

            if (!PathfindingManager.Doomed)
                PathfindingManager.Instance.Uninitialize();

            if (!ObstacleParticularResolutionManager.Doomed)
                ObstacleParticularResolutionManager.Instance.Uninitialize();
        }

        public static void Dispose3DNavEditMode()
        {
            ThreadDispatcher.Instance.Uninitialize();
            ObstacleManager.Instance.Uninitialize();
            PathfindingManager.Instance.Uninitialize();
            ObstacleParticularResolutionManager.Instance.Uninitialize();
            AgentManager.Instance.Uninitialize();
        }

        public static void CheckInitedSoft()
        {
            if (!Inited)
                Debug.LogWarning(NOT_INITIALIZED_WARNING);
        }

        public static void CheckInitedHard()
        {
            if (!Inited)
                throw new Nav3DManagerNotInitializedException();
        }

        #endregion

        #region Service methods

        static void MarksAsInited()
        {
            Inited = true;

            m_OnInitInternal?.Invoke();
            m_OnInit?.Invoke();

            UnsubscribeOnInitInternalSubscribers();
            UnsubscribeOnInitSubscribers();
        }

        static void MarksAsPreInited()
        {
            PreInited = true;

            m_OnPreInit?.Invoke();

            UnsubscribeOnPreInitSubscribers();
        }

        static void UnsubscribeOnInitSubscribers()
        {
            foreach (Action subscriber in m_OnInitSubscribers)
            {
                m_OnInit -= subscriber;
            }

            m_OnInitSubscribers.Clear();
        }

        static void UnsubscribeOnInitInternalSubscribers()
        {
            foreach (Action subscriber in m_OnInitInternalSubscribers)
            {
                m_OnInitInternal -= subscriber;
            }

            m_OnInitInternalSubscribers.Clear();
        }

        static void UnsubscribeOnPreInitSubscribers()
        {
            foreach (Action subscriber in m_OnPreInitSubscribers)
            {
                m_OnPreInit -= subscriber;
            }

            m_OnPreInitSubscribers.Clear();
        }

        #endregion
    }
}