using UnityEngine;
using Nav3D.Common;
using System;
using Nav3D.Obstacles;

namespace Nav3D.API
{
    public class Nav3DParticularResolutionRegion : MonoBehaviour, IBoundable
    {
        #region Constants

        readonly string RESOLUTION_ZERO_LESS_EX =
            $"[{nameof(Nav3DParticularResolutionRegion)}]: Initialization error. The min. bucket value must be positive. The given value is {{0}}.";

        readonly string RESOLUTION_INCORRECT_SIZE_EX =
            $"[{nameof(Nav3DParticularResolutionRegion)}]: Initialization error. The min. bucket value {{0}} must be greater than the minimum region size {{1}}.";

        readonly Color REGION_COLOR = new Color(1f, 0.75f, 0);

        const string MENU_PATH   = "Nav3D/Create Resolution region";
        const string PREFAB_NAME = "Nav3DResolutionRegion";

        #endregion

        #region Attributes

        [SerializeField] float m_MinBucketSize;

        [SerializeField] float m_SizeX;
        [SerializeField] float m_SizeY;
        [SerializeField] float m_SizeZ;

        [SerializeField] bool m_DrawAlways;

        Bounds m_Bounds;

        //cached transform
        Transform m_Transform;

        #endregion

        #region Properties

        public Bounds Bounds        => m_Bounds;
        public float  MinBucketSize => m_MinBucketSize;

        #endregion

        #region Public methods

        #if UNITY_EDITOR
        [UnityEditor.MenuItem(MENU_PATH)]
        public static void CreateOnScene()
        {
            UtilsEditor.InstantiateGOWithComponent<Nav3DParticularResolutionRegion>(nameof(Nav3DParticularResolutionRegion));
        }
        #endif

        public void InitializeEditMode()
        {
            ValidateParams();

            Initialize();

            ObstacleParticularResolutionManager.Instance.Register(this);
        }

        #endregion

        #region Service methods

        void ValidateParams()
        {
            if (m_MinBucketSize <= 0)
                throw new Exception(string.Format(RESOLUTION_ZERO_LESS_EX, m_MinBucketSize));

            if (m_MinBucketSize >= m_SizeX || m_MinBucketSize >= m_SizeY || m_MinBucketSize >= m_SizeZ)
                throw new Exception(string.Format(RESOLUTION_INCORRECT_SIZE_EX, m_MinBucketSize, Mathf.Min(m_SizeX, m_SizeY, m_SizeZ)));
        }

        #if UNITY_EDITOR
        void Draw()
        {
            using (Common.Debug.UtilsGizmos.ColorPermanence)
            {
                Gizmos.color = REGION_COLOR;

                Gizmos.DrawWireCube(transform.position, new Vector3(m_SizeX, m_SizeY, m_SizeZ));
            }
        }
        #endif

        void Register()
        {
            ValidateParams();

            Nav3DManager.OnNav3DPreInit += () =>
            {
                if (ObstacleParticularResolutionManager.Doomed)
                    return;

                ObstacleParticularResolutionManager.Instance.Register(this);

                ObstacleManager.Instance.UpdateBoundsCrossingObstacles(Bounds);
            };
        }

        void Unregister()
        {
            if (!Nav3DManager.Inited)
                return;

            if (!ObstacleParticularResolutionManager.Doomed)
                ObstacleParticularResolutionManager.Instance.Unregister(this);

            if (!ObstacleManager.Doomed)
                ObstacleManager.Instance.UpdateBoundsCrossingObstacles(Bounds);
        }

        void Initialize()
        {
            m_Transform = transform;
            m_Bounds    = new Bounds(m_Transform.position, new Vector3(m_SizeX, m_SizeY, m_SizeZ));
        }

        #endregion

        #region Unity events

        void Awake()
        {
            Initialize();
        }

        void Update()
        {
            if (m_Transform.hasChanged)
            {
                m_Bounds               = new Bounds(m_Transform.position, new Vector3(m_SizeX, m_SizeY, m_SizeZ));
                m_Transform.hasChanged = false;
            }
        }

        void OnEnable()
        {
            Register();
        }

        void OnDisable()
        {
            Unregister();
        }
        #if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (m_DrawAlways)
                return;

            Draw();
        }

        void OnDrawGizmos()
        {
            if (!m_DrawAlways)
                return;

            Draw();
        }
        #endif

        #endregion
    }
}