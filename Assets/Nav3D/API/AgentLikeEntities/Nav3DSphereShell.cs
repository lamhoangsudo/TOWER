using Nav3D.Agents;
using Nav3D.Common;
using Nav3D.LocalAvoidance;
using System;
using UnityEngine;

namespace Nav3D.API
{
    public sealed partial class Nav3DSphereShell : Nav3DAgentBase
    {
        #region Serialized fields

        [SerializeField] float m_Radius = 0.5f;

        #endregion

        #region Attributes

        Nav3DSphereShellMover m_Mover;

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

        #endregion

        #region Events

        public Action<Vector3> OnPositionChanged;

        #endregion

        #region Service methods

        void DoAwake()
        {
            m_CachedTransform = transform;
        }

        #endregion

        #region Public methods

        public override void DoFixedUpdate()
        {
            Vector3 curPosition = m_CachedTransform.position;

            if (curPosition != m_Mover.GetPosition())
            {
                m_Mover.SetCurrentPosition(curPosition);
                OnPositionChanged?.Invoke(curPosition);
            }
        }

        #endregion

        #region Service methods

        void Initialize()
        {
            if (m_IsInited)
                return;

            CheckSettings();

            AgentManager.Instance.RegisterAgent(this);

            Nav3DManager.OnNav3DInitInternal += () =>
            {
                m_Mover = new Nav3DSphereShellMover(m_CachedTransform.position, m_Radius);
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

        void CheckSettings()
        {
            m_Radius = Mathf.Max(0.00001f, m_Radius);
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

        void OnDisable()
        {
            Uninitialize();
        }

        void OnDestroy()
        {
            Uninitialize();
        }

        #endregion
    }
}