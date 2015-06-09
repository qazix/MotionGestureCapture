using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using MotionGestureCapture;

namespace Processing
{
    class Processing
    {
        #region Member Variables and Properties
        private static Processing m_instance = null;
        private CamCapture m_camCapture;
        public Semaphore CamToIsolation { get; set; }
        public Image ToIsolationImage { get; set; }
        public Semaphore IsolationToPCA { get; set; }
        public Image ToPCAImage { get; set; }
        public Semaphore PCAToGestures { get; set; }
        public Image ToGesturesImage { get; set; }
        public Semaphore GesturesToReturn { get; set; }
        public Image ToReturnImage { get; set; }
        #endregion

        /// <summary>
        /// Singleton constructor, not sure if I will keep it this way
        /// </summary>
        private Processing()
        {
            m_camCapture = CamCapture.getInstance();
            CamToIsolation   = new Semaphore(0, 1);
            IsolationToPCA   = new Semaphore(0, 1);
            PCAToGestures    = new Semaphore(0, 1);
            GesturesToReturn = new Semaphore(0, 1);
            ToIsolationImage = null;
            ToPCAImage       = null;
            ToGesturesImage  = null;
            ToReturnImage    = null;
        }

        /// <summary>
        /// Get an instance of this object
        /// </summary>
        /// <returns></returns>
        public Processing getInstance()
        {
            if (m_instance == null)
                m_instance = new Processing();

            return m_instance;
        }

        /// <summary>
        /// Starts initialize on those processes that require initialization
        /// </summary>
        public async void Initialize()
        {
            ToIsolationImage = await m_camCapture.grabImage();
        }
    }
}
