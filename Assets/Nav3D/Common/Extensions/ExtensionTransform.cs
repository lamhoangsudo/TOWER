using Nav3D.Obstacles;
using System.Collections.Generic;
using UnityEngine;
using Nav3D.API;

namespace Nav3D.Common
{
    public static class ExtensionTransform
    {
        #region Constants

        static readonly string MESH_ERROR =
            $"The obstacle transform (name: {{0}}, InstanceID: {{1}}) has no {nameof(MeshFilter)} component, or it's mesh has no any triangles";

        #endregion

        #region Public methods

        //Here we assume that each obstacle contains either a mesh or a terrain, and not both.
        public static bool TryGetObstacleInfo(
                this Transform         _Transform,
                Nav3DObstacle          _ObstacleRootController,
                out ObstacleInfoSingle _AdditionData
            )
        {
            if (TryGetObstacleMeshData(_Transform, _ObstacleRootController, out _AdditionData))
                return true;

            if (TryGetObstacleTerrainData(_Transform, _ObstacleRootController, out _AdditionData))
                return true;

            UnityEngine.Debug.LogWarning(string.Format(MESH_ERROR, _Transform.name, _Transform.GetInstanceID()));

            return false;
        }

        public static List<Transform> GetAllChildren(this Transform _Root, bool _CheckActive = true)
        {
            List<Transform> transformList = new List<Transform>();

            GetAllChildren(_Root, transformList, _CheckActive);

            return transformList;
        }

        #endregion

        #region Service methods

        static bool TryGetObstacleMeshData(
                this Transform         _Transform,
                Nav3DObstacle          _ObstacleRootController,
                out ObstacleInfoSingle _AdditionData
            )
        {
            if (!_Transform.TryGetComponent(out MeshFilter meshFilter) || meshFilter.sharedMesh.triangles.Length == 0)
            {
                _AdditionData = null;

                return false;
            }

            Mesh obstacleMesh = meshFilter.sharedMesh;

            _AdditionData = new ObstacleInfoMesh(
                    _ObstacleRootController.InstanceID,
                    obstacleMesh.vertices,
                    obstacleMesh.triangles,
                    _Transform.position,
                    _Transform.lossyScale,
                    _Transform.rotation
                );

            return true;
        }

        static bool TryGetObstacleTerrainData(
                this Transform         _Transform,
                Nav3DObstacle          _ObstacleRootController,
                out ObstacleInfoSingle _AdditionData
            )
        {
            if (!_Transform.TryGetComponent(out Terrain terrain))
            {
                _AdditionData = null;
                return false;
            }

            TerrainData terrainData         = terrain.terrainData;
            int         heightMapResolution = terrainData.heightmapResolution;

            _AdditionData = new ObstacleInfoTerrain(
                    _ObstacleRootController.InstanceID,
                    terrainData.GetHeights(0, 0, heightMapResolution, heightMapResolution),
                    heightMapResolution,
                    terrainData.size,
                    _Transform.position
                );

            return true;
        }

        //Parses transform tree. Resulting list contains root transform and all nested children.
        static void GetAllChildren(Transform _Transform, List<Transform> _Children, bool _CheckActive)
        {
            if (_CheckActive && !_Transform.gameObject.activeInHierarchy)
                return;

            if (_Children != null)
                _Children.Add(_Transform);
            else
                _Children = new List<Transform> { _Transform };

            foreach (Transform child in _Transform)
            {
                GetAllChildren(child, _Children, _CheckActive);
            }
        }

        #endregion
    }
}