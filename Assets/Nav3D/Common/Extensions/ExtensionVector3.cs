using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using System.Globalization;

namespace Nav3D.Common
{
    public static class ExtensionVector3
    {
        #region Static methods

        public static string ToStringExt(this Vector3 _Vector)
        {
            return $"({_Vector.x.ToString(CultureInfo.InvariantCulture)}, " +
                   $"{_Vector.y.ToString(CultureInfo.InvariantCulture)}, "  +
                   $"{_Vector.z.ToString(CultureInfo.InvariantCulture)})";
        }

        public static Vector3 RoundWith5Precision(this Vector3 _Vector)
        {
            return new Vector3(
                (float)Math.Round(_Vector.x, 5),
                (float)Math.Round(_Vector.y, 5),
                (float)Math.Round(_Vector.z, 5));
        }
        
        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        public static int LexCompare(this Vector3 _A, Vector3 _B)
        {
            if (_A.x != _B.x) return _A.x.CompareTo(_B.x);
            if (_A.y != _B.y) return _A.y.CompareTo(_B.y);
            return _A.z.CompareTo(_B.z);
        }
        
        #endregion
    }
}