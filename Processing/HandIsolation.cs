using ImageProcessing;
using DebugFunctions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MotionGestureProcessing
{
    class HandIsolation : Process
    {
        public delegate void ProcessReadyHandler();
        public event ProcessReadyHandler ProcessReady;

        private Processing.ImageReadyHandler m_isoImageHandler;
        private static bool m_isInitialized;
        private static byte[] m_backGround;
        private int m_threshold;

        //points further from the current point are most significant because there are so many ppi
        private static int[,] INVGAUSFILTER = {{46, 23, 13, 8, 13, 23, 46},
                                               {23, 10,  5, 3,  5, 10, 23},
                                               {13,  5,  2, 1,  2,  5, 13},
                                               { 8,  3,  1, 0,  1,  3,  8},
                                               {13,  5,  2, 1,  2,  5, 13},
                                               {23, 10,  5, 3,  5, 10, 23},
                                               {46, 23, 13, 8, 13, 23, 46}};
        private static double GSUM = 608.0;

        /// <summary>
        /// Empty constructor
        /// </summary>
        public HandIsolation()
        { }

        /// <summary>
        /// First populates the bit array for values then sets up the event listener
        /// </summary>
        /// <param name="p_toInit">The initialization frame</param>
        public void initialize(ImageData p_toInit)
        {
            Image toInit = p_toInit.Image;

            convert2PixelFormat(ref toInit);
            //convert background to greyscale and get threshold from image
            BitmapData data = BitmapManip.lockBitmap(out m_backGround, toInit);
            ImageProcess.convert2GreyScale(ref m_backGround);
            m_threshold = (int)ImageProcess.otsuThreshold(m_backGround) / 2;
            BitmapManip.unlockBitmap(ref m_backGround, ref data, toInit);

            m_isInitialized = true;
            setupListener();
        }

        /// <summary>
        /// Establishes a listening connection 
        /// </summary>
        protected override void setupListener()
        {
            m_isoImageHandler = (obj) =>
            {
                doWork(obj);
            };

            Processing.getInstance().IsolationImageFilled += m_isoImageHandler;
        }

        /// <summary>
        /// this method transforms the image into a filtered image
        /// UPDATE: this now performs almost insantly instead of the 2 seconds it took before
        /// </summary>
        /// <param name="p_imageData"></param>
        protected override async void doWork(Object p_imageData)
        {
            if (m_isInitialized)
            {
                Image procImage = ((ImageData)p_imageData).Image;
                //Setting up a buffer to be used for concurrent read/write
                byte[] buffer;
                convert2PixelFormat(ref procImage);
                BitmapData data = BitmapManip.lockBitmap(out buffer, procImage);
                ImageProcess.convert2GreyScale(ref buffer);

                #region Call Parallel.Invoke for each coordinate
                Parallel.Invoke(
                    () =>
                    {
                        //upper left
                        dividedDoWorkARGB(buffer, 0, 0, data.Width / 2, data.Height / 2, data.Width);
                    },
                    () =>
                    {
                        //upper right
                        dividedDoWorkARGB(buffer, data.Width / 2, 0, data.Width, data.Height / 2, data.Width);
                    },
                    () =>
                    {
                        //lower left
                        dividedDoWorkARGB(buffer, 0, data.Height / 2, data.Width / 2, data.Height, data.Width);
                    },
                    () =>
                    {
                        //lower right
                        dividedDoWorkARGB(buffer, data.Width / 2, data.Height / 2, data.Width, data.Height, data.Width);
                    });
                #endregion

                ((ImageData)p_imageData).DataPoints = ImageProcess.getDataPoints(ref data, ref buffer);

                //Provide finer area filtering and signal enhancing
                filterNoise(((ImageData)p_imageData).DataPoints, ref data, ref buffer);
                strengthenSignal(ref data, ref buffer);

                ((ImageData)p_imageData).DataPoints = findHand(ref data, ref buffer);
                ImageProcess.updateBuffer(((ImageData)p_imageData).DataPoints, ref data, ref buffer);

                BitmapManip.unlockBitmap(ref buffer, ref data, procImage);

                ((ImageData)p_imageData).Image = procImage;
                Processing.getInstance().ToPreProcessing = (ImageData)p_imageData;

                //If someone is listener raise an event
                if (ProcessReady != null)
                    ProcessReady();
            }
        }

        /// <summary>
        /// Uses inner to outer search to find a strong feature then
        /// scans from outside to in of a more reduced area to find a more accurate center
        /// </summary>
        /// <param name="p_imageData">Image data</param>
        /// <param name="p_data">bitmap data</param>
        /// <param name="p_buffer">used for testing</param>
        private List<Point> findHand(ref BitmapData p_data, ref byte[] p_buffer)
        {

            Dictionary<int, List<Point>> blobs = ImageProcess.findBlobs(ref p_data, ref p_buffer);

            if (blobs.Count > 0)
                return blobs.Aggregate((l, r) => l.Value.Count > r.Value.Count ? l : r).Value;
            else
                return new List<Point>();
        }

        /// <summary>
        /// goes through each datapoint and checks it's neighbors for strength if the strength is too low it is erased
        /// </summary>
        /// <param name="p_dataPoints">dataPoints within the filter</param>
        /// <param name="p_data"></param>
        /// <param name="p_buffer"></param>
        private void filterNoise(List<Point> p_dataPoints, ref BitmapData p_data, ref byte[] p_buffer)
        {
            List<Point> strongPoints = new List<Point>();
            List<Point> weakPoints = new List<Point>();
            double strength;

            foreach (Point point in p_dataPoints)
            {
                strength = getStrength(point, ref p_data, ref p_buffer);
                if (strength > .5)
                    strongPoints.Add(point);
                else
                    weakPoints.Add(point);
            }

            removePoints(ref weakPoints, ref p_data, ref p_buffer);
            p_dataPoints = strongPoints;
        }

        /// <summary>
        /// Widens the points that survived the filtering
        /// </summary>
        /// <param name="p_data"></param>
        /// <param name="p_buffer"></param>
        private void strengthenSignal(ref BitmapData p_data, ref byte[] p_buffer)
        {
            //setup 
            int strengthenValue = 2;
            bool ranLastIteration = false;
            byte[] newBuffer = new byte[p_buffer.Length];

            //copy our original buffer to include 
            Buffer.BlockCopy(p_buffer, 0, newBuffer, 0, p_buffer.Length);

            int depth = p_data.Stride / p_data.Width;
            int trueOffset, curOffset;

            //iterate accross image
            for (int trueY = 0; trueY < p_data.Height; ++trueY)
                for (int trueX = 0; trueX < p_data.Width; ++trueX)
                {
                    trueOffset = ImageProcess.getOffset(trueX, trueY, p_data.Width, depth);

                    //iterate accross window
                    if (p_buffer[trueOffset] != 0 || p_buffer[trueOffset + 1] != 0 || p_buffer[trueOffset + 2] != 0)
                    {
                        for (int y = (trueY - strengthenValue > 0 ? -strengthenValue : -trueY); y <= strengthenValue && trueY + y < p_data.Height; ++y)
                        {
                            //if the last pixel ran then i only need to update the far right edge
                            if (!ranLastIteration)
                                for (int x = (trueX - strengthenValue > 0 ? -strengthenValue : -trueX); x <= strengthenValue && trueX + x < p_data.Width; ++x)
                                {
                                    curOffset = ImageProcess.getOffset(trueX + x, trueY + y, p_data.Width, depth);
                                    newBuffer[curOffset] = newBuffer[curOffset + 1] = newBuffer[curOffset + 2] = 255;
                                    if (depth == 4)
                                        newBuffer[curOffset + 3] = 255;
                                }
                            else if (trueX + strengthenValue < p_data.Width)
                            {
                                curOffset = ImageProcess.getOffset(trueX + strengthenValue, trueY + y, p_data.Width, depth);
                                newBuffer[curOffset] = newBuffer[curOffset + 1] = newBuffer[curOffset + 2] = 255;
                                if (depth == 4)
                                    newBuffer[curOffset + 3] = 255;
                            }
                        }

                        ranLastIteration = true;
                    }
                    else
                        ranLastIteration = false;
                }

            //copy the new buffer into the old
            Buffer.BlockCopy(newBuffer, 0, p_buffer, 0, p_buffer.Length);
        }

        /// <summary>
        /// Find bounds of a threshold
        /// </summary>
        /// <param name="p_start">starting index</param>
        /// <param name="p_searchSpace">histogram</param>
        /// <param name="p_inc">diretion of traversal</param>
        /// <param name="p_thresh">threshold</param>
        private void findBound(ref int p_start, int[] p_searchSpace, int p_inc, int p_thresh, bool greaterThan)
        {
            if (greaterThan)
                while ((p_start > 1 && p_start < p_searchSpace.Length - 1) && p_searchSpace[p_start + p_inc] >= p_thresh)
                    p_start += p_inc;
            else
                while ((p_start > 0 && p_start < p_searchSpace.Length - 1) && p_searchSpace[p_start + p_inc] < p_thresh)
                    p_start += p_inc;

            if ((p_inc > 0 && p_start > p_searchSpace.Length - 2) ||
                (p_inc < 0 && p_start < 3))
            {
                p_start = p_searchSpace.Length / 2;
            }
        }

        /// <summary>
        /// Smooth a histogram
        /// </summary>
        /// <param name="p_searchSpace">initial histogram</param>
        /// <param name="p_smoothSpace">smoothed histogram</param>
        private void smoothing(ref int[] p_searchSpace, ref int[] p_smoothSpace)
        {
            int sum;
            int smoothingSize = 11; // must be odd
            int limit = smoothingSize / 2;
            for (int i = limit; i < p_searchSpace.Length - limit; ++i)
            {
                sum = 0;
                for (int j = -limit; j <= limit; ++j)
                {
                    sum += p_searchSpace[i + j];
                }
                p_smoothSpace[i] = sum / smoothingSize;
            }
        }

        /// <summary>
        /// remove points from p_buffer that were too weak
        /// </summary>
        /// <param name="p_dataPoints"></param>
        /// <param name="p_data"></param>
        /// <param name="p_buffer"></param>
        private void removePoints(ref List<Point> p_dataPoints, ref BitmapData p_data, ref byte[] p_buffer)
        {
            int depth = p_data.Stride / p_data.Width;
            int offset;

            foreach (Point point in p_dataPoints)
            {
                offset = ((point.Y * p_data.Width) + point.X) * depth;
                p_buffer[offset] = p_buffer[offset + 1] = p_buffer[offset + 2] = 0;
            }
        }

        /// <summary>
        /// evaluates neighboring points and returns the value of neighboring points
        /// </summary>
        /// <param name="p_point"></param>
        /// <param name="p_data"></param>
        /// <param name="p_buffer"></param>
        /// <returns></returns>
        private double getStrength(Point p_point, ref BitmapData p_data, ref byte[] p_buffer)
        {
            int depth = p_data.Stride / p_data.Width;
            int window = INVGAUSFILTER.GetLength(0) / 2;
            int yStart, yEnd, xStart, xEnd;
            yStart = (p_point.Y - window) * p_data.Stride;
            yEnd = (p_point.Y + window) * p_data.Stride;
            xStart = (p_point.X - window) * depth;
            xEnd = (p_point.X + window) * depth;

            if (p_point.X < window || p_point.X >= p_data.Width - window ||
                p_point.Y < window || p_point.Y >= p_data.Height - window)
                return 0.0;

            double sum = 0.0;

            int i, j;
            i = 0;
            for (int y = yStart; y <= yEnd; y += p_data.Stride, ++i)
            {
                j = 0;
                for (int x = xStart; x <= xEnd; x += depth, ++j)
                {
                    //If the point has any color in it add the filters value to the sum
                    if (p_buffer[y + x] != 0)// || p_buffer[y + x + 1] != 0 || p_buffer[y + x + 1] != 0)
                        sum += INVGAUSFILTER[i, j];
                }
            }

            return sum / GSUM;
        }

        /// <summary>
        /// Handles comparing pixels to the valid pixels for ARGB or 32bpp
        /// </summary>
        /// <param name="p_buffer">Byte array of image to process</param>
        /// <param name="startX">Start X position (0 = left)</param>
        /// <param name="startY">Start Y position (0 = top)</param>
        /// <param name="endX"></param>
        /// <param name="endY"></param>
        /// <param name="width">Width in pixels used to determine offset</param>
        private void dividedDoWorkARGB(byte[] p_buffer, int startX, int startY, int endX, int endY, int width)
        {
            int offset;
            int curPixelColor;

            for (int y = startY; y < endY; ++y)
                for (int x = startX; x < endX; ++x)
                {
                    offset = ImageProcess.getOffset(x, y, width, 4);

                    if ((m_backGround[offset] > p_buffer[offset] ? m_backGround[offset] - p_buffer[offset] :
                                                                   p_buffer[offset] - m_backGround[offset]) > m_threshold)
                        p_buffer[offset] = p_buffer[offset + 1] = p_buffer[offset + 2] = 255;
                    else
                        p_buffer[offset] = p_buffer[offset + 1] = p_buffer[offset + 2] = 0;
                }
        }
    }
}
