using Nav3D.API;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nav3D.Tutorials
{
    public class WaypointAgent : MonoBehaviour
    {
        #region Serialized fields

        [SerializeField] List<Transform> m_TargetPoints;

        #endregion

        #region Attributes

        Nav3DAgent m_Agent;

        #endregion

        #region Unity events

        void Awake()
        {
            Nav3DManager.OnNav3DInit += Init;
        }

        #endregion

        #region Service methods

        void Init()
        {
            m_Agent = GetComponent<Nav3DAgent>();
            m_Agent.MoveToPoints(m_TargetPoints.Select(_Point => _Point.position).ToArray(), _Loop: true, _OnFinished: () => { Destroy(gameObject); });
        }

        #endregion
    }
}