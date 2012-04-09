using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace Utils
{
    public static class Constants
    {
        public static float ToFarseer(float value)
        {
            return value / 64f;
        }

        public static Vector2 ToFarseer(Vector2 value)
        {
            return (value / 64f);
        }
    }
}
