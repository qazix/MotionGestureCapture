using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageProcessing
{
    /// <summary>
    /// This structure is actually a disjoint set implementation
    /// </summary>
    public class ComponentLabel
    {
        private static int m_curLabel = 2;
        private static Dictionary<int, ComponentLabel> m_instances = null;

        private int m_id;
        private ComponentLabel m_parent = null;
        private int m_size;

        public int Id { get { return m_id; } }
        /// <summary>
        /// get or set parent, on set updates eldest as well
        /// </summary>
        public ComponentLabel Parent
        {
            get { return m_parent; }
        }

        #region Singleton Constructor
        /// <summary>
        /// Only place the current label is incremented
        /// </summary>
        private ComponentLabel()
        {
            m_id = m_curLabel++;
            m_size = 1;
        }

        /// <summary>
        /// Returns the element specified (if in range) or returns new label with current id
        /// </summary>
        /// <param name="p_id"></param>
        /// <returns></returns>
        public static ComponentLabel getInstance(int p_id = -1)
        {
            if (m_instances == null)
                m_instances = new Dictionary<int, ComponentLabel>();

            if (p_id <= 1 || p_id > m_curLabel)
                p_id = m_curLabel;

            if (!m_instances.ContainsKey(p_id))
                m_instances.Add(p_id, new ComponentLabel());

            return m_instances[p_id];
        }

        public static void dispose()
        {
            m_instances = null;
            m_curLabel = 2;
        }
        #endregion

        /// <summary>
        /// This performs the union operation on the set which contains the values passed
        /// This Union is union by size for smaller trees
        /// Performs path compression upon performing this union becuase it must find the root
        /// </summary>
        /// <seealso cref="https://www.youtube.com/watch?v=gcmjC-OcWpI"/>
        /// <param name="p_labels">up to 4 values whose sets need to be merged</param>
        /// <returns>The new root of the unioned values</returns>
        public static ComponentLabel Union (int[] p_labels)
        {
            ComponentLabel bigger, smaller;
            bigger = null;

            for (int i = 0; i < p_labels.Length - 1; ++i)
                for (int j = i + 1; j < p_labels.Length; ++j)
                {
                    //Get the root of each value
                    smaller = Find(p_labels[j]);
                    bigger = Find(p_labels[i]);

                    if (bigger.m_id != smaller.m_id)
                    {
                        if ((bigger.m_size == smaller.m_size && smaller.m_id < bigger.m_id) ||
                            bigger.m_size < smaller.m_size)
                        {
                            //I want to merge smaller sets to larger ones and higher numbers to lesser numbers
                            ComponentLabel temp = smaller;
                            smaller = bigger;
                            bigger = temp;
                        }

                        smaller.m_parent = bigger;
                        bigger.m_size += smaller.m_size;
                    }
                }

            return bigger;
        }

        /// <summary>
        /// Perform the find operation based on the label, because the parent information is calss based
        /// the label must be instantiated and then recovered
        /// </summary>
        /// <param name="p_label">label as an int</param>
        /// <returns>the root of the set that contains this label</returns>
        public static ComponentLabel Find (int p_label)
        {
            return Find(m_instances[p_label]);
        }

        /// <summary>
        /// recursively search the parent structure of the label until it cannot go any further
        /// </summary>
        /// <param name="p_label">instantiated label</param>
        /// <returns>the root of the set that contains this label</returns>
        public static ComponentLabel Find(ComponentLabel p_label)
        {
            if (p_label.m_parent == null)
                return p_label;
            else
                return Find(p_label.m_parent);
        }
    }
}
