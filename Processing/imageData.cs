using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionGestureProcessing
{
    public class imageData
    {
        public bool InitialFrame { get; set; }
        public Image Image { get; set; }
        public List<List<int>> Datapoints { get; set; }
        public Rectangle Filter { get; set; }
        public float PC1 { get; set; }
        public float PC2 { get; set; }

        public imageData(bool p_isInit, Image p_image)
        {
            InitialFrame = p_isInit;
            Image = p_image;
        }
    }
}
