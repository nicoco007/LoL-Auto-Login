using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace LoLAutoLogin
{
    class Pixel
    {
        Point Position { get; set; }
        Color Color { get; set; }

        public Pixel(Point position, Color color)
        {
            this.Color = color;
            this.Position = position;
        }

        public bool Compare(Bitmap bmp)
        {
            return bmp.GetPixel(this.Position.X, this.Position.Y) == this.Color;
        }
    }
}