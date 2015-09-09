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
        private Processing.ImageReadyHandler m_handler;
        private Image m_testImage;
        private Point m_identifiedCenter;
        private Point m_identifiedOrientation;
        
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
            capturePic.SizeMode = PictureBoxSizeMode.StretchImage;
            testingPic.SizeMode = PictureBoxSizeMode.StretchImage;
            transperency.SizeMode = PictureBoxSizeMode.StretchImage;

            //Set up processing
            m_processing = Processing.getInstance();
            setupProcessingListener();

            //I want to have the feed running right when the aplication starts
            m_camCapture.CaptureWindow = mainLiveFeed;
            m_camCapture.start();
        }

        /// <summary>
        /// Establishes a listening connection 
        /// </summary>
        private void setupProcessingListener()
        {
            //Sets the altered feed to the image if the altered feed is on screen
            m_handler = (imgData) =>
            {   
                if (tabControl1.SelectedIndex == 0)
                    mainAlteredFeed.Image = imgData.Image;
                else
                    testingPic.Image = imgData.Image;
            };

            m_processing.ReturnImageFilled += m_handler;
        }

        /// <summary>
        /// removes the handler from the event
        /// </summary>
        private void disposeListener()
        {

            m_processing.ReturnImageFilled -= m_handler;
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
                m_camCapture.CaptureWindow = (tabControl1.SelectedIndex == 0 ? mainLiveFeed : capturePic);
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
            disposeListener();
            setupProcessingListener();

            switch (tabControl1.SelectedIndex)
            {
                case 0:
                    m_camCapture.CaptureWindow = mainLiveFeed;
                    break;
                case 1:
                    m_camCapture.CaptureWindow = capturePic;
                    capButton.Text = "Capture";
                    break;
            }

            m_camCapture.start();
        }

        /// <summary>
        /// Initialize for testing purposes
        /// </summary>
        private void testInit_Click(object sender, EventArgs e)
        {
            if (handUsedCombo.SelectedIndex <= 0)
                handUsedWarning.Visible = true;
            else
            {
                m_processing.initialize(false, handUsedCombo.SelectedIndex);
                handUsedWarning.Visible = false;
            }
        }

        /// <summary>
        /// Either captures a single image or resumes capture
        /// </summary>
        private async void capButton_Click(object sender, EventArgs e)
        {
            //Handle changing the text
            if (capButton.Text == "Capture")
            {
                identifiedX.Text = "";
                identifiedY.Text = "";
                identifiedOri.Text = "";
                identifiedGest.SelectedIndex = 0;
                capButton.Text = "Resume";
                testButton.Visible = true;
                m_testImage = await m_camCapture.grabImage();
                transperency.Image = m_testImage;
                transperency.Visible = true;
                m_camCapture.stop();
            }
            else
            {
                capButton.Text = "Capture";
                testButton.Visible = false;
                transperency.Visible = false;
                m_camCapture.start();
            }
        }

        private void testButton_Click(object sender, EventArgs e)
        {
            if (!identifiedX.Text.Equals("") && !identifiedY.Text.Equals("") &&
                !identifiedOri.Text.Equals("") && identifiedGest.SelectedIndex > 0)
            {
                featuresWarning.Visible = false;
                //Add the stop functionality after grabbing an image
                Processing.ImageReadyHandler handler = null;
                handler = (imgData) =>
                {
                    m_processing.ReturnImageFilled -= handler;
                    extractData(imgData);
                };

                m_processing.ReturnImageFilled += handler;

                m_processing.test(m_testImage);
            }
            else
                featuresWarning.Visible = true;

        }

        /// <summary>
        /// This takes the data from the imageData object and populates the GUI
        /// </summary>
        /// <param name="imgData"></param>
        private void extractData(ImageData imgData)
        {
            //Position
            double positionError, oriDiff;
            Point identifiedCenter = new Point(Convert.ToInt32(identifiedX.Text),
                                               Convert.ToInt32(identifiedY.Text));
            Point measuredCenter = imgData.Center;
            measuredCenter.X -= imgData.Image.Width / 2;
            measuredCenter.Y = (imgData.Image.Height / 2) - measuredCenter.Y;
            measuredX.Text = measuredCenter.X.ToString();
            measuredY.Text = measuredCenter.Y.ToString();

            positionError = (Math.Abs((double)(measuredCenter.X - identifiedCenter.X) / identifiedCenter.X) +
                             Math.Abs((double)(measuredCenter.Y - identifiedCenter.Y) / identifiedCenter.Y)) * 100;
            positionChange.Text = positionError.ToString("F2");

            //Orientation
            measuredOri.Text = imgData.Orientation.ToString("F2");
            oriDiff = Convert.ToDouble(identifiedOri.Text);
            oriDiff = Math.Abs((imgData.Orientation - oriDiff + 540) % 360 - 180);
            oriDifference.Text = oriDiff.ToString("F2");

            //Gesture
            measuredGest.SelectedIndex = (int)imgData.Gesture;
            if (identifiedGest.SelectedIndex == measuredGest.SelectedIndex)
                areGesturesEqual.SelectedIndex = 1;
            else
                areGesturesEqual.SelectedIndex = 2;

        }

        private void transperency_MouseDown(object sender, MouseEventArgs e)
        {
            m_identifiedCenter = e.Location;
            m_identifiedCenter.X = m_identifiedCenter.X * m_testImage.Width / ((PictureBox)sender).Width - (m_testImage.Width / 2);
            m_identifiedCenter.Y = (m_testImage.Height / 2) - m_identifiedCenter.Y * m_testImage.Height / ((PictureBox)sender).Height;
            identifiedX.Text = m_identifiedCenter.X.ToString();
            identifiedY.Text = m_identifiedCenter.Y.ToString();
        }

        private void transperency_MouseUp(object sender, MouseEventArgs e)
        {
            double sideA2, sideB2, sideC2, angleA;
            m_identifiedOrientation = e.Location;
            m_identifiedOrientation.X = m_identifiedOrientation.X * m_testImage.Width / ((PictureBox)sender).Width - (m_testImage.Width / 2);
            m_identifiedOrientation.Y = (m_testImage.Height / 2) - m_identifiedOrientation.Y * m_testImage.Height / ((PictureBox)sender).Height;
            Point north = new Point(m_identifiedCenter.X, m_testImage.Height / 2);

            sideA2 = (north.X - m_identifiedOrientation.X) * (north.X - m_identifiedOrientation.X) +
                     (north.Y - m_identifiedOrientation.Y) * (north.Y - m_identifiedOrientation.Y);
            sideB2 = (m_identifiedOrientation.X - m_identifiedCenter.X) * (m_identifiedOrientation.X - m_identifiedCenter.X) +
                     (m_identifiedOrientation.Y - m_identifiedCenter.Y) * (m_identifiedOrientation.Y - m_identifiedCenter.Y);
            sideC2 = (north.Y - m_identifiedCenter.Y) * (north.Y - m_identifiedCenter.Y);

            //Law of cosines a^2 = b^2 + c^2 - 2bc * cosA
            //Solving for A = cos^-1((b^2 + c^2 - a^2) / 2bc)
            angleA = Math.Acos((sideB2 + sideC2 - sideA2) / (2 * Math.Sqrt(sideB2) * Math.Sqrt(sideC2))) * 180 / Math.PI;

            if (m_identifiedOrientation.X < m_identifiedCenter.X)
                angleA = 360.0 - angleA;


            identifiedOri.Text = angleA.ToString("F2");
        }

    }
}
