using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DebugFunctions
{
    public class DebugBuffer
    {
        static public int getValidPixels(ref BitmapData p_data, ref byte[] p_buffer)
        {
            int depth = p_data.Stride / p_data.Width;
            int validPixels = 0;

            for (int i = 0; i < p_buffer.Length; i += depth)
            {
                if (p_buffer[i] != 0 || p_buffer[i + 1] != 0 || p_buffer[i + 2] != 0)
                    ++validPixels;
            }

            return validPixels;
        }
    }
}
