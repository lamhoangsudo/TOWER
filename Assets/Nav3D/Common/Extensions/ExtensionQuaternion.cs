using UnityEngine;
using System.Globalization;

namespace Nav3D.Common
{
    public static class ExtensionQuaternion
    {
        #region Static methods

        public static string ToStringExt(this Quaternion _Quaternion)
        {
            return $"({{{_Quaternion.w.ToString(CultureInfo.InvariantCulture)}}} " +
                   $"{{{_Quaternion.x.ToString(CultureInfo.InvariantCulture)}}} " +
                   $"{{{_Quaternion.y.ToString(CultureInfo.InvariantCulture)}}} " +
                   $"{{{_Quaternion.z.ToString(CultureInfo.InvariantCulture)}}})";
        }

        #endregion
    }
}