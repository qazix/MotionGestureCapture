using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;

namespace Processing
{
    class Processing
    {
        private static Processing m_instance = null;
        public Semaphore CamToIsolation { get; set; }
        public Image ToIsolationImage { get; set; }
        public Semaphore IsolationToPCA { get; set; }
        public Image ToPCAImage { get; set; }
        public Semaphore PCAToGestures { get; set; }
        public Image ToGesturesImage { get; set; }
        public Semaphore GesturesToReturn { get; set; }
        public Image ToReturnImage { get; set; }
        

        private Processing()
        {
            CamToIsolation   = new Semaphore(0, 1);
            IsolationToPCA   = new Semaphore(0, 1);
            PCAToGestures    = new Semaphore(0, 1);
            GesturesToReturn = new Semaphore(0, 1);
            ToIsolationImage = null;
            ToPCAImage       = null;
            ToGesturesImage  = null;
            ToReturnImage    = null;
        }

        public Processing getInstance()
        {
            if (m_instance == null)
                m_instance = new Processing();

            return m_instance;
        }
    }
}
