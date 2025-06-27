using Nav3D.Obstacles;
using System.Diagnostics;
using UnityEngine;

namespace Nav3D.API
{
    #if UNITY_EDITOR

    public partial class Nav3DObstacle
    {
        #region Serialized fields

        [SerializeField] bool m_DrawOccupiedLeaves;
        [SerializeField] bool m_DrawFreeLeaves;
        [SerializeField] bool m_DrawGraph;
        [SerializeField] bool m_DrawRoots;

        [SerializeField] int m_OccupiedLeavesDrawingMode; //0-all layers, 1-specific layer
        [SerializeField] int m_OccupiedLeavesDrawingLayerNumber;

        [SerializeField] int m_FreeLeavesDrawingMode; //0-all layers, 1-specific layer
        [SerializeField] int m_FreeLeavesDrawingLayerNumber;

        [SerializeField] int m_GraphNodesDrawingMode; //0-all layers, 1-specific layer
        [SerializeField] int m_GraphNodesDrawingLayerNumber;

        #endregion

        #region Attributes

        Common.Debug.GizmosDrawData m_GizmosData;

        #endregion

        #region Public methods

        [Conditional("UNITY_EDITOR")]
        public void ClearGizmosCache()
        {
            m_GizmosData?.Clear();
        }

        #endregion

        #region Unity events

        void OnDrawGizmos()
        {
            if (!Application.isPlaying || !enabled)
                return;

            if (m_GizmosData == null)
                m_GizmosData = new Common.Debug.GizmosDrawData();

            if (m_DrawOccupiedLeaves || m_DrawFreeLeaves || m_DrawGraph || m_DrawRoots)
            {
                if (m_GizmosData.IsClean)
                {
                    //add data
                    ObstacleManager.Instance.FillGizmosDrawData(
                            m_GizmosData,
                            m_InstanceID,
                            m_DrawOccupiedLeaves,
                            m_DrawFreeLeaves,
                            m_DrawGraph,
                            m_DrawRoots,
                            m_OccupiedLeavesDrawingMode,
                            m_OccupiedLeavesDrawingLayerNumber,
                            m_FreeLeavesDrawingMode,
                            m_FreeLeavesDrawingLayerNumber,
                            m_GraphNodesDrawingMode,
                            m_GraphNodesDrawingLayerNumber
                        );
                }
                else
                {
                    m_GizmosData.Draw();
                }
            }
        }

        #endregion
    }

    #endif
}