using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MotionGestureProcessing;

namespace MotionGestureCapture
{
    public partial class MainGUI : Form
    {
        /* List of capture devices recognized by the computer */
        private BindingSource m_capDevBinding; 
        /* Object for handling display and capture */
        private CamCapture m_camCapture;
        private Processing m_processing;
        private PictureBox m_initSquare;
        
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
            testingPic.SizeMode = PictureBoxSizeMode.StretchImage;

            //Set up processing
            m_processing = Processing.getInstance();
            setupProcessingListener();

            //I want to have the feed running right when the aplication starts
            m_camCapture.CaptureWindow = mainLiveFeed;
            m_camCapture.start();

            setupInitSquare();
        }

        /// <summary>
        /// Establishes a listening connection 
        /// </summary>
        private void setupProcessingListener()
        {
            Processing.ImageReadyHandler handler = null;

            //Sets the altered feed to the image if the altered feed is on screen
            handler = (imgData) =>
            {   
                if (tabControl1.SelectedIndex == 0)
                    mainAlteredFeed.Image = imgData.Image;
                else
                    testingPic.Image = imgData.Image;
            };

            m_processing.ReturnImageFilled += handler;
        }

        /// <summary>
        /// This is suppose to put a green overlay on th main picture box
        /// </summary>
        private void setupInitSquare()
        {
            m_initSquare = new PictureBox();
            m_initSquare.BackColor = Color.Transparent;
            m_initSquare.Parent = mainLiveFeed;
            m_initSquare.Size = mainLiveFeed.Size;
            m_initSquare.Location = mainLiveFeed.Location;
            
            //populate Bitmap
            Bitmap square = new Bitmap(mainLiveFeed.Width, mainLiveFeed.Height);

            int endX = (square.Width / 2) + 50;
            int endY = (square.Height / 2) + 50;
            int startX = endX - 100;
            int startY = endY - 100;
            for (int y = startY; y <= endY; ++y)
            {
                if (y != startY && y != endY)
                {
                    square.SetPixel(startX, y, Color.LimeGreen);
                    square.SetPixel(endX, y, Color.LimeGreen);
                }
                else
                {
                    for (int x = startX; x <= endX; ++x)
                        square.SetPixel(x, y, Color.LimeGreen);
                }
            }
            square.Save("TestSquare.bmp");

            //Add bitmap to pictureBox
            m_initSquare.Image = square;
        }

        /// <summary>
        /// This currently is for start video cpture but will eventually call
        ///  initialize on the processing component
        /// </summary>
        /// <param name="sender">Object calling this function</param>
        /// <param name="e">event type</param>
        private void button1_Click(object sender, EventArgs e)
        {
            m_processing.initialize(true);
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
            //m_processing.stop();
            switch (tabControl1.SelectedIndex)
            {
                case 0:
                    m_camCapture.CaptureWindow = mainLiveFeed;
                    m_camCapture.start();
                    break;
                case 1:
                    m_camCapture.CaptureWindow = testingPic;
                    capButton.Text = "Capture";
                    m_camCapture.start();
                    break;
            }
        }

        /// <summary>
        /// Initialize for testing purposes
        /// </summary>
        private void testInit_Click(object sender, EventArgs e)
        {
            m_processing.initialize(false);
        }

        /// <summary>
        /// Either captures a single image or resumes capture
        /// </summary>
        private void capButton_Click(object sender, EventArgs e)
        {
            //Handle changing the text
            if (capButton.Text == "Capture")
            {
                capButton.Text = "Resume";

                //Add the stop functionality after grabbing an image
                Processing.ImageReadyHandler handler = null;
                handler = (imgData) =>
                {
                    m_processing.ReturnImageFilled -= handler;
                    m_camCapture.stop();
                };

                m_processing.ReturnImageFilled += handler;
                
                m_processing.oneShot();
            }
            else
            {
                capButton.Text = "Capture";
                m_camCapture.start();
            }
        }
    }
}
