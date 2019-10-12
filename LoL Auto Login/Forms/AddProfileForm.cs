using System.Windows.Forms;
using LoLAutoLogin.Model;

namespace LoLAutoLogin.Forms
{
    public partial class AddProfileForm : Form
    {
        private bool save;

        public AddProfileForm()
        {
            InitializeComponent();
        }

        public new Profile ShowDialog()
        {
            base.ShowDialog();

            if (!save) return null;

            return new Profile(usernameTextBox.Text.Trim(), passwordTextBox.Text.Trim());
        }

        private void SaveButton_Click(object sender, System.EventArgs e)
        {
            bool invalid = false;

            if (string.IsNullOrWhiteSpace(usernameTextBox.Text))
            {
                errorProvider.SetError(usernameTextBox, "Username cannot be empty");
                invalid = true;
            }

            if (string.IsNullOrEmpty(passwordTextBox.Text))
            {
                errorProvider.SetError(passwordTextBox, "Password cannot be empty");
                invalid = true;
            }

            if (invalid) return;

            save = true;
            Close();
        }

        private void CancelButton_Click(object sender, System.EventArgs e)
        {
            Close();
        }

        private void UsernameTextBox_TextChanged(object sender, System.EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(usernameTextBox.Text))
            {
                errorProvider.SetError(usernameTextBox, null);
            }
        }

        private void PasswordTextBox_TextChanged(object sender, System.EventArgs e)
        {
            if (!string.IsNullOrEmpty(passwordTextBox.Text))
            {
                errorProvider.SetError(passwordTextBox, null);
            }
        }

        private void UsernameTextBox_Leave(object sender, System.EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(usernameTextBox.Text))
            {
                errorProvider.SetError(usernameTextBox, "Username cannot be empty");
            }
        }

        private void PasswordTextBox_Leave(object sender, System.EventArgs e)
        {
            if (string.IsNullOrEmpty(passwordTextBox.Text))
            {
                errorProvider.SetError(passwordTextBox, "Password cannot be empty");
            }
        }
    }
}
