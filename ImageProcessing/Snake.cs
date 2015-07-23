using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            direction[0] = -deltaY / len; //This is for getting a unit vector
            direction[1] = deltaX / len; // of orthonormal vector

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
        /// <returns>Snake results</returns>
        private SnakeResults findSignificantPoints()
        {
            SnakeResults sr = new SnakeResults();
            sr.Start = m_start;
            sr.End = m_end;

            double[] midPoint = new double[2];
            midPoint[0] = m_start.X + m_end.X / 2.0;
            midPoint[1] = m_start.Y + m_end.Y / 2.0;

            bool findingMax = true;
            SnakePoint last = new SnakePoint(m_start);
            double max, min, cur, deltaX, deltaY;
            max = min = 0; //Min will be overwritten before it's used

            foreach (SnakePoint point in m_points)
            {
                deltaX = point.X - midPoint[0];
                deltaY = point.Y - midPoint[1];

                cur = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

                //This step is for finding local maxima
                if (findingMax)
                {
                    //If the trend starts to head lower, mark last point as significant
                    if (cur < max)
                    {
                        sr.SignificantPoints.Add(new Point(last.X, last.Y));
                        findingMax = false;
                        min = max;
                    }

                    max = cur;
                }
                //This step is for finding local minima
                else
                {
                    //If the trend starts to head higher, mark last point as signifigant
                    if (cur > min)
                    {
                        sr.SignificantPoints.Add(new Point(last.X, last.Y));
                        findingMax = true;
                        max = min;
                    }

                    min = cur;
                }

                //update last point
                last = point;
            }

            return sr;
        }
    }

    class SnakePoint
    {
        private byte[,] m_navMap;
        private double[] m_direction;
        public bool Locked { get; set; }
        private double m_X;
        private double m_Y;
        private int[] m_forwardLook;
        private int[] m_rightLook;
        private int[] m_leftLook;
        private SnakePoint m_prev;
        private SnakePoint m_next;
        private Point m_start;

        public int X { get { return (int)m_X; } }
        public int Y { get { return (int)m_Y; } }

        /// <summary>
        /// Advances the points inward or off to the side should there be a void
        /// </summary>
        public void increment()
        {
            if (Locked)
                return;

            else if (!Locked && m_navMap[Y, X] == 1)
            {
                Locked = true;
                //Once we lock notify our neighbors which direction we were going.
                // Update their direction to that parralel to my own
                if (m_next != null && !m_next.Locked)
                {
                    m_next.m_direction = this.m_direction;
                    //setOrbit(ref m_next);
                }
                if (m_prev != null && !m_prev.Locked)
                {
                    m_prev.m_direction = this.m_direction;
                    //setOrbit(ref m_prev);
                }
                return;
            }
            

            //I want forward to have the highest priority
            if (m_navMap[Y + m_forwardLook[1], X + m_forwardLook[0]] == 1)
            {
                m_X += m_direction[0];
                m_Y += m_direction[1];
            }
            else if (m_navMap[Y + m_leftLook[1], X + m_leftLook[0]] == 1)
            {
                m_X += m_direction[1];
                m_Y += m_direction[0];
            }

            else if (m_navMap[Y + m_rightLook[1], X + m_rightLook[0]] == 1)
            {
                m_X -= m_direction[1];
                m_Y += m_direction[0];
            }

            else if (m_navMap[Y + m_forwardLook[1], X + m_leftLook[0]] == 1)
            {
                m_X -= m_direction[1];
                m_Y += m_direction[1];
            }
            else if (m_navMap[Y + m_forwardLook[1], X + m_rightLook[0]] == 1)
            {
                m_X += m_direction[1];
                m_Y += m_direction[1];
            }
            else
            {
                m_X += m_direction[0];
                m_Y += m_direction[1];
            }
        }

        /// <summary>
        /// This function adjusts an adjacent point through trig and stuff
        /// </summary>
        private void setOrbit(ref SnakePoint p_adjecent)
        {
            double[] displacement = getDisplacementVector(ref p_adjecent);
            p_adjecent.addDisplacement(displacement);
        }

        /// <summary>
        /// This is some trig that I've siphoned down into something more condensed
        ///  If you want more see my notes
        /// </summary>
        /// <param name="p_adjecent"></param>
        /// <returns></returns>
        private double[] getDisplacementVector(ref SnakePoint p_adjecent)
        {
            double deltaX = this.m_X - p_adjecent.m_X;
            double deltaY = this.m_Y - p_adjecent.m_Y;
            double len = Math.Sqrt(deltaX * deltaX +
                                   deltaY * deltaY);

            double relVector = len - (Math.Sin(Math.Acos(1 / len)));

            double[] absVector = new double[2];
            absVector[0] = relVector * (deltaX);
            absVector[1] = relVector * (deltaY);

            return absVector;
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
        /// Complete dummy constructor not viable
        /// </summary>
        /// <param name="p_startPos"></param>
        public SnakePoint(Point p_startPos)
        {
            m_X = p_startPos.X;
            m_Y = p_startPos.Y;
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
            m_start = p_startPos;

            //set up looking incrementers
            m_forwardLook = new int[2];
            m_rightLook = new int[2];
            m_leftLook = new int[2];

            setupCheckMatrix();
        }

        /// <summary>
        /// As the snake point is incrementing through the nav space it needs to look for 
        ///  points of the hand
        ///  
        /// This sets up orthoganal point from the forward direction to check in a discreet manner
        /// </summary>
        private void setupCheckMatrix()
        {
            m_forwardLook[0] = (int) Math.Round(m_direction[0], MidpointRounding.AwayFromZero);
            m_forwardLook[1] = (int) Math.Round(m_direction[1], MidpointRounding.AwayFromZero);
            m_rightLook[0] = -m_forwardLook[1];
            m_rightLook[1] = m_forwardLook[0];
            m_leftLook[0] = m_forwardLook[1];
            m_leftLook[1] = m_forwardLook[0];

            Debug.Assert((m_forwardLook[0] == 0 || m_forwardLook[0] == 1) &&
                         (m_forwardLook[1] == 0 || m_forwardLook[1] == 1));
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

        /// <summary>
        /// Add a displacement and normalizes the vector then updates the search field
        /// </summary>
        /// <param name="p_displacement"></param>
        private void addDisplacement(double[] p_displacement)
        {
            m_direction[0] += p_displacement[0];
            m_direction[1] += p_displacement[1];

            double len = Math.Sqrt(m_direction[0] * m_direction[0] +
                                   m_direction[1] * m_direction[1]);

            m_direction[0] /= len;
            m_direction[1] /= len;

            setupCheckMatrix();
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

        public SnakeResults()
        {
            SignificantPoints = new List<Point>();
        }
    }
}
