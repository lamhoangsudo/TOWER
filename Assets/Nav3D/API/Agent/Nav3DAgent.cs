using Nav3D.Common;
using Nav3D.LocalAvoidance;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Nav3D.Agents;
using Log = Nav3D.Common.Log;

namespace Nav3D.API
{
    public partial class Nav3DAgent : Nav3DAgentBase
    {
        #region Constants

        readonly string LOG_CONFIG_SET           = $"{nameof(Nav3DAgent)}.{nameof(SetConfig)}";
        readonly string LOG_MOVE_TO              = $"{nameof(Nav3DAgent)}.{nameof(MoveTo)}: {{0}}";
        readonly string LOG_MOVE_TO_POINTS       = $"{nameof(Nav3DAgent)}.{nameof(MoveToPoints)}: {{0}}";
        readonly string LOG_MOVE_TO_POINTS_TRIMM = $"{nameof(Nav3DAgent)}.{nameof(MoveToPoints)}: Source points sequence was trimmed: {{0}} -> {{1}}";

        readonly string LOG_FOLLOW_TARGET =
            $"{nameof(Nav3DAgent)}.{nameof(FollowTarget)}: Transfrom: {{0}}:{{1}}, ToleranceUpdate: {{2}}, Reach dist: {{3}}";

        readonly string LOG_STOP = $"{nameof(Nav3DAgent)}.{nameof(StopInternal)}";

        const string LOG_DISABLED_ERROR = "Logging is disabled for agent.";

        readonly string ON_INIT_UNSUBSCRIBE_ERROR = $"There is no need to unsubscribe from the {nameof(OnAgentInit)} event. " +
                                                    $"All subscriptions will be unsubscribed after {nameof(Nav3DAgent)} is initialized.";

        #endregion

        #region Events

        event Action OnAgentInitInternal
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

