using DirectShowLib;
using DirectX.Capture;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MotionGestureCapture
{
    class StillCapture
    {
        IGraphBuilder m_graph;
        DsDevice m_capDev;
        static byte[] m_imageBuf;

        StillCapture(ref DsDevice p_capDev)
        {
            m_capDev = p_capDev;
            buildGraph();
        }

        private void buildGraph()
        {
            int hr = 0;
            IBaseFilter captureFilter;AMMediaType pmt4 = new AMMediaType();
            IFilterGraph2 filtergraph = new FilterGraph() as IFilterGraph2;
            ICaptureGraphBuilder2 pBuilder = (ICaptureGraphBuilder2)new CaptureGraphBuilder2();

            hr = pBuilder.SetFiltergraph(m_graph);
            DsError.ThrowExceptionForHR(hr);

            filtergraph.AddSourceFilterForMoniker(m_capDev.Mon,
                                                  null, m_capDev.Name, 
                                                  out captureFilter);
            m_graph.AddFilter(captureFilter, "CapFilter");
            DsError.ThrowExceptionForHR(hr);

            IBaseFilter pSampleGrabber = (IBaseFilter)Activator.CreateInstance(typeof(SampleGrabber));
            hr = m_graph.AddFilter(pSampleGrabber, "SampleGrabber");
            DsError.ThrowExceptionForHR(hr);

            hr = ((ISampleGrabber)pSampleGrabber).SetCallback(new StillGrabberCallBack(), 0);

            AMMediaType media = new AMMediaType();
            media.majorType = MediaType.Video;
            media.subType = MediaSubType.RGB24;
            media.formatType = FormatType.VideoInfo;

            VideoInfoHeader format = new VideoInfoHeader();
            format.SrcRect = new DsRect();
            format.TargetRect = new DsRect();
            format.BmiHeader = new BitmapInfoHeader()
            format.BmiHeader.Size = 
        }
        public Image grabStill()
        {
            MemoryStream ms = new MemoryStream(m_imageBuf);
            return Image.FromStream(ms);
        }

        class StillGrabberCallBack : ISampleGrabberCB
        {
            public StillGrabberCallBack()
            { }
            public int BufferCB(double SampleTime, IntPtr pBuffer, int bufferLen)
            { return 0; }

            public int SampleCB(double SampleTime, IMediaSample pSample)
            {
                if (pSample == null) return -1;
                int len = pSample.GetActualDataLength();
                IntPtr pBuf;
                if (pSample.GetPointer(out pBuf) == 0 && len > 0)
                { 
                    m_imageBuf = null;
                    m_imageBuf = new byte[len];
                    Marshal.Copy(pBuf, m_imageBuf, 0, len);
                }
                return 0;
        }
    }
}
