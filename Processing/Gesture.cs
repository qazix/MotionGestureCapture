using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageProcessing;

namespace MotionGestureProcessing
{
    public class Gesture : Process
    {
        enum Gestures { NoGesture, RightClick, LeftClick, ClickAndHold, DoubleClick };

        //public delegate void gestureCaptured(Gestures g, Image i);
        private Processing.ImageReadyHandler m_GesturesImageHandler;

        public Gesture()
        { }

        public void initialize()
        {
            setupListener();
        }

        protected override void setupListener()
        {
            m_GesturesImageHandler = (obj) =>
            {
                //Thread t = new Thread(new ParameterizedThreadStart(doWork));
                //t.Start(obj);
                doWork(obj);
            };

            Processing.getInstance().GesturesImageFilled += m_GesturesImageHandler;
        }

        /// <summary>
        /// This get the gesture of the hand
        /// </summary>
        /// <param name="p_imgData"></param>
        protected override async void doWork(Object p_imgData)
        {
            if (((ImageData)p_imgData).ConvexHull != null)
            {
                Gestures gesture = Gestures.NoGesture;

                List<Point> convexHull = ((ImageData)p_imgData).ConvexHull;
                List<Point> contour = ((ImageData)p_imgData).Contour;
                double threshold = ((ImageData)p_imgData).Filter.Height * .25;

                List<ConvexDefect> convexDefects;
                ImageProcess.getConvexDefects(ref contour, ref convexHull, out convexDefects, threshold);

                ((ImageData)p_imgData).ConvexDefects = convexDefects;
            }

            //writeGesture(gesture);
            Processing.getInstance().ToDrawingImage = (ImageData)p_imgData;
        }        

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_gesture"></param>
        private void writeGesture(Gestures p_gesture)
        {
            throw new NotImplementedException();
        }
    }
}
