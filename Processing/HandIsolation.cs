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
            Image edges = ImageProcessing.findEdges(toInit);

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
                //Thread t = new Thread(new ParameterizedThreadStart(doWork));
                //t.Start(obj);
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

            BitmapData data = lockBitmap(out buffer, p_image);
            BitmapData edgeData = lockBitmap(out edgeBuffer, p_edges);

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

            /* Expanding down i troublesome becuase of shadow edges
            #region Expand Down
            isEdge = false;
            for (y = m_bottomLeft.Y; isEdge == false && y < p_image.Height - 3; ++y)
                for (x = m_bottomLeft.X; x < m_bottomRight.X && isEdge == false; ++x)
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

            m_bottomRight.Y = y - 1;
            m_bottomLeft.Y = y - 2;
            */
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

            unlockBitmap(ref edgeBuffer, ref edgeData, p_edges);
            unlockBitmap(ref buffer, ref data, p_image);
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
                BitmapData data = lockBitmap(out buffer, ((imageData)p_imageData).Image);

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

                ((imageData)p_imageData).Datapoints = getDataPoints(buffer, data);

                findHand((imageData)p_imageData, data, buffer);
                ((imageData)p_imageData).Filter = m_filterArea;
                drawCenter(buffer, data);

                //Guasian cancelling
                if (depth == 3)
                    performCancellingRGB(ref buffer, data);
                else
                    performCancellingARGB(ref buffer, data);
                
                unlockBitmap(ref buffer, ref data, ((imageData)p_imageData).Image);

                Processing.getInstance().ToPCAImage = (imageData)p_imageData;

                //If someone is listener raise an event
                if (ProcessReady != null)
                    ProcessReady();
            }
        }

        /// <summary>
        /// draws the center of the hand 
        /// FOR TESTING
        /// </summary>
        /// <param name="p_buffer"></param>
        /// <param name="p_data"></param>
        private void drawCenter(byte[] p_buffer, BitmapData p_data)
        {
            int depth = p_data.Stride / p_data.Width;
            int offset = m_center.X * depth;
            for (int y = 0; y < p_data.Height; ++y)
            {
                if (y != m_center.Y)
                {
                    p_buffer[offset] = p_buffer[offset + 2] = 0;
                    p_buffer[offset + 1] = 255;
                }
                else
                {
                    offset = (y * p_data.Stride);
                    for (int x = 0; x < p_data.Stride; x += depth)
                    {
                        p_buffer[offset + x] = p_buffer[offset + x + 2] = 0;
                        p_buffer[offset + x + 1] = 255;
                    }
                    offset += m_center.X * depth;
                }
                offset += p_data.Stride;
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

            //Popluate histogram for x and y intesities
            foreach (Point point in p_imageData.Datapoints)
            {
                ++xProjection[point.X];
                ++yProjection[point.Y];
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
                    findBound(ref yTopBound, ySmoothed, -1, m_filterArea.Width / 4, true);
                },
                () =>
                {
                    findBound(ref yBottomBound, ySmoothed, 1, m_filterArea.Width / 4, true);
                },
                () =>
                {
                    findBound(ref xLeftBound, xSmoothed, -1, m_filterArea.Height / 4, true);
                },
                () =>
                {
                    findBound(ref xRightBound, xSmoothed, 1, m_filterArea.Height / 4, true);
                });
            #endregion

            #region Second pass
            Point closest = new Point();

            //Save the center from first pass as this will be the start of the second
            closest.X = (xLeftBound + xRightBound) / 2;
            closest.Y = (yTopBound + yBottomBound) / 2;

            //Rectangles X and Y are based on the tip right so adjustment is necesary
            m_filterArea.X = closest.X - m_filterArea.Width / 2;
            m_filterArea.Y = closest.Y - m_filterArea.Height / 2;

            //Create a smaller window
            xProjection = new int[m_filterArea.Width];
            yProjection = new int[m_filterArea.Height];
            xSmoothed = new int[m_filterArea.Width];
            ySmoothed = new int[m_filterArea.Height];

            List<Point> toRemove = new List<Point>();

            //Populate it with only data points that are within the rectangle
            foreach (Point point in p_imageData.Datapoints)
            {
                if (m_filterArea.Contains(point))
                {
                    ++xProjection[point.X - m_filterArea.X];
                    ++yProjection[point.Y - m_filterArea.Y];
                }
                else
                {
                    toRemove.Add(point);
                }
            }
            
            //Remove all the points outside of the filter rectangle
            foreach (Point point in toRemove)
            {
                p_imageData.Datapoints.Remove(point);
            }

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
        private List<Point> getDataPoints(byte[] p_buffer, BitmapData p_data)
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
