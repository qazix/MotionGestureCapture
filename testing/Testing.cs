using MotionGestureProcessing;
using System.Drawing;

namespace testing
{
    class Testing
    {
        static void Main(string[] args)
        {
            Bitmap load = new Bitmap(@"D:\Documents\Temp\Demo\edgeDetection\Canny Edge Detection C#\Shrikrishna.bmp");
            ImageProcessing.findEdges(load);
        }
    }
}
