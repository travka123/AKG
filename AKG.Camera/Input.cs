using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AKG.Camera
{
    public struct Input
    {
        public int msDelta = 0;
        public DateTime time = new();
        public HashSet<int> pressedKeys = new();
        public Vector2 mousePrevPosition = new();
        public Vector2 mouseCurPosition = new();
        public Vector2 mouseOffset = new();
        public bool mouseBtn1Pressed = false;

        public Input()
        {

        }
    }
}
