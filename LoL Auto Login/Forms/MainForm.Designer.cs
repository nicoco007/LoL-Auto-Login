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

namespace LoLAutoLogin.Forms
{
    partial class MainForm
    {
        /// <summary>
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur Windows Form

        /// <summary>
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.quitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.deleteButton = new System.Windows.Forms.Button();
            this.addButton = new System.Windows.Forms.Button();
            this.profilesGroupBox = new System.Windows.Forms.GroupBox();
            this.profilesGridView = new System.Windows.Forms.DataGridView();
            this.autoLoginButton = new System.Windows.Forms.Button();
            this.launchButton = new System.Windows.Forms.Button();
            this.alwaysShowCheckBox = new System.Windows.Forms.CheckBox();
            this.menuStrip.SuspendLayout();
            this.profilesGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.profilesGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.toolsToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(432, 24);
            this.menuStrip.TabIndex = 1;
            this.menuStrip.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.quitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // quitToolStripMenuItem
            // 
            this.quitToolStripMenuItem.Name = "quitToolStripMenuItem";
            this.quitToolStripMenuItem.Size = new System.Drawing.Size(97, 22);
            this.quitToolStripMenuItem.Text = "Quit";
            this.quitToolStripMenuItem.Click += new System.EventHandler(this.QuitToolStripMenuItem_Click);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.optionsToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // optionsToolStripMenuItem
            // 
            this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
            this.optionsToolStripMenuItem.Size = new System.Drawing.Size(116, 22);
            this.optionsToolStripMenuItem.Text = "Options";
            // 
            // deleteButton
            // 
            this.deleteButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.deleteButton.Location = new System.Drawing.Point(87, 124);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(75, 23);
            this.deleteButton.TabIndex = 8;
            this.deleteButton.Text = "Delete";
            this.toolTip.SetToolTip(this.deleteButton, "Delete the selected profile");
            this.deleteButton.UseVisualStyleBackColor = true;
            this.deleteButton.Click += new System.EventHandler(this.DeleteButton_Click);
            // 
            // addButton
            // 
            this.addButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.addButton.Location = new System.Drawing.Point(6, 124);
            this.addButton.Name = "addButton";
            this.addButton.Size = new System.Drawing.Size(75, 23);
            this.addButton.TabIndex = 7;
            this.addButton.Text = "Add";
            this.toolTip.SetToolTip(this.addButton, "Add a profile (username and password)");
            this.addButton.UseVisualStyleBackColor = true;
            this.addButton.Click += new System.EventHandler(this.AddButton_Click);
            // 
            // profilesGroupBox
            // 
            this.profilesGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.profilesGroupBox.Controls.Add(this.profilesGridView);
            this.profilesGroupBox.Controls.Add(this.deleteButton);
            this.profilesGroupBox.Controls.Add(this.addButton);
            this.profilesGroupBox.Location = new System.Drawing.Point(12, 27);
            this.profilesGroupBox.Name = "profilesGroupBox";
            this.profilesGroupBox.Size = new System.Drawing.Size(408, 153);
            this.profilesGroupBox.TabIndex = 7;
            this.profilesGroupBox.TabStop = false;
            this.profilesGroupBox.Text = "Profiles";
            // 
            // profilesGridView
            // 
            this.profilesGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.profilesGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.profilesGridView.BackgroundColor = System.Drawing.SystemColors.Desktop;
            this.profilesGridView.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
            this.profilesGridView.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.None;
            this.profilesGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.profilesGridView.Location = new System.Drawing.Point(6, 19);
            this.profilesGridView.MultiSelect = false;
            this.profilesGridView.Name = "profilesGridView";
            this.profilesGridView.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Sunken;
            this.profilesGridView.RowHeadersVisible = false;
            this.profilesGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.profilesGridView.Size = new System.Drawing.Size(396, 99);
            this.profilesGridView.TabIndex = 9;
            this.profilesGridView.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.ProfilesGridView_CellDoubleClick);
            this.profilesGridView.CellMouseUp += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.ProfilesGridView_CellMouseUp);
            this.profilesGridView.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.ProfilesGridView_CellValueChanged);
            this.profilesGridView.SelectionChanged += new System.EventHandler(this.ProfilesGridView_SelectionChanged);
            // 
            // autoLoginButton
            // 
            this.autoLoginButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.autoLoginButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.autoLoginButton.Location = new System.Drawing.Point(31, 228);
            this.autoLoginButton.Name = "autoLoginButton";
            this.autoLoginButton.Size = new System.Drawing.Size(371, 29);
            this.autoLoginButton.TabIndex = 11;
            this.autoLoginButton.Text = "Launch League of Legends with Auto Login";
            this.autoLoginButton.UseVisualStyleBackColor = true;
            this.autoLoginButton.Click += new System.EventHandler(this.AutoLoginButton_Click);
            // 
            // launchButton
            // 
            this.launchButton.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.launchButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.launchButton.Location = new System.Drawing.Point(31, 263);
            this.launchButton.Name = "launchButton";
            this.launchButton.Size = new System.Drawing.Size(371, 23);
            this.launchButton.TabIndex = 12;
            this.launchButton.Text = "Launch League of Legends Normally";
            this.launchButton.UseVisualStyleBackColor = true;
            this.launchButton.Click += new System.EventHandler(this.LaunchButton_Click);
            // 
            // alwaysShowCheckBox
            // 
            this.alwaysShowCheckBox.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.alwaysShowCheckBox.AutoSize = true;
            this.alwaysShowCheckBox.Location = new System.Drawing.Point(79, 195);
            this.alwaysShowCheckBox.Name = "alwaysShowCheckBox";
            this.alwaysShowCheckBox.Size = new System.Drawing.Size(272, 17);
            this.alwaysShowCheckBox.TabIndex = 13;
            this.alwaysShowCheckBox.Text = "Always show this before running League of Legends";
            this.alwaysShowCheckBox.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(432, 298);
            this.Controls.Add(this.alwaysShowCheckBox);
            this.Controls.Add(this.launchButton);
            this.Controls.Add(this.autoLoginButton);
            this.Controls.Add(this.profilesGroupBox);
            this.Controls.Add(this.menuStrip);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "LoL Auto Login";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.profilesGroupBox.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.profilesGridView)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem quitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsToolStripMenuItem;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.GroupBox profilesGroupBox;
        private System.Windows.Forms.Button deleteButton;
        private System.Windows.Forms.Button addButton;
        private System.Windows.Forms.Button autoLoginButton;
        private System.Windows.Forms.Button launchButton;
        private System.Windows.Forms.CheckBox alwaysShowCheckBox;
        private System.Windows.Forms.DataGridView profilesGridView;
    }
}

