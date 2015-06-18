using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionGestureProcessing
{
    class ImageProcessing
    {
        /// <summary>
        /// use the Canny edge detection algorithm
        /// </summary>
        /// <seealso cref="http://www.codeproject.com/Articles/93642/Canny-Edge-Detection-in-C"/>
        /// <see cref="http://softwarebydefault.com/2013/05/11/image-edge-detection/"/>
        /// <param name="p_image">Image to find edges on</param>
        /// <returns>Bitmap with edges</returns>
        public static byte[] findEdges(Image p_image)
        {
            //step 0
            //Convert an image to an byte array
            Image edgeImage = new Bitmap(p_image);
            edgeImage.RotateFlip(RotateFlipType.Rotate180FlipY);
            byte[] resultImage = new byte [p_image.Width * p_image.Height];
            byte[] pixelBuffer;
            BitmapData data = Process.lockBitmap(out pixelBuffer, ref edgeImage);

            //step 1 blur image
            blurImage(ref pixelBuffer, p_image.Width, ref resultImage);

            //Restore byte array to image
            Process.unlockBitmap(ref resultImage, ref data, ref edgeImage);

            edgeImage.Save("FilteredImage.bmp");

            return resultImage;
        }

        /// <summary>
        /// The first step of Canny edge detection
        /// Filter the image we do this by applying a gausian filter over the image
        /// </summary>
        /// <param name="p_image">byte array of the image to blur</param>
        /// <param name="p_width">width in pixels of the image</param>
        /// <param name="p_resultImage"></param>
        private static void blurImage (ref byte[] p_image, int p_width, ref byte[] p_resultImage)
        {
            //setup gaussian kernel stuff
            int[,] gausianKernel;
            int kernalSize = 5;
            int sigma = 1;

            //Populate gaussian filter
            int weight = generateGausianKernel(kernalSize, sigma, out gausianKernel);

            //perform filtering
            int limit = kernalSize / 2;
            float red, green, blue;
            int byteOffset, filterOffset;
            p_resultImage = p_image;

            int height = p_image.Length / (p_width * 4);

            for (int y = limit; y < height - limit; ++y)
                for (int x = limit; x < p_width - limit; ++x)
                {
                    red = 0;
                    green = 0;
                    blue = 0;
                    byteOffset = ((y * p_width) + x) * 4;

                    for (int filterY = -limit; filterY <= limit; ++filterY)
                        for (int filterX = -limit; filterX <= limit; ++filterX)
                        {
                            //sum all the values in the gausian filter
                            filterOffset = byteOffset + ((filterY * p_width) + filterX) * 4;
                            blue += (float)(p_image[filterOffset]) *
                                        gausianKernel[limit + filterY, limit + filterX];
                            green += (float)(p_image[filterOffset + 1]) *
                                        gausianKernel[limit + filterY, limit + filterX];
                            red += (float)(p_image[filterOffset + 2]) *
                                        gausianKernel[limit + filterY, limit + filterX];
                        }
                    //Average them so they are within the byte range
                    blue /= weight;
                    green /= weight;
                    red /= weight;

                    //Put them into the result image
                    p_resultImage[byteOffset] = (byte)blue;
                    p_resultImage[byteOffset + 1] = (byte)green;
                    p_resultImage[byteOffset + 2] = (byte)red;
                    p_resultImage[byteOffset + 3] = 255;
                }
        }

        /// <summary>
        /// The kernel for the filter to be run over the data
        /// </summary>
        /// <param name="p_sizeOfKernel"></param>
        /// <param name="p_sigma"></param>
        /// <param name="p_gKernel"></param>
        /// <returns></returns>
        private static int generateGausianKernel(int p_sizeOfKernel, float p_sigma, out int[,] p_gKernel)
        {
            float pi = (float)Math.PI;

            //kernel is temporary to find precise 
            float[,] kernel = new float[p_sizeOfKernel, p_sizeOfKernel];
            p_gKernel = new int[p_sizeOfKernel, p_sizeOfKernel];

            float D2 = p_sigma * p_sigma * 2;
            float D1 = 1 / (pi * D2);

            int sum, mult;
            float min = 1000; //initialize min to something large
            int upper = p_sizeOfKernel / 2;
            int lower = -upper;

            //Find min value
            for (int i = lower; i <= upper; ++i)
                for (int j = lower; j <= upper; ++j)
                {
                    kernel[upper + i, upper + j] = ((1 / D1) * (float)Math.Exp(
                                                          -(i * i + j * j) / D2));
                    if (kernel[upper + i, upper + j] < min)
                        min = kernel[upper + i, upper + j];
                }

            mult = (int)(1 / min);
            sum = 0;

            //Calculate sum adjusting if the min was between 0 and 1
            if (min > 0 && min < 1)
            {
                for (int i = lower; i <= upper; ++i)
                    for (int j = lower; j <= upper; ++j)
                    {
                        p_gKernel[upper + i, upper + j] = (int)Math.Round(
                            kernel[upper + i, upper + j] * mult, 0);
                        sum += p_gKernel[upper + i, upper + j];
                    }
            }
            else
            {
                for(int i = lower; i <= upper; ++i)
                    for (int j = lower; j <= upper; ++j)
                    {
                        p_gKernel[upper + i, upper + j] = (int)Math.Round(
                            kernel[upper + i, upper + j], 0);
                        sum += p_gKernel[upper + i, upper + j];
                    }
            }

            return sum;
        }
    }
}
