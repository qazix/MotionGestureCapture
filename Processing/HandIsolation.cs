using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionGestureProcessing
{
    class HandIsolation
    {
        public delegate void ProcessReadyHandler();
        public event ProcessReadyHandler ProcessReady;

        private Processing.ImageReadyHandler m_isoImageHandler;
        private static BitArray m_validPixels;
        private static bool m_isInitialized;

        /// <summary>
        /// Empty constructor
        /// </summary>
        public HandIsolation()
        { }

        /// <summary>
        /// First populates the bit array for values then sets up the event listener
        /// </summary>
        /// <param name="p_toInit">The initialization frame</param>
        public void initialize(Image p_toInit)
        {
            m_isInitialized = false;
            populateValidPixels(p_toInit);
            m_isInitialized = true;
            
            setupListener();
            doWork(ref p_toInit);
        }

        /// <summary>
        /// Will populate the bitArray
        /// </summary>
        /// <param name="p_toInit">Image to scan</param>
        private void populateValidPixels(Image p_toInit)
        {
           //TODO populate this method
        }

        /// <summary>
        /// Establishes a listening connection 
        /// </summary>
        private void setupListener()
        {
            m_isoImageHandler = (obj, image) =>
            {
                this.doWork(ref image);
            };

            Processing.getInstance().IsolationImageFilled += m_isoImageHandler;
        }

        /// <summary>
        /// this method transforms the image into a filtered image
        /// </summary>
        /// <param name="p_image"></param>
        private void doWork(ref Image p_image)
        {
            if (m_isInitialized)
            {
                //TODO scan the image and compare with bit array

                Processing.getInstance().ToPCAImage = p_image;

                //If someone is listener raise an event
                if (ProcessReady != null)
                    ProcessReady();
            }
        }
    }
}
