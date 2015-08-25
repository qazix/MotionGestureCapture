using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing
{
    public class ContourTracer
    {
        public enum Direction{ UP = 1, RIGHT, DOWN, LEFT }; //Start at 1 so I can have wrap around, make logic easier 
        private int[] m_dirValues;
        private byte[] m_buffer;
        private int m_width;

        private Direction m_curFace;
        private int m_curOffset;
        private Point m_curPoint;
                
        private Direction m_startFace;
        private Point m_startPoint;


        /// <summary>
        /// A little worker that will move along the outside of the a blob and mark points
        /// </summary>
        /// <param name="p_point">Starting Position</param>
        /// <param name="p_width">Width of the image it's tracing</param>
        public ContourTracer(ref byte[] p_buffer, Point p_point, int p_width, Direction p_dir)
        {
            m_buffer = p_buffer;
            m_curPoint = m_startPoint = p_point;
            m_width = p_width;
            m_curFace = m_startFace = p_dir;
            m_curOffset = ImageProcess.getOffset(p_point.X, p_point.Y, p_width, 4);
            
            m_dirValues = new int[6];
            m_dirValues[(int)Direction.UP] = -p_width * 4;
            m_dirValues[(int)Direction.RIGHT] = 4;
            m_dirValues[(int)Direction.DOWN] = p_width * 4;
            m_dirValues[(int)Direction.LEFT] = -4;
            m_dirValues[0] = m_dirValues[(int)Direction.LEFT];
            m_dirValues[5] = m_dirValues[(int)Direction.UP];
        }

        /// <summary>
        /// Changes the facing negative represents ccw rotation and positive represents cw rotation
        /// </summary>
        /// <param name="p_relFacing"></param>
        private void changeFacing(int p_relFacing)
        {
            m_curFace += p_relFacing;
            if ((int)m_curFace == 0 || (int)m_curFace == 5)
                m_curFace += (-p_relFacing * 4);
        }

        /// <summary>
        /// performs the checks and movements required for that condition
        /// The check is based on facing as follows
        /// |_1_|_2_|_3_|
        ///     |_^_|
        /// The caret represents facing and checks these point in sequence
        /// If it is valid, contains a foreground pixel, move there and update facing, based on rules
        /// </summary>
        /// <param name="p_next"></param>
        /// <returns>true for continue or false meaning to stop</returns>
        private bool increment(ref Point p_next, int p_iterations = 0)
        {
            if (p_iterations == 3)
                return false;
            else
            {
                //If the point in it's direction and counterclockwise from the current position is a valid pixel
                if (m_buffer[m_curOffset + m_dirValues[(int)m_curFace] + m_dirValues[(int)m_curFace - 1]] != 0)
                {
                    m_curOffset +=  m_dirValues[(int)m_curFace] + m_dirValues[(int)m_curFace - 1];
                    changeFacing(-1); //Face left of current facing
                    
                    //I will copy this section of code becuase i don't want to have to run it up to 3 times each pixel per increment
                    m_curPoint = ImageProcess.getPoint(m_curOffset, m_width, 4);
                    p_next = m_curPoint;
                }
                else if (m_buffer[m_curOffset + m_dirValues[(int)m_curFace]] != 0)
                {
                    m_curOffset += m_dirValues[(int)m_curFace];
                    m_curPoint = ImageProcess.getPoint(m_curOffset, m_width, 4);
                    p_next = m_curPoint;
                }
                else if (m_buffer[m_curOffset + m_dirValues[(int)m_curFace] + m_dirValues[(int)m_curFace + 1]] != 0)
                {
                    m_curOffset += m_dirValues[(int)m_curFace] + m_dirValues[(int)m_curFace + 1];
                    m_curPoint = ImageProcess.getPoint(m_curOffset, m_width, 4);
                    p_next = m_curPoint;
                }
                else
                {
                    //Turn clockwise and try again
                    changeFacing(1); 
                    if (!m_curPoint.Equals(m_startPoint) || m_curFace != m_startFace)
                        return increment(ref p_next, ++p_iterations);
                }

                //The end condition is when the tracer is on the starting position and facing the same direction
                // cp == sp ^ cf == sf, perform de'morgans to get
                // cp != sp | cf != sf
                return !m_curPoint.Equals(m_startPoint) || m_curFace != m_startFace;
            }
        }

        /// <summary>
        /// starts a loop which will continue to increment until the entire contour has been discovered
        /// </summary>
        /// <returns></returns>
        public List<Point> trace()
        {
            Point toAdd = m_startPoint;
            List<Point> contour = new List<Point>();

            bool contInc;
            do 
            {
                contour.Add(new Point(toAdd.X, toAdd.Y));
                contInc = increment(ref toAdd);
            }
            while (contInc);

            return contour;
        }
    }
}
