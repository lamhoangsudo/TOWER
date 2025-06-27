using System.Linq;
using UnityEngine;
using Nav3D.API;

namespace Nav3D.Demo
{
    public class WaypointAgentController : MonoBehaviour
    {
        #region Serialized fields

        [SerializeField] Transform[] m_WaypointsTransforms;
        [SerializeField] bool        m_Loop;
        [SerializeField] Material    m_PathRendererMaterial;

        #endregion

        #region Attributes

        Nav3DAgent   m_Agent;
        LineRenderer m_PathRenderer;

        Vector3[] m_Waypoints;

        #endregion

        #region Unity events

        void Awake()
        {
            m_Agent = GetComponent<Nav3DAgent>();

            m_PathRenderer            = GetComponent<LineRenderer>();
            m_PathRenderer.material   = m_PathRendererMaterial;
            m_PathRenderer.startWidth = m_PathRenderer.endWidth = 0.05f;

            m_Agent.OnPathUpdated += UpdatePathRenderer;

            m_Waypoints = m_WaypointsTransforms.Select(_Transform => _Transform.position).ToArray();

            Nav3DManager.OnNav3DInit += MoveAlongWaypoints;
        }

        #endregion

        #region Service methods

        void MoveAlongWaypoints()
        {
            m_Agent.MoveToPoints(m_Waypoints, _Loop: m_Loop, _OnTargetPassed: OnTargetPassed, _OnPathfindingFail: OnPathfindingFail, _OnFinished: PointsFollowingFinished);
        }

        void UpdatePathRenderer(Vector3[] _Path)
        {
            m_PathRenderer.positionCount = _Path.Length;
            m_PathRenderer.SetPositions(_Path);
        }
        
        void OnTargetPassed(Vector3 _Target)
        {
            Debug.Log($"{name} passed target: {_Target}");
        }

        void OnPathfindingFail(PathfindingError _Error)
        {
            Debug.LogError(_Error.Msg);
        }

        void PointsFollowingFinished()
        {
            Debug.Log($"{name} finished points following.");
        }

        #endregion
    }
}