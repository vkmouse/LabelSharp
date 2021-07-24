using System.Drawing;

namespace ViewerLib
{
    public enum OperateType
    {
        VIEWER_ZOOM_IN,
        VIEWER_ZOOM_OUT,
        VIEWER_ZOOM_FIT,
        VIEWER_RESIZE,
        VIEWER_PANNING_BEGIN,
        VIEWER_PANNING_MOVE,
        VIEWER_PANNING_END,
        DETECTION_LABEL_BEGIN,
        DETECTION_LABEL_MOVE,
        DETECTION_LABEL_END,
        DETECTION_SELECT_ROI,
        DETECTION_DELETE_ROI,
        DETECTION_MOVE_ROI_BEGIN,
        DETECTION_MOVE_ROI_MOVE,
        DETECTION_MOVE_ROI_END,
        DETECTION_RENAME_ROI
    }

    public interface IKernel
    {
        Image Image { get; set; }
        Image Operate(OperateType type, params object[] values);
        void Clear();
    }
}
