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
        private static HashSet<int> m_validPixels;
        private static bool m_isInitialized;
        private static Point m_center;

        Point m_topLeft;
        Point m_topRight;
        Point m_bottomLeft;
        Point m_bottomRight;
        Rectangle m_filterArea;

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
        public void initialize(imageData p_toInit)
        {
            Image toInit = p_toInit.Image;
            Image edges = ImageProcess.findEdges(toInit);

            //Convert if edges size doesn't match given image size
            if (edges.PixelFormat != toInit.PixelFormat)
            {
                convert2PixelFormat(ref toInit);
                p_toInit.Image = toInit;
            }

            m_isInitialized = false;
            populateValidPixels(toInit, edges);
            setupFilter();
            m_center = new Point(toInit.Width / 2, toInit.Height / 2);
            m_isInitialized = true;
            
            setupListener();
            doWork(p_toInit);
        }

        /// <summary>
        /// This is used because edges is done in 32bpp and most cameras are only 24bpp
        /// So i must convert the pixel format for easy comparison
        /// </summary>
        /// <param name="p_toInit">image to convert</param>
        private void convert2PixelFormat(ref Image p_toInit)
        {
            Bitmap converted = new Bitmap(p_toInit);

            p_toInit = converted;
        }

        /// <summary>
        /// Will populate the bitArray
        /// </summary>
        /// <param name="p_toInit">Image to scan</param>
        private void populateValidPixels(Image p_toInit, Image p_edges)
        {
            //Clear out an old but array
            if (m_validPixels != null)
                m_validPixels = null;

            m_validPixels = new HashSet<int>();

            //Take a small window and set valid hand pixels
            //TODO i need to come up with a spreading algorithm to get more hand pixels
            int height = ((Bitmap)p_toInit).Height;
            int width = ((Bitmap)p_toInit).Width;
            Rectangle window = new Rectangle((width / 2 - 50), (height / 2 - 50), 100, 100);

            for (int y = window.Top; y < window.Bottom; ++y)
                 for (int x = window.Left; x < window.Right; ++x)
                {
                    Color color = ((Bitmap)p_toInit).GetPixel(x, y);
                    m_validPixels.Add(color.ToArgb());
                }

            expandSelection(p_toInit, p_edges);
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
        /// Run vertically and horizantally from center until meeting an edge
        /// </summary>
        /// <param name="p_image"></param>
        private void expandSelection(Image p_image, Image p_edges)
        {
            byte[] buffer;
            byte[] edgeBuffer;
            int byteOffset;
            bool isEdge;

            BitmapData data = BitmapManip.lockBitmap(out buffer, p_image);
            BitmapData edgeData = BitmapManip.lockBitmap(out edgeBuffer, p_edges);

            //Create the points
            m_topLeft = new Point((p_image.Width / 2) - 50, (p_image.Height / 2 - 50));
            m_topRight = new Point((p_image.Width / 2) + 50, (p_image.Height / 2 - 50));
            m_bottomLeft = new Point((p_image.Width / 2) - 50, (p_image.Height / 2 + 50));
            m_bottomRight = new Point((p_image.Width / 2) + 50, (p_image.Height / 2 + 50));

            int y, x;

            #region Expand Up
            isEdge = false;
            for (y = m_topLeft.Y; isEdge == false && y > 3; --y)
                for (x = m_topLeft.X; x < m_topRight.X && isEdge == false; ++x)
                {
                    byteOffset = ((y * p_image.Width) + x) * 4;

                    //is an edge
                    if (edgeBuffer[byteOffset] > 0)
                        isEdge = true;
                    else
                    {
                        m_validPixels.Add(Color.FromArgb(buffer[byteOffset + 2], buffer[byteOffset + 1],
                                                         buffer[byteOffset]).ToArgb());
                    }
                }
            #endregion

            m_topLeft.Y = y - 1;
            m_topRight.Y = y - 2; //because the left will be 1 pixel higher than the right

            #region Expand Right
            isEdge = false;
            for (x = m_topRight.X; isEdge == false && x < p_image.Width - 3; ++x)
                for (y = m_topRight.Y; y < m_bottomRight.Y && isEdge == false; ++y)
                {
                    byteOffset = ((y * p_image.Width) + x) * 4;

                    //is an edge
                    if (edgeBuffer[byteOffset] > 0)
                        isEdge = true;
                    else
                    {
                        m_validPixels.Add(Color.FromArgb(buffer[byteOffset + 2], buffer[byteOffset + 1],
                                                         buffer[byteOffset]).ToArgb());
                    }
                }
            #endregion

            m_topRight.X = x - 1;
            m_bottomRight.X = x - 2;

            /* Expanding down is troublesome becuase of shadow edges */

            #region Expand Left

            isEdge = false;
            for (x = m_topLeft.X; isEdge == false && x > 3; --x)
            {
                for (y = m_topLeft.Y; y < m_bottomLeft.Y && isEdge == false; ++y)
                {
                    byteOffset = ((y * p_image.Width) + x) * 4;

                    //is an edge
                    if (edgeBuffer[byteOffset] > 0)
                        isEdge = true;
                    else
                    {
                        m_validPixels.Add(Color.FromArgb(buffer[byteOffset + 2], buffer[byteOffset + 1],
                                                         buffer[byteOffset]).ToArgb());
                    }
                }
            }

            #endregion

            m_topLeft.X = x - 1;
            m_bottomLeft.X = x - 2;

            BitmapManip.unlockBitmap(ref edgeBuffer, ref edgeData, p_edges);
            BitmapManip.unlockBitmap(ref buffer, ref data, p_image);
        }

        /// <summary>
        /// This populates a filter to cancel out noise that isn't near the center
        /// </summary>
        private void setupFilter()
        {
            int max = Math.Max((m_topRight.X - m_topLeft.X) * 4, (m_bottomLeft.Y - m_topLeft.Y) * 4);
            Size size = new Size(max, max);
            m_filterArea = new Rectangle(m_topLeft, size);
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


                //Setting up a buffer to be used for concurrent read/write
                byte[] buffer;
                BitmapData data = BitmapManip.lockBitmap(out buffer, ((imageData)p_imageData).Image);

                //This method returns bit per pixel, we need bytes.
                int depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8; 

                #region Call Parallel.Invoke for each coordinate
                //Only want to do ARGB and RGB check one time
                // Creates more code but is faster
                if (depth == 3)
                    Parallel.Invoke(
                        () =>
                        {
                            //upper left
                            dividedDoWorkRGB(buffer, 0, 0, data.Width / 2, data.Height / 2, data.Width);
                        },
                        () =>
                        {
                            //upper right
                            dividedDoWorkRGB(buffer, data.Width / 2, 0, data.Width, data.Height / 2, data.Width);
                        },
                        () =>
                        {
                            //lower left
                            dividedDoWorkRGB(buffer, 0, data.Height / 2, data.Width / 2, data.Height, data.Width);
                        },
                        () =>
                        {
                            //lower right
                            dividedDoWorkRGB(buffer, data.Width / 2, data.Height / 2, data.Width, data.Height, data.Width);
                        });
                else
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

                ((imageData)p_imageData).Datapoints = getDataPoints(ref data, ref buffer);

                findHand((imageData)p_imageData, data, buffer);
                filterNoise(((imageData)p_imageData).Datapoints, ref data, ref buffer);
                strengthenSignal(ref data, ref buffer);


                ((imageData)p_imageData).Filter = m_filterArea;
                //drawCenter(buffer, data, m_center);

                //Guasian cancelling
                if (depth == 3)
                    performCancellingRGB(ref buffer, data);
                else
                    performCancellingARGB(ref buffer, data);

                ((imageData)p_imageData).Datapoints = getDataPoints(ref data, ref buffer);
                
                BitmapManip.unlockBitmap(ref buffer, ref data, ((imageData)p_imageData).Image);

                Processing.getInstance().ToPCAImage = (imageData)p_imageData;

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
        private void findHand(imageData p_imageData, BitmapData p_data, byte[] p_buffer)
        {
            Point handCenter = m_center;

            int[] xProjection = new int[p_data.Width];
            int[] yProjection = new int[p_data.Height];
            int[] xSmoothed = new int[p_data.Width];
            int[] ySmoothed = new int[p_data.Height];

            int xMax, yMax;
            xMax = yMax = 0;

            //Popluate histogram for x and y intesities
            foreach (Point point in p_imageData.Datapoints)
            {
                ++xProjection[point.X];
                ++yProjection[point.Y];
                
                //Because each point in the histogram is only increasing by one i can get away with this
                if (xProjection[point.X] > xMax)
                    ++xMax;
                if (yProjection[point.Y] > yMax)
                    ++yMax;
            }

            #region Smoothing
            smoothing(ref xProjection, ref xSmoothed);
            smoothing(ref yProjection, ref ySmoothed);
            #endregion

            #region find bounds around the previous center
            // work from the center out.  This will help ignore outside noise.
            //The bounds represent strong points in the histogram
            int xLeftBound, xRightBound;
            xLeftBound = xRightBound = handCenter.X;
            int yTopBound, yBottomBound;
            yTopBound = yBottomBound = handCenter.Y;
            
            Parallel.Invoke(
                () =>
                {
                    findBound(ref yTopBound, ySmoothed, -1, m_filterArea.Width / 8, true); 
                },
                () =>
                {
                    findBound(ref yBottomBound, ySmoothed, 1, m_filterArea.Width / 8, true); 
                },
                () =>
                {
                    findBound(ref xLeftBound, xSmoothed, -1, m_filterArea.Height / 8, true); 
                },
                () =>
                {
                    findBound(ref xRightBound, xSmoothed, 1, m_filterArea.Height / 8, true); 
                });
            #endregion

            #region Second pass
            Point closest = new Point();

            //Save the center from first pass as this will be the start of the second
            closest.X = (xLeftBound + xRightBound) / 2;
            closest.Y = (yTopBound + yBottomBound) / 2;

            //Rectangles X and Y are based on the top right so adjustment is necesary
            m_filterArea.X = closest.X - m_filterArea.Width / 2;
            m_filterArea.Y = closest.Y - m_filterArea.Height / 2;

            //Create a smaller window
            xProjection = new int[m_filterArea.Width];
            yProjection = new int[m_filterArea.Height];
            xSmoothed = new int[m_filterArea.Width];
            ySmoothed = new int[m_filterArea.Height];

            List<Point> pointsInFilter = new List<Point>();

            //Populate it with only data points that are within the rectangle
            foreach (Point point in p_imageData.Datapoints)
            {
                if (m_filterArea.Contains(point))
                {
                    ++xProjection[point.X - m_filterArea.X];
                    ++yProjection[point.Y - m_filterArea.Y];
                    pointsInFilter.Add(point);
                }
            }

            p_imageData.Datapoints = pointsInFilter;

            //Smooth it out before the finding the center
            smoothing(ref xProjection, ref xSmoothed);
            smoothing(ref yProjection, ref ySmoothed);
            #endregion

            #region find bounds around the first center
            //This second pass will work over a reduced area so I work from the outside
            // of the rectangle in , not worrying about noise
            xLeftBound = xRightBound = closest.X - m_filterArea.X;
            yTopBound = xLeftBound = 1;
            yBottomBound = m_filterArea.Height - 2;
            xRightBound = m_filterArea.Width - 2;

            Parallel.Invoke(
                () =>
                {
                    findBound(ref yTopBound, ySmoothed, 1, m_filterArea.Width / 8, false);
                },
                () =>
                {
                    findBound(ref yBottomBound, ySmoothed, -1, m_filterArea.Width / 8, false);
                },
                () =>
                {
                    findBound(ref xLeftBound, xSmoothed, 1, m_filterArea.Height / 8, false);
                },
                () =>
                {
                    findBound(ref xRightBound, xSmoothed, -1, m_filterArea.Height / 8, false);
                });
            #endregion
            closest.X = ((xLeftBound + xRightBound) / 2) + m_filterArea.X;
            closest.Y = ((yTopBound + yBottomBound) / 2) + m_filterArea.Y;

            //Update center and filter
            m_center = new Point(closest.X, closest.Y);

            m_filterArea.X = m_center.X - m_filterArea.Width / 2;
            m_filterArea.Y = m_center.Y - m_filterArea.Height / 2;
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
            int strengthenValue = 7;
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

            int validPixels = DebugFunctions.DebugBuffer.getValidPixels(ref p_data, ref newBuffer);
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
        /// This converts the image data into data points with x and y coordinates.
        /// I'm using a list becuase this should be a sparse dataset.
        /// </summary>
        /// <param name="p_buffer">image as bytes</param>
        /// <param name="p_data">BitmapData</param>
        /// <returns>list of (x, y) tuples</returns>
        private List<Point> getDataPoints(ref BitmapData p_data, ref byte[] p_buffer)
        {
            int x, y;
            int depth = p_data.Stride / p_data.Width;
            List<Point> dataPoints = new List<Point>();

            //I am iterating this way instead of with a double for loop with x and y because
            // this should be a sparse matrix
            for (int offset = 0; offset < p_buffer.Length; offset += depth)
            {
                if (p_buffer[offset] > 0)
                {
                    y = offset / p_data.Stride;
                    x = (offset % p_data.Stride) / depth;
                    dataPoints.Add(new Point(x, y));
                }
            }

            return dataPoints;
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
        /// Handles comparing pixels to the valid pixels for RGB or 24bpp
        /// </summary>
        /// <param name="p_buffer">Byte array of image to process</param>
        /// <param name="startX">Start X position (0 = left)</param>
        /// <param name="startY">Start Y position (0 = top)</param>
        /// <param name="endX"></param>
        /// <param name="endY"></param>
        /// <param name="width">Width in pixels used to determine offset</param>
        private void dividedDoWorkRGB(byte[] p_buffer, int startX, int startY, int endX, int endY, int width)
        {
            //To be overwritten 
            int offset;
            int curPixelColor;
            
            for (int y = startY; y < endY; ++y)
                for (int x = startX; x < endX; ++x)
                { 
                    //Just a basic transform from 1 dimension to 2
                    offset = ((y * width) + x) * 3;
                    
                    //FromArgb requires rgb but array is ordered bgr
                    curPixelColor = Color.FromArgb(p_buffer[offset + 2], p_buffer[offset + 1], p_buffer[offset]).ToArgb();
                    if (!m_validPixels.Contains(curPixelColor))
                    {
                        p_buffer[offset + 0] = p_buffer[offset + 1] = 
                        p_buffer[offset + 2] = 0; //black is all zeroes
                    }
                    else
                    {
                        //white is all 1
                        //p_buffer[offset] = p_buffer[offset + 1] = p_buffer[offset + 2] = 255;
                    }
                }
        }

        /// <summary>
        /// Handles comparing pixels to the valid pixels for ARGB or 32bpp
        /// This is an exact copy of dividedDoWorkRGB except the get and replace functions include space for ARGB format
        /// </summary>
        /// <param name="p_buffer">Byte array of image to process</param>
        /// <param name="startX">Start X position (0 = left)</param>
        /// <param name="startY">Start Y position (0 = top)</param>
        /// <param name="endX"></param>
        /// <param name="endY"></param>
        /// <param name="width">Width in pixels used to determine offset</param>
        private void dividedDoWorkARGB(byte[] p_buffer, int startX, int startY, int endX, int endY, int width)
        {
            //Read above comments.
            Color BLACK = Color.Black;

            int offset;
            int curPixelColor;
            
            for (int y = startY; y < endY; ++y)
                for (int x = startX; x < endX; ++x)
                {
                    offset = ((y * width) + x) * 4;
                    curPixelColor = Color.FromArgb(p_buffer[offset + 3], p_buffer[offset + 2], 
                                                   p_buffer[offset + 1], p_buffer[offset]).ToArgb();
                    if (!m_validPixels.Contains(curPixelColor))
                    {
                        p_buffer[offset + 0] = p_buffer[offset + 1] = 
                        p_buffer[offset + 2] = 0;
                        p_buffer[offset + 3] = 255;
                    }
                    else
                    {
                        //white is all 1
                        //p_buffer[offset] = p_buffer[offset + 1] = 
                        //p_buffer[offset + 2] = p_buffer[offset + 3] = 255;
                    }
                }
        }

        /// <summary>
        /// Cancels out the pixels that aren't near the hand
        /// </summary>
        /// <param name="p_buffer">image buffer</param>
        /// <param name="p_data">bitmap data for p_buffer</param>
        private void performCancellingRGB(ref byte[] p_buffer, BitmapData p_data)
        {
            //Get the bounds of the rectangle centered around the center given by the obj
            Point topLeft = new Point(m_filterArea.X, m_filterArea.Y);
            Point bottomRight = new Point(m_filterArea.X + m_filterArea.Width,
                                          m_filterArea.Y + m_filterArea.Height);

            int byteOffset = 0;

            //Iterate through each column
            for (int y = 0; y < p_data.Height; ++y)
            {
                //If the box is within this row
                if (y >= topLeft.Y && y <= bottomRight.Y)
                {
                    byteOffset = y * p_data.Stride;
                    int x = 0;
                    int xOffset;
                    //clear everything to the left of the box
                    while (x < topLeft.X)
                    {
                        xOffset = x * 3;
                        p_buffer[xOffset + byteOffset + 2] = p_buffer[xOffset + byteOffset + 1] =
                            p_buffer[xOffset + byteOffset] = 0;
                        ++x;
                    }

                    //do nothing to the values in the box
                    while (x < bottomRight.X)
                        ++x;

                    //clear everything to the right of the box
                    while (x < p_data.Width)
                    {
                        xOffset = x * 3;
                        p_buffer[xOffset + byteOffset + 2] = p_buffer[xOffset + byteOffset + 1] =
                            p_buffer[xOffset + byteOffset] = 0;
                        ++x;
                    }
                }

                else
                {
                    //clear the entire row
                    byteOffset = y * p_data.Stride;
                    for (int x = 0; x < p_data.Stride; x += 3)
                    {
                        p_buffer[x + byteOffset + 2] = p_buffer[x + byteOffset + 1] =
                            p_buffer[x + byteOffset] = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Cancels out the pixels that aren't near the hand
        /// </summary>
        /// <param name="p_buffer">image buffer</param>
        /// <param name="p_data">bitmap data for p_buffer</param>
        private void performCancellingARGB(ref byte[] p_buffer, BitmapData p_data)
        {
            //Get the bounds of the rectangle centered around the center given by the obj
            Point topLeft = new Point(m_filterArea.X, m_filterArea.Y);
            Point bottomRight = new Point(m_filterArea.X + m_filterArea.Width,
                                          m_filterArea.Y + m_filterArea.Height);

            int byteOffset = 0;

            //Iterate through each column
            for (int y = 0; y < p_data.Height; ++y)
            {
                //If the box is within this row
                if (y >= topLeft.Y && y <= bottomRight.Y)
                {
                    byteOffset = y * p_data.Stride;
                    int x = 0;
                    int xOffset;
                    //clear everything to the left of the box
                    while (x < topLeft.X)
                    {
                        xOffset = x * 4;
                        p_buffer[xOffset + byteOffset + 2] = p_buffer[xOffset + byteOffset + 1] =
                            p_buffer[xOffset + byteOffset] = 0;
                        p_buffer[xOffset + byteOffset + 3] = 255;
                        ++x;
                    }

                    //do nothing to the values in the box
                    while (x < bottomRight.X)
                        ++x;

                    //clear everything to the right of the box
                    while (x < p_data.Width)
                    {
                        xOffset = x * 4;
                        p_buffer[xOffset + byteOffset + 2] = p_buffer[xOffset + byteOffset + 1] =
                            p_buffer[xOffset + byteOffset] = 0;
                        p_buffer[xOffset + byteOffset + 3] = 255;
                        ++x;
                    }
                }

                else 
                {
                    //clear the entire row
                    byteOffset = y * p_data.Stride;
                    for (int x = 0; x < p_data.Stride; x += 4)
                    {
                        p_buffer[x + byteOffset + 2] = p_buffer[x + byteOffset + 1] =
                            p_buffer[x + byteOffset] = 0;
                        p_buffer[x + byteOffset + 3] = 255;
                    }
                }
            }
        }
    }
}