                m_OnAgentInitInternal += value;
            }
            remove
            {
                m_OnInitInternalSubscribers.Remove(value);

                m_OnAgentInitInternal -= value;
            }
        }

        public event Action OnAgentInit
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

                m_OnAgentInit += subscriber;
            }
            remove { Debug.LogError(ON_INIT_UNSUBSCRIBE_ERROR); }
        }

        public event Action<Vector3[]> OnPathUpdated;
        
        #endregion

        #region Serialized fields

        [SerializeField] Nav3DAgentConfig m_Config;

        #endregion

        #region Attributes

        readonly List<Action> m_OnInitSubscribers         = new List<Action>();
        readonly List<Action> m_OnInitInternalSubscribers = new List<Action>();

        event Action m_OnAgentInit;
        event Action m_OnAgentInitInternal;

        // ReSharper disable once MemberCanBePrivate.Global
        protected Nav3DAgentConfig m_ConfigInternal;
        protected Nav3DAgentMover       m_Mover;

        MovementLogic m_MovementLogic;

        Log m_Log;

        Vector3 m_RotationVector;
        
        #endregion

        #region Properties

        public bool Inited { get; private set; }

        //If agent executing any task.
        public bool Operates => m_MovementLogic is { IsValid: true };

        /// <summary>
        /// Cached agent's transform
        /// </summary>
        public Transform Transform => m_CachedTransform;

        /// <summary>
        /// The agent's radius taken from its config
        /// </summary>
        public float Radius => m_ConfigInternal?.Radius ?? 0;

        public Vector3[] CurrentPath => m_MovementLogic?.CurrentPath ?? new Vector3[] { };

        /// <summary>
        /// Agent config instance.
        /// </summary>
        public Nav3DAgentConfig Config => m_Config ?? m_ConfigInternal;

        #endregion

        #region Public methods
        
        /// <summary>
        /// Sets specific config to an agent.
        /// </summary>
        /// <param name="_Config"></param>
        public void SetConfig(Nav3DAgentConfig _Config)
        {
            Nav3DManager.CheckInitedSoft();

            OnAgentInitInternal += () =>
            {
                m_ConfigInternal = m_Config = _Config;

                //Workaround for the case when the project update was not performed or some parameters in the config have invalid values
                m_ConfigInternal.FixInvalidParams();

                InitLog();

                InitMover();

                m_Log?.Write(LOG_CONFIG_SET);
            };
        }
        
        /// <summary>
        /// Starts following to the given point.
        /// </summary>
        /// <param name="_Point">The given point.</param>
        /// <param name="_ReachDistance">Distance to reach the point.</param>
        /// <param name="_OnReach">The action to invoke when point reached.</param>
        /// <param name="_OnPathfindingFail">The action to do if pathfinding failed.</param>
        public void MoveTo(
                Vector3                  _Point,
                Action                   _OnReach           = null,
                Action<PathfindingError> _OnPathfindingFail = null,
                float?                   _ReachDistance     = null
            )
        {
            Nav3DManager.CheckInitedSoft();

            OnAgentInitInternal += () =>
            {
                m_Log?.WriteFormat(LOG_MOVE_TO, _Point.ToStringExt());

                DisposeMovementLogic();

                m_MovementLogic = new PointsFollowingLogic(
                        m_CachedTransform,
                        m_Mover,
                        new[] { _Point },
                        false,
                        false,
                        m_ConfigInternal.TryRepositionTargetIfOccupied,
                        _ReachDistance ?? m_ConfigInternal.TargetReachDistanceSqr,
                        null,
                        _OnReach,
                        InvokeOnPathUpdated,
                        _OnPathfindingFail
                    );

                m_MovementLogic.Init();
            };
        }

        /// <summary>
        /// Start movement to the given points.
        /// </summary>
        /// <param name="_Points">The given points.</param>
        /// <param name="_Loop">Loop the movement when reach last point.</param>
        /// <param name="_StartFromClosest">Whether to start movement from the closest point to the given one.</param>
        /// <param name="_SkipUnpassableTargets">Exclude point if pathfinding failed, or invoke _OnPathfindingFail callback.</param>
        /// <param name="_ReachDistance">Distance to reach the point.</param>
        /// <param name="_OnTargetPassed">The action to do when the one of target points has passed.</param>
        /// <param name="_OnFinished">The action to do when the last one point reached.</param>
        /// <param name="_OnPathfindingFail">The action to do if pathfinding failed.</param>
        public void MoveToPoints(
                Vector3[]                _Points,
                bool                     _Loop                  = false,
                bool                     _StartFromClosest      = false,
                bool                     _SkipUnpassableTargets = false,
                float?                   _ReachDistance         = null,
                Action<Vector3>          _OnTargetPassed        = null,
                Action                   _OnFinished            = null,
                Action<PathfindingError> _OnPathfindingFail     = null
            )
        {
            Nav3DManager.CheckInitedSoft();

            OnAgentInitInternal += () =>
            {
                m_Log?.WriteFormat(LOG_MOVE_TO_POINTS + $"\n{Environment.StackTrace}", UtilsCommon.GetPointsString(_Points));

                DisposeMovementLogic();

                Vector3[] trimmedPoints = UtilsCommon.TrimEqualPoints(_Points);

                if (trimmedPoints.Length < _Points.Length)
                    m_Log?.WriteFormat(LOG_MOVE_TO_POINTS_TRIMM, UtilsCommon.GetPointsString(_Points), UtilsCommon.GetPointsString(trimmedPoints));
                
                if (_StartFromClosest)
                    trimmedPoints = UtilsCommon.ReorderStartingFromClosest(trimmedPoints, m_CachedTransform.position, _Loop);
                
                m_MovementLogic = new PointsFollowingLogic(
                        m_CachedTransform,
                        m_Mover,
                        trimmedPoints,
                        _Loop,
                        _SkipUnpassableTargets,
                        m_ConfigInternal.TryRepositionTargetIfOccupied,
                        _ReachDistance ?? m_ConfigInternal.TargetReachDistance,
                        _OnTargetPassed,
                        _OnFinished,
                        InvokeOnPathUpdated,
                        _OnPathfindingFail
                    );

                m_MovementLogic.Init();
            };
        }

        /// <summary>
        /// Begins to follow the moving transform.
        /// </summary>
        /// <param name="_Target">Target transform to follow</param>
        /// <param name="_FollowContinuously">Follows continuously with no reach callback invocation.</param>
        /// <param name="_TargetOffsetUpdate">The transform offset needed to update the path. </param>
        /// <param name="_TargetReachDistance">The distance to the target when the agent should stop following.</param>
        /// <param name="_OnReach">On target reach callback.</param>
        /// <param name="_OnFail">Executes if the target transform reference is null or gameObject has disabled activeInHierarchy flag.</param>
        /// <param name="_OnPathfindingFail">On pathfinding failed action (can be executed multiple times).</param>
        public void FollowTarget(
                Transform                _Target,
                bool                     _FollowContinuously  = false,
                float                    _TargetOffsetUpdate  = 0.1f,
                float                    _TargetReachDistance = 0,
                Action                   _OnReach             = null,
                Action                   _OnFail              = null,
                Action<PathfindingError> _OnPathfindingFail   = null
            )
        {
            Nav3DManager.CheckInitedSoft();

            OnAgentInitInternal += () =>
            {
                DisposeMovementLogic();

                m_MovementLogic = new TargetFollowingLogic(
                        m_CachedTransform,
                        m_Mover,
                        _Target,
                        _TargetOffsetUpdate,
                        _TargetReachDistance,
                        _FollowContinuously,
                        _OnReach,
                        InvokeOnPathUpdated,
                        _OnFail,
                        _OnPathfindingFail
                    );

                m_MovementLogic.Init();

                if (m_ConfigInternal.UseLog)
                    m_Log?.WriteFormat(LOG_FOLLOW_TARGET, _Target.name, _Target.GetInstanceID(), _TargetOffsetUpdate, _TargetReachDistance);
            };
        }

        /// <summary>
        /// Stops current order execution.
        /// </summary>
        public void Stop()
        {
            Nav3DManager.CheckInitedSoft();

            OnAgentInitInternal += StopInternal;
        }

        /// <summary>
        /// Returns the list of agents distance to is less than radius.
        /// </summary>
        /// <param name="_Radius">Radius.</param>
        /// <param name="_Predicate">Predicate for agents filtering.</param>
        public Nav3DAgent[] GetAgentsInRadius(float _Radius, Predicate<Nav3DAgent> _Predicate = null)
        {
            Nav3DManager.CheckInitedHard();

            Nav3DAgent[] agentsInSphere = Nav3DAgentManager.GetAgentsInSphere(m_CachedTransform.position, _Radius);

            return _Predicate != null ? agentsInSphere.Where(_Agent => _Predicate(_Agent)).ToArray() : agentsInSphere;
        }

        public string GetLogText()
        {
            return m_ConfigInternal.UseLog ? m_Log.GetText(out _) : LOG_DISABLED_ERROR;
        }

        public string GetInnerStatusString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"{nameof(Nav3DAgent)} status:");
            stringBuilder.AppendLine($"{nameof(m_MovementLogic)}: {m_MovementLogic?.GetType()}");
            stringBuilder.AppendLine($"{nameof(m_MovementLogic)}: {nameof(m_MovementLogic.IsValid)}: {m_MovementLogic?.IsValid}");
            stringBuilder.Append($"{m_Mover.GetInnerStatusString()}");

            return stringBuilder.ToString();
        }

        public string GetLastPathfindingStatsString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("Last pathfinding stats:");
            stringBuilder.Append($"{m_Mover.GetLastPathfindingStatsString()}");

            return stringBuilder.ToString();
        }

        public override void DoFixedUpdate()
        {
            if (!Inited)
                return;

            DoFrameLogic();
        }

        #endregion

        #region Service methods

        void DoAwake()
        {
            m_CachedTransform = transform;
        }

        void Initialize()
        {
            if (Inited)
                return;

            AgentManager.Instance.RegisterAgent(this);

            Nav3DManager.OnNav3DInitInternal += () =>
            {
                SetConfig(m_Config?.Copy() ?? Nav3DAgentConfig.DefaultConfig);

                MarkAsInited();
            };
        }

        void InitLog()
        {
            m_Log = m_ConfigInternal.UseLog ? new Log($"{gameObject.name}: {gameObject.GetInstanceID()}", m_ConfigInternal.LogSize) : null;
        }

        protected virtual void InitMover()
        {
            m_Mover?.Uninitialize();

            m_Mover = m_ConfigInternal.MotionNavigationType switch
            {
                MotionNavigationType.LOCAL => new Nav3DAgentMoverLocal(m_CachedTransform.position, m_ConfigInternal, this, m_Log),
                MotionNavigationType.GLOBAL => new Nav3DAgentMoverGlobal(m_CachedTransform.position, m_ConfigInternal, this, m_Log),
                MotionNavigationType.COMBINED => new Nav3DAgentMoverCombined(m_CachedTransform.position, m_ConfigInternal, this, m_Log),
                _ => throw new ArgumentOutOfRangeException($"Unknown navigation type: {m_ConfigInternal.MotionNavigationType}")
            };
        }

        void Uninitialize()
        {
            Inited = false;

            if (!AgentManager.Doomed)
                AgentManager.Instance.UnregisterAgent(this);

            m_Mover?.Uninitialize();
            m_Log?.Clear();
        }

        void MarkAsInited()
        {
            Inited = true;

            m_OnAgentInitInternal?.Invoke();
            m_OnAgentInit?.Invoke();

            UnsubscribeOnInitInternalSubscribers();
            UnsubscribeOnInitSubscribers();
        }

        void UnsubscribeOnInitInternalSubscribers()
        {
            foreach (Action subscriber in m_OnInitInternalSubscribers)
            {
                m_OnAgentInitInternal -= subscriber;
            }

            m_OnInitInternalSubscribers.Clear();
        }

        void UnsubscribeOnInitSubscribers()
        {
            foreach (Action subscriber in m_OnInitSubscribers)
            {
                m_OnAgentInit -= subscriber;
            }

            m_OnInitSubscribers.Clear();
        }

        void StopInternal()
        {
            m_MovementLogic.Dispose();

            if (m_ConfigInternal.UseLog)
                m_Log?.Write(LOG_STOP);
        }

        void DoFrameLogic()
        {
            if (!(m_MovementLogic is { IsValid: true }))
                return;

            m_MovementLogic.DoOnFrameStart();

            Vector3 frameVelocity = m_MovementLogic.GetFrameVelocity();
            m_CachedTransform.position += frameVelocity;

            m_RotationVector = Vector3.Lerp(m_RotationVector, frameVelocity, m_ConfigInternal.RotationVectorLerpFactor);

            if (m_RotationVector != Vector3.zero)
                m_CachedTransform.rotation = Quaternion.RotateTowards(
                        m_CachedTransform.rotation,
                        Quaternion.LookRotation(m_RotationVector),
                        m_ConfigInternal.MaxAgentDegreesRotationPerTick
                    );

            m_MovementLogic.DoOnFrameEnd();
        }

        void DisposeMovementLogic()
        {
            m_MovementLogic?.Dispose();
            m_MovementLogic = null;
        }

        void InvokeOnPathUpdated(Vector3[] _Path)
        {
            OnPathUpdated?.Invoke(_Path);
        }
        
        #endregion

        #region Unity events

        void Awake()
        {
            DoAwake();
        }

        void OnEnable()
        {
            Initialize();
        }

        void OnDestroy()
        {
            Uninitialize();
        }

        void OnDisable()
        {
            Uninitialize();
        }

        #endregion
    }
}