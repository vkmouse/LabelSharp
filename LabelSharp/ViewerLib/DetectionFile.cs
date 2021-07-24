using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace ViewerLib
{
    public enum AnnotationFormat { ANNOTATION_FORMAT_PASCAL_VOC, ANNOTATION_FORMAT_TESSERACT }
    public class DetectionFile
    {
        public static void Save(DetectionFileInfo info, AnnotationFormat format)
        {
            switch (format)
            {
                case AnnotationFormat.ANNOTATION_FORMAT_PASCAL_VOC: // Save as .xml
                    SavePascalVOC(info);
                    break;
                case AnnotationFormat.ANNOTATION_FORMAT_TESSERACT: // Save as .box
                    SaveTesseract(info);
                    break;
            }
        }
        private static void SavePascalVOC(DetectionFileInfo info)
        {
            XmlDocument doc = new XmlDocument();

            Func<XmlNode, string, XmlNode> AppendChildName = (parent, childName) =>
            {
                XmlNode child = doc.CreateElement(childName);
                parent.AppendChild(child);
                return child;
            };

            Func<XmlNode, string, string, XmlNode> AppendChildNameText = (parent, childName, childText) =>
            {
                XmlNode child = AppendChildName(parent, childName);
                child.InnerText = childText;
                return child;
            };

            var top = (XmlElement)AppendChildName(doc, "annotation");
            {
                top.SetAttribute("verified", "yes");

                AppendChildNameText(top, "folder", Path.GetFileName(Path.GetDirectoryName(info.imagePath)));
                AppendChildNameText(top, "filename", Path.GetFileName(info.imagePath));
                AppendChildNameText(top, "path", info.imagePath);
                var source = AppendChildName(top, "source");
                {
                    AppendChildNameText(source, "database", "Unknown");
                }
                top.AppendChild(source);
                var size = AppendChildName(top, "size");
                {
                    AppendChildNameText(size, "width", info.imageWidth.ToString());
                    AppendChildNameText(size, "height", info.imageHeight.ToString());
                    AppendChildNameText(size, "depth", info.imageDepth.ToString());
                }
                AppendChildNameText(top, "segmented", "0");

                foreach (var bbox in info.bboxes)
                {
                    var objectItem = AppendChildName(top, "object");
                    {
                        AppendChildNameText(objectItem, "name", bbox.ClassName);
                        AppendChildNameText(objectItem, "pose", "Unspecified");
                        AppendChildNameText(objectItem, "truncated", "0");
                        AppendChildNameText(objectItem, "difficult", "0");
                        var bndbox = AppendChildName(objectItem, "bndbox");
                        {
                            AppendChildNameText(bndbox, "xmin", bbox.XMin.ToString());
                            AppendChildNameText(bndbox, "ymin", bbox.YMin.ToString());
                            AppendChildNameText(bndbox, "xmax", bbox.XMax.ToString());
                            AppendChildNameText(bndbox, "ymax", bbox.YMax.ToString());
                        }
                    }
                }
            }

            string savePath = Path.ChangeExtension(Path.Combine(info.saveDir, Path.GetFileName(info.imagePath)), ".xml");
            doc.Save(savePath);
        }

        private static void SaveTesseract(DetectionFileInfo info)
        {
            List<string> lines = new List<string>();
            foreach (var bbox in info.bboxes)
            {
                string line = $"{bbox.ClassName} {bbox.XMin} {info.imageHeight - bbox.YMax} {bbox.XMax} {info.imageHeight - bbox.YMin} 0";
                lines.Add(line);
            }

            string savePath = Path.ChangeExtension(Path.Combine(info.saveDir, Path.GetFileName(info.imagePath)), ".box");
            File.WriteAllLines(savePath, lines);
        }
    }
}
