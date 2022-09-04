using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AKG.Camera.Controls
{
    public abstract class CameraControl
    {
        public abstract bool Process(Input input);
    }
}
