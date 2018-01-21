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
    public partial class Form4 : Form
    {
        Form1 f1;
        public Form4(Form1 parent)
        {
            InitializeComponent();
            f1 = parent;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //Přenastavení fontu richtextboxu
            f1.protect(false); //Zrušení ochrany textu
            FontDialog fD = new FontDialog();
            try
            {
                if (fD.ShowDialog() == DialogResult.OK) //Dialog pro výběr fontu
                    f1.richTextBoxOutput.Font = fD.Font; //Nastavení fontu
            }
            catch (Exception err)
            {
                MessageBox.Show("Error: " + err.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            f1.protect(true); //Obnovení ochrany
            buttonFont.Text = f1.richTextBoxOutput.Font.ToString(); //Nastaví popis aktuálního fontu na text tlačítka
        }

        private void Form4_Load(object sender, EventArgs e)
        {
            buttonFont.Text = f1.richTextBoxOutput.Font.ToString(); //Nastaví popis aktuálního fontu na text tlačítka
            buttonBackgroundColor.BackColor = f1.richTextBoxOutput.BackColor; //Nastaví barvy aktuální barvy pozadí na tlačítko
            buttonTextColor.BackColor = f1.richTextBoxOutput.ForeColor; //Nastaví barvy aktuální barvy textu na tlačítko
            StreamReader sr = new StreamReader(@"etc\conf.txt");
            string x = "";
            for (int i = 0; i <= 3; i++) //Načte se ze souboru, jestli je richtextbox průhledný
                x = sr.ReadLine();
            if (x == "true") //Pokud je, checkbox se zaškrtne
                checkBoxTransparency.Checked = true;
            sr.Close();
            if (f1.asyncRedirection == true)
                checkBoxAsynchronous.Checked = true;
            else
                checkBoxSynchronous.Checked = true;
            if (f1.useShellRedirectionBool == true)
                checkBoxUseShellExecute.Checked = true;
            if (f1.TransparencyKey != Color.Empty)
                checkBoxTransparency.Checked = true;
            labelOpacity.Text = (100 - trackBarOpacity.Value) + "%"; //Text labelu se nastaví tak, aby ukazoval procentuální hodnotu průhlednosti formu 1
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            //Resetování nastavení fontu
            f1.protect(false); //Zrušení ochrany textu
            f1.richTextBoxOutput.Font = new Font("Consolas",10); //Změna fontu
            buttonFont.Text = f1.richTextBoxOutput.Font.ToString(); //Na tlačítko se nastaví teext popisující aktuální font
            f1.protect(true); //Obnovení ochrany textu
            f1.setCurcor(); //Nastaví kurzor na konec, aby mohl uživatel psát
        }

        private void buttonBackgroundColor_Click(object sender, EventArgs e)
        {
            DialogResult cR = colorDialog1.ShowDialog();
            try
            {
                if (cR == DialogResult.OK)
                {
                    f1.richTextBoxOutput.BackColor = colorDialog1.Color; //Nastavení barvy pozadí richtextboxu
                    buttonBackgroundColor.BackColor = f1.richTextBoxOutput.BackColor; //Nastavení barvy pozadí tlačítka

                    f1.protect(false); //Zrušení ochrany textu
                    f1.richTextBoxOutput.SelectionBackColor = f1.richTextBoxOutput.BackColor; //Musí se změnit SelectedText, protože jsem nenašel způsob, jak ho kompletně odstranit (právě včetně barvy backColor)
                    f1.protect(true); //Obnovení ochrany textu
                    f1.setCurcor();//Nastaví kurzor na konec, aby mohl uživatel psát
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("Error: " + err.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            
        }

        private void buttonTextColor_Click(object sender, EventArgs e)
        {
            f1.protect(false); //Zrušení ochrany textu
            DialogResult cR = colorDialog1.ShowDialog();
            try
            {
                if (cR == DialogResult.OK)
                {
                    f1.richTextBoxOutput.ForeColor = colorDialog1.Color;
                    buttonTextColor.BackColor = f1.richTextBoxOutput.ForeColor;
                }
            }
            catch (Exception err)
            {
                MessageBox.Show("Error: " + err.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            f1.protect(true); //Obnovení ochrany textu
            f1.setCurcor(); //Nastaví kurzor na konec, aby mohl uživatel psát
        }

        private void buttonResetColors_Click(object sender, EventArgs e)
        {
            //Resetování barev
            f1.protect(false); //Zrušení ochrany textu
            f1.richTextBoxOutput.ForeColor = Color.White; //Nastavení barvy textu na bílou
            buttonTextColor.BackColor = f1.richTextBoxOutput.ForeColor; //Nastavení barvy pozadí tlačítka
            f1.richTextBoxOutput.BackColor = Color.Black; //Nastavení barvy pozadí textu na černou
            buttonBackgroundColor.BackColor = f1.richTextBoxOutput.BackColor; //Nastavení barvy pozadí tlačítka
            f1.richTextBoxOutput.SelectionBackColor = f1.richTextBoxOutput.BackColor; //Musí se změnit SelectedText, protože jsem nenašel způsob, jak ho kompletně odstranit (právě včetně barvy backColor)
            f1.protect(true); //Obnovení ochrany textu
            f1.setCurcor(); //Nastaví kurzor na konec, aby mohl uživatel psát
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxTransparency.Checked == true) //Nastavení průhlednosti richtextboxu
                f1.TransparencyKey = f1.richTextBoxOutput.BackColor; //Richtextbox může být průhledný pouze pokud má stejnou barvu jako TransparencyKey
            else
                f1.TransparencyKey = Color.Empty;
        }

        private void buttonSave_Click(object sender, EventArgs e) //Uložení nastavení do souboru
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(@"etc\conf.txt")) //Uložení probíhá v souboru etc\conf.txt
                {
                    sw.WriteLine(f1.richTextBoxOutput.Font.Name); //Font
                    sw.WriteLine(f1.richTextBoxOutput.BackColor.ToArgb()); //Barva pozadí
                    sw.WriteLine(f1.richTextBoxOutput.ForeColor.ToArgb()); //Barva textu
                    if (checkBoxTransparency.Checked == true) //Průhlednost richtextboxu
                        sw.WriteLine("true");
                    else
                        sw.WriteLine("false");
                    sw.WriteLine(f1.Opacity); //Průhlednost formu 1
                    if (f1.asyncRedirection == true)
                        sw.WriteLine("true");
                    else
                        sw.WriteLine("false");
                    if (f1.useShellRedirectionBool == true)
                        sw.WriteLine("true");
                    else
                        sw.WriteLine("false");
                    sw.Flush();
                }
            }
            catch(Exception err)
            {
                 MessageBox.Show("Error: " + err.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close(); //Zavře tento form
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            //Průhlednost Formu 1
            double value = (double)(100-trackBarOpacity.Value)/100; //Získá se hodnota z trackbaru
            labelOpacity.Text = (value*100).ToString() + "%"; //Text labelu se nastaví tak, aby ukazoval procentuální hodnotu průhlednosti formu 1
            f1.Opacity = value; //Průhlednost formu 1 se nastaví na danou hodnotu
        }

        private void checkBoxSynchronous_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxSynchronous.Checked == true)
            {
                checkBoxAsynchronous.Checked = false; //Může být vybrán pouze jeden checkbox
                f1.asyncRedirection = false; //Asynchronní přesměrování se zakáže
            }
            else
            {
                checkBoxAsynchronous.Checked = true; //Může být vybrán pouze jeden checkbox
                f1.asyncRedirection = true; //Asynchronní přesměrování se povolí
            }
                
        }

        private void checkBoxAsynchronous_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxAsynchronous.Checked == true)
            {
                checkBoxSynchronous.Checked = false; //Může být vybrán pouze jeden checkbox
                f1.asyncRedirection = true; //Asynchronní přesměrování se povolí
            }
            else
            {
                checkBoxSynchronous.Checked = true; //Může být vybrán pouze jeden checkbox
                f1.asyncRedirection = false; //Asynchronní přesměrování se zakáže
            }
        }
        
        private void checkBoxUseShellExecute_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxUseShellExecute.Checked == true)
            {
                f1.useShellRedirectionBool = true;
                MessageBox.Show("Use shell Execute is set to true. This affects the system utilities! Use only when you need it.", "Use Shell Execute - Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
                f1.useShellRedirectionBool = false;
        }

        private void buttonUseShellExecuteHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Run applications in a new window without redirection. It affects the system utilities! Use only when you need it.", "Use Shell Execute - Help", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }

        private void buttonSyncHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("This is default synchronous redirection.", "Synchronous redirection - Help", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }

        private void buttonAsyncHelp_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Asynchronous redirect allows read output of applications running in shell in real time.", "Asynchronous redirection - Help", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }

             
    }
}
