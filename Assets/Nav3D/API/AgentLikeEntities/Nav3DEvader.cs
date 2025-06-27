using Nav3D.Agents;
using Nav3D.LocalAvoidance;
using System;
using UnityEngine;

namespace Nav3D.API
{
    public sealed partial class Nav3DEvader : Nav3DAgentBase
    {
        #region Serialized fields

        [SerializeField] float m_Radius;
        [SerializeField] float m_MaxSpeed;
        [SerializeField] float m_ORCATau;
        [SerializeField] float m_SpeedDecayFactor = 0.25f;

        #endregion

        #region Attributes

        Nav3DEvaderMover m_Mover;

        Vector3 m_PositionLast;

        bool m_IsInited;

        #endregion

        #region Properties

        public float Radius
        {
            get => m_Radius;
            set
            {
                m_Radius = value;

                Uninitialize();
                Initialize();
            }
        }

        public float MaxSpeed
        {
            get => m_MaxSpeed;
            set
            {
                m_MaxSpeed = value;

                Uninitialize();
                Initialize();
            }
        }

        public float ORCATau
        {
            get => m_ORCATau;
            set
            {
                m_ORCATau = value;

                Uninitialize();
                Initialize();
            }
        }

        public float SpeedDecayFactor
        {
            get => m_SpeedDecayFactor;
            set
            {
                m_SpeedDecayFactor = value;

                Uninitialize();
                Initialize();
            }
        }

        #endregion

        #region Events

        public Action<Vector3> OnPositionChanged;

        #endregion

        #region Public methods

        public override void DoFixedUpdate()
        {
            if (m_CachedTransform.position != m_Mover.GetPosition())
                m_Mover.SetCurrentPosition(m_CachedTransform.position);

            Vector3 newPos = m_CachedTransform.position + m_Mover.GetVelocity();

            m_CachedTransform.position = newPos;

            m_Mover.SetCurrentPosition(newPos);

            if (m_PositionLast != newPos)
            {
                m_PositionLast = newPos;
                OnPositionChanged?.Invoke(m_PositionLast);
            }
        }

        #endregion

        #region Service methods

        void DoAwake()
        {
            m_CachedTransform = transform;
        }

        void CheckSettings()
        {
            m_Radius           = Mathf.Max(0.00001f, m_Radius);
            m_MaxSpeed         = Mathf.Max(0.00001f, m_MaxSpeed);
            m_ORCATau          = Mathf.Max(1f, m_ORCATau);
            m_SpeedDecayFactor = Mathf.Max(0.000001f, m_SpeedDecayFactor);
        }

        void Initialize()
        {
            if (m_IsInited)
                return;

            CheckSettings();

            AgentManager.Instance.RegisterAgent(this);

            Nav3DManager.OnNav3DInitInternal += () =>
            {
                m_Mover = new Nav3DEvaderMover(m_CachedTransform.position, m_Radius, m_MaxSpeed, m_ORCATau, m_SpeedDecayFactor);
                m_Mover.Initialize();

                m_IsInited = true;
            };
        }

        void Uninitialize()
        {
            m_IsInited = false;

            if (!AgentManager.Doomed)
                AgentManager.Instance.UnregisterAgent(this);

            m_Mover.Uninitialize();
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