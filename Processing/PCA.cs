using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ImageProcessing;

namespace MotionGestureProcessing
{
    class PCA : Process
    {
        private Processing.ImageReadyHandler m_PCAImageHandler;

        public PCA()
        { }

        public void initialize()
        {
            setupListener();
        }

        protected override void setupListener()
        {
            m_PCAImageHandler = (obj) =>
            {
                //Thread t = new Thread(new ParameterizedThreadStart(doWork));
                //t.Start(obj);
                doWork(obj);
            };

            Processing.getInstance().PCAImageFilled += m_PCAImageHandler;
        }

        /// <summary>
        /// What this process does to an image
        /// </summary>
        /// <param name="obj">Data pertaining to this image</param>
        /// <param name="p_image">Image to be processed</param>
        /// <seealso cref="http://www.cs.otago.ac.nz/cosc453/student_tutorials/principal_components.pdf"/>
        protected override async void doWork(Object p_imgData)
        {
            //The point will be null if there were no points to observe
            if (((ImageData)p_imgData).DataPoints.Count != 0)
            {
                //Narrow data set
                byte[] buffer;
                BitmapData data = BitmapManip.lockBitmap(out buffer, ((ImageData)p_imgData).Image);
                ((ImageData)p_imgData).Filter = cropDataSet(ref data, ref buffer);
                ((ImageData)p_imgData).DataPoints = ImageProcess.getDataPoints(ref data, ref buffer);
                ((ImageData)p_imgData).ConvexHull = ImageProcess.getConvexHull(((ImageData)p_imgData).DataPoints);
                List<Point> dataPoints = ((ImageData)p_imgData).DataPoints;

                //Step 2 of PCA Step 1 is gathering data.
                PCAData pcaData = getMeanValues(dataPoints);
         
                //step 3 develop the coVariance matrix
                genCoVarMatrix(dataPoints, ref pcaData);

                //step 4 get eigenvectors and values
                calculateEigens(ref pcaData);

                //prepare variables for drawing later
                ((ImageData)p_imgData).EigenVectors = pcaData.eigenVectors;
                ((ImageData)p_imgData).Center = new Point((int)pcaData.XBar, (int)pcaData.YBar);
                //drawOrientation(buffer, data, pcaData.eigenVectors, new Point((int)pcaData.XBar, (int)pcaData.YBar));

                BitmapManip.unlockBitmap(ref buffer, ref data, ((ImageData)p_imgData).Image);
                ((ImageData)p_imgData).DataPoints = dataPoints;
                //Adjust datapoints
                //adjustDatapoints(ref dataPoints, pcaData);

                //((imageData)p_imgData).Datapoints = dataPoints;
                ((ImageData)p_imgData).Orientation = getOrientation(pcaData.eigenVectors);
            }

            Processing.getInstance().ToGesturesImage = (ImageData)p_imgData;
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
            int dir = -1;
            //find max histogram value this represent the wrist
            for (int i = 0; i < extremeHistogram.Length; ++i)
            {
                if (extremeHistogram[i] > max)
                {
                    max = extremeHistogram[i];
                    dir = i;
                }
            }

            int wristIndex;
            //based on the direction do some stuff
            switch(dir)
            {
                case 0: //left
                    wristIndex = findWrist(left, ref xHistogram);
                    if (wristIndex == -1)
                        return new Rectangle(0, 0, p_data.Width, p_data.Height);
                    removeWrist(0, wristIndex, 0, p_data.Height, ref p_data, ref p_buffer);
                    break;
                case 1: //right
                    wristIndex = findWrist(right, ref xHistogram);
                    if (wristIndex == -1)
                        return new Rectangle(0, 0, p_data.Width, p_data.Height);
                    removeWrist(wristIndex, p_data.Width, 0, p_data.Height, ref p_data, ref p_buffer);
                    break;
                case 2: //top
                    wristIndex = findWrist(top, ref yHistogram);
                    if (wristIndex == -1)
                        return new Rectangle(0, 0, p_data.Width, p_data.Height);
                    removeWrist(0, p_data.Width, 0, wristIndex, ref p_data, ref p_buffer);
                    break;
                case 3: //bottom
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
            int end = p_index > 0 ? 0 : p_histogram.Length;
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
        /// Gather info about the dataPoints
        /// </summary>
        /// <param name="p_dataPoints">dataset to evaluate</param>
        private PCAData getMeanValues(List<Point> p_dataPoints)
        {
            if (p_dataPoints.Count == 0)
                return null;

            PCAData pcaData = new PCAData();

            //Populate data members of the PCAData object
            foreach(Point point in p_dataPoints)
            {
                pcaData.Sumx += point.X;
                pcaData.Sumy += point.Y;
            }

            pcaData.N = p_dataPoints.Count;
            pcaData.XBar = pcaData.Sumx / pcaData.N;
            pcaData.YBar = pcaData.Sumy / pcaData.N;

            return pcaData;
        }

        /// <summary>
        /// This method is designed to create a covariance matrix.
        /// </summary>
        /// <param name="p_pcaData"></param>
        private void genCoVarMatrix(List<Point> p_dataPoints, ref PCAData p_pcaData)
        {
            //Sum up the mean product
            /*Parallel.ForEach(p_dataPoints, point =>
                {
                    p_pcaData.XVar = (point.X - p_pcaData.XBar) * (point.X - p_pcaData.XBar);
                    p_pcaData.YVar = (point.Y - p_pcaData.YBar) * (point.Y - p_pcaData.YBar);
                    p_pcaData.CoVar = (point.X - p_pcaData.XBar) * (point.Y - p_pcaData.YBar);
                });*/

            foreach (Point point in p_dataPoints)
            {
                p_pcaData.XVar += (point.X - p_pcaData.XBar) * (point.X - p_pcaData.XBar);
                p_pcaData.YVar += (point.Y - p_pcaData.YBar) * (point.Y - p_pcaData.YBar);
                p_pcaData.CoVar += (point.X - p_pcaData.XBar) * (point.Y - p_pcaData.YBar);
            }

            //perform the (n-1) division
            p_pcaData.XVar /= (p_pcaData.N - 1);
            p_pcaData.YVar /= (p_pcaData.N - 1);
            p_pcaData.CoVar /= (p_pcaData.N - 1);

        }

        /// <summary>
        /// This function first find the Eigen Values using discriminants
        /// 
        /// </summary>
        /// <param name="p_pcaData"></param>
        private void calculateEigens(ref PCAData p_pcaData)
        {
            /* let Oab equal covariance of ab so Oaa would be the variance
             * This is after the A - lambda*I 
             *  Oxx - lambda, Oxy
             *  Oxy,          Oyy - lambda
             *  find the determinant
             *  
             * lambda^2 + (-Oyy - Oxx)lambda + (OxxOyy - Oxy^2)
             */

            //The following performs the quadratic formula on the above function
            double diffVar = -p_pcaData.YVar - p_pcaData.XVar;   
            double discriminant = Math.Sqrt(diffVar * diffVar -
                                            4 * (p_pcaData.XVar * p_pcaData.YVar - p_pcaData.CoVar * p_pcaData.CoVar));
            double lambdaPlus = (-diffVar + discriminant) / 2;
            double lambdaMinus = (-diffVar - discriminant) / 2;

            p_pcaData.eigenValues[0] = lambdaPlus;
            p_pcaData.eigenValues[1] = lambdaMinus;

            //Now we have the eigen values time to get the eigenvectors that match them
            /*
             *  Oxx - lambda, Oxy
             *  Oxy,          Oyy - lambda
             * 
             * convert to  RRE format
             * 
             * x, y
             * 1, 0 : -Oxy / (Oxx - lambda)
             * 0, 1 : -Oxy / (Oyy - lambda)
             */

            //for some reason when the coVar is positive my primary is correct and secondary isn't normal
            // and when it's negative secondary is correct and primary isn't normal
            if (p_pcaData.CoVar > 0)
            {
                //first principal component
                p_pcaData.eigenVectors[0, 0] = p_pcaData.CoVar / (p_pcaData.XVar - p_pcaData.eigenValues[0]);
                p_pcaData.eigenVectors[0, 1] = p_pcaData.CoVar / (p_pcaData.YVar - p_pcaData.eigenValues[0]);

                //second is normal to the first
                p_pcaData.eigenVectors[1, 0] = -p_pcaData.eigenVectors[0, 1];
                p_pcaData.eigenVectors[1, 1] = p_pcaData.eigenVectors[0, 0];
            }
            else
            {
                //second principal component
                p_pcaData.eigenVectors[1, 0] = p_pcaData.CoVar / (p_pcaData.XVar - p_pcaData.eigenValues[1]);
                p_pcaData.eigenVectors[1, 1] = p_pcaData.CoVar / (p_pcaData.YVar - p_pcaData.eigenValues[1]);

                //first is normal to second
                p_pcaData.eigenVectors[0, 0] = -p_pcaData.eigenVectors[1, 1];
                p_pcaData.eigenVectors[0, 1] = p_pcaData.eigenVectors[1, 0];
            }
            //normalize component
            //normX = x / sqrt(x*x + y*y) normY = y / sqrt(x*x + y*y)
            double len = Math.Sqrt(p_pcaData.eigenVectors[0, 0] * p_pcaData.eigenVectors[0, 0] +
                                   p_pcaData.eigenVectors[0, 1] * p_pcaData.eigenVectors[0, 1]);
            p_pcaData.eigenVectors[0, 0] /= len;
            p_pcaData.eigenVectors[0, 1] /= len;

            len = Math.Sqrt(p_pcaData.eigenVectors[1, 0] * p_pcaData.eigenVectors[1, 0] +
                            p_pcaData.eigenVectors[1, 1] * p_pcaData.eigenVectors[1, 1]);
            p_pcaData.eigenVectors[1, 0] /= len;
            p_pcaData.eigenVectors[1, 1] /= len;
        }

        /// <summary>
        /// NOT USED FOR ISSUES IN GESTURES
        /// 
        /// this transforms the original data by multiplying the eigenvectors by the mean adjusted data
        /// The eigenvalues are generally stored in a matrix like so | x1, x2 | and so must be transposed
        ///                                                          | y1, y2 | 
        /// However I've already stored it as transposed so that won't happen.  Second the point matrix doesn't exist
        /// per say, I'll explain the implementation later
        /// </summary>
        /// <param name="p_dataPoints">points to transform</param>
        /// <param name="p_pcaData">holds the eigenvectors</param>
        private void adjustDatapoints(ref List<Point> p_dataPoints, PCAData p_pcaData)
        {
            //Lists are immutable so i need to create a new list in order to work out the transform
            List<Point> transPoints = new List<Point>();

            double x1, y1, x2, y2;
            float xbar, ybar;
            x1 = p_pcaData.eigenVectors[0, 0];
            y1 = p_pcaData.eigenVectors[0, 1];
            x2 = p_pcaData.eigenVectors[1, 0];
            y2 = p_pcaData.eigenVectors[1, 1];

            xbar = p_pcaData.XBar;
            ybar = p_pcaData.YBar;

            //performs a matrix multiply 
            foreach (Point point in p_dataPoints)
            {
                Point insert = new Point();

                insert.X = (int)(x1 * (point.X - xbar) + y1 * (point.Y - ybar));
                insert.Y = (int)(x2 * (point.X - xbar) + y2 * (point.Y - ybar));

                transPoints.Add(insert);
            }

            p_dataPoints = transPoints;
        }

        /// <summary>
        /// Use the principal component to determine the orientation
        /// The top of the image represents 0 deg
        /// 
        /// This methods use the law of Cosines a^2 = b^2 + c^2 - 2bc * cos(A)
        /// b = c = 1
        /// thus 
        /// a^2 = 2 - 2 * cos(A)
        /// a^2 = (0 - x)(0 - x) + (-1--y)(-1--y)
        /// a^2 = x^2 + (y-1)(y-1)
        /// 2 * cos(A) = 2 - x^2 + (y-1)(y-1)
        /// cos(A) = (2 - x^2 + (y-1)(y-1)) / 2
        /// A = Acos((2 - x^2 + (y-1)(y-1)) / 2)
        /// </summary>
        /// <param name="p_vectors"></param>
        /// <returns></returns>
        private double getOrientation(double[,] p_vectors)
        {
            double x = p_vectors[0, 0];
            double y = p_vectors[0, 1];

            double a2 = x * x + (y + 1) * (y + 1);

            double rads = Math.Acos(1 - a2 / 2);

            //convert to degrees
            if (x > 0)
                return rads / Math.PI * 180;
            else
                return -rads / Math.PI * 180;
        }

        /// <summary>
        /// struct for PCA data.  
        /// <remarks>volatile variable can only be int or float
        /// becuase of this I end up with mixed data types.
        /// </remarks>
        /// </summary>
        private class PCAData
        {
            public int N { get; set; }
            private volatile int m_sumx;
            private volatile int m_sumy;
            private double m_xvar;
            private double m_yvar;
            private double m_covar;

            public int Sumx {
                get { return m_sumx; }
                set { m_sumx = value; }
            }
            public int Sumy {
                get { return m_sumy; }
                set { m_sumy = value; }
            }
            public float XBar { get; set; }
            public float YBar { get; set; }
            public double XVar {
                get { return m_xvar; }
                set { m_xvar = value; }
            }
            public double YVar {
                get { return m_yvar; }
                set { m_yvar = value; }
            }
            public double CoVar {
                get { return m_covar; }
                set { m_covar = value; }
            }
            
            public double[] eigenValues = new double[2];
            public double[,] eigenVectors = new double[2, 2];
        }
    }

}
