using System;
using System.Text;
using UnityEngine;

namespace Nav3D.API
{
    [CreateAssetMenu(fileName = "AgentConfig", menuName = "Nav3D/Agent config", order = 150)]
    public class Nav3DAgentConfig : ScriptableObject
    {
        #region Constants : Debug

        readonly string ORCA_TAU_INVALID = $"There is invalid {nameof(ORCATau)} parameter value: {{0}}. Correct it, or perform project update.";

        readonly string VELOCITY_WEIGHTS_INVALID = "Some of the weight values are set incorrectly. " +
                                                   " Check the current values in the config. You probably set an incorrect value, or you didn't update the project.";

        #endregion

        #region Constants : Defaults

        const float DEFAULT_RADIUS                        = 0.25f;
        const float DEFAULT_SPEED                         = 0.01f;
        const float DEFAULT_SPEED_TO_MAX_SPEED_MULTIPLIER = 1.15f;

        const int DEFAULT_MAX_SPEED_OBTAIN_MODE = 0;

        const bool DEFAULT_USE_CONSIDERED_AGENTS_NUMBER_LIMIT = false;
        const int  DEFAULT_CONSIDERED_AGENTS_NUMBER_LIMIT     = 3;

        const int                  DEFAULT_PATHFINDING_TIMEOUT               = 2000; //ms
        const bool                 DEFAULT_SMOOTH_PATH                       = false;
        const int                  DEFAULT_SMOOTH_PATH_RATIO                 = 3;
        const MotionNavigationType DEFAULT_MOTION_NAV_TYPE                   = MotionNavigationType.COMBINED;
        const bool                 DEFAULT_AUTOUPDATE_PATH                   = true;
        const int                  DEFAULT_AUTOUPDATE_PATH_COOLDOWN          = 2000;
        const float                DEFAULT_MAX_DEGREES_ROTATION_PER_TICK     = 5f;
        const float                DEFAULT_ROTATION_VECTOR_LERP_FACTOR       = 0.1f;
        const bool                 DEFAULT_TRY_REPOSITION_TARGET_IF_OCCUPIED = false;

        const float DEFAULT_ORCA_TAU               = 2.5f;
        const bool  DEFAULT_AVOID_STATIC_OBSTACLES = true;

        //velocity blending weights for case when pathfinding performing and there are both agents and obstacles near
        const float DEFAULT_PATH_VELOCITY_WEIGHT                = 1f;
        const float DEFAULT_AGENTS_AVOIDANCE_VELOCITY_WEIGHT    = 10f;
        const float DEFAULT_OBSTACLES_AVOIDANCE_VELOCITY_WEIGHT = 5f;

        //velocity blending weights for case when pathfinding performing and there is only obstacles near
        const float DEFAULT_PATH_VELOCITY_WEIGHT1                = 1;
        const float DEFAULT_OBSTACLES_AVOIDANCE_VELOCITY_WEIGHT1 = 3f;

        //velocity blending weights for case when pathfinding performing and there is only agents near
        const float DEFAULT_PATH_VELOCITY_WEIGHT2             = 1f;
        const float DEFAULT_AGENTS_AVOIDANCE_VELOCITY_WEIGHT1 = 10f;

        //velocity blending weights for case when only local avoidance performing and there are both agents and obstacles near
        const float DEFAULT_OBSTACLES_AVOIDANCE_VELOCITY_WEIGHT2 = 1f;
        const float DEFAULT_AGENTS_AVOIDANCE_VELOCITY_WEIGHT2    = 1f;

        const bool DEFAULT_USE_LOG  = false;
        const int  DEFAULT_LOG_SIZE = 50;

        #endregion

        #region Serialized fields

        [SerializeField] float m_Radius = DEFAULT_RADIUS;

        [SerializeField] float m_Speed = DEFAULT_SPEED;
        [SerializeField] float m_MaxSpeed;
        [SerializeField] float m_SpeedToMaxSpeedMultiplier = DEFAULT_SPEED_TO_MAX_SPEED_MULTIPLIER;
        [SerializeField] int   m_MaxSpeedObtainMode             = DEFAULT_MAX_SPEED_OBTAIN_MODE; //0-multiplier, 1-absolute value
        [SerializeField] bool  m_UseConsideredAgentsNumberLimit = DEFAULT_USE_CONSIDERED_AGENTS_NUMBER_LIMIT;
        [SerializeField] int   m_ConsideredAgentsNumberLimit    = DEFAULT_CONSIDERED_AGENTS_NUMBER_LIMIT;

