// Copyright © 2015-2019 Nicolas Gnyra

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

using LoLAutoLogin.Managers;
using System;
using System.Windows.Forms;

namespace LoLAutoLogin.Forms
{
    public partial class MainForm : Form
    {
        private bool signIn = false;
        private readonly BindingSource bindingSource = new BindingSource();

        public MainForm()
        {
            InitializeComponent();
        }

        public new Profile ShowDialog()
        {
            base.ShowDialog();
            
            if (!signIn) return null;

            return bindingSource.List[profilesGridView.SelectedRows[0].Index] as Profile;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            var profiles = ProfileManager.GetProfiles();
            bindingSource.DataSource = profiles;
            profilesGridView.DataSource = bindingSource;
        }

        private void ProfilesGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            signIn = true;
            Close();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            signIn = true;
            Close();
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            AddProfileForm form = new AddProfileForm() { Parent = this };

            Profile profile = form.ShowDialog();
            ProfileManager.AddProfile(profile);
            bindingSource.ResetBindings(false);
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            ProfileManager.DeleteProfile(profilesGridView.SelectedRows[0].Index);
            bindingSource.ResetBindings(false);
        }

        private void QuitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
