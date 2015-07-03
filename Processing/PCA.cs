using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
            List<Point> dataPoints = ((imageData)p_imgData).Datapoints;

            //Step 2 of PCA Step 1 is gathering data.
            PCAData pcaData = getMeanValues(dataPoints, ((imageData)p_imgData).Filter);
            
            //The point will be null if there were no points to observe
            if (pcaData != null)
            {
                //Readjust center
                ((imageData)p_imgData).Filter = new Rectangle((int)(pcaData.XBar - ((imageData)p_imgData).Filter.Width),
                                                              (int)(pcaData.YBar - ((imageData)p_imgData).Filter.Height),
                                                              ((imageData)p_imgData).Filter.Width,
                                                              ((imageData)p_imgData).Filter.Height);
                //step 3 develop the coVariance matrix
                genCoVarMatrix(dataPoints, pcaData);

                //step 4 get eigenvectors and values
                calculateEigens(pcaData);
            }

            //todo replace with method that draws angles
            byte[] buffer;
            BitmapData data = lockBitmap(out buffer, ((imageData)p_imgData).Image);
            drawCenter(buffer, data, new Point(((imageData)p_imgData).Filter.X + ((imageData)p_imgData).Filter.Width,
                                               ((imageData)p_imgData).Filter.Y + ((imageData)p_imgData).Filter.Height));
            unlockBitmap(ref buffer, ref data, ((imageData)p_imgData).Image);
            Processing.getInstance().ToGesturesImage = (imageData)p_imgData;
        }

        /// <summary>
        /// Gather info about the dataPoints
        /// </summary>
        /// <param name="p_dataPoints">dataset to evaluate</param>
        private PCAData getMeanValues(List<Point> p_dataPoints, Rectangle p_filter)
        {
            if (p_dataPoints.Count == 0)
                return null;

            PCAData pcaData = new PCAData();

            //Populate data members of the PCAData object
            /*Parallel.ForEach(p_dataPoints, point =>
                {
                    pcaData.Sumx += point.X;
                    pcaData.Sumy += point.Y;
                });
            */
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
        private void genCoVarMatrix(List<Point> p_dataPoints, PCAData p_pcaData)
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
                p_pcaData.XVar = (point.X - p_pcaData.XBar) * (point.X - p_pcaData.XBar);
                p_pcaData.YVar = (point.Y - p_pcaData.YBar) * (point.Y - p_pcaData.YBar);
                p_pcaData.CoVar = (point.X - p_pcaData.XBar) * (point.Y - p_pcaData.YBar);
            }

            //perform the (n-1) division
            p_pcaData.XVar /= (p_pcaData.N - 1);
            p_pcaData.YVar /= (p_pcaData.N - 1);
            p_pcaData.CoVar /= (p_pcaData.N - 1);

            /*
             * var(x),      coVar(x, y)
             * coVar(y, x), var(y) 
             * */
            //populate covar matrix
            p_pcaData.coVarMatrix[0, 0] = p_pcaData.XVar;
            p_pcaData.coVarMatrix[0, 1] = p_pcaData.coVarMatrix[1, 0] = p_pcaData.CoVar;
            p_pcaData.coVarMatrix[1, 1] = p_pcaData.YVar;
        }

        /// <summary>
        /// This function first find the Eigen Values using discriminants
        /// 
        /// </summary>
        /// <param name="p_pcaData"></param>
        private void calculateEigens(PCAData p_pcaData)
        {
            /* let Oab equal covariance of ab so Oaa would be the variance
             * This is after the A - lambda*I 
             *  Oxx - lambda, Oxy
             *  Oxy,          Oyy - lambda
             *  find the determinant
             *  
             * lambda^2 -(Oyy - Oxx)lambda - (Oxy^2 -Oxx * Oyy)
             */

            //The following performs the quadratic formula on the above function
            double diffVar = p_pcaData.XVar - p_pcaData.YVar;
            double discriminant = Math.Sqrt(diffVar * diffVar -
                                            4 * (p_pcaData.CoVar * p_pcaData.CoVar - p_pcaData.XVar * p_pcaData.YVar));
            double lambdaPlus = (-diffVar + discriminant) / 2;
            double lambdaMinus = (-diffVar - discriminant) / 2;

            p_pcaData.eigenValues[0] = lambdaMinus;
            p_pcaData.eigenValues[1] = lambdaPlus;

            //Now we have the eigen values time to get the eigenvectors that match them
            /*
             *  Oxx - lambda, Oxy
             *  Oxy,          Oyy - lambda
             * 
             * convert to  RRE format
             * 
             * 1, 0 : -Oxy / (Oxx - lambda)
             * 0, 1 : -Oxy / (Oyy - lambda)
             */
            p_pcaData.eigenVectors[0, 0] = -p_pcaData.CoVar / (p_pcaData.XVar - p_pcaData.eigenValues[0]);
            p_pcaData.eigenVectors[0, 1] = -p_pcaData.CoVar / (p_pcaData.YVar - p_pcaData.eigenValues[0]);

            p_pcaData.eigenVectors[1, 0] = -p_pcaData.CoVar / (p_pcaData.XVar - p_pcaData.eigenValues[1]);
            p_pcaData.eigenVectors[1, 1] = -p_pcaData.CoVar / (p_pcaData.YVar - p_pcaData.eigenValues[1]);
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
            private float m_xvar;
            private float m_yvar;
            private float m_covar;

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
            public float XVar {
                get { return m_xvar; }
                set { m_xvar = value; }
            }
            public float YVar {
                get { return m_yvar; }
                set { m_yvar = value; }
            }
            public float CoVar {
                get { return m_covar; }
                set { m_covar = value; }
            }

            public double[,] coVarMatrix = new double[2, 2];

            public double[] eigenValues = new double[2];
            public double[,] eigenVectors = new double[2, 2];
        }
    }

}
