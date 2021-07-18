using System.Drawing;

namespace ViewerLib
{
    public class DetectionUnit
    {
        private Rectangle _rect;
        private string _className;

        public int X
        {
            get => _rect.X;
            set => _rect.X = value;
        }

        public int Y
        {
            get => _rect.Y;
            set => _rect.Y = value;
        }

        public int Width
        {
            get => _rect.Width;
            set => _rect.Width = value;
        }

        public int Height
        {
            get => _rect.Height;
            set => _rect.Height = value;
        }

        public Rectangle Rect
        {
            get => _rect;
            set => _rect = value;
        }

        public string ClassName
        {
            get => _className;
            set => _className = value;
        }

        public int XMin
        {
            get => _rect.Left;
        }

        public int XMax
        {
            get => _rect.Right;
        }

        public int YMin
        {
            get => _rect.Top;
        }

        public int YMax
        {
            get => _rect.Bottom;
        }

        public DetectionUnit(Rectangle rect, string className)
        {
            _rect = rect;
            _className = className;
        }

        public DetectionUnit(int x, int y, int width, int height, string className)
        {
            _rect = new Rectangle(x, y, width, height);
            _className = className;
        }
    }
}
