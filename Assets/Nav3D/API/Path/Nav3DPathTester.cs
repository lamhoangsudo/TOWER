using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nav3D.API
{
    public class Nav3DPathTester : MonoBehaviour
    {
        #region Serialized fields

        [SerializeField] List<Transform> m_Targets = new List<Transform>(2);
        [SerializeField] bool            m_Loop;
        [SerializeField] bool            m_Smooth      = true;
        [SerializeField] int             m_SmoothRatio = 3;
        [SerializeField] bool            m_SkipUnpassableTargets;

        #endregion

        #region Attributes

        readonly Dictionary<Transform, Vector3> m_CachedTargetPositions = new Dictionary<Transform, Vector3>();

        bool m_CachedLoop;
        bool m_CachedSmooth;
        int  m_CachedSmoothRatio;
        bool m_CachedSkipUnpassableTargets;

        Nav3DPath m_Path;

        #endregion

        #region Unity methods

        void Update()
        {
            if (m_Targets.Count < 2)
                return;

            if (m_CachedLoop                  != m_Loop                        ||
                m_CachedSmooth                != m_Smooth                      ||
                m_CachedSmoothRatio           != m_SmoothRatio                 ||
                m_CachedSkipUnpassableTargets != m_SkipUnpassableTargets       ||
                m_Targets.Count               != m_CachedTargetPositions.Count ||
                m_Targets.Any(_Target => m_CachedTargetPositions[_Target] != _Target.position))
                UpdatePath();
        }

        void OnDisable()
        {
            m_Path?.Dispose();
        }

        #if UNITY_EDITOR

        void OnDrawGizmos()
        {
            if (!Application.isPlaying || !isActiveAndEnabled)
                return;

            m_Path?.Draw();
        }

        #endif

        #endregion

        #region Service methods

        void UpdatePath()
        {
            m_CachedTargetPositions.Clear();

            m_Targets.ForEach(_Target => m_CachedTargetPositions.Add(_Target, _Target.position));

            m_CachedLoop                  = m_Loop;
            m_CachedSmooth                = m_Smooth;
            m_CachedSmoothRatio           = m_SmoothRatio = Mathf.Max(m_SmoothRatio, 1);
            m_CachedSkipUnpassableTargets = m_SkipUnpassableTargets;

            m_Path ??= new Nav3DPath($"{gameObject.name} [{GetInstanceID()}]");

            m_Path.Smooth      = m_CachedSmooth;
            m_Path.SmoothRatio = m_CachedSmoothRatio;

            Nav3DManager.OnNav3DInit += () =>
            {
                m_Path.Find(
                        m_Targets.Select(_Target => _Target.position).ToArray(),
                        m_CachedLoop,
                        m_CachedSkipUnpassableTargets,
                        _OnFail: OnPathfindingFail
                    );
            };
        }

        void OnPathfindingFail(PathfindingError _Error)
        {
            Debug.LogError(_Error.ToString());
        }

        #endregion
    }
}