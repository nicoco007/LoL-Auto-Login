namespace LoLAutoLogin
{

    public class PixelCoord
    {

        public float Coordinate { get; }
        public bool Relative { get; }

        public PixelCoord(float coord)
        {

            Coordinate = coord;
            Relative = false;

        }

        public PixelCoord(float coord, bool relative)
        {

            Coordinate = coord;
            Relative = relative;

        }

    }

}
