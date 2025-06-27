using Nav3D.Agents;
using Nav3D.LocalAvoidance;
using System.Linq;
using UnityEngine;

namespace Nav3D.API
{
    static class Nav3DAgentManager
    {
        #region Public methods

        /// <summary>
        /// Get agents inside the bounds.
        /// </summary>
        public static Nav3DAgent[] GetAgentsInBounds(Bounds _Bounds)
        {
            if (!Nav3DManager.Inited)
                throw new Nav3DManager.Nav3DManagerNotInitializedException();

            return AgentManager.Instance.GetMovablesInBounds(_Bounds)
                               .Where(_Movable => _Movable is Nav3DAgentMover)
                               .Select(_Movable => ((Nav3DAgentMover) _Movable).Agent)
                               .ToArray();
        }

        /// <summary>
        /// Get agents inside the sphere.
        /// </summary>
        public static Nav3DAgent[] GetAgentsInSphere(Vector3 _Center, float _Radius)
        {
            if (!Nav3DManager.Inited)
                throw new Nav3DManager.Nav3DManagerNotInitializedException();

            float diameter  = _Radius * 2;
            float sqrRadius = _Radius * _Radius;

            return AgentManager.Instance.GetMovablesInBounds(new Bounds(_Center, new Vector3(diameter, diameter, diameter)))
                               .Where(_Movable => _Movable is Nav3DAgentMover && (_Movable.GetPosition() - _Center).sqrMagnitude <= sqrRadius)
                               .Select(_Movable => ((Nav3DAgentMover) _Movable).Agent)
                               .ToArray();
        }

        #endregion
    }
}