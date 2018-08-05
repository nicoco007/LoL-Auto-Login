// Copyright © 2015-2018 Nicolas Gnyra

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published
// by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.

// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see http://www.gnu.org/licenses/.

using System;
using System.Windows.Forms;

namespace LoLAutoLogin
{
    public partial class MainForm : Form
    {
        public bool Success { get; private set; }
        public bool CheckForUpdates { get; private set; }

        public MainForm()
        {
            InitializeComponent();

            // trigger save when the 'enter' key is pressed
            AcceptButton = saveButton;
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            // check if a password was inputted
            if (string.IsNullOrEmpty(passTextBox.Text))
            {
                MessageBox.Show(this, "You must enter a password!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            PasswordManager.Save(passTextBox.Text);

            Success = true;
            CheckForUpdates = checkBox1.Checked;

            Close();
        }

        private void infoLabel_Click(object sender, EventArgs e)
        {

        }
    }
}
