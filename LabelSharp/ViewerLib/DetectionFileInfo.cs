using System.Collections.Generic;

namespace ViewerLib
{
    public class DetectionFileInfo
    {
        public string imagePath;
        public string saveDir;
        public int imageWidth;
        public int imageHeight;
        public int imageDepth;
        public List<DetectionUnit> bboxes;
    }
}
