using DirectShowLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MotionGestureCapture
{
    class CamCapture : IDisposable
    {
        #region Member variables and accessors and mutators
        private static CamCapture m_instance = null; /* Singleton Instance */
        private static bool m_running = false; /* is the cam capturing */
        private int m_filterIndex; /* the index into the DsDevice array */
        private IGraphBuilder m_graph; /* hold the capture from a cap device */
        private IBaseFilter m_sampleGrabber; /* grabs individual samples. */
        private Thread m_displayThread; /* Thread that holds the loop for displaying the images */
        private IMediaControl m_mediaControl; /* Display element for GUI */

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
                    m_filterIndex = value;
            }
        }
        #endregion

        /// <summary>
        /// Initialize a CamCapture object
        /// </summary>
        private CamCapture()
        {
            m_filterIndex = -1;
            m_graph = null;
            m_sampleGrabber = null;
        }

        /// <summary>
        /// Initializes the graph for which the capture data will be piped into
        /// </summary>
        /// <param name="p_capDev">The device to be capturesd</param>
        private void buildGraph(DsDevice p_capDev)
        {
            int hr = 0; //For error checking

            IBaseFilter captureFilter; //Filter for the captureDevice
            IFilterGraph2 filtergraph = new FilterGraph() as IFilterGraph2; //Here just for initializing captureFilter
            ICaptureGraphBuilder2 pBuilder = (ICaptureGraphBuilder2)new CaptureGraphBuilder2(); //Rendering portion

            //Add the graph to the builder, like adding canvas to the stand
            hr = pBuilder.SetFiltergraph(m_graph);
            DsError.ThrowExceptionForHR(hr);

            //Initialize captureFilter with the unique identifier from capDev and add it to the graph
            filtergraph.AddSourceFilterForMoniker(p_capDev.Mon,
                                                  null, p_capDev.Name, 
                                                  out captureFilter);
            m_graph.AddFilter(captureFilter, "CapFilter");
            DsError.ThrowExceptionForHR(hr);

            //Create a sample grabber and add it to the graph
            IBaseFilter pSampleGrabber = (IBaseFilter)Activator.CreateInstance(typeof(SampleGrabber));
            hr = m_graph.AddFilter(pSampleGrabber, "SampleGrabber");
            DsError.ThrowExceptionForHR(hr);

            //Set the callback function for the sample grabber.  It will be CamCaptureGrabberCallBack.SamplbeCB()
            hr = ((ISampleGrabber)pSampleGrabber).SetCallback(new CamCaptureGrabberCallBack(), 0);
            DsError.ThrowExceptionForHR(hr);

            //Get pins
            IPin capPin = DsFindPin.ByCategory(captureFilter, PinCategory.Capture, 0);
            IPin samPin = DsFindPin.ByDirection(pSampleGrabber, PinDirection.Input, 0);

            //Create the media type, just a video RGB24 with VideoInfo formatType
            AMMediaType media = new AMMediaType();
            media.majorType = MediaType.Video;
            media.subType = MediaSubType.RGB24;
            media.formatType = FormatType.VideoInfo;

            //Set the expected format of the image detail
            VideoInfoHeader format = new VideoInfoHeader();
            format.SrcRect = new DsRect();
            format.TargetRect = new DsRect();
            format.BmiHeader = getBmiHeader(capPin);

            //Attach the format to the media.formatPtr, attach it to the output pin and free it
            Marshal.StructureToPtr(format, media.formatPtr, false);
            hr = ((IAMStreamConfig)capPin).SetFormat(media);
            DsUtils.FreeAMMediaType(media);
            DsError.ThrowExceptionForHR(hr);

            //Connect capture device to the sample grabber
            hr = m_graph.ConnectDirect(capPin, samPin, null);
            DsError.ThrowExceptionForHR(hr);

            //Render video
            hr = pBuilder.RenderStream(null, null, pSampleGrabber, null, null);
            DsError.ThrowExceptionForHR(hr);
        }

        /// <summary>
        /// Returns a point with the max size of capture device capture window
        /// </summary>
        /// <param name="p_capPin">The pin that we are polling</param>
        /// <returns></returns>
        private BitmapInfoHeader getBmiHeader(IPin p_capPin)
        {
            int hr = 0;
            int curMax = 0;

            //Return variable
            BitmapInfoHeader bmiHeader = null;

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
                if (v.BmiHeader.Size != 0 && v.BmiHeader.BitCount != 0)
                {
                    if (v.BmiHeader.BitCount > curMax)
                    {
                        bmiHeader = v.BmiHeader;
                        curMax = v.BmiHeader.BitCount;
                    }
                }

                hr = mediaTypeEnum.Next(1, mediaTypes, fetched);
                DsError.ThrowExceptionForHR(hr);
            }
            
            return bmiHeader;
        }

        public void start()
        {
            if (!m_running)
            {
                int hr = 0;
                if (FilterIndex > 0)
                {
                    buildGraph(CapDev[FilterIndex]);
                    hr = m_mediaControl.Run();
                    DsError.ThrowExceptionForHR(hr);
                }
                else
                    throw new Exception("No capture Device Selected");
            }
            else
                throw new Exception("Device is already running.");
        }

        public void stop()
        {
            if (m_running)
            {

            }
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

        class CamCaptureGrabberCallBack : ISampleGrabberCB
        {
            public CamCaptureGrabberCallBack()
            {}
            public int BufferCB(double SampleTime, IntPtr pBuffer, int bufferLen)
            { return 0; }

            public int SampleCB(double SampleTime, IMediaSample pSample)
            {
                if (pSample == null) return -1;
                int len = pSample.GetActualDataLength();
                IntPtr pBuf;
                if (pSample.GetPointer(out pBuf) == 0 && len > 0)
                {

                }
                return 0;
            }
        }
    }
}
