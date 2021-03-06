﻿using ImageProcessing;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionGestureProcessing
{
    public class ImageData
    {
        public enum Gestures { INITIALIZING, MOVE, RIGHTCLICK, LEFTCLICK, CLICKANDHOLD, DOUBLECLICK };
        public bool InitialFrame { get; set; }
        public Image Image { get; set; }
        public List<Point> DataPoints { get; set; }
        public Rectangle Filter { get; set; }
        public Point Center { get; set; }
        public double[,] EigenVectors { get; set; }
        public double Orientation { get; set; }
        public List<Point> ConvexHull { get; set; }
        public List<ConvexDefect> ConvexDefects { get; set; }
        public List<Point> Contour { get; set; }
        public List<Point> FingerTips { get; set; }
        public Gestures Gesture { get; set; }

        public ImageData(bool p_isInit, Image p_image)
        {
            InitialFrame = p_isInit;
            Image = p_image;
        }
    }
}
