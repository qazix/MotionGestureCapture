using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MotionGestureCapture
{
    public partial class MainGUI : Form
    {
        private BindingSource m_capDevBinding; 
        private CamCapture m_camCapture;
        public MainGUI()
        {
            InitializeComponent();

            //Set up combo box with devices
            m_capDevBinding = new BindingSource();
            m_capDevBinding.DataSource = CamCapture.CapDev;
            comboBox1.DisplayMember = "Name";
            comboBox1.ValueMember = "DevicePath";
            comboBox1.DataSource = m_capDevBinding.DataSource;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!m_camCapture.Running)
            {
                m_camCapture.start();
                m_camCapture.CaptureWindow = pictureBox1;
            }
            else
            {
                m_camCapture.stop();
                m_camCapture.CaptureWindow = null;
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (m_camCapture != null)
                m_camCapture.Dispose();

            m_camCapture = new CamCapture((string) comboBox1.SelectedValue);
        }
    }
}