        // ReSharper disable once InconsistentNaming
        [SerializeField] float m_ORCATau              = DEFAULT_ORCA_TAU;
        [SerializeField] bool  m_AvoidStaticObstacles = DEFAULT_AVOID_STATIC_OBSTACLES;

        [SerializeField] int                  m_PathfindingTimeout             = DEFAULT_PATHFINDING_TIMEOUT; //ms
        [SerializeField] bool                 m_SmoothPath                     = DEFAULT_SMOOTH_PATH;
        [SerializeField] int                  m_SmoothRatio                    = DEFAULT_SMOOTH_PATH_RATIO;
        [SerializeField] MotionNavigationType m_MotionNavigationType           = DEFAULT_MOTION_NAV_TYPE;
        [SerializeField] float                m_TargetReachDistance            = DEFAULT_RADIUS;
        [SerializeField] bool                 m_AutoUpdatePath                 = DEFAULT_AUTOUPDATE_PATH;
        [SerializeField] int                  m_PathAutoUpdateCooldown         = DEFAULT_AUTOUPDATE_PATH_COOLDOWN; //ms
        [SerializeField] float                m_MaxAgentDegreesRotationPerTick = DEFAULT_MAX_DEGREES_ROTATION_PER_TICK;
        [SerializeField] float                m_RotationVectorLerpFactor       = DEFAULT_ROTATION_VECTOR_LERP_FACTOR;
        [SerializeField] bool                 m_TryRepositionTargetIfOccupied  = DEFAULT_TRY_REPOSITION_TARGET_IF_OCCUPIED;

        //velocities blending rules:
        //1) Following global path and there is some agents and obstacles near
        [SerializeField] float m_PathVelocityWeight              = DEFAULT_PATH_VELOCITY_WEIGHT;
        [SerializeField] float m_AgentsAvoidanceVelocityWeight   = DEFAULT_AGENTS_AVOIDANCE_VELOCITY_WEIGHT;
        [SerializeField] float m_ObstacleAvoidanceVelocityWeight = DEFAULT_OBSTACLES_AVOIDANCE_VELOCITY_WEIGHT;

        //2) Following global path and there is some obstacles near
        [SerializeField] float m_PathVelocityWeight1              = DEFAULT_PATH_VELOCITY_WEIGHT1;
        [SerializeField] float m_ObstacleAvoidanceVelocityWeight1 = DEFAULT_OBSTACLES_AVOIDANCE_VELOCITY_WEIGHT1;

        //3) Following global path and there is some agents near
        [SerializeField] float m_PathVelocityWeight2            = DEFAULT_PATH_VELOCITY_WEIGHT2;
        [SerializeField] float m_AgentsAvoidanceVelocityWeight1 = DEFAULT_OBSTACLES_AVOIDANCE_VELOCITY_WEIGHT1;

        //4) We just use local avoidance and there is agents and obstacles near
        [SerializeField] float m_AgentsAvoidanceVelocityWeight2   = DEFAULT_AGENTS_AVOIDANCE_VELOCITY_WEIGHT2;
        [SerializeField] float m_ObstacleAvoidanceVelocityWeight2 = DEFAULT_AGENTS_AVOIDANCE_VELOCITY_WEIGHT2;

        [SerializeField] bool m_UseLog  = DEFAULT_USE_LOG;
        [SerializeField] int  m_LogSize = DEFAULT_LOG_SIZE;

        #endregion

        #region Attributes

        float? m_TargetReachDistanceSqr;

        float m_VelocityDangerRadiusTau;
        float m_VelocityDangerRadius;
        float m_VelocityDangerRadiusSqr;
        bool  m_VelocityDangerRadiusTauIsDirty = true;
        bool  m_VelocityDangerRadiusIsDirty    = true;

        #endregion

        #region Properties

        /// <summary>
        /// Agent radius used for local avoidance.
        /// </summary>
        public float Radius
        {
            get => m_Radius;
            set
            {
                if (value <= 0 || Mathf.Approximately(m_Radius, value))
                    return;

                m_Radius = value;

                m_VelocityDangerRadiusTauIsDirty = true;
                m_VelocityDangerRadiusIsDirty    = true;
            }
        }
        
