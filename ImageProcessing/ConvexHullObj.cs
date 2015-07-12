using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing
{
    /// <summary>
    /// Convext Hull object holds root point and hull
    /// </summary>
    class ConvexHullObject
    {
        //first point is top or bottom, second is left or right
        public Quadrant m_Q1;
        public Quadrant m_Q2;
        public Quadrant m_Q3;
        public Quadrant m_Q4;

        public ConvexHullObject()
        {
            m_Q1 = new Quadrant1();
            m_Q2 = new Quadrant2();
            m_Q3 = new Quadrant3();
            m_Q4 = new Quadrant4();
        }
    }

    /// <summary>
    /// because each quadrant uses the data slightly differently.  This class facilitates
    /// that working separately.
    /// </summary>
    abstract class Quadrant
    {
        public Point yPoint { get; set; } //this means y extreme point. top or bottom in lay man
        public Point xPoint { get; set; } //x extreme point
        public Point rootPoint {
            get {
                return new Point(yPoint.X, xPoint.Y);
            }
        }

        public List<Point> hullPoints = null;

        public Quadrant()
        {
            hullPoints = new List<Point>();
        }

        /// <summary>
        /// This method must be overwritten becuase the evaluation 
        /// ends up being if on slope is less then another
        /// </summary>
        /// <param name="p_evalPoints"></param>
        public abstract void evaluate(List<Point> p_evalPoints);
    }

    /// <summary>
    /// Cartesian Quadrant 1
    /// </summary>
    class Quadrant1 : Quadrant
    {
        public Quadrant1() : base()
        {
            xPoint = yPoint = new Point(0, int.MaxValue);
        }

        /// <summary>
        /// Q1 is xpoint is down and to the right or +y and +x
        /// So xPoint.y - yPoint.y / xPoint.x - yPoint.x > 0
        /// This is a problem becuase graphically the slope should be negative
        /// Flip the y values to make this the correct slope
        /// </summary>
        /// <param name="p_evalPoints"></param>
        public override void evaluate(List<Point> p_evalPoints)
        {
            //Put end points in hullPoints
            hullPoints.Add(yPoint);
            //If there is only only point in the quadrant return
            if (xPoint.Equals(yPoint))
                return;
            hullPoints.Add(xPoint);

            Point lastEntry = yPoint;
            double curSlope, canSlope;
            int index, removeCount;
            List<Point>.Enumerator e;
            bool cont;

            foreach (Point point in p_evalPoints)
            {
                //Becuase this is a sorted list we can ignore anything that is left of the last point added
                if (point.X > lastEntry.X && point.Y < rootPoint.Y)
                {
                    index = 0;
                    e = hullPoints.GetEnumerator();

                    //iterate through to the 
                    while (e.MoveNext() && point.X >= e.Current.X) 
                        ++index;

                    //Evaluate current slope with the current (next point further right) and the last point
                    curSlope = (double)(hullPoints[index - 1].Y - e.Current.Y) / (e.Current.X - hullPoints[index - 1].X);
                    
                    //evaluate candidate slope
                    canSlope = (double)(point.Y - e.Current.Y) / (e.Current.X - point.X);

                    if (canSlope < curSlope)
                    {
                        hullPoints.Insert(index, point);
                        lastEntry = point;

                        cont = true;  

                        //validate previous points
                        for (removeCount = index; removeCount > 1 && cont; --removeCount)
                        {
                            canSlope = (double)(hullPoints[removeCount - 2].Y - point.Y) / 
                                               (point.X - hullPoints[removeCount - 2].X);
                            curSlope = (double)(hullPoints[removeCount - 2].Y - hullPoints[removeCount - 1].Y) / 
                                               (hullPoints[removeCount - 1].X - hullPoints[removeCount - 2].X);

                            cont = curSlope < canSlope;
                        }

                        //If we were supposed to continue but didn't becuase i <= 1
                        if (cont == true && index != removeCount)
                            --removeCount;

                        //Remove Points that should be removed 
                        if (index - removeCount > 1)
                        {
                            removeCount = index - removeCount - 1;
                            for (int i = removeCount; i > 0; --i)
                            {
                                hullPoints.RemoveAt(index - removeCount);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Cartesian Quadrant 2
    /// </summary>
    class Quadrant2 : Quadrant
    {
        public Quadrant2() : base()
        {
            xPoint = yPoint = new Point(int.MaxValue, int.MaxValue);
        }

        /// <summary>
        /// Q2's goes from xPoint to yPoint so +x and -y
        /// So normal yPoint.Y - xPoint.Y / yPoint.Y - xPoint.Y works
        /// </summary>
        /// <param name="p_evalPoints"></param>
        public override void evaluate(List<Point> p_evalPoints)
        {
            //Put end points in hullPoints
            hullPoints.Add(xPoint);
            //If there is only only point in the quadrant return
            if (yPoint.Equals(xPoint))
                return;
            hullPoints.Add(yPoint);

            Point lastEntry = yPoint;
            double curSlope, canSlope;
            int index, removeCount;
            bool cont;

            foreach (Point point in p_evalPoints)
            {
                //Becuase this is a sorted list we can ignore anything that is left of the last point added
                if (point.X < lastEntry.X && point.Y < rootPoint.Y)
                {
                    index = 1;

                    //The way the data is set up this will always be the hullPoints[1]

                    //Evaluate current slope with the current (next point further right) and the last point
                    curSlope = (double)(hullPoints[1].Y - hullPoints[0].Y) / (hullPoints[1].X - hullPoints[0].X);

                    //evaluate candidate slope
                    canSlope = (double)(point.Y - hullPoints[0].Y) / (point.X - hullPoints[0].X);

                    if (canSlope < curSlope)
                    {
                        hullPoints.Insert(index, point);
                        lastEntry = point;

                        cont = true;

                        //validate previous points
                        for (removeCount = index; removeCount < hullPoints.Count - 2 && cont; ++removeCount)
                        {
                            canSlope = (double)(hullPoints[removeCount + 2].Y - point.Y) / 
                                               (hullPoints[removeCount + 2].X - point.X);
                            curSlope = (double)(hullPoints[removeCount + 2].Y - hullPoints[removeCount + 1].Y) / 
                                               (hullPoints[removeCount + 2].X - hullPoints[removeCount + 1].X);

                            cont = curSlope < canSlope;
                        }

                        //If we were supposed to continue but didn't becuase i <= 1
                        if (cont == true && index != removeCount)
                            ++removeCount;

                        //Remove Points that should be removed 
                        if (removeCount - index > 1)
                        {
                            removeCount = removeCount - index - 1;
                            for (int i = removeCount; i > 0; --i)
                            {
                                hullPoints.RemoveAt(2);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Cartesian Quadrant 3
    /// </summary>
    class Quadrant3 : Quadrant
    {
        public Quadrant3() : base()
        {
            xPoint = yPoint = new Point(int.MaxValue, 0);
        }

        /// <summary>
        /// Q3 starts from yPoint and goes to xPoint so -x, -y
        /// </summary>
        /// <param name="p_evalPoints"></param>
        public override void evaluate(List<Point> p_evalPoints)
        {
            //Put end points in hullPoints
            hullPoints.Add(yPoint);
            //If there is only only point in the quadrant return
            if (xPoint.Equals(yPoint))
                return;
            hullPoints.Add(xPoint);

            Point lastEntry = xPoint;
            double curSlope, canSlope;
            int index, removeCount;
            bool cont;

            foreach (Point point in p_evalPoints)
            {
                //Becuase this is a sorted list we can ignore anything that is left of the last point added
                if (point.X > lastEntry.X && point.Y > rootPoint.Y)
                {
                    //With x sorting this will only ever go in index 1
                    index = 1;

                    //Evaluate current slope with the current (next point further right) and the last point
                    curSlope = (double)(hullPoints[1].Y - hullPoints[0].Y) / (hullPoints[1].X - hullPoints[0].X);

                    //evaluate candidate slope
                    canSlope = (double)(hullPoints[1].Y - point.Y) / (hullPoints[1].X - point.X);

                    if (canSlope > curSlope)
                    {
                        hullPoints.Insert(index, point);
                        lastEntry = point;

                        cont = true;

                        //validate previous points
                        for (removeCount = index; removeCount < hullPoints.Count - 2 && cont; ++removeCount)
                        {
                            canSlope = (double)(hullPoints[removeCount + 2].Y - point.Y) / 
                                               (hullPoints[removeCount + 2].X - point.X);
                            curSlope = (double)(hullPoints[removeCount + 2].Y - hullPoints[removeCount + 1].Y) / 
                                               (hullPoints[removeCount + 2].X - hullPoints[removeCount + 1].X);

                            cont = curSlope < canSlope;
                        }

                        //If we were supposed to continue but didn't becuase i <= 1
                        if (cont == true && index != removeCount)
                            ++removeCount;

                        //Remove Points that should be removed 
                        if (removeCount - index > 1)
                        {
                            removeCount = removeCount - index - 1;
                            for (int i = removeCount; i > 0; --i)
                            {
                                hullPoints.RemoveAt(2);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Cartesian Quadrant 4
    /// </summary>
    class Quadrant4 : Quadrant
    {
        public Quadrant4() : base()
        {
            xPoint = yPoint = new Point(0, 0);
        }

        /// <summary>
        /// Q4 goes from xPoint to yPoint so -x, +y
        /// </summary>
        /// <param name="p_evalPoints"></param>
        public override void evaluate(List<Point> p_evalPoints)
        {
            //Put end points in hullPoints
            hullPoints.Add(xPoint);
            //If there is only only point in the quadrant return
            if (xPoint.Equals(yPoint))
                return;
            hullPoints.Add(yPoint);

            Point lastEntry = yPoint;
            double curSlope, canSlope;
            int index, removeCount;
            bool cont;

            foreach (Point point in p_evalPoints)
            {
                //Becuase this is a sorted list we can ignore anything that is left of the last point added
                if (point.X > lastEntry.X && point.Y > rootPoint.Y)
                {
                    index = 1;

                    //Evaluate current slope with the current (next point further right) and the last point
                    curSlope = (double)(hullPoints[1].Y - hullPoints[0].Y) / (hullPoints[1].X - hullPoints[0].X);

                    //evaluate candidate slope
                    canSlope = (double)(point.Y - hullPoints[0].Y) / (point.X - hullPoints[0].X);

                    if (canSlope < curSlope)
                    {
                        hullPoints.Insert(index, point);
                        lastEntry = point;

                        cont = true;

                        //validate previous points
                        for (removeCount = index; removeCount < hullPoints.Count - 2 && cont; ++removeCount)
                        {
                            canSlope = (double)(hullPoints[removeCount + 2].Y - point.Y) / 
                                               (hullPoints[removeCount + 2].X - point.X);
                            curSlope = (double)(hullPoints[removeCount + 2].Y - hullPoints[removeCount + 1].Y) / 
                                               (hullPoints[removeCount + 2].X - hullPoints[removeCount + 1].X);

                            cont = curSlope < canSlope;
                        }

                        //If we were supposed to continue but didn't becuase i <= 1
                        if (cont == true && index != removeCount)
                            ++removeCount;

                        //Remove Points that should be removed 
                        if (removeCount - index > 1)
                        {
                            removeCount = removeCount - index - 1;
                            for (int i = removeCount; i > 0; --i)
                            {
                                hullPoints.RemoveAt(2);
                            }
                        }
                    }
                }
            }
        }
    }
}
