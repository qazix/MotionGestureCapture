using System;
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
    class HandIsolation
    {
        public delegate void ProcessReadyHandler();
        public event ProcessReadyHandler ProcessReady;

        private Processing.ImageReadyHandler m_isoImageHandler;
        private static HashSet<int> m_validPixels;
        private static bool m_isInitialized;

        /// <summary>
        /// Empty constructor
        /// </summary>
        public HandIsolation()
        {}

        /// <summary>
        /// First populates the bit array for values then sets up the event listener
        /// </summary>
        /// <param name="p_toInit">The initialization frame</param>
        public void initialize(Image p_toInit)
        {
            m_isInitialized = false;
            populateValidPixels(p_toInit);
            m_isInitialized = true;
            
            setupListener();
            doWork(p_toInit);
        }

        /// <summary>
        /// Will populate the bitArray
        /// </summary>
        /// <param name="p_toInit">Image to scan</param>
        private void populateValidPixels(Image p_toInit)
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

            for (int x = window.Left; x < window.Right; ++x)
                for (int y = window.Top; y < window.Bottom; ++y)
                {
                    Color color = ((Bitmap)p_toInit).GetPixel(x, y);
                    m_validPixels.Add(color.ToArgb());
                }
        }

        /// <summary>
        /// Establishes a listening connection 
        /// </summary>
        private void setupListener()
        {
            m_isoImageHandler = (obj, image) =>
            {
                this.doWork(image);
            };

            Processing.getInstance().IsolationImageFilled += m_isoImageHandler;
        }

        /// <summary>
        /// Used to pass in data to dividedDoWork for multithreaded doWork
        /// </summary>
        class workerData : Object
        {
            public Image m_imagePtr;
            public int m_quadrant;

            public workerData(Image p_image, int p_quadrant)
            {
                m_imagePtr = p_image;
                m_quadrant = p_quadrant;
            }
        }

        /// <summary>
        /// this method transforms the image into a filtered image
        /// </summary>
        /// <param name="p_image"></param>
        private void doWork(Image p_image)
        {
            if (m_isInitialized)
            {
                int width = ((Bitmap)p_image).Width;
                int height = ((Bitmap)p_image).Height;

                int curPixelColor;
                int BLACK = Color.Black.ToArgb();

                //Currently just a working concept but is to slow
                //TODO find a way to thread the updating of the image
                for (int x = 0; x < width; ++x)
                    for (int y = 0; y < height; ++y)
                    {
                        curPixelColor = ((Bitmap)p_image).GetPixel(x, y).ToArgb();
                        if (!m_validPixels.Contains(curPixelColor))
                        {
                            ((Bitmap)p_image).SetPixel(x, y, Color.FromArgb(BLACK));
                        }
                    }

                //ThreadPool.SetMaxThreads(4, 4);
                //ThreadPool.QueueUserWorkItem(new WaitCallback(dividedDoWork), new workerData(p_image, 0));


                Processing.getInstance().ToPCAImage = p_image;

                //If someone is listener raise an event
                if (ProcessReady != null)
                    ProcessReady();
            }
        }

        /// <summary>
        /// Tried to come up with a working model of a multithreaded dowork call
        /// </summary>
        /// <param name="p_workerData"></param>
        private void dividedDoWork(object p_workerData)
        {
            Bitmap p_image = (Bitmap)((workerData)p_workerData).m_imagePtr;
            int width = ((workerData)p_workerData).m_imagePtr.Width;
            int height = ((workerData)p_workerData).m_imagePtr.Height;
            int startX = ((workerData)p_workerData).m_quadrant % 2 * (width / 2);
            int startY = ((workerData)p_workerData).m_quadrant % 2 * (height / 2);
            int endX = (startX > 0 ? width : width / 2);
            int endY = (startY > 0 ? height : height / 2);

            int curPixelColor;
            int BLACK = Color.Black.ToArgb();

            for (int x = 0; x < endX; ++x)
                for (int y = startY; y < endY; ++y)
                {
                    curPixelColor = p_image.GetPixel(x, y).ToArgb();
                    if (!m_validPixels.Contains(curPixelColor))
                    {
                        p_image.SetPixel(x, y, Color.FromArgb(BLACK));
                    }
                }
        }
    }
}
