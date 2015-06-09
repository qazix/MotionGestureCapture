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
        /* List of capture devices recognized by the computer */
        private BindingSource m_capDevBinding; 
        /* Object for handling display and capture */
        private CamCapture m_camCapture;
        
        /// <summary>
        /// Initialize all the components
        /// </summary>
        public MainGUI()
        {
            InitializeComponent();

            m_camCapture = CamCapture.getInstance();

            //Set up combo box with devices
            m_capDevBinding = new BindingSource();
            m_capDevBinding.DataSource = CamCapture.CapDev;
            comboBox1.DisplayMember = "Name"; /* use Name feild for display */
            comboBox1.ValueMember = "DevicePath"; /* use DevicePath (moniker) for value */
            comboBox1.DataSource = m_capDevBinding.DataSource;

            mainAlteredFeed.SizeMode = PictureBoxSizeMode.StretchImage;

            //I want to have the feed running right when the aplication starts
            m_camCapture.CaptureWindow = mainLiveFeed;
            m_camCapture.start();

        }

        /// <summary>
        /// This currently is for start video cpture but will eventually call
        ///  initialize on the processing component
        /// </summary>
        /// <param name="sender">Object calling this function</param>
        /// <param name="e">event type</param>
        private async void button1_Click(object sender, EventArgs e)
        {
            mainAlteredFeed.Image = await m_camCapture.grabImage();
        }

        /// <summary>
        /// Capture data from a different source
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool tempRunning = false;

            //If the cam is recording stop it to make the switch
            if (m_camCapture != null)
            {
                tempRunning = m_camCapture.Running;
                m_camCapture.stop();
            }

            //Resume the display if it was running at the beginning
            m_camCapture.FilterIndex = comboBox1.SelectedIndex;
            if (tempRunning)
            {
                m_camCapture.CaptureWindow = (tabControl1.SelectedIndex == 0 ? mainLiveFeed : testingPic);
                m_camCapture.start();
            }
        }

        /// <summary>
        /// if we change tabs ensure that the live feed is going to the right image panel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            m_camCapture.stop();
            switch (tabControl1.SelectedIndex)
            {
                case 0:
                    m_camCapture.CaptureWindow = mainLiveFeed;
                    m_camCapture.start();
                    break;
                case 1:
                    m_camCapture.CaptureWindow = testingPic;
                    m_camCapture.start();
                    break;
            }
        }

        private void capButton_Click(object sender, EventArgs e)
        {
            //Handle changing the text
            if (capButton.Text == "Capture")
            {
                capButton.Text = "Resume";
               
            }
            else
            {
                capButton.Text = "Capture";
                m_camCapture.CaptureWindow = testingPic;
            }
        }
    }
}