        /// <summary>
        /// Desired speed in Unity distance per tick of the FixedUpdate event.
        /// </summary>
        public float Speed
        {
            get => m_Speed;
            set
            {
                if (value <= 0 || Mathf.Approximately(m_Speed, value))
                    return;

                if (m_MaxSpeedObtainMode == 0)
                {
                    m_Speed = value;

                    MaxSpeed = m_Speed * m_SpeedToMaxSpeedMultiplier;
                }
                else if (m_MaxSpeedObtainMode == 1)
                {
                    m_Speed = Mathf.Clamp(value, 0, m_MaxSpeed);
                }
                else
                {
                    throw new Exception($"Unknown m_MaxSpeedObtainMode type {value}");
                }

                m_VelocityDangerRadiusTauIsDirty = true;
                m_VelocityDangerRadiusIsDirty    = true;
            }
        }
        
        /// <summary>
        /// The maximum speed allowed for performing local avoidance.
        /// </summary>
        public float MaxSpeed
        {
            get => m_MaxSpeed;
            set
            {
                float valueToSet = Mathf.Max(value, m_Speed);

                if (Mathf.Approximately(m_MaxSpeed, valueToSet))
                    return;

                m_MaxSpeed = valueToSet;

                m_VelocityDangerRadiusTauIsDirty = true;
                m_VelocityDangerRadiusIsDirty    = true;
            }
        }

        public float SqrMaxSpeed => MaxSpeed * MaxSpeed;

        public float FactualMaxSpeed
        {
            get
            {
                if (MotionNavigationType == MotionNavigationType.GLOBAL)
                    return Speed;

                return MaxSpeed;
            }
        }

        public float SpeedToMaxSpeedMultiplier
        {
            get => m_SpeedToMaxSpeedMultiplier;
            set => m_SpeedToMaxSpeedMultiplier = Mathf.Max(1 + float.Epsilon, value);
        }

        public int MaxSpeedObtainMode
        {
            get => m_MaxSpeedObtainMode;
            set => m_MaxSpeedObtainMode = value;
        }

        public bool UseConsideredAgentsNumberLimit
        {
            get => m_UseConsideredAgentsNumberLimit;
            set => m_UseConsideredAgentsNumberLimit = value;
        }

        public int ConsideredAgentsNumberLimit
        {
            get => m_ConsideredAgentsNumberLimit;
            set => m_ConsideredAgentsNumberLimit = Mathf.Max(1, value);
        }

        public int PathfindingTimeout
        {
            get => m_PathfindingTimeout;
            set
            {
                if (value <= 0 || m_PathfindingTimeout == value)
                    return;

                m_PathfindingTimeout = value;
            }
        }

        public bool SmoothPath
        {
            get => m_SmoothPath;
            set => m_SmoothPath = value;
        }

        public int SmoothRatio
        {
            get => m_SmoothRatio;
            set
            {
                if (value <= 0 || m_SmoothRatio == value)
                    return;

                m_SmoothRatio = value;
            }
        }

        public MotionNavigationType MotionNavigationType
        {
            get => m_MotionNavigationType;
            set
            {
                m_MotionNavigationType = value;

                m_VelocityDangerRadiusTauIsDirty = true;
                m_VelocityDangerRadiusIsDirty    = true;
            }
        }

        public float TargetReachDistance
        {
            get => m_TargetReachDistance;
            set
            {
                if (value < 0)
                    return;

                m_TargetReachDistance    = value;
                m_TargetReachDistanceSqr = m_TargetReachDistance * m_TargetReachDistance;
            }
        }

        public float TargetReachDistanceSqr => m_TargetReachDistanceSqr ??= m_TargetReachDistance * m_TargetReachDistance;

        public bool AutoUpdatePath
        {
            get => m_AutoUpdatePath;
            set => m_AutoUpdatePath = value;
        }

        public int PathAutoUpdateCooldown
        {
            get => m_PathAutoUpdateCooldown;
            set => m_PathAutoUpdateCooldown = Mathf.Max(1, value);
        }

        public float MaxAgentDegreesRotationPerTick
        {
            get => m_MaxAgentDegreesRotationPerTick;
            set => m_MaxAgentDegreesRotationPerTick = Mathf.Max(0f, value);
        }

        public float RotationVectorLerpFactor
        {
            get => m_RotationVectorLerpFactor;
            set => m_RotationVectorLerpFactor = Mathf.Max(0f, value);
        }

        public bool TryRepositionTargetIfOccupied
        {
            get => m_TryRepositionTargetIfOccupied;
            set => m_TryRepositionTargetIfOccupied = value;
        }

