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
    public class Drawing : Process
    {
        private Processing.ImageReadyHandler m_DrawingImageHandler;

        public Drawing()
        { }

        public void initialize()
        {
            setupListener();
        }

        protected override void setupListener()
        {
            m_DrawingImageHandler = (obj) =>
            {
                doWork(obj);
            };

            Processing.getInstance().DrawingImageFilled += m_DrawingImageHandler;
        }

        /// <summary>
        /// Handles all the drawing on the image
        /// </summary>
        /// <param name="p_imgData"></param>
        protected override void doWork(object p_imgData)
        {
            if (((ImageData)p_imgData).ConvexDefects != null)
            {
                List<Point> convexHull = ((ImageData)p_imgData).ConvexHull;
                List<Point> contour = ((ImageData)p_imgData).Contour;
                List<ConvexDefect> convexDefects = ((ImageData)p_imgData).ConvexDefects;
                List<Point> fingerTips = ((ImageData)p_imgData).FingerTips;
                MotionGestureProcessing.ImageData.Gestures gesture = ((ImageData)p_imgData).Gesture;

                byte[] buffer;
                BitmapData data = BitmapManip.lockBitmap(out buffer, ((ImageData)p_imgData).Image);

                drawOrientation(data, buffer, ((ImageData)p_imgData).EigenVectors, ((ImageData)p_imgData).Center);
                drawLines(ref data, ref buffer, convexHull, Color.Yellow);
                drawLines(ref data, ref buffer, contour, Color.Blue);
                drawDefects(ref data, ref buffer, convexDefects, Color.Orange);
                drawFingers(ref data, ref buffer, fingerTips, 20, Color.Red);
                drawGesture(ref data, ref buffer, gesture);

                BitmapManip.unlockBitmap(ref buffer, ref data, ((ImageData)p_imgData).Image);

                ((ImageData)p_imgData).Image.Save("Final.jpg");
            }

            Processing.getInstance().ToReturnImage = (ImageData)p_imgData;
        }

        /// <summary>
        /// draws the center of the hand 
        /// FOR TESTING
        /// </summary>
        /// <param name="p_buffer"></param>
        /// <param name="p_data"></param>
        protected void drawCenter(byte[] p_buffer, BitmapData p_data, Point p_center)
        {
            int depth = p_data.Stride / p_data.Width;
            int offset = p_center.X * depth;
            for (int y = 0; y < p_data.Height; ++y)
            {
                if (y != p_center.Y)
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
                    offset += p_center.X * depth;
                }
                offset += p_data.Stride;
            }
        }

        /// <summary>
        /// Draws a cross at the center and accounts for rotation
        /// </summary>
        /// <param name="p_buffer"></param>
        /// <param name="p_data"></param>
        /// <param name="p_vectors"></param>
        protected void drawOrientation(BitmapData p_data, byte[] p_buffer, double[,] p_vectors, Point p_center)
        {
            double[] primPos = new double[2];
            double[] primNeg = new double[2];
            double[] secPos = new double[2];
            double[] secNeg = new double[2];

            primNeg[0] = -1 * (primPos[0] = p_vectors[0, 0]);
            primNeg[1] = -1 * (primPos[1] = p_vectors[0, 1]);
            secNeg[0] = -1 * (secPos[0] = p_vectors[1, 0]);
            secNeg[1] = -1 * (secPos[1] = p_vectors[1, 1]);

            Parallel.Invoke(
            () =>
            {
                dividedDrawOrientation(p_buffer, p_data, primPos, p_center);
            },
            () =>
            {
                dividedDrawOrientation(p_buffer, p_data, primNeg, p_center);
            },
            () =>
            {
                dividedDrawOrientation(p_buffer, p_data, secPos, p_center);
            },
            () =>
            {
                dividedDrawOrientation(p_buffer, p_data, secNeg, p_center);
            });
        }

        /// <summary>
        /// Draws a ray from the center following the vector given
        /// </summary>
        /// <param name="p_buffer">medium to draw upon</param>
        /// <param name="p_data">information about the buffer</param>
        /// <param name="p_vector">vector to draw</param>
        /// <param name="p_center">starting point</param>
        private void dividedDrawOrientation(byte[] p_buffer, BitmapData p_data, double[] p_vector, Point p_center)
        {
            double[] curPos = new double[2];
            curPos[0] = p_center.X + p_vector[0];
            curPos[1] = p_center.Y + p_vector[1];

            int depth = p_data.Stride / p_data.Width;

            bool valid;
            int offset;

            do
            {
                if (curPos[0] > 0 && curPos[0] < p_data.Width &&
                    curPos[1] > 0 && curPos[1] < p_data.Height)
                    valid = true;
                else
                    valid = false;
                if (valid)
                {
                    offset = (((int)curPos[1] * p_data.Width) + (int)curPos[0]) * depth;
                    p_buffer[offset] = p_buffer[offset + 2] = 0;
                    p_buffer[offset + 1] = 255;
                }

                curPos[0] += p_vector[0];
                curPos[1] += p_vector[1];
            }
            while (valid);
        }

        /// <summary>
        /// KNows how to use draws lines for convex defects
        /// </summary>
        /// <param name="data"></param>
        /// <param name="buffer"></param>
        /// <param name="convexDefects"></param>
        /// <param name="color"></param>
        protected void drawDefects(ref BitmapData p_data, ref byte[] p_buffer, List<ConvexDefect> p_convexDefects, Color p_color)
        {
            foreach (ConvexDefect cd in p_convexDefects)
            {
                drawLine(ref p_data, ref p_buffer, cd.StartPoint, cd.DeepestPoint, p_color);
                drawLine(ref p_data, ref p_buffer, cd.DeepestPoint, cd.EndPoint, p_color);
            }
        }

        /// <summary>
        /// Draws boxes around the fingertip
        /// </summary>
        /// <param name="p_data"></param>
        /// <param name="p_buffer"></param>
        /// <param name="p_fingerTips"></param>
        /// <param name="p_color"></param>
        private void drawFingers(ref BitmapData p_data, ref byte[] p_buffer, List<Point> p_fingerTips, int p_size, Color p_color)
        {
            p_size = p_size / 2;
            foreach (Point tip in p_fingerTips)
            {
                drawLine(ref p_data, ref p_buffer, new Point(tip.X, tip.Y - p_size), new Point(tip.X + p_size, tip.Y), p_color);
                drawLine(ref p_data, ref p_buffer, new Point(tip.X + p_size, tip.Y), new Point(tip.X, tip.Y + p_size), p_color);
                drawLine(ref p_data, ref p_buffer, new Point(tip.X, tip.Y + p_size), new Point(tip.X - p_size, tip.Y), p_color);
                drawLine(ref p_data, ref p_buffer, new Point(tip.X - p_size, tip.Y), new Point(tip.X, tip.Y - p_size), p_color);
            }
        }

        /// <summary>
        /// loads an image for the gesture and puts it in top right corner
        /// </summary>
        /// <param name="p_gesture"></param>
        private void drawGesture(ref BitmapData p_data, ref byte[] p_buffer, ImageData.Gestures p_gesture)
        {
            string resourceName = "";
            byte[] buffer;
            BitmapData data;
            int offset, sourceOffset;

            switch(p_gesture)
            {
                case ImageData.Gestures.INITIALIZING:
                    resourceName = "initialize";
                    break;
                case ImageData.Gestures.MOVE:
                    resourceName = "move";
                    break;
                case ImageData.Gestures.RIGHTCLICK:
                    resourceName = "mouse_right_click";
                    break;
                case ImageData.Gestures.LEFTCLICK:
                    resourceName = "mouse_left_click_128";
                    break;
                case ImageData.Gestures.DOUBLECLICK:
                    resourceName = "double_click";
                    break;
                case ImageData.Gestures.CLICKANDHOLD:
                    resourceName = "click_and_hold";
                    break;
            }


            Image im = (Bitmap)MotionGestureProcessing.Properties.Resources.ResourceManager.GetObject(resourceName);
            {
                convert2PixelFormat(ref im);
                data = BitmapManip.lockBitmap(out buffer, im);
                BitmapManip.unlockBitmap(ref buffer, ref data, im);
            }
            im.Dispose();

            offset = sourceOffset = 0;
            for (int y = 0; y < data.Height; ++y)
            {
                sourceOffset = ImageProcess.getOffset(0, y, p_data.Width, 4);
                for (int x = 0; x < data.Width; ++x, sourceOffset += 4, offset += 4)
                {
                    p_buffer[sourceOffset] = buffer[offset];
                    p_buffer[sourceOffset + 1] = buffer[offset + 1];
                    p_buffer[sourceOffset + 2] = buffer[offset + 2];
                    p_buffer[sourceOffset + 3] = buffer[offset + 3];
                }
            }
        }

        /// <summary>
        /// Takes a list of points and connects the dots
        /// </summary>
        /// <param name="p_lines"></param>
        protected void drawLines(ref BitmapData p_data, ref byte[] p_buffer, List<Point> p_lines, Color p_color)
        {
            int i;
            for (i = 0; i < p_lines.Count - 1; ++i)
            {
                drawLine(ref p_data, ref p_buffer, p_lines[i], p_lines[i + 1], p_color);
            }
        }

        /// <summary>
        /// Actually draws the line
        /// </summary>
        /// <param name="p_buffer"></param>
        /// <param name="p_data"></param>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        private void drawLine(ref BitmapData p_data, ref byte[] p_buffer, Point point1, Point point2, Color p_color)
        {
            double[] curPos = new double[2];
            curPos[0] = point1.X;
            curPos[1] = point1.Y;

            double deltaX = point2.X - point1.X;
            double deltaY = point2.Y - point1.Y;

            double len = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            deltaX /= len;
            deltaY /= len;

            int offset;
            int depth = p_data.Stride / p_data.Width;
            bool valid;
            do
            {
                if (curPos[0] > 0 && curPos[0] < p_data.Width &&
                    curPos[1] > 1 && curPos[1] < p_data.Height - 1)
                    valid = true;
                else
                    valid = false;
                if (valid)
                {
                    offset = (((int)curPos[1] * p_data.Width) + (int)curPos[0]) * depth;
                    p_buffer[offset] = p_buffer[offset - p_data.Stride] = p_buffer[offset + p_data.Stride] = p_color.B;
                    p_buffer[offset + 1] = p_buffer[offset + 1 - p_data.Stride] = p_buffer[offset + 1 + p_data.Stride] = p_color.G;
                    p_buffer[offset + 2] = p_buffer[offset + 2 - p_data.Stride] = p_buffer[offset + 2 + p_data.Stride] = p_color.R;

                    curPos[0] += deltaX;
                    curPos[1] += deltaY;
                }
            }
            while (valid && (int)curPos[0] != point2.X);
        }
    }
}
