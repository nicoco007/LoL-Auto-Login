using System.Drawing;

/// LoL Auto Login - Automatic Login for League of Legends
/// Copyright © 2015-2016 nicoco007
///
/// This program is free software: you can redistribute it and/or modify
/// it under the terms of the GNU Affero General Public License as published
/// by the Free Software Foundation, either version 3 of the License, or
/// (at your option) any later version.
///
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
/// GNU Affero General Public License for more details.
///
/// You should have received a copy of the GNU Affero General Public License
/// along with this program. If not, see http://www.gnu.org/licenses/.
namespace LoLAutoLogin
{

    class Pixels
    {

        public static Pixel LaunchButton = new Pixel(new PixelCoord(0.5f, true), new PixelCoord(15), Color.FromArgb(255, 170, 110, 10), Color.FromArgb(255, 210, 140, 30));
        public static Pixel PasswordBox = new Pixel(new PixelCoord(0.192f, true), new PixelCoord(0.48f, true), Color.FromArgb(255, 240, 240, 240), Color.FromArgb(255, 250, 250, 250));

    }

}
