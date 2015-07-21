using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing
{
    class Snake
    {
        private List<SnakePoint> m_points;
        private Point m_start;
        private Point m_end;
        private byte[,] m_snakeMap;

        /// <summary>
        /// Initializes the start and end and creates a number of points
        /// </summary>
        /// <param name="p_start">Reduced starting position</param>
        /// <param name="p_end">Reduced ending position</param>
        public Snake(Point p_start, Point p_end, byte[,] p_snakeMap)
        {
            m_start = p_start;
            m_end = p_end;
            m_snakeMap = p_snakeMap;

            createPoints();
        }

        /// <summary>
        /// Creates a number of points based on the length of the line
        /// </summary>
        private void createPoints()
        {
 	        double deltaX = m_end.X - m_start.X;
            double deltaY = m_end.Y - m_start.Y;

            double len = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            //parallel angles are -1/slope = run/rise
            double[] direction = new double[2];

            direction[0] = deltaY / len; //This is for getting a unit vector
            direction[1] = -deltaX / len; // of orthonormal vector

            //normalize deltas
            deltaX /= len;
            deltaY /= len;

            //There will be a snake point every 2 pixels in the reduced map
            SnakePoint[] sp = new SnakePoint[(int)(len / 2)];
            double startX = m_start.X + (deltaX / 2);
            double startY = m_start.Y + (deltaY / 2);
            sp[0] = new SnakePoint(direction, ref m_snakeMap, new Point ((int)startX, (int)startY));

            for (int i = 1; i < sp.Length; ++i)
            {
                startX += (deltaX * 2);
                startY += (deltaY * 2);
                sp[i] = new SnakePoint(direction, ref sp[i - 1], ref m_snakeMap, new Point((int)startX, (int)startY));
            }

            m_points = sp.ToList();
        }

        /// <summary>
        /// iteratively calls the snakepoints to advance inward.
        /// </summary>
        /// <returns>The results from the snaking</returns>
        public SnakeResults getResults()
        {
            int completed;
            do
            {
                completed = 0;
                foreach (SnakePoint point in m_points)
                {
                    point.increment();
                    if (point.Locked)
                        ++completed;
                }
            }
            while (m_points.Count - completed > 0);

            return findSignificantPoints();
        }


        /// <summary>
        /// Populates the Snake Results object with m_start and m_end then 
        /// finds local minima and maxima to report at significant points.
        /// </summary>
        /// <returns></returns>
        private SnakeResults findSignificantPoints()
        {
            SnakeResults sr = new SnakeResults();
            sr.Start = m_start;
            sr.End = m_end;

            return sr;
        }
    }

    class SnakePoint
    {
        private static int[,] m_travelMap = {{ 0, 0, 0 },
                                             { 1, 5, 1 },
                                             { 1, 1, 1 }};
        private byte[,] m_navMap;
        private double[] m_direction;
        public bool Locked { get; set; }
        private double m_X;
        private double m_Y;
        private SnakePoint m_prev;
        private SnakePoint m_next;

        public int X { get { return (int)m_X; } }
        public int Y { get { return (int)m_Y; } }

        /// <summary>
        /// Advances the points inward or off to the side should there be a void
        /// </summary>
        public void increment()
        {
            if (m_navMap[Y, X] == 1)
            {
                Locked = true;
                return;
            }

            Locked = true;
        }

        /// <summary>
        /// The constructor for the head of the list
        /// </summary>
        /// <param name="p_direction"></param>
        /// <param name="p_snakeMap"></param>
        /// <param name="p_startPos"></param>
        public SnakePoint(double[] p_direction, ref byte[,] p_snakeMap, Point p_startPos)
        {
            memberSetup(p_direction, ref p_snakeMap, p_startPos);         
        }

        /// <summary>
        /// Constructor for all the following points
        /// </summary>
        /// <param name="p_direction"></param>
        /// <param name="p_prev"></param>
        /// <param name="p_snakeMap"></param>
        /// <param name="p_startPos"></param>
        public SnakePoint(double[] p_direction, ref SnakePoint p_prev, ref byte[,] p_snakeMap, Point p_startPos)
        {
            memberSetup(p_direction, ref p_snakeMap, p_startPos); 

            setPrev(ref p_prev);
        }

        /// <summary>
        /// Assigns member variables to the parameters
        /// </summary>
        /// <param name="p_direction"></param>
        /// <param name="p_snakeMap"></param>
        /// <param name="p_startPos"></param>
        private void memberSetup(double[] p_direction, ref byte[,] p_snakeMap, Point p_startPos)
        {
            m_direction = p_direction;
            m_navMap = p_snakeMap;
            m_prev = null;
            m_X = p_startPos.X;
            m_Y = p_startPos.Y;
        }

        /// <summary>
        /// Sets the reference to the previous point and sets the 
        /// previous point's next pointer to this one
        /// </summary>
        /// <param name="p_prev"></param>
        private void setPrev(ref SnakePoint p_prev)
        {
            p_prev.setNext(this);
            m_prev = p_prev;
        }

        /// <summary>
        /// Sets the reference for the next point
        /// </summary>
        /// <param name="p_next"></param>
        private void setNext(SnakePoint p_next)
        {
            m_next = p_next;
        }
    }

    /// <summary>
    /// A simple object to store the results
    /// </summary>
    class SnakeResults
    {
        public Point Start { get; set; }
        public Point End { get; set; }
        public List<Point> SignificantPoints { get; set; }
    }
}
