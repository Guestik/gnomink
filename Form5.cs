using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace gnomnik
{
    public partial class Form5 : Form
    {
        Form f1;
        public Form5(Form1 parent)
        {
            InitializeComponent();
            f1 = parent;
        }

        private void Form5_Load(object sender, EventArgs e)
        {
            loadUtils();
        }

        private void loadUtils()
        {
            checkedListBoxUtilities.Items.Clear();
            try
            {
                DirectoryInfo d = new DirectoryInfo(@"bin/"); //Adresář s utilitama
                FileInfo[] files = d.GetFiles("*.exe");
                foreach (FileInfo file in files)
                {
                    checkedListBoxUtilities.Items.Add(file.Name);
                }
            }
            catch
            {
                MessageBox.Show("Error has occurred while loading utilities.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close(); //Zavře tento form
            }
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close(); //Zavře tento form
        }

        private void buttonAddNew_Click(object sender, EventArgs e)
        {
            string selectedPath;
            OpenFileDialog addUtil = new OpenFileDialog();
            addUtil.Filter = "Exe Files|*.exe";
            addUtil.Title = "Select utility";
            if (addUtil.ShowDialog() == DialogResult.OK)
            {
                selectedPath = addUtil.FileName;
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(addUtil.FileName);
                try
                {
                    File.Copy(selectedPath, "bin/" + fileNameWithoutExtension + ".exe");
                }
                catch
                {
                    MessageBox.Show("Error has occurred while copying utility.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            loadUtils();
        }

        private void buttonDelete_Click(object sender, EventArgs e) //Smazání utility
        {
            DialogResult dialogResult = MessageBox.Show("Do you really want to delete checked utilities? This is an irreversible action.", "Delete", MessageBoxButtons.YesNo);
            try
            {
                if (dialogResult == DialogResult.Yes)
                {
                    foreach (object itemChecked in checkedListBoxUtilities.CheckedItems)
                    {
                        File.Delete("bin/" + itemChecked.ToString());
                    }
                }
            }
            catch
            {
                MessageBox.Show("Error has occurred while deleting utilities.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            loadUtils();
        }
    }
}
