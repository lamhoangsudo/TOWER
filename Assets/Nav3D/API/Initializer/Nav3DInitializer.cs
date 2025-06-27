using UnityEngine;
using System;
using System.Linq;
using Nav3D.Common;

namespace Nav3D.API
{
    [ExecuteInEditMode]
    public class Nav3DInitializer : MonoBehaviour
    {
        #region Constants

        readonly string INIT_ON_AWAKE_ERROR = $"{nameof(Nav3DInitializer)}.{nameof(m_InitOnAwake)} flag is set to true, " +
            "so it is not possible to call initialization manually. The Nav3D initialization will happen on the Awake event.";

        const string MENU_PATH = "Nav3D/Create Initializer";

        readonly string INSTANCE_HAS_EXIST_ON_SCENE_ERROR = $"An instance of {nameof(Nav3DInitializer)} is already present in the scene, duplication is not possible.";

        #endregion

        #region Serialized fields

        [SerializeField] bool m_InitOnAwake = true;
        [SerializeField] bool m_DisposeOnDestroy = true;
        [SerializeField] float m_MinBucketSize = 0.5f;

        #endregion

        #region Properties

        public float MinBucketSize
        {
            get => m_MinBucketSize;
            set => m_MinBucketSize = value;
        }

        public bool IsSettingsValid => m_MinBucketSize > 0;

        #endregion

        #region Public methods

        /// <summary>
        /// Initializes Nav3D with a preset minimum bucket size equal to MinBucketSize.
        /// </summary>
        public void Init()
        {
            if (m_InitOnAwake)
                throw new Exception(INIT_ON_AWAKE_ERROR);

            Nav3DManager.InitNav3DRuntime(m_MinBucketSize);
        }

        /// <summary>
        /// Disposes Nav3D and all its entities;
        /// </summary>
        public void Utilize()
        {
            Nav3DManager.Dispose3DNav();
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem(MENU_PATH)]
        public static void CreateOnScene()
        {
            UtilsEditor.InstantiateGOWithComponent<Nav3DInitializer>(nameof(Nav3DInitializer));
        }
#endif

        #endregion

        #region Unity events

        void Awake()
        {
#if UNITY_EDITOR
            if (FindObjectsOfType<Nav3DInitializer>().Any(_Instance => _Instance != this))
            {
                Debug.LogError(INSTANCE_HAS_EXIST_ON_SCENE_ERROR);

                UtilsCommon.SmartDestroy(this);

                return;
            }

            if (!Application.isPlaying || !enabled)
                return;
#endif

            if (!m_DisposeOnDestroy)
                DontDestroyOnLoad(this);

            if (m_InitOnAwake)
                Nav3DManager.InitNav3DRuntime(m_MinBucketSize);
        }

        void OnDestroy()
        {
            if (m_DisposeOnDestroy)
                Nav3DManager.Dispose3DNav();
        }

        #endregion
    }
}