        public float PathVelocityWeight
        {
            get => m_PathVelocityWeight;
            set => m_PathVelocityWeight = Mathf.Max(value, float.Epsilon);
        }

        public float PathVelocityWeight1
        {
            get => m_PathVelocityWeight1;
            set => m_PathVelocityWeight1 = Mathf.Max(value, float.Epsilon);
        }

        public float PathVelocityWeight2
        {
            get => m_PathVelocityWeight2;
            set => m_PathVelocityWeight2 = Mathf.Max(value, float.Epsilon);
        }

        public float AgentsAvoidanceVelocityWeight
        {
            get => m_AgentsAvoidanceVelocityWeight;
            set => m_AgentsAvoidanceVelocityWeight = Mathf.Max(value, float.Epsilon);
        }

        public float AgentsAvoidanceVelocityWeight1
        {
            get => m_AgentsAvoidanceVelocityWeight1;
            set => m_AgentsAvoidanceVelocityWeight1 = Mathf.Max(value, float.Epsilon);
        }

        public float AgentsAvoidanceVelocityWeight2
        {
            get => m_AgentsAvoidanceVelocityWeight2;
            set => m_AgentsAvoidanceVelocityWeight2 = Mathf.Max(value, float.Epsilon);
        }

        public float ObstaclesAvoidanceVelocityWeight
        {
            get => m_ObstacleAvoidanceVelocityWeight;
            set => m_ObstacleAvoidanceVelocityWeight = Mathf.Max(value, float.Epsilon);
        }

        public float ObstaclesAvoidanceVelocityWeight1
        {
            get => m_ObstacleAvoidanceVelocityWeight1;
            set => m_ObstacleAvoidanceVelocityWeight1 = Mathf.Max(value, float.Epsilon);
        }

        public float ObstaclesAvoidanceVelocityWeight2
        {
            get => m_ObstacleAvoidanceVelocityWeight2;
            set => m_ObstacleAvoidanceVelocityWeight2 = Mathf.Max(value, float.Epsilon);
        }

        // ReSharper disable once InconsistentNaming
        public float ORCATau
        {
            get => m_ORCATau;
            set
            {
                if (Mathf.Approximately(m_ORCATau, value) || m_ORCATau <= 0)
                    return;

                m_ORCATau = value;

                m_VelocityDangerRadiusTauIsDirty = true;
            }
        }

        public bool AvoidStaticObstacles
        {
            get => m_AvoidStaticObstacles;
            set => m_AvoidStaticObstacles = value;
        }
        
        public float VelocityRadius
        {
            get
            {
                if (m_VelocityDangerRadiusIsDirty)
                {
                    m_VelocityDangerRadius    = Radius + FactualMaxSpeed;
                    m_VelocityDangerRadiusSqr = m_VelocityDangerRadius * m_VelocityDangerRadius;

                    m_VelocityDangerRadiusIsDirty = false;
                }

                return m_VelocityDangerRadius;
            }
        }

        public float VelocityRadiusSqr => m_VelocityDangerRadiusSqr;

        public bool UseLog
        {
            get => m_UseLog;
            set => m_UseLog = value;
        }

        public int LogSize
        {
            get => m_LogSize;
            set
            {
                int finalValue = Mathf.Max(value, 5);

                m_LogSize = finalValue;
            }
        }

        public static Nav3DAgentConfig DefaultConfig => CreateInstance(typeof(Nav3DAgentConfig)) as Nav3DAgentConfig;

        #endregion

        #region Public methods

