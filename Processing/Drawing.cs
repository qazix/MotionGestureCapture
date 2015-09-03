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

                byte[] buffer;
                BitmapData data = BitmapManip.lockBitmap(out buffer, ((ImageData)p_imgData).Image);

                drawOrientation(data, buffer, ((ImageData)p_imgData).EigenVectors, ((ImageData)p_imgData).Center);
                drawLines(ref data, ref buffer, convexHull, Color.Yellow);
                drawLines(ref data, ref buffer, contour, Color.Blue);
                drawDefects(ref data, ref buffer, convexDefects, Color.Orange);

                BitmapManip.unlockBitmap(ref buffer, ref data, ((ImageData)p_imgData).Image);
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
                    curPos[1] > 0 && curPos[1] < p_data.Height)
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
