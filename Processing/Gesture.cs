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
                Point center = ((ImageData)p_imgData).Center;
                List<ConvexDefect> convexDefects = ((ImageData)p_imgData).ConvexDefects;
                List<Point> fingerTips = ((ImageData)p_imgData).FingerTips;

              





                //orientClockwise(ref convexDefects, ref center);

                ((ImageData)p_imgData).ConvexDefects = convexDefects;
            }

            //writeGesture(gesture);
            Processing.getInstance().ToDrawingImage = (ImageData)p_imgData;
        }

        private void orientClockwise(ref List<ConvexDefect> p_defects, ref Point p_center)
        {
            List<ConvexDefect> tempDefects = new List<ConvexDefect>();
            ConvexDefect start, end, current, minDefect;
            double sideA2, sideB2, sideC2, angleA, max, dist, min;
            start = end = minDefect = null;
            max = 0;
            min = Int32.MaxValue;

            //FInd the defects that represent the widest degree of variance.  This represent the pinky and thumb
            foreach (ConvexDefect cdStart in p_defects)
            {
                foreach(ConvexDefect cdEnd in p_defects)
                {
                    if (!cdStart.Equals(cdEnd))
                    {
                        //This is explained in ImageProcess.calculateDefest
                        sideA2 = (cdEnd.EndPoint.X - cdStart.StartPoint.X) * (cdEnd.EndPoint.X - cdStart.StartPoint.X) +
                                 (cdEnd.EndPoint.Y - cdStart.StartPoint.Y) * (cdEnd.EndPoint.Y - cdStart.StartPoint.Y);
                        sideB2 = (p_center.X - cdEnd.EndPoint.X) * (p_center.X - cdEnd.EndPoint.X) +
                                 (p_center.Y - cdEnd.EndPoint.Y) * (p_center.Y - cdEnd.EndPoint.Y);
                        sideC2 = (cdStart.StartPoint.X - p_center.X) * (cdStart.StartPoint.X - p_center.X) +
                                 (cdStart.StartPoint.Y - p_center.Y) * (cdStart.StartPoint.Y - p_center.Y);

                        angleA = Math.Acos((sideB2 + sideC2 - sideA2) / (2 * Math.Sqrt(sideB2) * Math.Sqrt(sideC2)));

                        if (angleA > max)
                        {
                            max = angleA;
                            start = cdStart;
                            end = cdEnd;
                        }
                    }
                }
            }

            current = start;

            //Connect the closest defects end to start
            while (!current.Equals(end) && tempDefects.Count < p_defects.Count)
            {
                tempDefects.Add(current);
                foreach (ConvexDefect cd in p_defects)
                {
                    if (!cd.Equals(current))
                    {
                        dist = (cd.StartPoint.X - current.EndPoint.X) * (cd.StartPoint.X - current.EndPoint.X) +
                               (cd.StartPoint.Y - current.EndPoint.Y) * (cd.StartPoint.Y - current.EndPoint.Y);

                        if (dist < min)
                        {
                            minDefect = cd;
                        }
                    }
                }

                current = minDefect;
            }

            tempDefects.Add(current);
            p_defects = tempDefects;
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