        /// <summary>
        /// Configures config by default.
        /// </summary>
        public void SetDefaultAttributes()
        {
            m_Radius                           = DEFAULT_RADIUS;
            m_Speed                            = DEFAULT_SPEED;
            m_SpeedToMaxSpeedMultiplier        = DEFAULT_SPEED_TO_MAX_SPEED_MULTIPLIER;
            m_MaxSpeedObtainMode               = DEFAULT_MAX_SPEED_OBTAIN_MODE;
            m_UseConsideredAgentsNumberLimit   = DEFAULT_USE_CONSIDERED_AGENTS_NUMBER_LIMIT;
            m_ConsideredAgentsNumberLimit      = DEFAULT_CONSIDERED_AGENTS_NUMBER_LIMIT;
            m_TargetReachDistance              = m_Radius;
            m_PathfindingTimeout               = DEFAULT_PATHFINDING_TIMEOUT;
            m_SmoothPath                       = DEFAULT_SMOOTH_PATH;
            m_SmoothRatio                      = DEFAULT_SMOOTH_PATH_RATIO;
            m_MotionNavigationType             = DEFAULT_MOTION_NAV_TYPE;
            m_AutoUpdatePath                   = DEFAULT_AUTOUPDATE_PATH;
            m_PathAutoUpdateCooldown           = DEFAULT_AUTOUPDATE_PATH_COOLDOWN;
            m_MaxAgentDegreesRotationPerTick   = DEFAULT_MAX_DEGREES_ROTATION_PER_TICK;
            m_RotationVectorLerpFactor         = DEFAULT_ROTATION_VECTOR_LERP_FACTOR;
            m_TryRepositionTargetIfOccupied    = DEFAULT_TRY_REPOSITION_TARGET_IF_OCCUPIED;
            m_PathVelocityWeight               = DEFAULT_PATH_VELOCITY_WEIGHT;
            m_PathVelocityWeight1              = DEFAULT_PATH_VELOCITY_WEIGHT1;
            m_PathVelocityWeight2              = DEFAULT_PATH_VELOCITY_WEIGHT2;
            m_AgentsAvoidanceVelocityWeight    = DEFAULT_AGENTS_AVOIDANCE_VELOCITY_WEIGHT;
            m_AgentsAvoidanceVelocityWeight1   = DEFAULT_AGENTS_AVOIDANCE_VELOCITY_WEIGHT1;
            m_AgentsAvoidanceVelocityWeight2   = DEFAULT_AGENTS_AVOIDANCE_VELOCITY_WEIGHT2;
            m_ObstacleAvoidanceVelocityWeight  = DEFAULT_OBSTACLES_AVOIDANCE_VELOCITY_WEIGHT;
            m_ObstacleAvoidanceVelocityWeight1 = DEFAULT_OBSTACLES_AVOIDANCE_VELOCITY_WEIGHT1;
            m_ObstacleAvoidanceVelocityWeight2 = DEFAULT_OBSTACLES_AVOIDANCE_VELOCITY_WEIGHT2;
            m_ORCATau                          = DEFAULT_ORCA_TAU;
            m_AvoidStaticObstacles             = DEFAULT_AVOID_STATIC_OBSTACLES;
            m_UseLog                           = DEFAULT_USE_LOG;
            m_LogSize                          = DEFAULT_LOG_SIZE;
        }

        // ReSharper disable once InconsistentNaming
        public void SetDefaultORCATau()
        {
            m_ORCATau = DEFAULT_ORCA_TAU;
        }

        public void SetDefaultTargetReachDistance()
        {
            m_TargetReachDistance = m_Radius;
        }

        public void SetDefaultPathVelocityWeight1()
        {
            m_PathVelocityWeight1 = DEFAULT_PATH_VELOCITY_WEIGHT1;
        }

        public void SetDefaultObstacleAvoidanceVelocityWeihgt1()
        {
            m_ObstacleAvoidanceVelocityWeight1 = DEFAULT_OBSTACLES_AVOIDANCE_VELOCITY_WEIGHT1;
        }

        public void SetDefaultPathVelocityWeight2()
        {
            m_PathVelocityWeight2 = DEFAULT_PATH_VELOCITY_WEIGHT2;
        }

        public void SetDefaultAgentsAvoidanceVelocityWeight1()
        {
            m_AgentsAvoidanceVelocityWeight1 = DEFAULT_AGENTS_AVOIDANCE_VELOCITY_WEIGHT1;
        }

        public void SetDefaultAgentsAvoidanceVelocityWeight2()
        {
            m_AgentsAvoidanceVelocityWeight2 = DEFAULT_AGENTS_AVOIDANCE_VELOCITY_WEIGHT2;
        }

        public void SetDefaultObstacleAvoidanceVelocityWeight2()
        {
            m_ObstacleAvoidanceVelocityWeight2 = DEFAULT_OBSTACLES_AVOIDANCE_VELOCITY_WEIGHT2;
        }

