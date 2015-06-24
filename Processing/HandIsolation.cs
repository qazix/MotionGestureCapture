﻿using System;
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

            if (edges.PixelFormat != toInit.PixelFormat)
            {
                convert2PixelFormat(ref toInit);
                p_toInit.Image = toInit;
            }

            m_isInitialized = false;
            populateValidPixels(toInit, edges);
            setupFilter();
            m_isInitialized = true;
            
            setupListener();
            doWork(p_toInit);
        }

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
                this.doWork(obj);
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
            Size size = new Size((m_topRight.X - m_topLeft.X) * 4, (m_bottomLeft.Y - m_topLeft.Y) * 4);
            m_filterArea = new Rectangle(m_topLeft, size);
        }

        /// <summary>
        /// this method transforms the image into a filtered image
        /// UPDATE: this now performs almost insantly instead of the 2 seconds it took before
        /// </summary>
        /// <param name="p_imageData"></param>
        protected override void doWork(imageData p_imageData)
        {
            if (m_isInitialized)
            {              
                //Setting up a buffer to be used for concurrent read/write
                byte[] buffer;
                BitmapData data = lockBitmap(out buffer, p_imageData.Image);

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

                //Guasian cancelling
                performCancelling(ref buffer, data, p_imageData);

                unlockBitmap(ref buffer, ref data, p_imageData.Image);

                Processing.getInstance().ToPCAImage = p_imageData;

                //If someone is listener raise an event
                if (ProcessReady != null)
                    ProcessReady();
            }
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
        /// <param name="p_buffer"></param>
        /// <param name="p_data"></param>
        /// <param name="obj">imageData</param>
        private void performCancelling(ref byte[] p_buffer, BitmapData p_data, Object obj)
        {
            Point filterCenter = ((imageData)obj).Center;
            filterCenter.X += p_data.Width / 2;
            filterCenter.Y = (p_data.Height / 2) - filterCenter.Y;

            //Get the bounds of the rectangle centered around the center given by the obj
            Point topLeft = new Point(filterCenter.X - (m_filterArea.Width / 2), 
                                      filterCenter.Y - (m_filterArea.Height / 2));
            Point bottomRight = new Point(filterCenter.X + (m_filterArea.Width / 2),
                                          filterCenter.Y + (m_filterArea.Height / 2));

            int byteOffset = 0;

            //Iterate through each column
            for (int y = 0; y < p_data.Height; ++y)
            {
                //If the box is within this row
                if (y >= topLeft.Y && y <= bottomRight.Y)
                {
                    byteOffset = y * p_data.Stride;
                    int x = 0;
                    //clear everything to the left of the box
                    while (x < topLeft.X)
                    {
                        p_buffer[(x * 4) + byteOffset + 2] = p_buffer[(x * 4) + byteOffset + 1] =
                            p_buffer[(x * 4) + byteOffset] = 0;
                        p_buffer[(x * 4) + byteOffset + 3] = 255;
                        ++x;
                    }

                    //do nothing to the values in the box
                    while (x < bottomRight.X)
                        ++x;

                    //clear everything to the right of the box
                    while(x < p_data.Width)
                    {
                        p_buffer[(x * 4) + byteOffset + 2] = p_buffer[(x * 4) + byteOffset + 1] =
                            p_buffer[(x * 4) + byteOffset] = 0;
                        p_buffer[(x * 4) + byteOffset + 3] = 255;
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
