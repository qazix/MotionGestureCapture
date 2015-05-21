
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
    class CamCapture : IDisposable
    {
        private bool m_running;
        private Capture m_cap;
        private Filter m_filter;

        public void Dispose()
        {
            m_cap.Dispose();
            m_filter = null;
            GC.SuppressFinalize(this);
        }

        static public DsDevice[] CapDev
        {   get {
                return DsDevice.GetDevicesOfCat( DirectShowLib.FilterCategory.VideoInputDevice );
            }
        }

        public System.Windows.Forms.Control CaptureWindow
        {   set {
                m_cap.PreviewWindow = value;
            }
        }

        public bool Running { get { return m_running; } }

        /// <summary>
        /// Initialize a CamCapture object
        /// </summary>
        /// <param name="p_devMonStr">Device Moniker String</param>
        public CamCapture(string p_devMonStr = null)
        {
            //TODO: if there are no devices fail gracefully 
            //If the moniker string is "" then grab the first device
            if (p_devMonStr == null && CapDev.Length > 0)
                p_devMonStr = CapDev[0].DevicePath;
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

        public void start()
        {
            if (!m_running)
            {
                m_running = true;
                m_cap = new Capture(m_filter, null);
            }
        }

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
