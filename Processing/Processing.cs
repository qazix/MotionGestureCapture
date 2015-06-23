using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using MotionGestureCapture;

namespace MotionGestureProcessing
{
    public class Processing
    {
        public delegate void ImageReadyHandler(object data, Image i);
        public event ImageReadyHandler IsolationImageFilled;
        public event ImageReadyHandler ReturnImageFilled;

        #region Member Variables and Properties
        private static Processing m_instance = null;
        private CamCapture m_camCapture;

        //Hand Isolation class and handler for ready event
        private HandIsolation m_handIso;
        private HandIsolation.ProcessReadyHandler m_HIHandler;

        public bool IsInitialized { get; set; }
        public Semaphore CamToIsolation { get; set; }
        public Semaphore IsolationToPCA { get; set; }
        public Semaphore PCAToGestures { get; set; }
        public Semaphore GesturesToReturn { get; set; }

        private Image m_toIsolationImage;

        public Image ToIsolationImage { get { return m_toIsolationImage; } 
            set{
                m_toIsolationImage = value;
                if (IsolationImageFilled != null)
                    IsolationImageFilled(null, value);
            }
        }
        public Image ToPCAImage { set { ToReturnImage = value; } }
        public Image ToGesturesImage { get; set; }
        public Image ToReturnImage { set{
                ReturnImageFilled(null, value);
            } 
        }
        #endregion

        /// <summary>
        /// Singleton constructor, not sure if I will keep it this way
        /// </summary>
        private Processing()
        {
            m_camCapture = CamCapture.getInstance();
            CamToIsolation   = new Semaphore(1, 1);
            IsolationToPCA   = new Semaphore(1, 1);
            PCAToGestures    = new Semaphore(1, 1);
            GesturesToReturn = new Semaphore(1, 1);

            CamToIsolation.WaitOne();
            CamToIsolation.Release();

            m_handIso = new HandIsolation();
        }

        /// <summary>
        /// Get an instance of this object
        /// </summary>
        /// <returns></returns>
        public static Processing getInstance()
        {
            if (m_instance == null)
                m_instance = new Processing();

            return m_instance;
        }

        /// <summary>
        /// Starts initialize on those processes that require initialization
        /// </summary>
        public async void initialize(bool toRun)
        {
            //If it's already initialized first stop
            if (IsInitialized)
            {
                stop();
                IsInitialized = false;
            }

            ToIsolationImage = await m_camCapture.grabImage();
            m_handIso.initialize(ToIsolationImage);

            IsInitialized = true;
            if (toRun)
                start();
        }

        /// <summary>
        /// Start constantly grabbing images
        /// </summary>
        public void start()
        {
            if (IsInitialized && ReturnImageFilled != null)
            {
                readyListener();
                oneShot();
            }
        }

        /// <summary>
        /// This just clears out the event handlers
        /// </summary>
        public void stop()
        {
            m_handIso.ProcessReady -= m_HIHandler;
            IsolationImageFilled = null;
        }

        /// <summary>
        /// Takes an image through the 
        /// </summary>
        public async void oneShot()
        {
            if (m_camCapture.Running)
                ToIsolationImage = await m_camCapture.grabImage();
        }

        /// <summary>
        /// Listens for handIsolation to be ready so that a new image can be brought in
        /// </summary>
        public void readyListener()
        {
            m_HIHandler = () =>
            {
                this.oneShot();
            };

            m_handIso.ProcessReady += m_HIHandler;
        }
    }
}
