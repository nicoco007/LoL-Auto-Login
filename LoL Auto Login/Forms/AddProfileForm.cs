using System.Windows.Forms;

namespace LoLAutoLogin.Forms
{
    public partial class AddProfileForm : Form
    {
        private bool save = false;

        public AddProfileForm()
        {
            InitializeComponent();
        }

        public new Profile ShowDialog()
        {
            base.ShowDialog();

            if (!save) return null;

            return new Profile(usernameTextBox.Text, passwordTextBox.Text);
        }

        private void SaveButton_Click(object sender, System.EventArgs e)
        {
            save = true;
            Close();
        }

        private void CancelButton_Click(object sender, System.EventArgs e)
        {
            Close();
        }
    }
}
