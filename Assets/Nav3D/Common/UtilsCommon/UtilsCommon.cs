using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nav3D.Common
{
    public static class UtilsCommon
    {
        #region Nested types

        class RandomSeedInvariant : IDisposable
        {
            #region Atributes

            UnityEngine.Random.State m_RandomStateBuffer;

            #endregion

            #region Construction

            public RandomSeedInvariant()
            {
                m_RandomStateBuffer = UnityEngine.Random.state;
            }

            #endregion

            #region IDisposable methods

            public void Dispose() => UnityEngine.Random.state = m_RandomStateBuffer;

            #endregion
        }

        #endregion

        #region Properties

        public static IDisposable RandomSeedPermanence => new RandomSeedInvariant();

        #endregion

        #region Public methods

        public static Color[] GetNDistancedColors(int _N)
        {
            Color[] result = new Color[_N];

            float step = (360f / _N) / 360f;
            float hue  = 0;

            for (int i = 0; i < _N; i++)
            {
                result[i] = Color.HSVToRGB(hue, 1f, 1f);

                hue += step;
            }

            return result;
        }

        public static void SmartDestroy(Object _Object)
        {
            if (_Object == null)
            {
                return;
            }

            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                GameObject gameObjectRef = null;

                if (_Object is Component component)
                    gameObjectRef = component.gameObject;

                Object.DestroyImmediate(_Object);

                if (gameObjectRef != null)
                    Object.DestroyImmediate(gameObjectRef);
            }
            else
                #endif
            {
                Object.Destroy(_Object);
            }
        }

        public static string GetPointsString(IEnumerable<Vector3> _Points)
        {
            return $"[{string.Join(", ", _Points.Select(_Target => _Target.ToStringExt()))}]";
        }

        //Trims subsequences of equal points
        //[{0.1, 0.1, 0.1}, {0.1, 0.1, 0.1}, {0.6, 0.6, 0.6}, {0.1, 0.1, 0.1}] -> [{0.1, 0.1, 0.1}, {0.6, 0.6, 0.6}, {0.1, 0.1, 0.1}]
        public static Vector3[] TrimEqualPoints(Vector3[] _Points)
        {
            if (_Points == null || _Points.Length < 2)
                return _Points?.ToArray();

            List<Vector3> result = new List<Vector3>(_Points.Length) { _Points[0] };

            Vector3 prePoint = _Points[0];

            for (int i = 1; i < _Points.Length; i++)
            {
                Vector3 curPoint = _Points[i];

                if (prePoint == curPoint)
                    continue;

                result.Add(curPoint);

                prePoint = curPoint;
            }

            return result.ToArray();
        }
        
        //Enumerates point starting from the closest one. If loop is true, then add beginning of the sequence to the end of ordered set
        public static Vector3[] ReorderStartingFromClosest(Vector3[] _Points, Vector3 _Position, bool _Loop)
        {
            int   indexOfClosest = -1;
            float minSqrDistance = float.MaxValue;

            List<Vector3> result = new List<Vector3>(_Points.Length);
            
            for (int i = 0; i < _Points.Length; i++)
            {
                Vector3 point       = _Points[i];
                float   sqrDistance = (_Position - point).sqrMagnitude;

                if (!(sqrDistance < minSqrDistance))
                    continue;
                
                minSqrDistance = sqrDistance;
                indexOfClosest = i;
            }

            for (int i = indexOfClosest; i < _Points.Length; i++)
            {
                result.Add(_Points[i]);
            }

            if (_Loop)
            {
                for (int i = 0; i < indexOfClosest; i++)
                {
                    result.Add(_Points[i]);
                }
            }

            return result.ToArray();
        }

        #endregion
    }
}