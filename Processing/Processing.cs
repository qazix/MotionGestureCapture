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
        public delegate void ImageReadyHandler(ImageData data);
        public event ImageReadyHandler IsolationImageFilled;
        public event ImageReadyHandler PreprocessImageFilled;
        public event ImageReadyHandler PCAImageFilled;
        public event ImageReadyHandler GesturesImageFilled;
        public event ImageReadyHandler DrawingImageFilled;
        public event ImageReadyHandler ReturnImageFilled;

        #region Member Variables and Properties
        private static Processing m_instance = null;
        private CamCapture m_camCapture;

        //Hand Isolation class and handler for ready event
        private HandIsolation m_handIso;
        private HandIsolation.ProcessReadyHandler m_HIHandler;
        private Preprocessing m_preProc;
        private PCA m_PCA;
        private Gesture m_gesture;
        private Drawing m_drawing;

        public bool IsInitialized { get; set; }
        public Semaphore IsolationToPreprocess { get; set; }
        public Semaphore PreprocessToPCA { get; set; }
        public Semaphore PCAToGestures { get; set; }
        public Semaphore GesturesToDrawing { get; set; }
        public Mutex feedBackData { get; set; }

        private ImageData m_toIsolationImage;

        public ImageData ToIsolationImage { get { return m_toIsolationImage; } 
            set{
                m_toIsolationImage = value;
                if (IsolationImageFilled != null)
                {
                    IsolationImageFilled(value);
                }
                else
                    ToPreProcessing = value;
            }
        }

        public ImageData ToPreProcessing
        {
            set {
                IsolationToPreprocess.WaitOne();
                if (PreprocessImageFilled != null)
                    PreprocessImageFilled(value);
                else
                    ToPCAImage = value;
            }
        }
        public ImageData ToPCAImage { 
            set {
                IsolationToPreprocess.Release();
                PreprocessToPCA.WaitOne();
                if (PCAImageFilled != null)
                {
                    PCAImageFilled(value);
                }
                else
                    ToGesturesImage = value;
            }
        }

        public ImageData ToGesturesImage {
            set{
                PreprocessToPCA.Release();
                PCAToGestures.WaitOne();
                if (GesturesImageFilled != null)
                    GesturesImageFilled(value);
                else
                    ToDrawingImage = value;
            } 
        }

        public ImageData ToDrawingImage {
            set {
                PCAToGestures.Release();
                GesturesToDrawing.WaitOne();
                if (DrawingImageFilled != null)
                    DrawingImageFilled(value);
                else
                    ToReturnImage = value;
            }
        }

        public ImageData ToReturnImage { 
            set{
                GesturesToDrawing.Release();
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
            IsolationToPreprocess = new Semaphore(1, 1);
            PreprocessToPCA   = new Semaphore(1, 1);
            PCAToGestures     = new Semaphore(1, 1);
            GesturesToDrawing = new Semaphore(1, 1);
            feedBackData      = new Mutex();

            m_handIso = new HandIsolation();
            m_preProc = new Preprocessing();
            m_PCA = new PCA();
            m_gesture = new Gesture();
            m_drawing = new Drawing();
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
                m_preProc.initialize();
                m_PCA.initialize();
                m_gesture.initialize();
                m_drawing.initialize();
            }

            //ToIsolationImage = new ImageData(true, await m_camCapture.grabImage());
            ImageData id = new ImageData(true, await m_camCapture.grabImage());
            m_handIso.initialize(id);

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
            PreprocessImageFilled = null;
            PCAImageFilled = null;
            GesturesImageFilled = null;
            DrawingImageFilled = null;
        }

        /// <summary>
        /// Takes an image through the 
        /// </summary>
        public async void oneShot()
        {
            if (m_camCapture.Running)
                ToIsolationImage = new ImageData(false, await m_camCapture.grabImage());
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
