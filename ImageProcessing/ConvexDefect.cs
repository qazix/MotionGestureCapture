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
        public Point StartPoint { get; set; }
        public Point EndPoint { get; set; }
        public Point DeepestPoint { get; set; }
        public double distance { get; set; }

        public ConvexDefect(Point p_start, Point p_end)
        {
            StartPoint = p_start;
            EndPoint = p_end;
        }

        public override bool Equals(object obj)
        {
            return StartPoint.Equals(((ConvexDefect)obj).StartPoint);
        }
    }
}
