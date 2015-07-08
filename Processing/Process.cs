using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MotionGestureProcessing
{
    public abstract class Process
    {
        /// <summary>
        /// performs Bitmap.lockBits but does all the setup as well
        /// </summary>
        /// <param name="p_buffer">buffer to write out to</param>
        /// <param name="p_image">Image to write</param>
        /// <returns></returns>
        public static BitmapData lockBitmap(out byte[] p_buffer, Image p_image)
        {
            //Setting up a buffer to be used for concurrent read/write
            int width = ((Bitmap)p_image).Width;
            int height = ((Bitmap)p_image).Height;
            Rectangle rect = new Rectangle(0, 0, width, height);
            BitmapData data = ((Bitmap)p_image).LockBits(rect, ImageLockMode.ReadWrite,
                                                         ((Bitmap)p_image).PixelFormat);
            //This method returns bit per pixel, we need bytes.
            int depth = Bitmap.GetPixelFormatSize(data.PixelFormat) / 8;

            //Create a buffer to host the image data and copy the data in
            p_buffer = new Byte[data.Width * data.Height * depth];
            Marshal.Copy(data.Scan0, p_buffer, 0, p_buffer.Length);

            return data;
        }

        /// <summary>
        /// Unlocks the image, created this just for conformities sake
        /// </summary>
        /// <param name="p_buffer"></param>
        /// <param name="p_data"></param>
        /// <param name="p_image"></param>
        public static void unlockBitmap(ref byte[] p_buffer, ref BitmapData p_data, Image p_image)
        {
            //Copy it back and fill the image with the modified data
            Marshal.Copy(p_buffer, 0, p_data.Scan0, p_buffer.Length);
            ((Bitmap)p_image).UnlockBits(p_data);
        }

        protected abstract void setupListener();

        /// <summary>
        /// What this process does to an image
        /// </summary>
        /// <param name="obj">Data pertaining to this image</param>
        /// <param name="p_image">Image to be processed</param>
        protected abstract void doWork(Object p_imgData);

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
        protected void drawOrientation(byte[] p_buffer, BitmapData p_data, double[,] p_vectors, Point p_center)
        {
            double[] primPos = new double[2];
            double[] primNeg = new double[2];
            double[] secPos = new double [2];
            double[] secNeg = new double [2];

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
        private void dividedDrawOrientation(byte[] p_buffer, BitmapData p_data, double[]p_vector, Point p_center)
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
            while(valid);
        }
    }
}
