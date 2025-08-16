using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIDIGimp
{
    public static class MIDIFunc
    {
        public static float MapValue(int x)
        {
            float xMin = 0;
            float xMax = 127;
            float yMin = 0.5f;
            float yMax = 2.0f;

            float y = yMin + ((x - xMin) * (yMax - yMin)) / (xMax - xMin);
            return y;
        }
    }
}
