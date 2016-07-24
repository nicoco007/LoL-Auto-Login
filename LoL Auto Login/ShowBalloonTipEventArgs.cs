using System.Windows.Forms;

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

    public class ShowBalloonTipEventArgs
    {

        public string Title { get; }
        public string Message { get; }
        public ToolTipIcon Icon { get; }

        public ShowBalloonTipEventArgs(string title, string message, ToolTipIcon icon)
        {

            Title = title;
            Message = message;
            Icon = icon;

        }

    }

}
