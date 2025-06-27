using System;

namespace Nav3D.Common
{
    public struct DVector3
    {
        #region Attributes

        public double x;
        public double y;
        public double z;

        #endregion

        #region Properties

        public static DVector3 zero       => zeroVector;
        public        DVector3 normalized => Normalize(this);

        #endregion

        #region Constructors

        public DVector3(double _X, double _Y, double _Z)
        {
            x = _X;
            y = _Y;
            z = _Z;
        }

        static readonly DVector3 zeroVector = new DVector3(0.0, 0.0, 0.0);

        #endregion

        #region Static methods

        public static double Magnitude(DVector3 _Vector)
        {
            return Math.Sqrt(_Vector.x * _Vector.x + _Vector.y * _Vector.y + _Vector.z * _Vector.z);
        }

        public static DVector3 Normalize(DVector3 _Value)
        {
            double num = Magnitude(_Value);
            return num > 9.999999747378752E-06 ? _Value / num : zero;
        }

        public static DVector3 Cross(DVector3 _Lhs, DVector3 _Rhs)
        {
            return new DVector3(
                (_Lhs.y * _Rhs.z - _Lhs.z * _Rhs.y),
                (_Lhs.z * _Rhs.x - _Lhs.x * _Rhs.z),
                (_Lhs.x * _Rhs.y - _Lhs.y * _Rhs.x));
        }

        public static DVector3 operator -(DVector3 _A, DVector3 _B)
        {
            return new DVector3(_A.x - _B.x, _A.y - _B.y, _A.z - _B.z);
        }

        public static DVector3 operator /(DVector3 _A, double _D) => new DVector3(_A.x / _D, _A.y / _D, _A.z / _D);

        #endregion
    }
}