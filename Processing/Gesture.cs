using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Processing
{
    class Gesture
    {
        enum Gestures { NoGesture, RightClick, LeftClick, ClickAndHold, DoubleClick}
        public delegate void gestureCaptured(Gestures g, Image i);
    }
}
