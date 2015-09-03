using ImageProcessing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MotionGestureProcessing
{
    public abstract class Process
    {
        protected abstract void setupListener();

        /// <summary>
        /// What this process does to an image
        /// </summary>
        /// <param name="obj">Data pertaining to this image</param>
        /// <param name="p_image">Image to be processed</param>
        protected abstract void doWork(Object p_imgData);

        /// <summary>
        /// This is used because edges is done in 32bpp and most cameras are only 24bpp
        /// So i must convert the pixel format for easy comparison
        /// </summary>
        /// <param name="p_toInit">image to convert</param>
        protected void convert2PixelFormat(ref Image p_toInit)
        {
            Bitmap converted = new Bitmap(p_toInit);

            p_toInit = converted;
        }
    }
}
