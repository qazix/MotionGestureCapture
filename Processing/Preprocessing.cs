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
        enum DIRECTION { INVALID = -1, LEFT, RIGHT, TOP, BOTTOM };
        DIRECTION m_direction;
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
                m_direction = DIRECTION.INVALID;

                ((ImageData)p_imgData).Filter = cropDataSet(ref data, ref buffer);
                ((ImageData)p_imgData).Contour = ImageProcess.getContour(ref data, ref buffer);
                ((ImageData)p_imgData).ConvexHull = ImageProcess.getConvexHull(((ImageData)p_imgData).Contour);
                
                //defects
                double defectThreshold;
                if (m_direction == DIRECTION.LEFT || m_direction == DIRECTION.RIGHT)
                    defectThreshold = ((ImageData)p_imgData).Filter.Width * .2;
                else
                    defectThreshold = ((ImageData)p_imgData).Filter.Height * .2;
                ((ImageData)p_imgData).ConvexDefects = ImageProcess.getConvexDefects(((ImageData)p_imgData).Contour, ((ImageData)p_imgData).ConvexHull,
                                                                                     defectThreshold);
                ((ImageData)p_imgData).ConvexDefects = organizeDefects(((ImageData)p_imgData).ConvexDefects);

                //fingers
                Dictionary<int, List<Point>> fingerTips = findFingers(ref data, ref buffer);
                ((ImageData)p_imgData).FingerTips = refineFingerTips(ref fingerTips, ((ImageData)p_imgData).ConvexDefects);
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
            DIRECTION dir = DIRECTION.INVALID;
            //find max histogram value this represent the wrist
            for (int i = 0; i < extremeHistogram.Length; ++i)
            {
                if (extremeHistogram[i] > max)
                {
                    max = extremeHistogram[i];
                    dir = (DIRECTION)i;
                }
            }

            m_direction = dir;
            int wristIndex;
            //based on the direction do some stuff
            switch (dir)
            {
                case DIRECTION.LEFT: //left
                    wristIndex = findWrist(left, ref xHistogram);
                    if (wristIndex == -1)
                        return new Rectangle(0, 0, p_data.Width, p_data.Height);
                    removeWrist(0, wristIndex, 0, p_data.Height, ref p_data, ref p_buffer);
                    break;
                case DIRECTION.RIGHT: //right
                    wristIndex = findWrist(right, ref xHistogram);
                    if (wristIndex == -1)
                        return new Rectangle(0, 0, p_data.Width, p_data.Height);
                    removeWrist(wristIndex, p_data.Width, 0, p_data.Height, ref p_data, ref p_buffer);
                    break;
                case DIRECTION.TOP: //top
                    wristIndex = findWrist(top, ref yHistogram);
                    if (wristIndex == -1)
                        return new Rectangle(0, 0, p_data.Width, p_data.Height);
                    removeWrist(0, p_data.Width, 0, wristIndex, ref p_data, ref p_buffer);
                    break;
                case DIRECTION.BOTTOM: //bottom
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
            int end = p_index > 1 ? 0 : p_histogram.Length;
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

        /// <summary>
        /// Organizes the defects to be clockwise and adjust start and end point to meet
        /// </summary>
        /// <param name="p_defects"></param>
        /// <returns></returns>
        private List<ConvexDefect> organizeDefects(List<ConvexDefect> p_defects)
        {
            List<ConvexDefect> orderedDefects = new List<ConvexDefect>();
            while (p_defects.Count > 4)
            {
                p_defects.Remove(p_defects.Where(x => (int)x.Area == (int)p_defects.Max(d => d.Area)).ToList().First());
            }

            if (p_defects.Count > 1)
            {
                int minIndex = -1;
                double dist, min;
                Point mergePoint = new Point();


                //Merge points of end and starts that are close together
                for (int i = 0; i < p_defects.Count; ++i)
                {
                    min = int.MaxValue;
                    for (int j = (i + 1) % p_defects.Count; j != i; j = (j + 1) % p_defects.Count)
                    {
                        dist = (p_defects[i].EndPoint.X - p_defects[j].StartPoint.X) * (p_defects[i].EndPoint.X - p_defects[j].StartPoint.X) +
                               (p_defects[i].EndPoint.Y - p_defects[j].StartPoint.Y) * (p_defects[i].EndPoint.Y - p_defects[j].StartPoint.Y);

                        if (dist < min)
                        {
                            min = dist;
                            minIndex = j;
                        }
                    }

                    dist = Math.Sqrt((p_defects[i].EndPoint.X - p_defects[minIndex].StartPoint.X) * (p_defects[i].EndPoint.X - p_defects[minIndex].StartPoint.X) +
                                     (p_defects[i].EndPoint.Y - p_defects[minIndex].StartPoint.Y) * (p_defects[i].EndPoint.Y - p_defects[minIndex].StartPoint.Y));

                    //If the distance between the two defects is less than distance within a defect then merge them
                    if (dist < p_defects[i].DistanceToEnd)
                    {
                        mergePoint.X = (p_defects[i].EndPoint.X + p_defects[minIndex].StartPoint.X) / 2;
                        mergePoint.Y = (p_defects[i].EndPoint.Y + p_defects[minIndex].StartPoint.Y) / 2;

                        p_defects[i].EndPoint = new Point(mergePoint.X, mergePoint.Y);
                        p_defects[minIndex].StartPoint = new Point(mergePoint.X, mergePoint.Y);
                    }
                }

                //find the startpoint that is hanging
                foreach (ConvexDefect cd in p_defects)
                {
                    if (!p_defects.Select(x => x.EndPoint).ToList().Contains(cd.StartPoint))
                    {
                        orderedDefects.Add(cd);
                        break;
                    }
                }

                if (orderedDefects.Count == 0)
                    orderedDefects.Add(p_defects[0]);

                //Add the defects that have endPoints connected to startPoints in the ordered list
                while (orderedDefects.Count != p_defects.Count)
                {
                    List<ConvexDefect> candidates = p_defects.Where(x => x.StartPoint.Equals(orderedDefects.Last().EndPoint)).ToList();
                    if (candidates.Count == 1)
                        orderedDefects.Add(candidates.First());
                    else
                    {
                        int next;
                        for (next = 0; next < p_defects.Count && !p_defects[next].Equals(orderedDefects.Last()); ++next)
                            ;

                        orderedDefects.Add(p_defects[(next + 1) % p_defects.Count]);
                    }
                }

            }
            else
                orderedDefects.AddRange(p_defects);

            return orderedDefects;
        }

        /// <summary>
        /// This method implements J. Raheja, K. Das, A. Chaudhary's fast finger algorithm
        /// </summary>
        /// <seealso cref="http://arxiv.org/ftp/arxiv/papers/1212/1212.0134.pdf"/>
        /// <param name="p_data"></param>
        /// <param name="p_buffer"></param>
        /// <returns></returns>
        private Dictionary<int, List<Point>> findFingers(ref BitmapData p_data, ref byte[] p_buffer)
        {
            if (m_direction == DIRECTION.INVALID)
                throw new Exception("Invalid direction");

            //we don't want to alter the original image
            byte[] fingerBuffer = new byte[p_buffer.Length];
            Buffer.BlockCopy(p_buffer, 0, fingerBuffer, 0, p_buffer.Length);

            int[] xHistogram, yHistogram;
            project2Histogram(out xHistogram, out yHistogram, ref p_data, ref fingerBuffer);

            //Populate fingerBuffer
            switch (m_direction)
            {
                case DIRECTION.LEFT:
                    modifyBufferForFinger(ref p_data, ref fingerBuffer, ref yHistogram, 0, true);
                    break;
                case DIRECTION.RIGHT:
                    modifyBufferForFinger(ref p_data, ref fingerBuffer, ref yHistogram, p_data.Width - 1, true);
                    break;
                case DIRECTION.TOP:
                    modifyBufferForFinger(ref p_data, ref fingerBuffer, ref xHistogram, 0, false);
                    break;
                case DIRECTION.BOTTOM:
                    modifyBufferForFinger(ref p_data, ref fingerBuffer, ref xHistogram, p_data.Height - 1, false);
                    break;                  
            }
            
            Dictionary<int, List<Point>> fingerTips = ImageProcess.findBlobs(ref p_data, ref fingerBuffer);

            //Clean up some noise
            int threshold = fingerTips.Max(x => x.Value.Count) / 4;
            List<KeyValuePair<int, List<Point>>> kvps =  fingerTips.Where(x => x.Value.Count < threshold).ToList();
            if (fingerTips.Count - kvps.Count >= 5)
                foreach (KeyValuePair<int, List<Point>> item in kvps)
                {
                    fingerTips.Remove(item.Key);
                }
            else
                fingerTips = fingerTips.OrderByDescending(x => x.Value.Count).Take(5).ToDictionary(pair => pair.Key, pair => pair.Value);

            List<Point> fingerTipPoints = new List<Point>();
            foreach (KeyValuePair<int, List<Point>> tip in fingerTips)
                fingerTipPoints.AddRange(tip.Value);

            
            Image img = new Bitmap(p_data.Width, p_data.Height);
            byte[] garbage;
            BitmapData garbageData = BitmapManip.lockBitmap(out garbage, img);
            Buffer.BlockCopy(fingerBuffer, 0, garbage, 0, fingerBuffer.Length);

            ImageProcess.updateBuffer(fingerTipPoints, ref garbageData, ref garbage);

            BitmapManip.unlockBitmap(ref garbage, ref garbageData, img);
            img.Save("fingerTips.jpg");

            return fingerTips;
        }
         
        /// <summary>
        /// perfoms the modify image function described in the article
        /// </summary>
        /// <param name="p_data"></param>
        /// <param name="p_buffer"></param>
        /// <param name="p_histogram"></param>
        /// <param name="p_start"></param>
        private void modifyBufferForFinger(ref BitmapData p_data, ref byte[] p_buffer, ref int[] p_histogram, int p_start, bool p_rowFirst)
        {
            int offset, end, inc, x, y, currentCount, pixelCount;
            inc = (p_start == 0 ? 1 : -1);

            //Left and right iterate x then y
            if (p_rowFirst)
            {
                end = p_data.Width - p_start - 1;
                for (y = 0; y < p_data.Height; ++y)
                {
                    offset = ImageProcess.getOffset(p_start, y, p_data.Width, 4);
                    currentCount = 0;
                    
                    if ((pixelCount = p_histogram[y]) != 0)
                        for (x = p_start; x != end && currentCount < pixelCount; x += inc, offset += inc * 4)
                        {
                            if (p_buffer[offset] == 255)
                            {
                                ++currentCount;

                                if ((int)Math.Round(currentCount * 255.0 / p_histogram[y]) == 255)
                                    p_buffer[offset] = p_buffer[offset + 1] = p_buffer[offset + 2] = 255;
                                else
                                    p_buffer[offset] = p_buffer[offset + 1] = p_buffer[offset + 2] = 0;
                            }
                        }
                }
            }
            else //Top and bottom iterate y then x
            {
                end = p_data.Height - p_start - 1;
                for (x = 0; x < p_data.Width; ++x)
                {
                    offset = ImageProcess.getOffset(x, p_start, p_data.Width, 4);
                    currentCount = 0;

                    if ((pixelCount = p_histogram[x]) != 0)
                        for (y = p_start; y != end && currentCount < pixelCount; y += inc, offset += inc * p_data.Stride)
                        {
                            if (p_buffer[offset] == 255)
                            {
                                ++currentCount;

                                if ((int)Math.Round(currentCount * 255.0 / p_histogram[x]) == 255)
                                    p_buffer[offset] = p_buffer[offset + 1] = p_buffer[offset + 2] = 255;
                                else
                                    p_buffer[offset] = p_buffer[offset + 1] = p_buffer[offset + 2] = 0;
                            }
                        }
                }
            }
        }

        /// <summary>
        /// Refines entire blobs to single points
        /// </summary>
        /// <param name="fingerTips"></param>
        /// <returns></returns>
        private List<Point> refineFingerTips(ref Dictionary<int, List<Point>> p_fingerTips, List<ConvexDefect> p_defects)
        {
            double min, max, dist;
            Point  avgPoint;
            List<Point> fingerTipPoints = new List<Point>();

            #region Easy reduction
            //the following will only work for cases where there are 4 defects
            if (p_defects.Count == 4)
            {
                Point minPoint = new Point();
                foreach (List<Point> tip in p_fingerTips.Values)
                {
                    //If the fingertip is touching a convex defect start/end point add it in
                    foreach (ConvexDefect cd in p_defects)
                    {
                        if (tip.Contains(cd.StartPoint))
                        {
                            fingerTipPoints.Add(new Point(cd.StartPoint.X, cd.StartPoint.Y));
                            break;
                        }
                        else if (tip.Contains(cd.EndPoint))
                        {
                            fingerTipPoints.Add(new Point(cd.EndPoint.X, cd.EndPoint.Y));
                            break;
                        }
                    }

                    //If the fingertip doesn't contain an endpoint of a convex defect map it to the closest one
                    if (fingerTipPoints.Count == 0 || !tip.Contains(fingerTipPoints.Last()))
                    {
                        //average all points to one
                        avgPoint = tip.Aggregate((acc, cur) => new Point(acc.X + cur.X, acc.Y + cur.Y));
                        avgPoint.X /= tip.Count;
                        avgPoint.Y /= tip.Count;

                        min = int.MaxValue;
                        foreach (ConvexDefect cd in p_defects)
                        {
                            dist = (cd.StartPoint.X - avgPoint.X) * (cd.StartPoint.X - avgPoint.X) +
                                    (cd.StartPoint.Y - avgPoint.Y) * (cd.StartPoint.Y - avgPoint.Y);

                            if (dist < min)
                            {
                                min = dist;
                                minPoint = cd.StartPoint;
                            }

                            dist = (cd.EndPoint.X - avgPoint.X) * (cd.EndPoint.X - avgPoint.X) +
                                    (cd.EndPoint.Y - avgPoint.Y) * (cd.EndPoint.Y - avgPoint.Y);

                            if (dist < min)
                            {
                                min = dist;
                                minPoint = cd.EndPoint;
                            }
                        }

                        fingerTipPoints.Add(new Point(minPoint.X, minPoint.Y));
                    }
                }
            }
            #endregion
            #region Hard Reduction
            else if (p_defects.Count > 0 && p_defects.Count < 4)
            {
                Dictionary<int, Point> reducedPoints = new Dictionary<int, Point>();
                KeyValuePair<int, Point> minPoint, maxPoint;
                minPoint = new KeyValuePair<int, Point>();
                maxPoint = new KeyValuePair<int, Point>();
                int index = 0;

                //Sometimes I get fingertips in defects and this will remove those
                if (p_fingerTips.Count > 5)
                {
                    foreach (ConvexDefect cd in p_defects)
                        foreach (KeyValuePair<int, List<Point>> tip in p_fingerTips.Where(x => x.Value.Contains(cd.DeepestPoint)).ToList())
                        {
                            p_fingerTips.Remove(tip.Key);
                        }
                }

                //Sometimes a fingertip gets split into two parts This will merge them together
                if (p_fingerTips.Count > 5)
                {
                    p_fingerTips = p_fingerTips.OrderBy(kvp => kvp.Value.Min(point => point.X)).ToDictionary(pair => pair.Key, pair => pair.Value);

                    List<Rectangle> proximalBoxes = new List<Rectangle>();
                    Rectangle box;
                    int minX, maxX, minY, maxY;
                    
                    //create boxes
                    foreach (KeyValuePair<int, List<Point>> tip in p_fingerTips)
                    {
                        maxX = maxY = 0;
                        minX = minY = int.MaxValue;
                        foreach (Point p in tip.Value)
                        {
                            minX = (minX < p.X ? minX : p.X);
                            maxX = (maxX > p.X ? maxX : p.X);
                            minY = (minY < p.Y ? minY : p.Y);
                            maxY = (maxY > p.Y ? maxY : p.Y);
                        }
                        box = new Rectangle();
                        box.Width = maxX - minX;
                        box.Height = maxY - minY;
                        proximalBoxes.Add(box);
                    }

                    if (proximalBoxes.Count != p_fingerTips.Count)
                        throw new Exception("Rectangles not equal to boxes");

                    //combine fingertip candidates that fall within eachother's boxes
                    List<int> keys = p_fingerTips.Keys.ToList(); ;
                    for (int i = 0; i < keys.Count - 1; ++i)
                    {
                        box = proximalBoxes[i];
                        box.X = p_fingerTips[keys[i]].Max(x => x.X);
                        box.Y = p_fingerTips[keys[i]].Min(y => y.Y) - box.Height;
                        box.Height += p_fingerTips[keys[i]].Max(y => y.Y) - box.Y + box.Height;

                        if (p_fingerTips[keys[i + 1]].Where(point => box.Contains(point)).ToList().Count > 0)
                        {
                            p_fingerTips[keys[i + 1]].AddRange(p_fingerTips[keys[i]]);
                            p_fingerTips[keys[i]].Clear();
                        }
                    }

                    //remove the ones that are now empty
                    foreach (KeyValuePair<int, List<Point>> item in p_fingerTips.Where(kvp => kvp.Value.Count == 0).ToList())
                        p_fingerTips.Remove(item.Key);
                }

                min = int.MaxValue;
                foreach (List<Point> tip in p_fingerTips.Values)
                {
                    //average all points to one
                    avgPoint = tip.Aggregate((acc, cur) => new Point(acc.X + cur.X, acc.Y + cur.Y));
                    avgPoint.X /= tip.Count;
                    avgPoint.Y /= tip.Count;

                    dist = (p_defects[0].StartPoint.X - avgPoint.X) * (p_defects[0].StartPoint.X - avgPoint.X) +
                           (p_defects[0].StartPoint.Y - avgPoint.Y) * (p_defects[0].StartPoint.Y - avgPoint.Y);

                    if (dist < min)
                    {
                        min = dist;
                        minPoint = new KeyValuePair<int, Point>(index, avgPoint);
                    }

                    reducedPoints.Add(index++, new Point(avgPoint.X, avgPoint.Y));
                }

                fingerTipPoints.Add(new Point(p_defects[0].StartPoint.X, p_defects[0].StartPoint.Y));
                reducedPoints.Remove(minPoint.Key);

                //add points to fingerTipPoints
                foreach (ConvexDefect cd in p_defects)
                {
                    min = int.MaxValue;
                    foreach (KeyValuePair<int, Point> tip in reducedPoints)
                    {
                        dist = (cd.EndPoint.X - tip.Value.X) * (cd.EndPoint.X - tip.Value.X) +
                               (cd.EndPoint.Y - tip.Value.Y) * (cd.EndPoint.Y - tip.Value.Y);

                        if (dist < min)
                        {
                            min = dist;
                            minPoint = tip;
                        }
                    }

                    fingerTipPoints.Add(new Point(cd.EndPoint.X, cd.EndPoint.Y));
                    reducedPoints.Remove(minPoint.Key);
                }
                
                //Check to make sure I'm only seeing 5 fingers
                if (reducedPoints.Count > 4 - p_defects.Count)
                {
                    //determine which defect has the largest area
                    ConvexDefect maxDefect = p_defects.Where(x => (int)x.Area == (int)p_defects.Max(d => d.Area)).ToList().First();
                    
                    do
                    {
                        max = 0.0;
                        //remove points farthest from the defect with the largest area 
                        foreach (KeyValuePair<int, Point> tip in reducedPoints)
                        {
                            dist = (maxDefect.DeepestPoint.X - tip.Value.X) * (maxDefect.DeepestPoint.X - tip.Value.X) +
                                   (maxDefect.DeepestPoint.Y - tip.Value.Y) * (maxDefect.DeepestPoint.Y - tip.Value.Y);

                            if (dist > max)
                            {
                                max = dist;
                                maxPoint = tip;
                            }
                        }

                        reducedPoints.Remove(maxPoint.Key);
                    }
                    while (reducedPoints.Count > 5 - p_defects.Count + 1);
                }

                fingerTipPoints.AddRange(reducedPoints.Values);
            }
            #endregion

            return fingerTipPoints.Distinct().ToList();
        }
    }
}
