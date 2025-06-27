using UnityEngine;

namespace Nav3D.Common
{
    public interface IBoundsIntersectable
    {
        public bool Intersects(Bounds _Boudns);
    }
}