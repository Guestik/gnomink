using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gnomnik
{
    public partial class Form2 : Form
    {
        Form1 f1;
        public Form2(Form1 parent)
        {
            InitializeComponent();
            f1 = parent;
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            f1.setTitle(textBoxTitle.Text);
            f1.title = textBoxTitle.Text;
            this.Close();
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Form2_Load(object sender, EventArgs e)
        {

        }
    }
}
