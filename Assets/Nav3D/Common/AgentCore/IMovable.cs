using System;
using UnityEngine;

namespace Nav3D.Common
{
    public interface IMovable
    {
        #region Events

        event Action<IMovable, Vector3> OnPositionChanged;

        #endregion

        #region Properties

        //Whether is need to avoid the movable.
        bool NeedToBeAvoided { get; }
        //Whether the movable perform avoiding.
        bool Avoiding                 { get; }
        bool IsNeighborMoversDirty    { get; }
        bool IsNeighborObstaclesDirty { get; }

        #endregion

        #region Public methods

        /// <summary>
        /// The current position of the agent after the last move (usually the same as transform.position).
        /// </summary>
        Vector3 GetPosition();

        /// <summary>
        /// Velocity vector used at las fixed update tick. Used by other movables to compute avoidance velocity.
        /// </summary>
        Vector3 GetLastFrameVelocity();

        Vector3 GetLastNonZeroVelocity();

        /// <summary>
        /// Radius in world units. Used by other movables to compute avoidance velocity.
        /// </summary>
        float GetRadius();

        /// <summary>
        /// The maximum allowed movement speed per fixed update tick 
        /// </summary>
        float GetMaxSpeed();

        float GetORCATau();
        
        float GetStaticObstaclesDangerDistance();

        void SetNeighborMovablesDirty(bool  _Dirty);
        void SetNeighborObstaclesDirty(bool _Dirty);

        #endregion
    }
}