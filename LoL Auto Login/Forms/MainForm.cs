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
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using LoLAutoLogin.Model;
using LoLAutoLogin.Utility;

namespace LoLAutoLogin.Forms
{
    public partial class MainForm : Form
    {
        public Profile SelectedProfile { get; private set; }

        private readonly BindingSource bindingSource = new BindingSource();
        private bool isFirstRun;

        private IList<Profile> Profiles => bindingSource.List as IList<Profile>;

        public MainForm()
        {
            InitializeComponent();
            isFirstRun = !ProfileManager.ProfilesFileExists;
            ProfilesGridView_SelectionChanged(this, new EventArgs());
            alwaysShowCheckBox.Checked = Config.GetBooleanValue("always-show-config", false);
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            bindingSource.DataSource = ProfileManager.Profiles;
            profilesGridView.DataSource = bindingSource;

            int defaultProfileIndex = Profiles.IndexOf(Profiles.FirstOrDefault(p => p.IsDefault));

            if (defaultProfileIndex > 0)
            {
                profilesGridView.Rows[defaultProfileIndex].Selected = true;
            }
        }

        private void ProfilesGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            DialogResult = DialogResult.Yes;
            Close();
        }

        private void AutoLoginButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Yes;
            Close();
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            ShowAddFormDialog();
        }

        private void ShowAddFormDialog()
        {
            AddProfileForm form = new AddProfileForm { Owner = this };

            Profile profile = form.ShowDialog();

            if (profile == null) return;

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

        private void LaunchButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.No;
            Close();
        }

        private void ProfilesGridView_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (profilesGridView.Columns[e.ColumnIndex].CellType == typeof(DataGridViewCheckBoxCell))
            {
                profilesGridView.EndEdit();
            }
        }

        private void ProfilesGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            foreach (Profile profile in Profiles)
            {
                profile.IsDefault = profile == Profiles[e.RowIndex];
            }

            bindingSource.ResetBindings(false);
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            if (bindingSource.List.Count == 0)
            {
                ShowAddFormDialog();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isFirstRun)
            {
                MessageBox.Show("You can press the SHIFT key while running LoL Auto Login to show the configuration window again.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            Config.SetValue("always-show-config", alwaysShowCheckBox.Checked);

            if (DialogResult == DialogResult.Yes && profilesGridView.SelectedRows.Count > 0)
            {
                SelectedProfile = profilesGridView.SelectedRows[0].DataBoundItem as Profile;
            }

            ProfileManager.SaveProfiles();
        }

        private void ProfilesGridView_SelectionChanged(object sender, EventArgs e)
        {
            deleteButton.Enabled = profilesGridView.SelectedRows.Count > 0;
            autoLoginButton.Enabled = profilesGridView.SelectedRows.Count == 1;
        }
    }
}
