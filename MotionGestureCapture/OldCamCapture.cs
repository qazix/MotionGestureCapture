
using DirectShowLib;
using DirectX.Capture;
using DShowNET;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace MotionGestureCapture
{
    /// <summary>
    /// CamCapture provide several functions for displaying and capturing image data
    /// CamCapture is a singleton class so that all classes are working with the same 
    /// set of data.
    /// </summary>
    public class OldCamCapture : IDisposable
    {
        #region Member variables and Getters and Setters
        private static OldCamCapture m_instance; /* Singleton Instance */ 
        private bool m_running; /* is the cam capturing */
        private Capture m_cap; /* DirectX Capture */
        private Filter m_filter; /* this determines which device to use */
        private int m_filterIndex; /* the index into the DsDevice array */
        private Image m_image; /* this is a single image grabbed from a pictureBox*/

        public int FilterIndex { get { return m_filterIndex; } set { m_filterIndex = value; } };

        /// <summary>
        /// A static means of getting the capture devices
        /// </summary>
        static public DsDevice[] CapDev
        {   get {
                return DsDevice.GetDevicesOfCat( DirectShowLib.FilterCategory.VideoInputDevice );
            }
        }

        /// <summary>
        /// Getter for running status
        /// </summary
        public bool Running { get { return m_running; } }

        /// <summary>
        /// Setter for displaying
        /// </summary>
        public System.Windows.Forms.PictureBox CaptureWindow
        {   set {          
                m_cap.PreviewWindow = value;
            }
        }

        /// <summary>
        /// This is used to reset the filter on the capture object
        /// </summary>
        public String Filter
        {   set {
                 if (m_filter != null)
                     m_filter = null;
                 m_filter = new Filter(value);
            }   
        }

        public Image Image
        {
            get { return m_image; }
            //set { m_image = value;  }
        }
        #endregion

        /// <summary>
        /// Initialize a CamCapture object
        /// </summary>
        /// <param name="p_devMonStr">Device Moniker String</param>
        private OldCamCapture(string p_devMonStr = null)
        {
            if (p_devMonStr != null)
            {
                try
                {
                    m_filter = new Filter(p_devMonStr);
                    m_running = false;
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }
            else if (CapDev.Length > 0)
            {
                m_filter = null;
                m_running = false;
            }
            else
                throw new Exception("No Capture Devices");
        }

        /// <summary>
        /// Singleton instantiation
        /// </summary>
        /// <returns>A running instance if one exists or create a blank one</returns>
        public static OldCamCapture getInstance()
        {
            if (m_instance == null)
            {
                m_instance = new OldCamCapture();
            }

            return m_instance;
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        public void Dispose()
        {
            if (m_cap != null)
                m_cap.Dispose();
            m_filter = null;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Capture using the filter set
        /// </summary>
        public void start()
        {
            if (!m_running)
            {
                m_running = true;
                m_cap = new Capture(m_filter, null);
                
            }
        }

        /// <summary>
        /// Stops the capture
        /// </summary>
        public void stop()
        {
            if (m_running)
            {
                m_running = false;
                m_cap.Dispose();
                
            }
        }

        public Task<Bitmap> grabImage()
        {
            /*TaskCompletionSource<Image> tcs = new TaskCompletionSource<Image>();

            Capture.FrameCapHandler handler = null;
            handler = (frame) =>
            {
                m_cap.FrameCaptureComplete -= handler;
                tcs.SetResult(frame.Image);
                m_image = frame.Image;
            };

            m_cap.FrameCaptureComplete += handler;
            m_cap.CaptureFrame();

            return tcs.Task;*/

            
            /*object filt = null;
            CapDev[0].Mon.BindToObject(null, null, typeof(DirectShowLib.IBaseFilter).GUID, out filt);

            DirectShowLib.IPin stillPin = DirectShowLib.DsFindPin.ByCategory((DirectShowLib.IBaseFilter) filt,
                                          DirectShowLib.PinCategory.Still, 0);

            IAMVideoControl vidControl = null;
            vidControl.SetMode(stillPin, VideoControlFlags.Trigger);

            DirectShowLib.ISampleGrabber sgFilt = new SampleGrabber() as DirectShowLib.ISampleGrabber;
            sgFilt.
            

            Marshal.ReleaseComObject(CapDev[0]);*/

            StillCapture.grabStill(CapDev[m_filterIndex]);
        }
    }
}
