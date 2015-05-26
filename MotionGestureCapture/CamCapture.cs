
using DirectX.Capture;
using DShowNET;
using DirectShowLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace MotionGestureCapture
{
    /// <summary>
    /// CamCapture provide several functions for displaying and capturing image data
    /// </summary>
    public class CamCapture : IDisposable
    {
        #region Member variables and Getters and Setters
        private static CamCapture m_instance; 
        private bool m_running; /* is the cam capturing */
        private Capture m_cap; /* DirectX Capture */
        private Filter m_filter; /* this determines which device to use */

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
        public System.Windows.Forms.Control CaptureWindow
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
        #endregion

        /// <summary>
        /// Initialize a CamCapture object
        /// </summary>
        /// <param name="p_devMonStr">Device Moniker String</param>
        public CamCapture(string p_devMonStr = null)
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
    }
}
