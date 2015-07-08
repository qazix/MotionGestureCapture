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
        public delegate void ImageReadyHandler(imageData data);
        public event ImageReadyHandler IsolationImageFilled;
        public event ImageReadyHandler PCAImageFilled;
        public event ImageReadyHandler GesturesImageFilled;
        public event ImageReadyHandler ReturnImageFilled;

        #region Member Variables and Properties
        private static Processing m_instance = null;
        private CamCapture m_camCapture;

        //Hand Isolation class and handler for ready event
        private HandIsolation m_handIso;
        private HandIsolation.ProcessReadyHandler m_HIHandler;
        private PCA m_PCA;
        private Gesture m_gesture;

        public bool IsInitialized { get; set; }
        public Semaphore IsolationToPCA { get; set; }
        public Semaphore PCAToGestures { get; set; }
        public Mutex feedBackData { get; set; }

        private imageData m_toIsolationImage;

        public imageData ToIsolationImage { get { return m_toIsolationImage; } 
            set{
                m_toIsolationImage = value;
                if (IsolationImageFilled != null)
                {
                    IsolationImageFilled(value);
                }
            }
        }
        public imageData ToPCAImage { 
            set {
                IsolationToPCA.WaitOne();
                if (PCAImageFilled != null)
                {
                    PCAImageFilled(value);
                }
                else
                    ToGesturesImage = value;
            }
        }

        public imageData ToGesturesImage {
            set{
                IsolationToPCA.Release();
                PCAToGestures.WaitOne();
                if (GesturesImageFilled != null)
                    GesturesImageFilled(value);
                else
                    ToReturnImage = value;
            } 
        }

        public imageData ToReturnImage { 
            set{
                PCAToGestures.Release();
                ReturnImageFilled(value);
            } 
        }
        #endregion

        /// <summary>
        /// Singleton constructor, not sure if I will keep it this way
        /// </summary>
        private Processing()
        {
            m_camCapture = CamCapture.getInstance();
            IsolationToPCA   = new Semaphore(1, 1);
            PCAToGestures    = new Semaphore(1, 1);
            feedBackData     = new Mutex();

            m_handIso = new HandIsolation();
            m_PCA = new PCA();
            m_gesture = new Gesture();
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
            else
            {
                m_PCA.initialize();
                m_gesture.initialize();
            }

            ToIsolationImage = new imageData(true, await m_camCapture.grabImage());
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
                ToIsolationImage = new imageData(false, await m_camCapture.grabImage());
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
