using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing
{
    public class ConvexDefect
    {
        public Point Start { get; set; }
        public Point End { get; set; }
        public Point DeepestPoint { get; set; }
        public double distance { get; set; }
    }
}
