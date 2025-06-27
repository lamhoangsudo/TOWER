using System.Collections.Generic;

#if UNITY_EDITOR
namespace Nav3D.Common.Debug
{
    public class GizmosDrawData
    {
        #region Attributes

        List<IDrawable> m_Drawables = new List<IDrawable>();

        #endregion

        #region Properties

        //No data has been added yet
        public bool IsClean { get; private set; }

        #endregion

        #region Public methods

        public void Add(IDrawable _Drawable)
        {
            m_Drawables.Add(_Drawable);
            IsClean = false;
        }

        public void Clear()
        {
            m_Drawables.Clear();
            IsClean = true;
        }

        public void Draw()
        {
            m_Drawables.ForEach(_Line => _Line.Draw());
        }

        #endregion
    }
}
#endif
