using System.Windows.Forms;

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