        public void SetDefaultVelocitiesBlendingWeights()
        {
            m_PathVelocityWeight  = DEFAULT_PATH_VELOCITY_WEIGHT;
            m_PathVelocityWeight1 = DEFAULT_PATH_VELOCITY_WEIGHT1;
            m_PathVelocityWeight2 = DEFAULT_PATH_VELOCITY_WEIGHT2;

            m_ObstacleAvoidanceVelocityWeight  = DEFAULT_OBSTACLES_AVOIDANCE_VELOCITY_WEIGHT;
            m_ObstacleAvoidanceVelocityWeight1 = DEFAULT_OBSTACLES_AVOIDANCE_VELOCITY_WEIGHT1;
            m_ObstacleAvoidanceVelocityWeight2 = DEFAULT_OBSTACLES_AVOIDANCE_VELOCITY_WEIGHT2;

            m_AgentsAvoidanceVelocityWeight  = DEFAULT_AGENTS_AVOIDANCE_VELOCITY_WEIGHT;
            m_AgentsAvoidanceVelocityWeight1 = DEFAULT_AGENTS_AVOIDANCE_VELOCITY_WEIGHT1;
            m_AgentsAvoidanceVelocityWeight2 = DEFAULT_AGENTS_AVOIDANCE_VELOCITY_WEIGHT2;
        }

        public void FixInvalidParams(bool _Verbose = true)
        {
            if (m_ORCATau <= 0)
            {
                if (_Verbose)
                    Debug.LogWarning(string.Format(ORCA_TAU_INVALID, ORCATau));

                SetDefaultORCATau();
            }

            if (m_PathVelocityWeight1              == 0 || m_PathVelocityWeight2              == 0 ||
                m_AgentsAvoidanceVelocityWeight1   == 0 || m_AgentsAvoidanceVelocityWeight2   == 0 ||
                m_ObstacleAvoidanceVelocityWeight1 == 0 || m_ObstacleAvoidanceVelocityWeight2 == 0)
            {
                if (_Verbose)
                    Debug.LogWarning(VELOCITY_WEIGHTS_INVALID);

                SetDefaultVelocitiesBlendingWeights();
            }
        }

        public Nav3DAgentConfig Copy()
        {
            Nav3DAgentConfig copy = CreateInstance(typeof(Nav3DAgentConfig)) as Nav3DAgentConfig;

            // ReSharper disable once PossibleNullReferenceException
            copy.m_Radius                           = m_Radius;
            copy.m_MaxSpeedObtainMode               = m_MaxSpeedObtainMode;
            copy.m_SpeedToMaxSpeedMultiplier        = m_SpeedToMaxSpeedMultiplier;
            copy.m_Speed                            = m_Speed;
            copy.m_MaxSpeed                         = m_MaxSpeed;
            copy.m_UseConsideredAgentsNumberLimit   = m_UseConsideredAgentsNumberLimit;
            copy.m_ConsideredAgentsNumberLimit      = m_ConsideredAgentsNumberLimit;
            copy.m_TargetReachDistance              = m_TargetReachDistance;
            copy.m_PathfindingTimeout               = m_PathfindingTimeout;
            copy.m_SmoothPath                       = m_SmoothPath;
            copy.m_SmoothRatio                      = m_SmoothRatio;
            copy.m_MotionNavigationType             = m_MotionNavigationType;
            copy.m_AutoUpdatePath                   = m_AutoUpdatePath;
            copy.m_PathAutoUpdateCooldown           = m_PathAutoUpdateCooldown;
            copy.m_MaxAgentDegreesRotationPerTick   = m_MaxAgentDegreesRotationPerTick;
            copy.m_RotationVectorLerpFactor         = m_RotationVectorLerpFactor;
            copy.m_TryRepositionTargetIfOccupied      = m_TryRepositionTargetIfOccupied;
            copy.m_PathVelocityWeight               = m_PathVelocityWeight;
            copy.m_PathVelocityWeight1              = m_PathVelocityWeight1;
            copy.m_PathVelocityWeight2              = m_PathVelocityWeight2;
            copy.m_AgentsAvoidanceVelocityWeight    = m_AgentsAvoidanceVelocityWeight;
            copy.m_AgentsAvoidanceVelocityWeight1   = m_AgentsAvoidanceVelocityWeight1;
            copy.m_AgentsAvoidanceVelocityWeight2   = m_AgentsAvoidanceVelocityWeight2;
            copy.m_ObstacleAvoidanceVelocityWeight  = m_ObstacleAvoidanceVelocityWeight;
            copy.m_ObstacleAvoidanceVelocityWeight1 = m_ObstacleAvoidanceVelocityWeight1;
            copy.m_ObstacleAvoidanceVelocityWeight2 = m_ObstacleAvoidanceVelocityWeight2;
            copy.m_ORCATau                          = m_ORCATau;
            copy.m_AvoidStaticObstacles             = m_AvoidStaticObstacles;
            copy.m_UseLog                           = m_UseLog;
            copy.m_LogSize                          = m_LogSize;

            return copy;
        }
        
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"{nameof(m_Radius)}: {m_Radius}");

