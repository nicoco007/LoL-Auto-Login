using System.Windows.Forms;

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
