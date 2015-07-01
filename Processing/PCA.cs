using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
        protected override async void doWork(Object p_imgData)
        {
            List<Point> dataPoints = ((imageData)p_imgData).Datapoints;

            PCAData pcaData = centerData(dataPoints, ((imageData)p_imgData).Filter);
            
            if (pcaData != null)
                processPCA(pcaData);

            Processing.getInstance().ToGesturesImage = (imageData)p_imgData;
        }

        /// <summary>
        /// Gather info about the dataPoints
        /// </summary>
        /// <param name="p_dataPoints">Evaluate</param>
        private PCAData centerData(List<Point> p_dataPoints, Rectangle p_filter)
        {
            if (p_dataPoints.Count == 0)
                return null;

            PCAData pcaData = new PCAData();
            int xOffset, yOffset;
            xOffset = p_filter.X + p_filter.Width / 2;
            yOffset = p_filter.Y + p_filter.Height / 2;

            //Populate volatile data members of the PCAData object
            Parallel.ForEach(p_dataPoints, point =>
                {
                    int x = point.X - xOffset;
                    int y = point.Y - yOffset;

                    Interlocked.MemoryBarrier();
                    pcaData.Sumx += x;
                    pcaData.Sumy += y;
                    pcaData.Sumxx += x * x;
                    pcaData.Sumyy += y * y;
                    pcaData.Sumxy += x * y;
                });

            pcaData.N = p_dataPoints.Count;
            pcaData.XBar = pcaData.Sumx / pcaData.N;
            pcaData.YBar = pcaData.Sumy / pcaData.N;

            return pcaData;
        }

        private void processPCA(PCAData p_pcaData)
        {
            //variance and covariance
            p_pcaData.XVar = p_pcaData.Sumxx / p_pcaData.N - p_pcaData.XBar * p_pcaData.XBar;
            p_pcaData.YVar = p_pcaData.Sumyy / p_pcaData.N - p_pcaData.YBar * p_pcaData.YBar;
            p_pcaData.CoVarXY = p_pcaData.Sumxy / p_pcaData.N - p_pcaData.XBar * p_pcaData.YBar;

            p_pcaData.SumVars = p_pcaData.XVar + p_pcaData.YVar;
            p_pcaData.diffVars = p_pcaData.XVar - p_pcaData.YVar;
            p_pcaData.Discriminant = p_pcaData.diffVars * p_pcaData.diffVars + 4 * p_pcaData.CoVarXY * p_pcaData.CoVarXY;
            p_pcaData.SqrtDisc = Math.Sqrt(p_pcaData.Discriminant);

            //Eigenvalues
            p_pcaData.LambdaPlus = (p_pcaData.SumVars + p_pcaData.SqrtDisc) / 2;
            p_pcaData.LambdaMinus = (p_pcaData.SumVars - p_pcaData.SqrtDisc) / 2;

            //Eigenvectors  
            p_pcaData.APlus = p_pcaData.XVar + p_pcaData.CoVarXY - p_pcaData.LambdaMinus;
            p_pcaData.BPlus = p_pcaData.YVar + p_pcaData.CoVarXY - p_pcaData.LambdaMinus;

            p_pcaData.AMinus = p_pcaData.XVar + p_pcaData.CoVarXY - p_pcaData.LambdaPlus;
            p_pcaData.BMinus = p_pcaData.YVar + p_pcaData.CoVarXY - p_pcaData.LambdaPlus;

            //Normalize the vectors
            double aParallel, bParallel, aNormal, bNormal;

            double denomPlus = Math.Sqrt(p_pcaData.APlus * p_pcaData.APlus + p_pcaData.BPlus * p_pcaData.BPlus);
            double denomMinus = Math.Sqrt(p_pcaData.AMinus * p_pcaData.AMinus + p_pcaData.BMinus * p_pcaData.BMinus);

            aParallel = p_pcaData.APlus / denomPlus;
            bParallel = p_pcaData.BPlus / denomPlus;
            aNormal = p_pcaData.AMinus / denomMinus;
            bNormal = p_pcaData.BMinus / denomMinus;

            // Semi axes
            double k = 2;
            double majoraxis = k * Math.Sqrt(p_pcaData.LambdaPlus);
            double minoraxis = k * Math.Sqrt(p_pcaData.LambdaMinus);
        }

        private class PCAData
        {
            public int N { get; set; }
            public long Sumx { get; set; }
            public long Sumy { get; set; }
            public long Sumxx { get; set; }
            public long Sumyy { get; set; }
            public long Sumxy { get; set; }
            public double XBar { get; set; }
            public double YBar { get; set; }
            public double XVar { get; set; }
            public double YVar { get; set; }
            public double CoVarXY { get; set; }
            public double SumVars { get; set; }
            public double diffVars { get; set; }
            public double Discriminant { get; set; }
            public double SqrtDisc { get; set; }
            public double LambdaPlus { get; set; }
            public double LambdaMinus { get; set; }
            public double APlus { get; set; }
            public double BPlus { get; set; }
            public double AMinus { get; set; }
            public double BMinus { get; set; }
        }
    }

}