            stringBuilder.AppendLine($"{nameof(m_Speed)}: {m_Radius}");
            stringBuilder.AppendLine($"{nameof(m_MaxSpeed)}: {m_MaxSpeed}");
            stringBuilder.AppendLine($"{nameof(m_SpeedToMaxSpeedMultiplier)}: {m_SpeedToMaxSpeedMultiplier}");
            stringBuilder.AppendLine($"{nameof(m_MaxSpeedObtainMode)}: {m_MaxSpeedObtainMode}");
            stringBuilder.AppendLine($"{nameof(m_UseConsideredAgentsNumberLimit)}: {m_UseConsideredAgentsNumberLimit}");
            stringBuilder.AppendLine($"{nameof(m_ConsideredAgentsNumberLimit)}: {m_ConsideredAgentsNumberLimit}");

            stringBuilder.AppendLine($"{nameof(m_PathfindingTimeout)}: {m_PathfindingTimeout}");
            stringBuilder.AppendLine($"{nameof(m_SmoothPath)}: {m_SmoothPath}");
            stringBuilder.AppendLine($"{nameof(m_SmoothRatio)}: {m_SmoothRatio}");

            stringBuilder.AppendLine($"{nameof(m_MotionNavigationType)}: {m_MotionNavigationType}");

            stringBuilder.AppendLine($"{nameof(m_TargetReachDistance)}: {m_TargetReachDistance}");
            stringBuilder.AppendLine($"{nameof(m_AutoUpdatePath)}: {m_AutoUpdatePath}");
            stringBuilder.AppendLine($"{nameof(m_PathAutoUpdateCooldown)}: {m_PathAutoUpdateCooldown}");
            stringBuilder.AppendLine($"{nameof(m_MaxAgentDegreesRotationPerTick)}: {m_MaxAgentDegreesRotationPerTick}");
            stringBuilder.AppendLine($"{nameof(m_RotationVectorLerpFactor)}: {m_RotationVectorLerpFactor}");
            stringBuilder.AppendLine($"{nameof(m_TryRepositionTargetIfOccupied)}: {m_TryRepositionTargetIfOccupied}");

            stringBuilder.AppendLine($"{nameof(m_PathVelocityWeight)}: {m_PathVelocityWeight}");
            stringBuilder.AppendLine($"{nameof(m_PathVelocityWeight1)}: {m_PathVelocityWeight1}");
            stringBuilder.AppendLine($"{nameof(m_PathVelocityWeight2)}: {m_PathVelocityWeight2}");
            stringBuilder.AppendLine($"{nameof(m_AgentsAvoidanceVelocityWeight)}: {m_AgentsAvoidanceVelocityWeight}");
            stringBuilder.AppendLine($"{nameof(m_AgentsAvoidanceVelocityWeight1)}: {m_AgentsAvoidanceVelocityWeight1}");
            stringBuilder.AppendLine($"{nameof(m_AgentsAvoidanceVelocityWeight2)}: {m_AgentsAvoidanceVelocityWeight2}");
            stringBuilder.AppendLine($"{nameof(m_ObstacleAvoidanceVelocityWeight)}: {m_ObstacleAvoidanceVelocityWeight}");
            stringBuilder.AppendLine($"{nameof(m_ObstacleAvoidanceVelocityWeight1)}: {m_ObstacleAvoidanceVelocityWeight1}");
            stringBuilder.AppendLine($"{nameof(m_ObstacleAvoidanceVelocityWeight2)}: {m_ObstacleAvoidanceVelocityWeight2}");
            stringBuilder.AppendLine($"{nameof(m_ORCATau)}: {m_ORCATau}");
            stringBuilder.AppendLine($"{nameof(m_AvoidStaticObstacles)}: {m_AvoidStaticObstacles}");

            stringBuilder.AppendLine($"{nameof(m_UseLog)}: {m_UseLog}");
            stringBuilder.AppendLine($"{nameof(m_LogSize)}: {m_LogSize}");

            return stringBuilder.ToString();
        }

        #endregion
    }
}
