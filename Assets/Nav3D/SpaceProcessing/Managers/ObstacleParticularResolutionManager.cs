using UnityEngine;
using System.Collections.Generic;
using Nav3D.Common;
using Nav3D.API;

namespace Nav3D.Obstacles
{
    class ObstacleParticularResolutionManager : MonoBehaviour
    {
        #region Attributes

        bool m_Inited;
        BoundablesSpatialHashMap<Nav3DParticularResolutionRegion> m_RegionStorage;

        #endregion

        #region Properties

        public static ObstacleParticularResolutionManager Instance => Singleton<ObstacleParticularResolutionManager>.Instance;

        public static bool Doomed { get; private set; } = false;

        #endregion

        #region Public methods

        public bool HasCrossingBoundables(Bounds _Bounds)
        {
            return m_RegionStorage.HasCrossingBoundables(_Bounds);
        }

        public int GetCrossingBoundablesCount(Bounds _Bounds)
        {
            return m_RegionStorage.GetCrossingBoundablesCount(_Bounds);
        }

        public bool TryGetCrossingRegions(Bounds _Bounds, out HashSet<Nav3DParticularResolutionRegion> _CrossingRegions)
        {
            return m_RegionStorage.TryGetCrossingBoundables(_Bounds, out _CrossingRegions);
        }

        public bool TryGetEmbracingRegions(Bounds _Bounds, out HashSet<Nav3DParticularResolutionRegion> _EmbracingRegions)
        {
            return m_RegionStorage.TryGetEmbracingBoundables(_Bounds, out _EmbracingRegions);
        }
        
        public bool TryGetEmbracedBoundables(Bounds _Bounds, out HashSet<Nav3DParticularResolutionRegion> _EmbracingRegions)
        {
            return m_RegionStorage.TryGetEmbracedBoundables(_Bounds, out _EmbracingRegions);
        }

        public void Register(Nav3DParticularResolutionRegion _Boundable)
        {
            if (!m_Inited)
                Initialize();

            m_RegionStorage.Register(_Boundable);
        }

        public void Unregister(Nav3DParticularResolutionRegion _Boundable)
        {
            if (!m_Inited)
                return;

            m_RegionStorage.Unregister(_Boundable);
        }

        public void Initialize()
        {
            m_RegionStorage = new BoundablesSpatialHashMap<Nav3DParticularResolutionRegion>();

            m_Inited = true;
        }

        public void Uninitialize()
        {
            m_RegionStorage = null;

            m_Inited = false;

            UtilsCommon.SmartDestroy(this);
        }

#if UNITY_EDITOR
        public void Draw()
        {
            m_RegionStorage?.Draw();
        }
#endif

#endregion

        #region Unity events

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