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
    public partial class Form3 : Form
    {
        Form1 f1;
        public Form3(Form1 parent)
        {
            InitializeComponent();
            f1 = parent;
        }

        private void buttonFind_Click(object sender, EventArgs e)
        {
            f1.protect(false);
            try
            {
                unselect();
                int pocetNalezenych = 0;
                int index = 0;
                while (index < f1.richTextBoxOutput.Text.LastIndexOf(textBoxFind.Text))
                {
                    f1.richTextBoxOutput.Find(textBoxFind.Text, index, f1.richTextBoxOutput.TextLength, RichTextBoxFinds.None);
                    f1.richTextBoxOutput.SelectionBackColor = Color.Red;
                    index = f1.richTextBoxOutput.Text.IndexOf(textBoxFind.Text, index) + 1;
                    pocetNalezenych++;
                }

                labelFound.Text = "Found: " + pocetNalezenych.ToString();
            }
            catch
            {
                labelFound.Text = "Found: 0";
            }
            f1.protect(true);
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            f1.protect(false);
            unselect();
            f1.protect(true);
            f1.setCurcor();
            this.Close();
        }

        private void unselect()
        {
            f1.richTextBoxOutput.SelectAll();
            f1.richTextBoxOutput.SelectionBackColor = f1.richTextBoxOutput.BackColor;            
            f1.setCurcor();
        }

        private void Form3_FormClosing(object sender, FormClosingEventArgs e)
        {
            f1.protect(false);
            unselect();
            f1.protect(true);
            f1.setCurcor();
        }
    }
}
