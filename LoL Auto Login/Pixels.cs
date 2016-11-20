using System.Drawing;

/// Copyright © 2015-2016 nicoco007
///
/// Licensed under the Apache License, Version 2.0 (the "License");
/// you may not use this file except in compliance with the License.
/// You may obtain a copy of the License at
///
///     http://www.apache.org/licenses/LICENSE-2.0
///
/// Unless required by applicable law or agreed to in writing, software
/// distributed under the License is distributed on an "AS IS" BASIS,
/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and
/// limitations under the License.
namespace LoLAutoLogin
{

    internal class Pixels
    {

        public static Pixel LaunchButton = new Pixel(new PixelCoord(0.5f, true), new PixelCoord(15), Color.FromArgb(255, 170, 110, 10), Color.FromArgb(255, 220, 150, 50));
        public static Pixel PasswordBox = new Pixel(new PixelCoord(0.192f, true), new PixelCoord(0.48f, true), Color.FromArgb(255, 240, 240, 240), Color.FromArgb(255, 250, 250, 250));

    }

}
