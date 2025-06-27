using Nav3D.API;
using Nav3D.Common;
using UnityEngine;

public class SphereShellDemo : MonoBehaviour
{
    #region Serialized fields

    [SerializeField] Transform m_Center;

    [SerializeField] GameObject m_RedAgentPrefab;
    [SerializeField] GameObject m_BlueAgentPrefab;
    [SerializeField] GameObject m_GreenAgentPrefab;

    #endregion
    
    #region Attributeas

    Vector3[] m_CircleSmallPoints;
    Vector3[] m_CircleMediumPoints;
    Vector3[] m_CircleLargePoints;
    Vector3[] m_CircleLarge1Points;
    
    #endregion
    
    #region Service methods

    void Init()
    {
        GameObject agentsRoot = new GameObject("AgentsRoot");

        m_CircleSmallPoints  = UtilsMath.GetSpaceCirclePoints(m_Center.position, 1, new Vector3(1, 1, 1), 32);
        m_CircleMediumPoints = UtilsMath.GetSpaceCirclePoints(m_Center.position, 2, new Vector3(-1, 1, 1), 32);
        m_CircleLargePoints  = UtilsMath.GetSpaceCirclePoints(m_Center.position, 3, new Vector3(1, 1, -1), 64);
        m_CircleLarge1Points = UtilsMath.GetSpaceCirclePoints(m_Center.position + new Vector3(1, 1, -1) * 0.25f, 3, new Vector3(1, 1, -1), 64);

        foreach (Vector3 t in m_CircleSmallPoints)
        {
            Instantiate(m_RedAgentPrefab, t, Quaternion.identity, agentsRoot.transform)
               .GetComponent<Nav3DAgent>()
               .MoveToPoints(m_CircleSmallPoints, true, true);
        }

        foreach (Vector3 t in m_CircleMediumPoints)
        {
            Instantiate(m_BlueAgentPrefab, t, Quaternion.identity, agentsRoot.transform)
               .GetComponent<Nav3DAgent>()
               .MoveToPoints(m_CircleMediumPoints, true, true);
        }

        foreach (Vector3 t in m_CircleLargePoints)
        {
            Instantiate(m_GreenAgentPrefab, t, Quaternion.identity, agentsRoot.transform)
               .GetComponent<Nav3DAgent>()
               .MoveToPoints(m_CircleLargePoints, true, true);
        }
        
        foreach (Vector3 t in m_CircleLarge1Points)
        {
            Instantiate(m_GreenAgentPrefab, t, Quaternion.identity, agentsRoot.transform)
               .GetComponent<Nav3DAgent>()
               .MoveToPoints(m_CircleLarge1Points, true, true);
        }
    }

    void DrawPointsSequence(Vector3[] _Points, Color _Color)
    {
        if (_Points == null)
            return;
        
        Gizmos.color = _Color;
        
        for (int i = 0; i < _Points.Length - 1; i++)
        {
            Gizmos.DrawLine(_Points[i], _Points[i + 1]);
        }
    }
    
    #endregion
    
    #region Unity events

    void Start()
    {
        Nav3DManager.OnNav3DInit += Init;
    }

    void OnDrawGizmos()
    {
        if (!enabled || !Application.isPlaying)
            return;

        DrawPointsSequence(m_CircleSmallPoints, Color.red);
        DrawPointsSequence(m_CircleMediumPoints, Color.blue);
        DrawPointsSequence(m_CircleLargePoints, Color.green);
    }

    #endregion
}
