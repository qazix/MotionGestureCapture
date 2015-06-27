using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        protected override void doWork(Object p_imgData)
        {
            Processing.getInstance().ToGesturesImage = (imageData)p_imgData;
        }
    }

}
