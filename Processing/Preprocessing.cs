using ImageProcessing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionGestureProcessing
{
    class Preprocessing : Process
    {
        private Processing.ImageReadyHandler m_preprocImageHandler;

        public Preprocessing()
        { }

        public void initialize()
        {
            setupListener();
        }

        /// <summary>
        /// Listener for preprocessing image ready
        /// </summary>
        protected override void setupListener()
        {
            m_preprocImageHandler = (obj) =>
            {
                doWork(obj);
            };

            Processing.getInstance().PreprocessImageFilled += m_preprocImageHandler;
        }

        /// <summary>
        /// There are some rather labor intensive portions of code that I need to have a section of pipeline to handle
        /// These include:
        ///     Cropping the wrist from the hand
        ///     Finding the contour of the hand
        ///     wrapping a shell around the contour
        /// </summary>
        /// <param name="p_imgData"></param>
        protected override async void doWork(object p_imgData)
        {

            byte[] buffer;
            BitmapData data = BitmapManip.lockBitmap(out buffer, ((ImageData)p_imgData).Image);
            ((ImageData)p_imgData).DataPoints = ImageProcess.getDataPoints(ref data, ref buffer);

            if (((ImageData)p_imgData).DataPoints.Count > 0)
            {
                ((ImageData)p_imgData).Filter = cropDataSet(ref data, ref buffer);
                ((ImageData)p_imgData).Contour = ImageProcess.getContour(ref data, ref buffer);
                ((ImageData)p_imgData).ConvexHull = ImageProcess.getConvexHull(((ImageData)p_imgData).Contour);
            }

            BitmapManip.unlockBitmap(ref buffer, ref data, ((ImageData)p_imgData).Image);

            Processing.getInstance().ToPCAImage = (ImageData)p_imgData;
        }

        /// <summary>
        /// first determine the wrist end and then crop around just the hand
        /// </summary>
        /// <seealso cref="http://arxiv.org/ftp/arxiv/papers/1212/1212.0134.pdf"/>
        /// <param name="p_data"></param>
        /// <param name="p_buffer"></param>
        private Rectangle cropDataSet(ref BitmapData p_data, ref byte[] p_buffer)
        {

            int[] xHistogram;
            int[] yHistogram;
            int left, right, top, bottom;

            int[] extremeHistogram = { 0, 0, 0, 0 };

            project2Histogram(out xHistogram, out yHistogram, ref p_data, ref p_buffer);

            #region set boundaries
            left = setBoundary(0, ref xHistogram);
            right = setBoundary(xHistogram.Length - 1, ref xHistogram);
            top = setBoundary(0, ref yHistogram);
            bottom = setBoundary(yHistogram.Length - 1, ref yHistogram);

            extremeHistogram[0] = xHistogram[left];
            extremeHistogram[1] = xHistogram[right];
            extremeHistogram[2] = yHistogram[top];
            extremeHistogram[3] = yHistogram[bottom];
            #endregion

            int max = 0;
            int dir = -1;
            //find max histogram value this represent the wrist
            for (int i = 0; i < extremeHistogram.Length; ++i)
            {
                if (extremeHistogram[i] > max)
                {
                    max = extremeHistogram[i];
                    dir = i;
                }
            }

            int wristIndex;
            //based on the direction do some stuff
            switch (dir)
            {
                case 0: //left
                    wristIndex = findWrist(left, ref xHistogram);
                    if (wristIndex == -1)
                        return new Rectangle(0, 0, p_data.Width, p_data.Height);
                    removeWrist(0, wristIndex, 0, p_data.Height, ref p_data, ref p_buffer);
                    break;
                case 1: //right
                    wristIndex = findWrist(right, ref xHistogram);
                    if (wristIndex == -1)
                        return new Rectangle(0, 0, p_data.Width, p_data.Height);
                    removeWrist(wristIndex, p_data.Width, 0, p_data.Height, ref p_data, ref p_buffer);
                    break;
                case 2: //top
                    wristIndex = findWrist(top, ref yHistogram);
                    if (wristIndex == -1)
                        return new Rectangle(0, 0, p_data.Width, p_data.Height);
                    removeWrist(0, p_data.Width, 0, wristIndex, ref p_data, ref p_buffer);
                    break;
                case 3: //bottom
                    wristIndex = findWrist(bottom, ref yHistogram);
                    if (wristIndex == -1)
                        return new Rectangle(0, 0, p_data.Width, p_data.Height);
                    removeWrist(0, p_data.Width, wristIndex, p_data.Height, ref p_data, ref p_buffer);
                    break;
                default:
                    throw new Exception("Direction of wrist is invalid");
            }

            //Get the updated data
            project2Histogram(out xHistogram, out yHistogram, ref p_data, ref p_buffer);
            left = setBoundary(0, ref xHistogram);
            right = setBoundary(xHistogram.Length - 1, ref xHistogram);
            top = setBoundary(0, ref yHistogram);
            bottom = setBoundary(yHistogram.Length - 1, ref yHistogram);

            return new Rectangle(left - 10, top - 10, right - left + 20, bottom - top + 20);
        }

        /// <summary>
        /// Takes a bitmap and projects it onto x and y histograms
        /// </summary>
        /// <param name="p_xHistogram"></param>
        /// <param name="p_yHistogram"></param>
        /// <param name="p_data"></param>
        /// <param name="p_buffer"></param>
        private void project2Histogram(out int[] p_xHistogram, out int[] p_yHistogram, ref BitmapData p_data, ref byte[] p_buffer)
        {
            p_xHistogram = new int[p_data.Width];
            p_yHistogram = new int[p_data.Height];

            int offset = 0;

            //populate x, y histograms
            for (int y = 0; y < p_data.Height; ++y)
                for (int x = 0; x < p_data.Width; ++x, offset += 4)
                {
                    if (p_buffer[offset] != 0)
                    {
                        ++p_xHistogram[x];
                        ++p_yHistogram[y];
                    }
                }
        }

        /// <summary>
        /// iterates through a histogram to find a the first column with data
        /// </summary>
        /// <param name="p_index"></param>
        /// <param name="p_histogram"></param>
        /// <returns></returns>
        private int setBoundary(int p_index, ref int[] p_histogram)
        {
            int index;
            int end = p_histogram.Length - p_index - 1;
            int inc = (p_index == 0 ? 1 : -1);
            for (index = p_index; index != end && p_histogram[index] == 0; index += inc)
                ;
            return index;
        }

        /// <summary>
        /// Based on the rule that the thinest part from the widest point will be where the wrist meets the hand
        /// This method finds the max and then the min between the max and the start to get the wrist index.
        /// </summary>
        /// <param name="p_index">left to right, up to down or their counterparts</param>
        /// <param name="p_histogram">the histogram to traverse</param>
        /// <returns></returns>
        private int findWrist(int p_index, ref int[] p_histogram)
        {
            int end = p_index > 0 ? 0 : p_histogram.Length;
            int inc = p_index > end ? -1 : 1;
            int maxIndex, minIndex, max, min;
            maxIndex = minIndex = max = -1;

            //find max
            for (int i = p_index; i != end && p_histogram[i] != 0; i += inc)
            {
                if (p_histogram[i] > max)
                {
                    max = p_histogram[i];
                    maxIndex = i;
                }
            }

            min = max + 1;
            //find min before max
            // the histogram should have this kind of shape
            //__|/\
            //  |  \
            //the vertical line represents the end of the wrist
            for (int i = p_index; i != maxIndex; i += inc)
            {
                if (p_histogram[i] <= min)
                {
                    min = p_histogram[i];
                    minIndex = i;
                }
            }

            return minIndex;
        }

        /// <summary>
        /// Cut the wrist from the buffer, not really cropping but essentially does the same thing
        /// </summary>
        /// <param name="p_xStart"></param>
        /// <param name="p_xEnd"></param>
        /// <param name="p_yStart"></param>
        /// <param name="p_yEnd"></param>
        /// <param name="p_data"></param>
        /// <param name="p_buffer"></param>
        private void removeWrist(int p_xStart, int p_xEnd, int p_yStart, int p_yEnd, ref BitmapData p_data, ref byte[] p_buffer)
        {
            int offset = ImageProcess.getOffset(p_xStart, p_yStart, p_data.Width, 4);
            for (int y = p_yStart; y < p_yEnd; ++y)
            {
                offset = ImageProcess.getOffset(p_xStart, y, p_data.Width, 4);
                for (int x = p_xStart; x < p_xEnd; ++x, offset += 4)
                {
                    if (p_buffer[offset] != 0)
                        p_buffer[offset] = p_buffer[offset + 1] = p_buffer[offset + 2] = 0;
                }
            }
        }
    }
}
