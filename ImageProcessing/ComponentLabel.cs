using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing
{
    public class ComponentLabel
    {
        private static int m_curLabel = 2;
        private static Dictionary<int, ComponentLabel> m_instances = null;

        private int m_id;
        private ComponentLabel m_parent = null;
        private int m_eldestId;

        public int Id { get { return m_id; } }
        /// <summary>
        /// get or set parent, on set updates eldest as well
        /// </summary>
        public ComponentLabel Parent { get { return m_parent; } 
            set{
                m_parent = value;
                m_eldestId = getEldestID(value);
            }
        }

        public int EldestId { get { return m_eldestId; } }

        /// <summary>
        /// Only place the current label is incremented
        /// </summary>
        #region Singleton Constructor
        private ComponentLabel()
        {
            m_id = m_curLabel++;
            m_eldestId = m_id;
        }

        /// <summary>
        /// Returns the element specified (if in range) or returns new label with current id
        /// </summary>
        /// <param name="p_id"></param>
        /// <returns></returns>
        public static ComponentLabel getInstance(int p_id = -1)
        {
            if (m_instances == null)
                m_instances = new Dictionary<int,ComponentLabel>();

            if (p_id <= 1 || p_id > m_curLabel)
                p_id = m_curLabel;

            if (!m_instances.ContainsKey(p_id))
                m_instances.Add(p_id, new ComponentLabel());

            return m_instances[p_id];
        }

        #endregion

        #region Getters and Setters
        /// <summary>
        /// Recursively call parents until we find the eldest
        /// </summary>
        /// <param name="p_curLab"></param>
        /// <returns></returns>
        private int getEldestID(ComponentLabel p_curLab)
        {
            if (p_curLab.Parent == null)
                return Id;
            else
                return getEldestID(p_curLab.Parent);
        }
        #endregion
    }
}
