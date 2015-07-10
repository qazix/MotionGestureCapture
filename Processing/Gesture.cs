using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionGestureProcessing
{
    public class Gesture : Process
    {
        enum Gestures { NoGesture, RightClick, LeftClick, ClickAndHold, DoubleClick };

        private int[] m_histogram;
        private int m_blockSize;

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
        /// This starts the process of determining which gesture is being shown by the hand
        /// </summary>
        /// <param name="p_imgData"></param>
        protected override async void doWork(Object p_imgData)
        {
            Gestures gesture = Gestures.NoGesture;

            if (((imageData)p_imgData).InitialFrame)
            {
                m_histogram = new int[((imageData)p_imgData).Image.Height];
                populateHistogram(((imageData)p_imgData).Datapoints);
                getBaseline();
                adjustHistogram();
                viewHistogram();
            }
            else
            {
                populateHistogram(((imageData)p_imgData).Datapoints);
                adjustHistogram();
                viewHistogram();
            }

            //writeGesture(gesture);
            Processing.getInstance().ToReturnImage = (imageData)p_imgData;
        }

        /// <summary>
        /// Puts the y projection into a histogram
        /// </summary>
        /// <param name="p_dataPoints"></param>
        private void populateHistogram(List<Point> p_dataPoints)
        {
            int length = m_histogram.Length;
            int adjust = length/ 2;
            int[] temp = new int[length];

            foreach (Point point in p_dataPoints)
            {
                if (point.Y > -adjust && point.Y < adjust)
                    ++temp[point.Y + adjust];
            }

            for (int i = 2; i < length - 2; ++i)
            {
                for (int j = i - 2; j < i + 2; ++j)
                    m_histogram[i] += temp[j];
                
                m_histogram[i] /= 4;
            }
        }

        /// <summary>
        /// This assigns values of the thresholds
        /// </summary>
        /// <param name="p_dataPoints"></param>
        private void getBaseline()
        {
            int max = 0;

            foreach (int i in m_histogram)
            {
                if (i > max)
                {
                    max = i;
                }
            }

            //because the max represents the middle finger, this represents full length finger
            max = (int)(max * .5);

            m_blockSize = max / 4;
        }

        /// <summary>
        /// Simple division of each point to represent block sizes
        /// </summary>
        private void adjustHistogram()
        {
            int len = m_histogram.Length;

            for (int i = 0; i < len; ++i)
                m_histogram[i] /= m_blockSize;            
        }

        /// <summary>
        /// A simple printing of the histogram
        /// </summary>
        private void viewHistogram()
        {
            Bitmap bm = new Bitmap(m_blockSize * 8, m_histogram.Length, PixelFormat.Format24bppRgb);
            byte[] buffer;
            int offset, block;
            BitmapData data = lockBitmap(out buffer, bm);

            int depth = data.Stride / data.Width;

            for (int i = 0; i < m_histogram.Length; ++i)
            {
                block = m_histogram[i] * m_blockSize;
                for (int j = 0; j < block; ++j)
                {
                    offset = ((i * data.Width) + j) * depth;
                    buffer[offset] = buffer[offset + 2] = 0;
                    buffer[offset + 1] = 255;
                }
            }

            unlockBitmap(ref buffer, ref data, bm);
            bm.Save("histogram.bmp");
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
