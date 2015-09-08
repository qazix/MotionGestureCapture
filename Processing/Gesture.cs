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
        private int m_thumbPos;

        //public delegate void gestureCaptured(Gestures g, Image i);
        private Processing.ImageReadyHandler m_GesturesImageHandler;

        public Gesture()
        { m_thumbPos = 0; }

        public void initialize()
        {
            setupListener();
        }

        protected override void setupListener()
        {
            m_GesturesImageHandler = (obj) =>
            {
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
            if (((ImageData)p_imgData).ConvexDefects != null)
            {
                MotionGestureProcessing.ImageData.Gestures gesture = ImageData.Gestures.INITIALIZING;

                List<ConvexDefect> convexDefects = ((ImageData)p_imgData).ConvexDefects;
                List<Point> fingerTips = ((ImageData)p_imgData).FingerTips;

                ((ImageData)p_imgData).Gesture = deriveGesture(ref convexDefects, ref fingerTips);

            }
            else
                m_thumbPos = 0;


            //writeGesture(gesture);
            Processing.getInstance().ToDrawingImage = (ImageData)p_imgData;
        }

        /// <summary>
        /// Runs through some defaults for determining gesture
        /// </summary>
        /// <param name="p_convexDefects"></param>
        /// <param name="p_fingerTips"></param>
        /// <returns>Gesture represented</returns>
        private MotionGestureProcessing.ImageData.Gestures deriveGesture(ref List<ConvexDefect> p_convexDefects, ref List<Point> p_fingerTips)
        {
            int max;
            ConvexDefect maxDefect;
            MotionGestureProcessing.ImageData.Gestures gesture = ImageData.Gestures.INITIALIZING;

            switch(p_convexDefects.Count)
            {
                case 0:
                    gesture = ImageData.Gestures.CLICKANDHOLD;
                    break;
                case 1:
                    gesture = ImageData.Gestures.CLICKANDHOLD;
                    break;
                case 2:
                    gesture = ImageData.Gestures.DOUBLECLICK;
                    break;
                case 3:
                    if (m_thumbPos != 0)
                        gesture = parse3Defect(ref p_convexDefects, ref p_fingerTips);
                    break;
                case 4: 
                    if (p_fingerTips.Count == 5 && m_thumbPos == 0)
                    {
                        max = (int)p_convexDefects.Max(x => x.Area);
                        maxDefect = p_convexDefects.Where(cd => (int)cd.Area == max).ToList().First();
                        if (maxDefect.Equals(p_convexDefects[0]))
                            m_thumbPos = -1;
                        else
                            m_thumbPos = 1;
                    }
                    gesture = ImageData.Gestures.MOVE;
                    break;
                default:
                    throw new Exception("more than 4 defects");
            }

            if (m_thumbPos == 0)
                gesture = ImageData.Gestures.INITIALIZING;

            return gesture;
        }

        /// <summary>
        /// parses out the more complicated portion entering this means a few things we have a thumbe position
        /// </summary>
        /// <param name="p_convexDefects"></param>
        /// <param name="p_fingerTips"></param>
        /// <returns></returns>
        private MotionGestureProcessing.ImageData.Gestures parse3Defect(ref List<ConvexDefect> p_convexDefects, ref List<Point> p_fingerTips)
        {
            int containingIndex = -2; //-2 becuase i do an increment at the end and i still want negative to signify error
            MotionGestureProcessing.ImageData.Gestures gesture;

            //find out which defect contains another point
            for (int i = 0; i < p_convexDefects.Count && containingIndex < 0; ++i)
            {
                foreach (Point p in p_fingerTips)
                {
                    if (!p.Equals(p_convexDefects[i].StartPoint) && !p.Equals(p_convexDefects[i].EndPoint))
                        if (p_convexDefects[i].contains(p))
                        {
                            containingIndex = i;
                            break;
                        }
                }
            }

            if (m_thumbPos == -1)
                containingIndex = (containingIndex + 1) % 3;

            switch (containingIndex)
            {
                case 0:
                    gesture = ImageData.Gestures.DOUBLECLICK;
                    break;
                case 1:
                    gesture = ImageData.Gestures.LEFTCLICK;
                    break;
                case 2:
                    gesture = ImageData.Gestures.RIGHTCLICK;
                    break;
                default:
                    gesture = ImageData.Gestures.MOVE;
                    break;
            }

            return gesture;
        }
    }
}
