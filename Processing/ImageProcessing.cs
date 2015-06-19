using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionGestureProcessing
{
    public class ImageProcessing
    {
        //Sobel filters
        private static int[,] m_xFilter = {{-1, 0, 1},
                                           {-2, 0, 2},
                                           {-1, 0, 1}};

        private static int[,] m_yFilter = {{-1, -2, -1},
                                           { 0,  0,  0},
                                           { 1,  2,  1}};

        /// <summary>
        /// use the Canny edge detection algorithm
        /// </summary>
        /// <seealso cref="http://www.codeproject.com/Articles/93642/Canny-Edge-Detection-in-C"/>
        /// <see cref="http://softwarebydefault.com/2013/05/11/image-edge-detection/"/>
        /// <param name="p_image">Image to find edges on</param>
        /// <returns>Bitmap with edges</returns>
        public static Image findEdges(Image p_image)
        {
            //step 0
            //Convert an image to an byte array
            Image edgeImage = new Bitmap(p_image);
            byte[] resultBuffer = new byte[edgeImage.Width * edgeImage.Height];
            byte[] pixelBuffer;
            BitmapData data = Process.lockBitmap(out pixelBuffer, ref edgeImage);
            convert2GreyScale(ref pixelBuffer);

            //step 1 blur image
            blurImage(ref pixelBuffer, edgeImage.Width, ref resultBuffer);

            //TODO: comment this out and replace with this (pixelBuffer = resultBuffer;)
            //Check the images as I go strictly for testing
            Process.unlockBitmap(ref resultBuffer, ref data, ref edgeImage);
            edgeImage.Save("SmoothedImage.bmp");
            data = Process.lockBitmap(out pixelBuffer, ref edgeImage);

            //step 2 apply sobel filters to find gradients
            float[,] angleMap = new float[edgeImage.Height, edgeImage.Width];
            findGradients(ref pixelBuffer, edgeImage.Width, ref resultBuffer, ref angleMap);

            //TODO: comment this out and replace with this (pixelBuffer = resultBuffer;)
            Process.unlockBitmap(ref resultBuffer, ref data, ref edgeImage);
            edgeImage.Save("FilteredImage.bmp");
            data = Process.lockBitmap(out pixelBuffer, ref edgeImage);

            //step 3 clear out all non local maximum values
            nonMaxSuppression(ref pixelBuffer, edgeImage.Width, ref resultBuffer, angleMap);

            //Restore byte array to image
            Process.unlockBitmap(ref resultBuffer, ref data, ref edgeImage);
            edgeImage.Save("NonMaxSuppressImage.bmp");

            return edgeImage;
        }

        /// <summary>
        /// converts image to greyscale
        /// </summary>
        /// <param name="pixelBuffer">Buffer array to convert</param>
        private static void convert2GreyScale(ref byte[] pixelBuffer)
        {
            float rgb;

            for (int i = 0; i < pixelBuffer.Length; i += 4)
            {
                //grey scale transform is .2R + .59G + .11B
                rgb = pixelBuffer[i] * 0.11f;
                rgb += pixelBuffer[i + 1] * 0.59f;
                rgb += pixelBuffer[i + 2] * .2f;

                pixelBuffer[i] = pixelBuffer[i + 1] = pixelBuffer[i + 2] = (byte)rgb;
                pixelBuffer[i + 3] = 255;
            }
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
            double red, green, blue;
            int byteOffset, filterOffset;
            p_resultImage = p_image;

            int height = p_image.Length / (p_width * 4);

            for (int y = limit; y < height - limit; ++y)
                for (int x = limit; x < p_width - limit; ++x)
                {
                    //red = 0;
                    //green = 0;
                    blue = 0;
                    byteOffset = ((y * p_width) + x) * 4;

                    for (int filterY = -limit; filterY <= limit; ++filterY)
                        for (int filterX = -limit; filterX <= limit; ++filterX)
                        {
                            //sum all the values in the gausian filter
                            filterOffset = byteOffset + ((filterY * p_width) + filterX) * 4;
                            blue += (double)p_image[filterOffset] *
                                        gausianKernel[limit + filterY, limit + filterX];
                            //green += (double)(p_image[filterOffset + 1]) *
                            //            gausianKernel[limit + filterY, limit + filterX];
                            //red += (double)(p_image[filterOffset + 2]) *
                            //            gausianKernel[limit + filterY, limit + filterX];
                        }
                    //Average them so they are within the byte range
                    blue /= weight;
                    //green /= weight;
                    //red /= weight;

                    //Put them into the result image
                    p_resultImage[byteOffset] = p_resultImage[byteOffset + 1] =
                        p_resultImage[byteOffset + 2] = (byte)blue;

                    //p_resultImage[byteOffset + 1] = (byte)green;
                    //p_resultImage[byteOffset + 2] = (byte)red;
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

        /// <summary>
        /// Apply Sobel filtering to the smoothed image
        /// </summary>
        /// <param name="p_image">Byte array of Smoothed image</param>
        /// <param name="p_width">width in pixels</param>
        /// <param name="p_resultImage">Byte array to write the results to</param>
        private static void findGradients(ref byte[] p_image, int p_width, ref byte[] p_resultImage, ref float[,] p_angleMap)
        {
            //variables needed for this funciton
            int limit = 3 / 2;
            double blueX, greenX, redX;
            double blueY, greenY, redY;
            double blueTot, greenTot, redTot;
            int byteOffset, filterOffset, xFilterVal, yFilterVal;
            float pi = (float)Math.PI;

            int height = p_image.Length / (p_width * 4);

            //Iterate through pixels
            for (int y = limit; y < height - limit; ++y)
                for (int x = limit; x < p_width - limit; ++x)
                {
                    blueX = greenX = redX = 0.0;
                    blueY = greenY = redY = 0.0;
                    blueTot = greenTot = redTot = 0.0;

                    byteOffset = ((y * p_width) + x) * 4;

                    //Apply sobel filters
                    for (int filterY = -limit; filterY <= limit; ++filterY)
                        for (int filterX = -limit; filterX <= limit; ++filterX)
                        {
                            filterOffset = byteOffset + ((filterY * p_width) + filterX) * 4;
                            xFilterVal = m_xFilter[limit + filterY, limit + filterX];
                            yFilterVal = m_yFilter[limit + filterY, limit + filterX];

                            blueX  += (double)p_image[filterOffset] * xFilterVal;
                            //greenX += (double)p_image[filterOffset + 1] * xFilterVal;
                            //redX   += (double)p_image[filterOffset + 2] * xFilterVal;

                            blueY  += (double)p_image[filterOffset] * yFilterVal;
                            //greenY += (double)p_image[filterOffset + 1] * yFilterVal;
                            //redX   += (double)p_image[filterOffset + 2] * yFilterVal;
                        }

                    //Take the cartesian product of colors
                    blueTot = Math.Sqrt((blueX * blueX) +
                                        (blueY * blueY));
                    //greenTot = Math.Sqrt((greenX * greenX) +
                    //                     (greenY * greenY));
                    //redTot = Math.Sqrt((redX * redX) +
                    //                   (redY * redY));

                    p_resultImage[byteOffset] = p_resultImage[byteOffset + 1] = 
                        p_resultImage[byteOffset + 2] = (byte)blueTot;
                    //p_resultImage[byteOffset + 1] = (byte)greenTot;
                    //p_resultImage[byteOffset + 2] = (byte)redTot;
                    p_resultImage[byteOffset + 3] = 255;

                    if (blueX == 0)
                        p_angleMap[y, x] = 90f;
                    else
                        p_angleMap[y, x] = (float)Math.Abs(Math.Atan(blueY / blueX) * 180 / pi);
                }
        }
       
        /// <summary>
        /// This removes the pixels that arent edges
        /// </summary>
        /// <param name="p_image">Gradient Image byte array</param>
        /// <param name="p_width">width in pixels of image</param>
        /// <param name="p_resultBuffer">byte array to store results</param>
        /// <param name="p_angleMap">tangents of the angles, used in Canny edge deteciton</param>
        private static void nonMaxSuppression(ref byte[] p_image, int p_width, ref byte[] p_resultBuffer, float[,] p_angleMap)
        {
            int limit = 5 / 2;
            int height = p_image.Length / (p_width * 4);
            int byteOffset, posTanOffset, negTanOffset;

            for (int y = limit; y < height - limit; ++y)
                for (int x = limit; x < p_width - limit; ++x)
                {
                    byteOffset = ((y * p_width) + x) * 4;

                    //Horizantal edge
                    if (p_angleMap[y, x] <= 22.5 || p_angleMap[y, x] > 157.5)
                    {
                        posTanOffset = byteOffset + 4;
                        negTanOffset = byteOffset - 4;
                        if (p_image[byteOffset] < p_image[posTanOffset] || 
                            p_image[byteOffset] < p_image[negTanOffset])
                        {
                            p_resultBuffer[byteOffset] = 
                                p_resultBuffer[byteOffset + 1] =
                                p_resultBuffer[byteOffset + 2] = 0;
                            p_resultBuffer[byteOffset + 3] = 255;
                        }
                    }
                    //+45 degree edge
                    else if (p_angleMap[y, x] <= 67.5)
                    {
                        posTanOffset = byteOffset + (p_width - 1) * 4;
                        negTanOffset = byteOffset - (p_width - 1) * 4;
                        if (p_image[byteOffset] < p_image[posTanOffset] ||
                            p_image[byteOffset] < p_image[negTanOffset])
                        {
                            p_resultBuffer[byteOffset] = 
                                p_resultBuffer[byteOffset + 1] =
                                p_resultBuffer[byteOffset + 2] = 0;
                            p_resultBuffer[byteOffset + 3] = 255;
                        }
                    }
                    //Vertical edge
                    else if (p_angleMap[y, x] <= 112.5)
                    {
                        posTanOffset = byteOffset + p_width * 4;
                        negTanOffset = byteOffset - p_width * 4;
                        if (p_image[byteOffset] < p_image[posTanOffset] ||
                            p_image[byteOffset] < p_image[negTanOffset])
                        {
                            p_resultBuffer[byteOffset] = 
                                p_resultBuffer[byteOffset + 1] =
                                p_resultBuffer[byteOffset + 2] = 0;
                            p_resultBuffer[byteOffset + 3] = 255;
                        }
                    }
                    //-45 degree edge
                    else
                    {
                        posTanOffset = byteOffset + (p_width + 1) * 4;
                        negTanOffset = byteOffset - (p_width + 1) * 4;
                        if (p_image[byteOffset] < p_image[posTanOffset] ||
                            p_image[byteOffset] < p_image[negTanOffset])
                        {
                            p_resultBuffer[byteOffset] = 
                                p_resultBuffer[byteOffset + 1] =
                                p_resultBuffer[byteOffset + 2] = 0;
                            p_resultBuffer[byteOffset + 3] = 255;
                        }
                    }
                }
        }
    }
}
