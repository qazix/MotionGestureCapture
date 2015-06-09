using DirectShowLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MotionGestureCapture
{
    public class CamCapture : IDisposable
    {
        #region Member variables and accessors and mutators
        private static CamCapture m_instance = null; /* Singleton Instance */
        private static bool m_running = false; /* is the cam capturing */
        private int m_filterIndex; /* the index into the DsDevice array */
        private IGraphBuilder m_graph; /* hold the capture from a cap device */
        private IBaseFilter m_sampleGrabber; /* grabs individual samples. */
        private IMediaControl m_mediaControl; /* Display element for GUI */
        private IVideoWindow m_videoWindow; /* pictureBox control */
        private static System.Windows.Forms.PictureBox m_picBox = null; /* handl to picturebox */
        private Image m_image = null; /* Acting as a globald variable I will fix this */
        private const int WMGraphNotify = 13; /* refence number not realy sure why 13 */
        private const string NO_CAP_DEV = "There are no capture devices";
        private const string INVALID_CAP_DEV = "The selected capture device is invalid";

        /// <summary>
        /// A static means of getting the capture devices
        /// </summary>
        static public DsDevice[] CapDev
        { get {
                return DsDevice.GetDevicesOfCat(DirectShowLib.FilterCategory.VideoInputDevice);
            }
        }

        /// <summary>
        /// Getter for running status
        /// </summary
        public bool Running { get { return m_running; } }

        /// <summary>
        /// Getter and Setter for FilterIndex
        /// </summary>
        public int FilterIndex
        {
            get { return m_filterIndex; }
            set {
                if (value >= 0 && value < CapDev.Length)
                {
                    disposeInterfaces();
                    m_filterIndex = value;
                    getInterfaces();
                }
                else
                    throw new Exception(INVALID_CAP_DEV);
            }   
        }

        /// <summary>
        /// Sets the output to the chosen capture window
        /// </summary>
        public System.Windows.Forms.PictureBox CaptureWindow
        {
            set
            {
                m_picBox = value;
                setupPicBox();
            }
        }
        #endregion

        /// <summary>
        /// Initialize a CamCapture object
        /// </summary>
        private CamCapture()
        {
            if (CapDev.Length > 0)
                m_filterIndex = 0;
            else
                throw new Exception(NO_CAP_DEV);
            m_graph = null;
            m_sampleGrabber = null;
        }

        /// <summary>
        /// Sets up graph and interfaces to the graph
        /// </summary>
        private void getInterfaces()
        {
            buildGraph(CapDev[m_filterIndex]);
            m_mediaControl = m_graph as IMediaControl;
            m_videoWindow = m_graph as IVideoWindow;

            if (m_picBox != null)
            {
                setupPicBox();
            }
        }

        /// <summary>
        /// Establishes the connection between the graph and the pictureBox
        /// </summary>
        private void setupPicBox()
        {
            int hr;
            //Get handle to pictureBox
            IntPtr picBoxPtr = m_picBox.Handle;

            //Setup the notification link 
            hr = ((IMediaEventEx)m_graph).SetNotifyWindow(picBoxPtr, WMGraphNotify, IntPtr.Zero);
            DsError.ThrowExceptionForHR(hr);

            m_videoWindow.SetWindowPosition(0, 0, m_picBox.Width, m_picBox.Height);

            //Take graph window and set it to be owned by pictureBox
            hr = m_videoWindow.put_Owner(picBoxPtr);
            DsError.ThrowExceptionForHR(hr);

            //Set it as a child
            hr = m_videoWindow.put_WindowStyle(WindowStyle.Child);
            DsError.ThrowExceptionForHR(hr);
        }

        /// <summary>
        /// Dispose interface information
        /// </summary>
        private void disposeInterfaces()
        {
            m_graph = null;
            m_mediaControl = null;
            m_videoWindow = null;
        }

        /// <summary>
        /// Initializes the graph for which the capture data will be piped into
        /// </summary>
        /// <param name="p_capDev">The device to be capturesd</param>
        private void buildGraph(DsDevice p_capDev)
        {
            int hr = 0; //For error checking

            if (m_graph != null)
                m_graph = null;

            m_graph = (IGraphBuilder)new FilterGraph(); 
            IBaseFilter captureFilter; //Filter for the captureDevice
            ICaptureGraphBuilder2 pBuilder = (ICaptureGraphBuilder2)new CaptureGraphBuilder2(); //Rendering portion

            //Add the graph to the builder, like adding canvas to the stand
            hr = pBuilder.SetFiltergraph(m_graph);
            DsError.ThrowExceptionForHR(hr);

            //Initialize captureFilter with the unique identifier from capDev and add it to the graph
            captureFilter = createFilterByDevice(p_capDev);
            hr = m_graph.AddFilter(captureFilter, "CapFilter");
            DsError.ThrowExceptionForHR(hr);

            //Create a sample grabber and add it to the graph
            m_sampleGrabber = (IBaseFilter)Activator.CreateInstance(typeof(CamSampleGrabber));
            hr = m_graph.AddFilter(m_sampleGrabber, "SampleGrabber");
            DsError.ThrowExceptionForHR(hr);

            //Set the callback function for the sample grabber.  It will be CamCaptureGrabberCallBack.bufferCB()
            // this is because sampleCB only support single image getting.
            hr = ((CamSampleGrabber)m_sampleGrabber).SetCallback(new CamCaptureGrabberCallBack(), 1);
            DsError.ThrowExceptionForHR(hr);
            hr = ((ISampleGrabber)m_sampleGrabber).SetOneShot(false);
            DsError.ThrowExceptionForHR(hr);

            //Get pins
            IPin capPin = DsFindPin.ByCategory(captureFilter, PinCategory.Capture, 0);
            IPin samPin = DsFindPin.ByDirection(m_sampleGrabber, PinDirection.Input, 0);

            //Create the media type, just a video RGB24 with VideoInfo formatType
            AMMediaType media = null;
            hr = getMedia(capPin, out media);
            DsError.ThrowExceptionForHR(hr);
            media.majorType = MediaType.Video;

            hr = ((IAMStreamConfig)capPin).SetFormat(media);
            DsError.ThrowExceptionForHR(hr);
            DsUtils.FreeAMMediaType(media);
            
            //Connect capture device to the sample grabber
            hr = m_graph.Connect(capPin, samPin);
            DsError.ThrowExceptionForHR(hr);

            //Render video
            // For a filter with only an output filter (ie. m_sample) then the first two 
            // parameters are null.  The 4 and 5 parameter could not be null, however the 4th
            // is an intermediate filter which i don't want and the 5th is the sink if not defined
            // will end up being a default filter.
            hr = pBuilder.RenderStream(null, null, m_sampleGrabber, null, null);
            DsError.ThrowExceptionForHR(hr);
        }

        /// <summary>
        /// Creates a filter for the given capture device
        /// </summary>
        /// <param name="p_capDev">Capture device to create a filter for</param>
        /// <returns>Filter of the capture device</returns>
        private IBaseFilter createFilterByDevice(DsDevice p_capDev)
        {
            object obj;
            p_capDev.Mon.BindToObject(null, null, 
                                        typeof(IBaseFilter).GUID, out obj);
            return (IBaseFilter)obj;                        
        }

        /// <summary>
        /// Returns a point with the max size of capture device capture window
        /// </summary>
        /// <param name="p_capPin">The pin that we are polling</param>
        /// <returns></returns>
        private int getMedia(IPin p_capPin, out AMMediaType p_refMedia)
        {
            int hr = 0;
            int curMax = 0;

            p_refMedia = null;

            //Holds the video info
            VideoInfoHeader v = new VideoInfoHeader();

            //Enumeration of media types
            IEnumMediaTypes mediaTypeEnum;
            hr = p_capPin.EnumMediaTypes(out mediaTypeEnum);
            DsError.ThrowExceptionForHR(hr);

            //This array is size one because we will only be loading in one value at a time
            AMMediaType[] mediaTypes = new AMMediaType[1];
            IntPtr fetched = IntPtr.Zero;
            hr = mediaTypeEnum.Next(1, mediaTypes, fetched);
            DsError.ThrowExceptionForHR(hr);

            //Iterate through viable mediatypes and find the largest media
            while(fetched != null && mediaTypes[0] != null)
            {
                //Load the format pointer into v
                Marshal.PtrToStructure(mediaTypes[0].formatPtr, v);
                if (v.BmiHeader.Size != 0 && v.BmiHeader.ImageSize != 0)
                {
                    //Find the largest image with format of VideoFormat.  There will be two one with Video
                    // and another with VideoFormat2, SammpleGrabber will ony accept VideoFormat
                    if (v.BmiHeader.ImageSize > curMax && mediaTypes[0].formatType == FormatType.VideoInfo)
                    {
                        p_refMedia = mediaTypes[0];
                        curMax = v.BmiHeader.ImageSize;
                    }
                }

                hr = mediaTypeEnum.Next(1, mediaTypes, fetched);
                DsError.ThrowExceptionForHR(hr);
            }

            hr = 0;

            if (p_refMedia == null)
            {
                hr = -1;
            }

            Marshal.ReleaseComObject(mediaTypeEnum);
            
            return hr;
        }

        /// <summary>
        /// Start the preview feed
        /// </summary>
        public void start()
        {
            if (!m_running)
            {
                int hr = 0; 
                m_running = true;
                hr = m_mediaControl.Run();
                DsError.ThrowExceptionForHR(hr);
            }
            else
            {
                throw new Exception("Device is already running.");
                //m_mediaControl.Stop();
                //m_running = false;
            }
        }

        /// <summary>
        /// Stop the preview feed
        /// </summary>
        public void stop()
        {
            int hr = 0;
            if (m_running)
            {
                hr = m_mediaControl.Stop();
                DsError.ThrowExceptionForHR(hr);
                m_running = false;
            }
        }

        /// <summary>
        /// Method that will be used to grab a single image 
        /// </summary>
        /// <returns>Image from Callback</returns>
        public async Task<Image> grabImage()
        {
            Task<Image> imageTask = ((CamSampleGrabber)m_sampleGrabber).grabImg();
            m_image = await imageTask;

            return await imageTask;
        }

        /// <summary>
        /// Deallocates memory
        /// </summary>
        public void Dispose()
        {
            Marshal.ReleaseComObject(m_graph);
            m_running = false;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Singleton instantiation
        /// </summary>
        /// <returns>A running instance if one exists or create a blank one</returns>
        public static CamCapture getInstance()
        {
            if (m_instance == null)
            {
                m_instance = new CamCapture();
            }

            return m_instance;
        }

        /// <summary>
        /// The sample grabber that also handles grabbing stills
        /// </summary>
        public class CamSampleGrabber : SampleGrabber
        {
            //Handle to the CB class
            private CamCaptureGrabberCallBack m_callBackHandle = null;
            
            /// <summary>
            /// This is an asynchronous call to grab image
            /// </summary>
            /// <returns>The task awaiting the image</returns>
            public async Task<Image> grabImg()
            {
                TaskCompletionSource<Image> tcs = new TaskCompletionSource<Image>();
                CamCaptureGrabberCallBack.FrameCapHandler handler = null;

                //A lambda function that listens for the FrameCaptureComplete event
                handler = (frame) =>
                {
                    m_callBackHandle.FrameCaptureComplete -= handler;
                    tcs.SetResult(frame);
                };

                m_callBackHandle.FrameCaptureComplete += handler;
                m_callBackHandle.CaptureFrame();

                return await tcs.Task;
            }

            //Step in to capture the handler then proceed to call the ISampleGrabber.SetCallbcak
            public int SetCallback(ISampleGrabberCB pCallback, int WhichMethodToCallback)
            {
                m_callBackHandle = (CamCaptureGrabberCallBack)pCallback;
                return (this as ISampleGrabber).SetCallback((ISampleGrabberCB)pCallback, WhichMethodToCallback);
            }
        }

        /// <summary>
        /// CamCapture supports capturing stills and live feed
        /// </summary>
        class CamCaptureGrabberCallBack : ISampleGrabberCB
        {
            public delegate void FrameCapHandler(Image p_image);
            public event FrameCapHandler FrameCaptureComplete;

            public bool ToGrabImage { get; set; }
         
            //Triggers a FrameCaptureComplete event
            public Image SetBitMap { set { FrameCaptureComplete(value); } }

            //Constructor
            public CamCaptureGrabberCallBack()
            { ToGrabImage = false; }
            
            /// <summary>
            /// The callback method that allows continuous capture
            /// </summary>
            /// <param name="SampleTime">Time sample was taken</param>
            /// <param name="pBuffer">The image in buffer form</param>
            /// <param name="bufferLen">Size of the buffer</param>
            /// <returns>Error condition</returns>
            public int BufferCB(double SampleTime, IntPtr pBuffer, int bufferLen)
            {
                if (ToGrabImage)
                {
                    if (pBuffer != null && bufferLen > 0)
                    {
                        if (m_picBox != null)
                        {
                            //Copy the volatile buffer into a marshall controlled buffer
                            byte[] buf = new byte[bufferLen];
                            Marshal.Copy(pBuffer, buf, 0, bufferLen);
                            using (MemoryStream ms = new MemoryStream(buf))
                            {
                                SetBitMap = new Bitmap(ms);
                            }
                        }
                    }
                    ToGrabImage = false;
                }
                return 0; 
            }

            /// <summary>
            /// unused
            /// </summary>
            public int SampleCB(double SampleTime, IMediaSample pSample)
            { return 0; }

            /// <summary>
            /// Starts the capture frame
            /// </summary>
            public void CaptureFrame()
            {
                ToGrabImage = true;
            }

        }
    }
}
