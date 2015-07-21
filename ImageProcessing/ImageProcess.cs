using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing
{
    public class ImageProcess
    {
        //Sobel filters
        private static int[,] m_xFilter = {{-1, 0, 1},
                                           {-2, 0, 2},
                                           {-1, 0, 1}};

        private static int[,] m_yFilter = {{-1, -2, -1},
                                           { 0,  0,  0},
                                           { 1,  2,  1}};

        #region Edge Detection
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
            BitmapData data = BitmapManip.lockBitmap(out pixelBuffer, edgeImage);
            convert2GreyScale(ref pixelBuffer);

            //step 1 blur image
            blurImage(ref pixelBuffer, edgeImage.Width, ref resultBuffer);

            //TODO: comment this out and replace with this (pixelBuffer = resultBuffer;)
            //Check the images as I go strictly for testing
            BitmapManip.unlockBitmap(ref resultBuffer, ref data, edgeImage);
            edgeImage.Save("SmoothedImage.bmp");
            data = BitmapManip.lockBitmap(out pixelBuffer, edgeImage);

            //step 2 apply sobel filters to find gradients
            float[,] angleMap = new float[edgeImage.Height, edgeImage.Width];
            findGradients(ref pixelBuffer, edgeImage.Width, ref resultBuffer, ref angleMap);

            //TODO: comment this out and replace with this (pixelBuffer = resultBuffer;)
            BitmapManip.unlockBitmap(ref resultBuffer, ref data, edgeImage);
            edgeImage.Save("FilteredImage.bmp");
            data = BitmapManip.lockBitmap(out pixelBuffer, edgeImage);

            //step 3 clear out all non local maximum values
            nonMaxSuppression(ref pixelBuffer, edgeImage.Width, ref resultBuffer, angleMap);

            //TODO: comment this out and replace with this (pixelBuffer = resultBuffer;)
            BitmapManip.unlockBitmap(ref resultBuffer, ref data, edgeImage);
            edgeImage.Save("NonMaxSupressImage.bmp");
            data = BitmapManip.lockBitmap(out pixelBuffer, edgeImage);

            //Step 4 dual edge thresholding
            thresholding(ref pixelBuffer, edgeImage.Width, ref resultBuffer);

            //thresholding doens't give a human recognizable output
            BitmapManip.unlockBitmap(ref resultBuffer, ref data, edgeImage);
            data = BitmapManip.lockBitmap(out pixelBuffer, edgeImage);

            //Step 5 HysteresisThresholding
            hysterisisThresholding(ref pixelBuffer, edgeImage.Width, ref resultBuffer);

            //Restore byte array to image
            BitmapManip.unlockBitmap(ref resultBuffer, ref data, edgeImage);
            edgeImage.Save("Thresholding.bmp");

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
                /*rgb = pixelBuffer[i] * 0.11f;
                rgb += pixelBuffer[i + 1] * 0.59f;
                rgb += pixelBuffer[i + 2] * .2f;*/

                rgb = pixelBuffer[i] * 0.3f;
                rgb += pixelBuffer[i + 1] * 0.3f;
                rgb += pixelBuffer[i + 2] * .3f;

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
            double pi = (double)Math.PI;

            //kernel is temporary to find precise 
            double[,] kernel = new double[p_sizeOfKernel, p_sizeOfKernel];
            p_gKernel = new int[p_sizeOfKernel, p_sizeOfKernel];

            double D2 = p_sigma * p_sigma * 2;
            double D1 = 1 / (pi * D2);

            int sum, mult;
            double min = 1000; //initialize min to something large
            int upper = p_sizeOfKernel / 2;
            int lower = -upper;

            //Find min value
            for (int i = lower; i <= upper; ++i)
                for (int j = lower; j <= upper; ++j)
                {
                    kernel[upper + i, upper + j] = ((1 / D1) * (double)Math.Exp(
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
            int limit = m_xFilter.GetLength(0) / 2;
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
            int limit = m_xFilter.GetLength(0) / 2; //Because my sobel filter is 3 wide
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

        /// <summary>
        /// This is a rather simple process of only populating the result buffer with values that exceed 
        /// 
        /// </summary>
        /// <param name="p_image"></param>
        /// <param name="p_width"></param>
        /// <param name="p_resultBuffer"></param>
        private static void thresholding(ref byte[] p_image, int p_width, ref byte[] p_resultBuffer)
        {
            int limit = m_xFilter.GetLength(0) / 2;
            int height = p_image.Length / (p_width * 4);
            int byteOffset;

            //Determine the thresholds
            float maxThreshold = otsuThreshold(p_image) * 0.4f;  //Otsu's algorithm cut out too much
            float minThreshold = 0.5f * maxThreshold;

            //Dual thresholding
            for (int y = limit; y < height - limit; ++y)
                for (int x = limit; x < p_width - limit; ++x)
                {
                    byteOffset = ((y * p_width) + x) * 4;
                    
                    //set flags for strong and weak edges, 1 and 2 repectively
                    if (p_image[byteOffset] >= maxThreshold)
                        p_resultBuffer[byteOffset] = 1;

                    else if (p_image[byteOffset] >= minThreshold)
                        p_resultBuffer[byteOffset] = 2;

                    else
                        p_resultBuffer[byteOffset] = 0;
                }
        }

        /// <summary>
        /// Otsu wanted a dynamic means of finding a good threshold in image processing
        /// I can only barely grasp what this actually does so the comments are lacking
        /// </summary>
        /// <seealso cref="https://en.wikipedia.org/wiki/Otsu%27s_method"/>
        /// <param name="p_image">Image to evaluate</param>
        /// <returns></returns>
        private static float otsuThreshold(byte[] p_image)
        {
            int total = p_image.Length / 4; //total is pixels in the image
            int[] histogram = new int[256]; //a byte only has 256 values

            //populate histogram
            for (int i = 0; i < total; ++i)
            {
                //increment the histogram for each pixel value
                //I use i * 4 because the compiler will convert it to i << 2 which is 
                // faster than adding 4 every iteration
                ++histogram[p_image[i * 4]];
            }

            int omegaB, omegaF, sum, sumB;
            float mewB, mewF, max, threshold1, threshold2, between;

            omegaB = omegaF = sum = sumB = 0;
            mewB = mewF = max = threshold1 = threshold2 = between = 0;

            //Populate a sum, this is multiplied by i for uniqueness
            for (int i = 0; i < histogram.Length; ++i)
                sum += i * histogram[i];

            for (int i = 0; i < histogram.Length; ++i)
            {
                //add the current value to omegaB
                omegaB += histogram[i];
                if (omegaB == 0)
                    continue;

                //omegaF is the complement of omegaB
                omegaF = total - omegaB;
                if (omegaF == 0)
                    break;

                //SumB uses the same process as sum to track progress
                sumB += i * histogram[i];
                mewB = sumB / omegaB;
                mewF = (sum - sumB) / omegaF;

                //From here down I'm lost
                between = omegaB * omegaF * (mewB - mewF) * (mewB - mewF);

                if (between >= max)
                {
                    threshold1 = i;
                    if (between > max)
                        threshold2 = i;

                    max = between;
                }
            }

            return (threshold1 + threshold2) / 2;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p_image"></param>
        /// <param name="p_width"></param>
        /// <param name="p_resultBuffer"></param>
        private static void hysterisisThresholding(ref byte[] p_image, int p_width, ref byte[] p_resultBuffer)
        {
            int limit = m_xFilter.GetLength(0) / 2;
            int height = p_image.Length / (p_width * 4);
            int byteOffset;

            int[,] edgeMap = new int[height, p_width];
            int[,] resultMap = new int[height, p_width];
            int[,] visitedMap = new int[height, p_width];

            //populate edgemap
            for (int y = limit; y < height - limit; ++y)
                for (int x = limit; x < p_width - limit; ++x)
                {
                    byteOffset = ((y * p_width) + x) * 4;

                    if (p_image[byteOffset] == 1)
                        edgeMap[y, x] = 1;
                    else if (p_image[byteOffset] == 2)
                        edgeMap[y, x] = 2;
                }

            resultMap = edgeMap;

            //Perform traversal process
            for (int y = limit; y < height - limit; ++y)
                for (int x = limit; x < p_width - limit; ++x)
                {
                    byteOffset = ((y * p_width) + x) * 4;

                    if (p_image[byteOffset] == 1)
                    {
                        traverse(x, y, ref visitedMap, ref edgeMap, ref resultMap);
                        visitedMap[y, x] = 1;
                    }
                }

            //Write results to resultbuffer
            for (int y = limit; y < height - limit; ++y)
                for (int x = limit; x < p_width - limit; ++x)
                {
                    byteOffset = ((y * p_width) + x) * 4;
                    
                    if (resultMap[y, x] == 1)
                    {
                        p_resultBuffer[byteOffset] = p_resultBuffer[byteOffset + 1] =
                            p_resultBuffer[byteOffset + 2] = 255;
                    }
                    else
                    {
                        p_resultBuffer[byteOffset] = p_resultBuffer[byteOffset + 1] =
                            p_resultBuffer[byteOffset + 2] = 0;
                    }

                    p_resultBuffer[byteOffset + 3] = 255;
                }
        }

        /// <summary>
        /// A recursive walking algorithm to ensure all weak edges are attached
        ///  to strong edges
        /// </summary>
        /// <param name="X">X position</param>
        /// <param name="Y">Y position</param>
        /// <param name="visitedMap">Map to determine if the traversal has visited this square</param>
        /// <param name="p_width">width of image in pixels</param>
        private static void traverse(int X, int Y, ref int[,] visitedMap, ref int[,] edgeMap, ref int[,] resultMap)
        {
            if (visitedMap[Y, X] == 1)
                return;

            //1
            if (edgeMap[Y + 1, X] == 2)
            {
                resultMap[Y + 1, X] = 1;
                visitedMap[Y + 1, X] = 1;
                traverse(X, Y + 1, ref visitedMap, ref edgeMap, ref resultMap);
                return;
            }

            //2
            if (edgeMap[Y + 1, X - 1] == 2)
            {
                resultMap[Y + 1, X - 1] = 1;
                visitedMap[Y + 1, X - 1] = 1;
                traverse(X - 1, Y + 1, ref visitedMap, ref edgeMap, ref resultMap);
                return;
            }

            //3
            if (edgeMap[Y, X - 1] == 2)
            {
                resultMap[Y, X - 1] = 1;
                visitedMap[Y, X - 1] = 1;
                traverse(X - 1, Y, ref visitedMap, ref edgeMap, ref resultMap);
                return;
            }

            //4
            if (edgeMap[Y - 1, X - 1] == 2)
            {
                resultMap[Y - 1, X - 1] = 1;
                visitedMap[Y - 1, X - 1] = 1;
                traverse(X - 1, Y - 1, ref visitedMap, ref edgeMap, ref resultMap);
                return;
            }

            //5
            if (edgeMap[Y - 1, X] == 2)
            {
                resultMap[Y - 1, X] = 1;
                visitedMap[Y - 1, X] = 1;
                traverse(X, Y - 1, ref visitedMap, ref edgeMap, ref resultMap);
                return;
            }

            //6
            if (edgeMap[Y - 1, X + 1] == 2)
            {
                resultMap[Y - 1, X + 1] = 1;
                visitedMap[Y - 1, X + 1] = 1;
                traverse(X + 1, Y - 1, ref visitedMap, ref edgeMap, ref resultMap);
                return;
            }

            //7
            if (edgeMap[Y, X + 1] == 2)
            {
                resultMap[Y, X + 1] = 1;
                visitedMap[Y, X + 1] = 1;
                traverse(X + 1, Y, ref visitedMap, ref edgeMap, ref resultMap);
                return;
            }

            //8
            if (edgeMap[Y + 1, X -+ 1] == 2)
            {
                resultMap[Y + 1, X + 1] = 1;
                visitedMap[Y + 1, X + 1] = 1;
                traverse(X + 1, Y + 1, ref visitedMap, ref edgeMap, ref resultMap);
                return;
            }
        }
        #endregion

        #region Convex Hull
        /// <summary>
        /// Using Liu and Chen's Convex shell algorithm to quicly find the outside points of the hand
        /// </summary>
        /// <param name="p_dataPoints"></param>
        /// <returns>List of hull points</returns>
        /// <seealso cref="http://www.codeproject.com/Articles/775753/A-Convex-Hull-Algorithm-and-its-implementation-in"/>
        public static List<Point> getConvexHull(List<Point> p_dataPoints)
        {
            ConvexHullObject cho = new ConvexHullObject();
            
            //get root points
            getRoots(p_dataPoints, ref cho);

            //Get the shell in each quadrant
            getShell(p_dataPoints, cho);

            return connectTheDots(cho);
        }

        /// <summary>
        /// This part is to find the vertex of the extreme points thus creating 4 areas with shell points.
        /// </summary>
        /// <param name="p_dataPoints">data points to search</param>
        /// <param name="p_cho">object that stores data</param>
        private static void getRoots(List<Point> p_dataPoints, ref ConvexHullObject p_cho)
        {
            int xMin, yMin, xMax, yMax;
            xMax = yMax = 0;
            xMin = yMin = Int32.MaxValue;
            foreach (Point point in p_dataPoints)
            {
                #region Setting Left Roots
                //If this is the leftmost point set both roots Y to its Y
                if (point.X < xMin)
                {
                    //I'm worried about them having references to each other 
                    // becuase they are not always the same
                    p_cho.m_Q2.xPoint = point;
                    p_cho.m_Q3.xPoint = point;
                    
                    xMin = point.X;
                }
                //If the X point equals the min set the Y values to the extremes
                else if (point.X == xMin)
                {
                    if (point.Y < p_cho.m_Q2.xPoint.Y)
                    {
                        p_cho.m_Q2.xPoint = point;
                    }
                    else if (point.Y > p_cho.m_Q3.xPoint.Y)
                    {
                        p_cho.m_Q3.xPoint = point;
                    }
                }
                #endregion
                #region Setting Right Roots
                //If this is the rightmost point set both roots Y to its Y
                else if (point.X > xMax)
                {
                    p_cho.m_Q1.xPoint = point;
                    p_cho.m_Q4.xPoint = point;

                    xMax = point.X;
                }
                //If the X point equals the max, set the Y values to the extremes
                else if (point.X == xMax)
                {
                    if (point.Y < p_cho.m_Q1.xPoint.Y)
                        p_cho.m_Q1.xPoint = point;
                    else if (point.Y > p_cho.m_Q4.xPoint.Y)
                        p_cho.m_Q4.xPoint = point;
                }
                #endregion
                #region Setting Top Roots
                //If this is the topmost point, set both roots X to its X
                if (point.Y < yMin)
                {
                    p_cho.m_Q1.yPoint = point;
                    p_cho.m_Q2.yPoint = point;
                    yMin = point.Y;
                }
                //If the Y point equals the min, set the X values to the extremes
                else if (point.Y == yMin)
                {
                    if (point.X < p_cho.m_Q2.yPoint.X)
                        p_cho.m_Q2.yPoint = point;
                    else if (point.X > p_cho.m_Q1.yPoint.X)
                        p_cho.m_Q1.yPoint = point;
                }
                #endregion
                #region Setting Bottom Roots
                //If this is the bottommost point, set both roots X to its X
                else if (point.Y > yMax)
                {
                    p_cho.m_Q3.yPoint = point;
                    p_cho.m_Q4.yPoint = point;
                    yMax = point.Y;
                }
                //If the Y point equals the max, set the X values to the extremes
                else if (point.Y == yMax)
                {
                    if (point.X < p_cho.m_Q3.yPoint.X)
                        p_cho.m_Q3.yPoint = point;
                    else if (point.X > p_cho.m_Q4.yPoint.X)
                        p_cho.m_Q4.yPoint = point;
                }
                #endregion
            }
        }

        /// <summary>
        /// prepare the data and send it off to be worked on in parallel
        /// </summary>
        /// <param name="p_dataPoints"></param>
        /// <param name="p_cho"></param>
        private static void getShell(List<Point> p_dataPoints, ConvexHullObject p_cho)
        {
            List<Point> ySortedList = p_dataPoints.OrderBy(p => p.Y).ThenBy(p => p.X).ToList();
            List<Point> xSortedList = p_dataPoints.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
            Parallel.Invoke(
                () =>
                {
                    p_cho.m_Q1.evaluate(ySortedList);
                },
                () =>
                {
                    p_cho.m_Q2.evaluate(ySortedList);
                },
                () => 
                {
                    p_cho.m_Q3.evaluate(xSortedList);
                },
                () =>
                {
                    p_cho.m_Q4.evaluate(xSortedList);
                });
        }

        /// <summary>
        /// Takes all the hull points and strings them together
        /// </summary>
        /// <param name="p_cho"></param>
        /// <returns></returns>
        private static List<Point> connectTheDots(ConvexHullObject p_cho)
        {
            List<Point> hullPoints = new List<Point>();

            //add all of Q1
            hullPoints.AddRange(p_cho.m_Q1.hullPoints);
            //remove duplicates
            if (hullPoints.Last().Equals(p_cho.m_Q4.hullPoints.First()))
                hullPoints.RemoveAt(hullPoints.Count - 1);

            hullPoints.AddRange(p_cho.m_Q4.hullPoints);
            if (hullPoints.Last().Equals(p_cho.m_Q3.hullPoints.First()))
                hullPoints.RemoveAt(hullPoints.Count - 1);

            hullPoints.AddRange(p_cho.m_Q3.hullPoints);
            if (hullPoints.Last().Equals(p_cho.m_Q2.hullPoints.First()))
                hullPoints.RemoveAt(hullPoints.Count - 1);

            //rinse and repeat
            hullPoints.AddRange(p_cho.m_Q2.hullPoints);
            if (hullPoints.Last().Equals(hullPoints.First()))
                hullPoints.RemoveAt(hullPoints.Count - 1);

            return hullPoints;
        }
        #endregion

        private static int M_REDUCTION = 2;

        #region Convex Defects
        /// <summary>
        /// Using snakes from each line of the hull towards the center to find
        ///  the convex defects 
        /// </summary>
        /// <param name="p_convexHull">The hull around the datapoints</param>
        /// <param name="p_interiorPoints"></param>
        /// <param name="p_size"></param>
        /// <returns></returns>
        public static List<Point> getConvexDefects(List<Point> p_convexHull, List<Point> p_interiorPoints,
                                                   Size p_size)
        {
            //Create a task list
            Task<SnakeResults>[] tasks = new Task<SnakeResults>[p_convexHull.Count];

            //Populate the snake navigation map
            byte[,] snakeMap = new byte[p_size.Height / M_REDUCTION, p_size.Width / M_REDUCTION];
            populateSnakeMap(ref snakeMap, p_interiorPoints);

            Point[] reducedConvexHull = p_convexHull.ToArray();
            Point start;
            Point end;

            for (int i = 0; i < p_convexHull.Count; ++i)
            {
                start = new Point(reducedConvexHull[i].X /= M_REDUCTION,
                                  reducedConvexHull[i].Y /= M_REDUCTION);
                end = new Point(reducedConvexHull[(i + 1) % p_convexHull.Count].X /= M_REDUCTION,
                                reducedConvexHull[(i + 1) % p_convexHull.Count].Y /= M_REDUCTION);

                //Start a task for each line in the hull to find 
                tasks[i] = Task.Factory.StartNew(() => runSnakes(start, end, snakeMap));
            }

            //Wait for the tasks to finish
            Task.WaitAll(tasks); 

            //return a list of the points returned
            return organizePoints(tasks.Select(x => x.Result).ToList());
        }

        /// <summary>
        /// Populates the navigation map for the snakes.
        /// </summary>
        /// <param name="p_snakeMap">The map to be populated</param>
        /// <param name="p_interiorPoints">Points to reduce into snake map</param>
        private static void populateSnakeMap(ref byte[,] p_snakeMap, List<Point> p_interiorPoints)
        {
            //put all the points into an array, reducing the size for speed and accuracy
            foreach (Point point in p_interiorPoints)
                p_snakeMap[point.Y / M_REDUCTION, point.X / M_REDUCTION] = 1;
        }

        /// <summary>
        /// Gradually moves points of the snake into the defect areas of the hull
        /// </summary>
        /// <param name="p_point1">Starting point</param>
        /// <param name="p_point2">Ending point</param>
        /// <returns></returns>
        private static SnakeResults runSnakes(Point p_point1, Point p_point2, byte[,] p_snakeMap)
        {
            Snake snake = new Snake(p_point1, p_point2, p_snakeMap);
            return snake.getResults();
        }

        private static List<Point> organizePoints(List<SnakeResults> list)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
