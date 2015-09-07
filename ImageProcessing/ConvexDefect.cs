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
        private double m_area;
        private double m_distanceToEnd;
        private Point m_startPoint;
        private Point m_endPoint;
        public Point StartPoint {
            get { return m_startPoint; }
            set {
                m_startPoint = value;
                m_area = -1.0;
            }
        }

        public Point EndPoint {
            get { return m_endPoint; }
            set {
                m_endPoint = value;
                m_area = -1.0;
            }
        }
        public Point DeepestPoint { get; set; }
        public double DistanceToDeepestPoint { get; set; }
        public double DistanceToEnd { //this performing math becuase it isn't used very often
            get {
                if (m_distanceToEnd < 0)
                    m_distanceToEnd = Math.Sqrt((EndPoint.X - StartPoint.X) * (EndPoint.X - StartPoint.X) +
                                                (EndPoint.Y - StartPoint.Y) * (EndPoint.Y - StartPoint.Y));

                return m_distanceToEnd;
            }
        }

        /// <summary>
        /// uses Heron's formula to calculate area
        /// </summary>
        public double Area {
            get {
                if (m_area < 0 && DeepestPoint != null)
                {
                    double sideA, sideB, sideC, perimeter;
                    sideA = Math.Sqrt((StartPoint.X - EndPoint.X) * (StartPoint.X - EndPoint.X) +
                                      (StartPoint.Y - EndPoint.Y) * (StartPoint.Y - EndPoint.Y));
                    sideB = Math.Sqrt((EndPoint.X - DeepestPoint.X) * (EndPoint.X - DeepestPoint.X) +
                                      (EndPoint.Y - DeepestPoint.Y) * (EndPoint.Y - DeepestPoint.Y));
                    sideC = Math.Sqrt((DeepestPoint.X - StartPoint.X) * (DeepestPoint.X - StartPoint.X) +
                                      (DeepestPoint.Y - StartPoint.Y) * (DeepestPoint.Y - StartPoint.Y));

                    perimeter = sideA + sideB + sideC;

                    m_area = Math.Sqrt(perimeter * (perimeter - sideA) * (perimeter - sideB) * (perimeter - sideC));
                }

                return m_area;
            }
        }
        public ConvexDefect(Point p_start, Point p_end)
        {
            StartPoint = p_start;
            EndPoint = p_end;
            m_area = -1.0;
            m_distanceToEnd = -1.0;
        }

        public bool contains(Point p_point)
        {
            if (DeepestPoint != null)
            {
                bool b1, b2, b3;
                b1 = sign(p_point, StartPoint, EndPoint);
                b2 = sign(p_point, EndPoint, DeepestPoint);
                b3 = sign(p_point, DeepestPoint, StartPoint);

                return b1 == b2 && b2 == b3;
            }
            else
                return false;

        }

        //determines which side pt1 is of the plane through pt2 and pt3 
        private bool sign (Point pt1, Point pt2, Point pt3)
        {
            return (pt1.X - pt3.X) * (pt2.Y - pt3.Y) -
                   (pt2.X - pt3.X) * (pt1.Y - pt3.Y) > 0.0;
        }

        public override bool Equals(object obj)
        {
            return StartPoint.Equals(((ConvexDefect)obj).StartPoint);
        }
    }
}
