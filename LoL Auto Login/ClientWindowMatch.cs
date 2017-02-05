using System;
using System.Drawing;

namespace LoLAutoLogin
{
    public class ClientWindowMatch : IEquatable<ClientWindowMatch>
    {
        public IntPtr Handle { get; set; }
        public string Name { get; set; }
        public string Class { get; set; }
        public Rectangle WindowRectangle { get; }

        public ClientWindowMatch(IntPtr handle, string name, string windowClass, Rectangle rect)
        {
            Handle = handle;
            Name = name;
            Class = windowClass;
            WindowRectangle = rect;
        }

        public override string ToString()
        {
            return $"{{Handle={Handle}, Name=\"{Name}\", Class=\"{Class}\", Rectangle={WindowRectangle}}}";
        }

        public bool Equals(ClientWindowMatch other)
        {
            return Handle == other.Handle && Name == other.Name && Class == other.Class && WindowRectangle == other.WindowRectangle;
        }
    }
}
