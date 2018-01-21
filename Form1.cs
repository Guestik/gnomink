//Shell gnomnik - Lukáš Anděl
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Principal;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace gnomnik
{
    public partial class Form1 : Form
    {
        Process proc; //Spouštěný proces
        public string title = "Gnomnik"; //Defaultní titulek (Změní se při změně titulku uživatelem)
        string prompt; //uživatel@počítač:pracovní_adresář$ - výzva k uživateli
        string currentDir; //Aktuální/pracovní adresář (zároveň startovní adresář)
        int count = 0; //Počítadlo stisknutí Tabu (kvůli doplňování)
        int upCount = 0; //Počítadlo pro příkaz history (sčítání stisknutí klávesy Up)
        public bool asyncRedirection = false;
        public bool useShellRedirectionBool = false;
        public Form1()
        {
            InitializeComponent();
        }

        public void setTitle(string title) //Změní titulek hlavního formu (Formu 1)
        {
            this.Text = title;
        }

        private void writeLine(string text) //Console.WriteLine(); do hlavního richtextboxu
        {
            richTextBoxOutput.AppendText(text + "\n");
        }

        private void work()
        {
            int totalLines = richTextBoxOutput.Lines.Length; //Získání počtu řádků v richtextboxu
            string command = "";
            try
            {
                command = richTextBoxOutput.Lines[totalLines - 1].Remove(0, prompt.Length + 1); //Odstranění promptu od nově zadaného commandu
            }
            catch
            {
                //Ošetření kvůli "blbosti" uživatele. Program nespadne a začne na dalším řádku s novou výzvou.
            }

            richTextBoxOutput.AppendText("\n"); //Enter je po stisku klávesy blokován, aby se dal příkaz potvrdit odkudkoli, nejen z konce řádku. Zde je tedy jeho náhrada.

            if (command == "") //Je-li odentrováno "nic", není čím se dále zabývat
                return;

            if (!String.IsNullOrEmpty(command)) //Každý příkaz se zapíše do souboru, odkud ho pak lze načíst stiskem klávesy Up (součáct příkazu history)
            {
                FileStream fs = new FileStream(@"etc\history.txt", FileMode.Append);
                StreamWriter file = new StreamWriter(fs);
                file.WriteLine(command);
                file.Close();
            }

            //Pokud je v souboru více jak 50 řádků, počet se zredukuje na 50
            var lines = File.ReadLines(@"etc\history.txt").Count();
            if (lines > 50)
            {
                var reduceLines = File.ReadAllLines(@"etc\history.txt").Skip(lines - 50);
                File.WriteAllLines(@"etc\history.txt", reduceLines);
            }

            //Aktuální adresář se získá z promptu
            //Ten se rozdělí podle znaků ':', '', '' - tyto znaky ohraničují aktuální adresář (cestu) a název PC takové znaky nesmí obsahovat
            int count2 = 0;
            string[] split2 = prompt.Split(new Char[] { ':', '$', '#' });
            foreach (string s in split2)
            {
                if (count2 == 1)
                    currentDir = s;
            }
            if (currentDir == "~") //Pokud se currentDir rovná znaku domovského adresáře, je nutné ho nahradit názvem (cestou) domovského adresáře
            {
                currentDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); //Získá se domovský adresář uživatele a nahradí se tím řeťezec currentDir
                currentDir = reverseSlash(currentDir); //Obrácení lomítek
            }

            //Postup shellu ve vyhledávání povelů:
            //1. Vnitřní příkazy
            //   1.1 - Příkazy uvnitř shellu
            //2. Vnější příkazy
            //   2.1 - Příkazy v adresáři /bin
            //      2.1.1 - Zkusí spustit utilitu (executable soubor)
            //   2.2 - Příkazy v aktuálním adresáři
            //      2.2.1 - Zkusí spustit utilitu (executable soubor)
            //      2.2.2 - Zkusí spustit jakýkoli soubor
            //   2.3 - Příkazy s úplnou cestou
            //      2.3.1 - Zkusí spustit utilitu (executable soubor)
            //      2.3.2 - Zkusí spustit jakýkoli soubor
            //3. Příkaz neexistuje

            string[] split = Regex.Split(command, "&&"); //Rozdělení podle "&&" - řetězení příkazů
            foreach (string action in split) //Cyklus jede znovu pro každé další zřetězené příkazy
            {
                List<string> commandParts = new List<string>();
                string[] splitBySpaces = action.Trim().Split(new Char[] { ' ' }); //Rozdělení commandu podle mezer
                foreach (string commandPart in splitBySpaces)
                {
                    commandParts.Add(commandPart); //Vložení všech částí commandu do listu
                }
                int countOfParts = commandParts.Count; //Počet commandPartů v listu

                //1. Vnitřní příkazy
                //1.1 - příkazy uvnitř shellu
                if (commandParts[0] == "cd")
                {
                    currentDir = changeDirectory(countOfParts, commandParts, currentDir);
                }
                else if (commandParts[0] == "exit")
                {
                    System.Windows.Forms.Application.Exit();
                }
                else if (commandParts[0] == "mkdir")
                {
                    //mkdir - Basic command
                    string patchWithGap = unGap(countOfParts, commandParts); //Bližší popis metody v jejím kódu

                    if (System.IO.Directory.Exists(currentDir + "/" + patchWithGap)) //Relativní cesta
                        writeLine("mkdir: cannot create directory '" + currentDir + "/" + patchWithGap + "': Directory exist");
                    else
                    {
                        try
                        {
                            System.IO.Directory.CreateDirectory(currentDir + "/" + patchWithGap); //Vytvoří adresář se zadanou relativní cestou
                        }
                        catch //Pokud se jedná o absolutní cestu, program spadne do catch
                        {
                            try
                            {
                                if (System.IO.Directory.Exists(patchWithGap)) //Pokud takový adresář již existuje, nelze tudíž vytvořit. Namísto toho se zobrazí text oznamující tuto skutečnost
                                    writeLine("mkdir: cannot create directory '" + patchWithGap + "': Directory exist");
                                else
                                    System.IO.Directory.CreateDirectory(patchWithGap); //Vytvoří adresář se zadanou absolutní cestou
                            }
                            catch
                            {
                                writeLine("mkdir: cannot create directory");
                            }
                        }
                    }
                }
                else if (commandParts[0] == "update") //Pro aktualizaci programu
                {
                    updateCommand(countOfParts, commandParts);
                }
                else if (commandParts[0] == "help")
                {
                    //help command
                    writeLine(@"GNU gnomnik, version 1.0.0.0
The project Gnomnik uses standard these commands. Type 'help' to see this list.

Default internal commands:

cd [dir]
clear
exit
help
history
update

Default external commands:

cat
cp [-r] [-v] [source dir/file] [target]
date
grep [-n] [-v] [-c] [source file]
head [-n] [-v] [source file]
ls [-a] [-l] [-h] [target dir]
mkdir [target]
mv [source dir/file] [target]
pwd
rm [target dir/file]
rmdir [target dir]
sort [-r] [-R] [-v] [source file]
tail [-n] [-v] [source file]
wc [-m] [-w] [-l] [source file]
whoami");
                }
                else if (commandParts[0] == "clear")
                {
                    //clear command
                    richTextBoxOutput.Text = "";
                }
                else if (commandParts[0] == "history")
                {
                    //history command
                    if (File.Exists(@"etc\history.txt") == true) //Pokud soubor se záznamy existuje
                    {
                        StreamReader sr = new StreamReader(@"etc\history.txt"); //Soubor s předešlými příkazy
                        string commandBefore = sr.ReadLine(); //Přečte první řádek
                        while (commandBefore != null) //Pokračuje dokud jsou v souboru řádky
                        {
                            richTextBoxOutput.AppendText(commandBefore + "\n");
                            commandBefore = sr.ReadLine(); //Přečte další řádek
                        }
                        sr.Close(); //Uzavře soubor
                    }
                    else
                        richTextBoxOutput.AppendText("history: no commands yet\n");
                }
                else //2. Vnější příkazy
                {
                    //Nejprve se získají argumenty z listu
                    string arguments = "";
                    foreach (string s in commandParts)
                    {
                        if (s != commandParts[0])
                        {
                            arguments = arguments + " " + s;
                        }
                    }

                    try //2.1 - příkazy v adresáři /bin
                    {
                        //2.1.1 - Zkusí spustit utilitu (executable soubor)
                        //Původně: startProcess(@"bin\" + commandParts[0], arguments.Trim(), currentDir);
                        startProcess(@"bin\", action, currentDir);
                    }
                    catch
                    {
                        try //2.2 - příkazy v aktuálním adresáři
                        {
                            try //2.2.1 - Zkusí spustit utilitu (executable soubor)
                            {
                                //C:\Aktuální adresář\příkaz.přípona
                                //Původně: startProcess(@currentDir + "\\" + commandParts[0], arguments.Trim(), currentDir);
                                startProcess(@currentDir + "\\", action, currentDir);
                            }
                            catch //2.2.2 - Zkusí spustit jakýkoli soubor
                            {
                                //C:\Aktuální adresář\příkaz.přípona
                                if (System.IO.Directory.Exists(@currentDir + "\\" + commandParts[0])) //Když takový adresář existuje, donutí program spadnout do catch, aby ho Process.Start nespustil s exploreru
                                    throw new Exception();
                                //Původně: Process.Start(@currentDir + "\\" + commandParts[0], arguments.Trim());
                                Process.Start(@currentDir + "\\" + commandParts[0], arguments.Trim());
                            }
                            //Process.Start umí otevřít soubory v defaultních programech, nikoli pouze spustitelné soubory.
                            //V linuxu je každý program spustitelný z \usr\bin, ve windowsu nikoli.
                            //Proto je zde stejná funkčnost jakou má cmd - spouštění jakýchkoli souborů odkudkoli bez nutnosti znát software potřebný pro spuštění.
                        }
                        catch
                        {
                            //2.3 - příkazy s úplnou cestou
                            try
                            {
                                try //2.3.1 - Zkusí spustit utilitu (executable soubor)
                                {
                                    //Původně: startProcess(@commandParts[0], arguments.Trim(), currentDir);
                                    startProcess("", action, currentDir);
                                }
                                catch //2.3.2 - Zkusí spustit jakýkoli soubor
                                {
                                    if (System.IO.Directory.Exists(@commandParts[0])) //Když takový adresář existuje, donutí program spadnout do catch, aby ho Process.Start nespustil s exploreru
                                        throw new Exception();
                                    Process.Start(@commandParts[0], arguments.Trim());
                                }
                            }
                            catch //3. Příkaz neexistuje
                            {
                                writeLine(commandParts[0] + ": command not found");
                                setCurcor();
                                setTitle(title);
                            }
                        }
                    }
                }
            }
            currentDir = reverseSlash(currentDir); //Obrácení lomítek

            while (currentDir.Contains("//")) //Dokud řetězec obsahuje dvě lomítka vedle sebe, odtraňuje je do té doby, dokud de nikde v řetězci nenachazí více jak jedno vedle sebe
                currentDir = currentDir.Replace(@"//", "/");

            if (currentDir == reverseSlash(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile))) //Pokud je currentDir roven domovskému adresáři uživatele, nastaví se místo něj znak "~"
                currentDir = "~";
            //currentDir se může během průběhu příkazu změnit, je tudíž nutné pozměnit prompt
            prompt = Environment.UserName.ToLower() + "@" + Environment.MachineName.ToLower() + ":" + currentDir + rights();
        }

        public string reverseSlash(string input) //Obrácení lomítek
        {
            //V Unixu se adresáře dělí klasickými lomítky, tudíž se všechna zpětná lomítka nahradí klasickými (pouze pro lepší efekt při zobrazení promptu)
            string[] splitString = input.Split(new Char[] { '\\' });
            input = "";
            foreach (string subString in splitString)
            {
                input += subString + "/"; //Výsledný řetězec obsahující pouze klasické lomítka
            }
            return input = input.Remove(input.Length - 1); //Odstraní se poslední znak z řeťezce (tím je lomítko), aby nebyl mezi adresáři větší počet lomítek než jedna
        }

        //void display(string output) //ASYNC
        //{
        //    if (richTextBoxOutput.InvokeRequired && !String.IsNullOrEmpty(output))
        //    {
        //        richTextBoxOutput.Invoke(new Action(() => richTextBoxOutput.AppendText(output + "\n")));
        //    }
        //}

        private void AppendLine(string line)
        {
            if (richTextBoxOutput.InvokeRequired)
            {
                Action act = () =>
                {
                    this.richTextBoxOutput.AppendText(line + Environment.NewLine);
                };

                // UI objects must be accessed in UI thread
                this.BeginInvoke(act);
            }
            else
            {
                richTextBoxOutput.AppendText(line + Environment.NewLine);
            }
        }

        private void startProcess(string commandLocation, string fullCommand, string workingDirectory)
        {
            string procesName = "";
            string inputText = "";
            string stderr = "";
            int counter = 1;
            string[] splitBySpaces = fullCommand.Trim().Split(new Char[] { ' ' }); //Rozloží se podle mezer
            List<string> typeOfRedirecting = new List<string>(); //List, ve kterém jsou zaznamenávány typy přesměrování

            //PŘESMĚROVÁNÍ - FUNKCE
            //> soubor : standardní výstup je přesměrován do zadaného souboru.
            //>> soubor : přesměruj standardní výstup do souboru. Jestliže soubor existuje, je výstup přidáván na konec souboru .
            //< soubor : standardní vstup je nahrazen obsahem souboru.

            foreach (string word in splitBySpaces)
            {
                //Zjistí se počet přesměrování ("|", ">", ">>", "<")
                if (word == "|")
                {
                    counter++;
                    typeOfRedirecting.Add("|"); //Zapíše do listu typ přesměrování
                }
                else if (word == ">")
                {
                    counter++;
                    typeOfRedirecting.Add(">"); //Zapíše do listu typ přesměrování
                }
                else if (word == ">>")
                {
                    counter++;
                    typeOfRedirecting.Add(">>"); //Zapíše do listu typ přesměrování
                }
                else if (word == "<")
                {
                    counter++;
                    typeOfRedirecting.Add("<"); //Zapíše do listu typ přesměrování
                }

            }

            List<string>[] arrayOfLists = new List<string>[counter]; //Založí se pole, které bude obsahovat listy, které se budou postupně plnit s příkazy a argumenty. Založí se tolik polí, kolikrát se spouští nový proces (tznm. přesměrování + 1)
            //Na nultém indexu v každém listu tohoto pole se nachází vždy příkaz, na dalších argumenty
            int foreachCounter = 0; //Počítadlo následujícího foreachu
            foreach (string word in splitBySpaces)
            {
                if (word == "|" || word == ">" || word == ">>" || word == "<") //Pokud "word" obsahuje jedno z přesměrování, následuje příkaz do kterého byl přesměrován výstup předchozího procesu
                    foreachCounter++;
                else
                {
                    try
                    {
                        arrayOfLists[foreachCounter].Add(word); //Přidá se do daného pole argument
                    }
                    catch //Pokud tento list ještě neexistuje, musí se nejprve založit a přidat do něj příkaz. Až poté následují argumenty
                    {
                        arrayOfLists[foreachCounter] = new List<string>(); //Založí se list v poli pro příkaz + argumenty
                        arrayOfLists[foreachCounter].Add(word); //Přidá se do daného pole příkaz
                    }
                }
            }

            for (int x = 0; x < counter; x++)
            {
                bool proces = false; //false = nedochází k procesu, je to přesměrováno
                //Nejprve se získají argumenty z listu
                string arguments = "";
                procesName = arrayOfLists[x][0]; //Zjistí příkaz z listu
                foreach (string s in arrayOfLists[x])
                {
                    if (s != arrayOfLists[x][0])
                    {
                        arguments = arguments + " " + s; //Zjistí argumenty z listu
                    }
                }

                if (x == 0) //Pokud jde cyklus poprvé, nejedná se zatím o žádné přesměrování
                    proces = true;
                else if (typeOfRedirecting[x - 1] == ">") //Pokud bude tento proces ovlivňován přesměrování ">"
                {
                    this.setTitle("Redirecting ...");
                    //> soubor : standardní výstup je přesměrován do zadaného souboru
                    FileStream fs = new FileStream(Path.Combine(workingDirectory, arrayOfLists[x][0]), FileMode.Create); //Soubor se založí nebo přepíše.
                    StreamWriter file = new StreamWriter(fs, Encoding.UTF8);
                    file.WriteLine(inputText);
                    file.Close();
                    this.setTitle(title);
                }
                else if (typeOfRedirecting[x - 1] == ">>") //Pokud bude tento proces ovlivňován přesměrování ">>"
                {
                    this.setTitle("Redirecting ...");
                    //>> soubor : přesměruj standardní výstup do souboru. Jestliže soubor existuje, je výstup přidáván na konec souboru .
                    FileStream fs = new FileStream(Path.Combine(workingDirectory, arrayOfLists[x][0]), FileMode.Append);
                    StreamWriter file = new StreamWriter(fs, Encoding.UTF8);
                    file.WriteLine(inputText);
                    file.Close();
                    this.setTitle(title);
                }
                else if (typeOfRedirecting[x - 1] == "<") //Pokud bude tento proces ovlivňován přesměrování "<"
                {
                    this.setTitle("Redirecting ...");
                    //< soubor : standardní vstup je nahrazen obsahem souboru.
                    FileStream fs = new FileStream(Path.Combine(workingDirectory, arrayOfLists[x][0]), FileMode.Open);
                    StreamReader file = new StreamReader(fs, Encoding.UTF8);
                    inputText = file.ReadToEnd();
                    file.Close();
                    procesName = arrayOfLists[x - 1][0]; //Zjistí příkaz z listu
                    foreach (string s in arrayOfLists[x - 1])
                    {
                        if (s != arrayOfLists[x - 1][0])
                        {
                            arguments = arguments + " " + s; //Zjistí argumenty z listu
                        }
                    }
                    proces = true;
                    this.setTitle(title);
                }
                else
                    proces = true;

                if (proces == true)
                {
                    //VÝSTUP PŘESMĚROVÁN Z CONSOLE DO RICHTEXTBOXU

                    proc = new Process();
                    proc.StartInfo.FileName = commandLocation + procesName; //Procesu se přiděli název
                    proc.StartInfo.UseShellExecute = useShellRedirectionBool; //Chceme spustit v shellu, nikoli v novém okně console
                    proc.StartInfo.Arguments = arguments; //Argumenty procesu
                    proc.StartInfo.RedirectStandardOutput = true; //Přesměrovat výstup
                    proc.StartInfo.RedirectStandardInput = true; //Přesměrovat vstup
                    proc.StartInfo.RedirectStandardError = true; //Přesměrovat chybová hlášení
                    proc.StartInfo.WorkingDirectory = workingDirectory; //Pracovní adresář pro příkaz (vyžadují pouze některé příkazy - např. příkaz "ls")
                    proc.StartInfo.CreateNoWindow = true; //Nevytvářet nové okno
                    proc.StartInfo.StandardOutputEncoding = Encoding.GetEncoding(852); //Změna kódování výstupu na 852 (Latin II) - Zdroj: http://stackoverflow.com/questions/16803748/how-to-decode-cmd-output-correctly, všechna kódování:https://msdn.microsoft.com/cs-cz/library/system.text.encodinginfo.getencoding(v=vs.110).aspx

                    if (asyncRedirection == true && x == counter - 1) //Jedná-li se o asynchronní metodu a zároveň o poslední cykl, dojde k výpisu asynchronně
                    {
                        #region Async_redirection
                        //***********************ASYNC * **********************\\
                        proc.OutputDataReceived += (sender, args) => AppendLine(args.Data);
                        proc.ErrorDataReceived += (sender, args) => AppendLine(args.Data);
                        proc.Start(); //Spustí proces
                        proc.StandardInput.WriteLine(inputText); //Při přesměrování - standardní výstup jednoho procesu přesměruje na standardní vstup druhého
                        //proc.StandardInput.Close(); //Ukončení inputu
                        proc.BeginOutputReadLine();
                        this.setTitle("Waiting for " + proc.ProcessName + " ...");
                        while (!proc.HasExited) //Namísto proc.WaitForExit (aby form "nezamrzl")
                        {
                            Thread.Sleep(100);
                            Application.DoEvents();
                            if (!proc.Responding)
                                setTitle("Waiting for " + proc.ProcessName + " ..." + " - Not responding");
                        }
                        this.setTitle(title);
                        #endregion
                    }
                    else
                    {                        
                        #region Sync_redirection
                        this.setTitle("Working ...");
                        proc.Start(); //Spustí proces
                        proc.StandardInput.WriteLine(inputText); //Při přesměrování - standardní výstup jednoho procesu přesměruje na standardní vstup druhého
                        proc.StandardInput.Close(); //Ukončení inputu
                        inputText = proc.StandardOutput.ReadToEnd();
                        stderr = proc.StandardError.ReadToEnd();
                        if (x == counter - 1) //Jedná-li se o poslední přesměrování, dojde k výpisu
                        {
                            //Pokud by došlo k přesměrování, je nutné mít z inputu odstraněné prázdné řádky, které občas vzniknou například zalomením posledního řádku při výstupu z nějaké utility
                            inputText = Regex.Replace(inputText, @"^\s+$[\r\n]*", "", RegexOptions.Multiline); //Odstranění prázdných řádků - Zdroj: http://stackoverflow.com/questions/7647716/how-to-remove-empty-lines-from-a-formatted-string
                            richTextBoxOutput.AppendText(inputText); //Vypíše output
                            richTextBoxOutput.AppendText(stderr); //Vypíše chybová hlášení
                        }
                        proc.WaitForExit(); //Počká dokud se proces neukončí
                        proc.Close(); //Zavře proces
                        this.setTitle(title);
                        #endregion
                    }

                    #region novejsi_ale_vytezuje_cpu
                    //Process proc = new Process();
                    //proc.StartInfo.FileName = commandLocation + arrayOfLists[x][0]; //Procesu se přiděli název (Na nultém indexu v každém listu se nachází vždy příkaz, na dalších argumenty)
                    //proc.StartInfo.UseShellExecute = false; //Chceme spustit v shellu, nikoli v novém okně console
                    //proc.StartInfo.Arguments = arguments; //Argumenty procesu
                    //proc.StartInfo.RedirectStandardOutput = true; //Přesměrovat výstup
                    //proc.StartInfo.RedirectStandardInput = true;
                    ////proc.StartInfo.RedirectStandardError = true; //Přesměrovat chybová hlášení
                    //proc.StartInfo.WorkingDirectory = workingDirectory; //Pracovní adresář pro příkaz (vyžadují pouze některé příkazy - např. příkaz "ls")
                    //proc.StartInfo.CreateNoWindow = true; //Nevytvářet nové okno
                    //proc.StartInfo.StandardOutputEncoding = Encoding.GetEncoding(852); //Změna kódování výstupu na 852 (Latin II) - Zdroj: http://stackoverflow.com/questions/16803748/how-to-decode-cmd-output-correctly, všechna kódování:https://msdn.microsoft.com/cs-cz/library/system.text.encodinginfo.getencoding(v=vs.110).aspx
                    //proc.Start(); //Spustí proces


                    //proc.StandardInput.WriteLine(inputText);
                    //while (!proc.HasExited)
                    //{                        
                    //    var line = proc.StandardOutput.ReadLine();
                    //    inputText += line + "\n";

                    //    this.AppendLine(line);
                    //}

                    ////richTextBoxOutput.AppendText(proc.StandardOutput.ReadToEnd()); //Vypíše output
                    ////proc.WaitForExit(); //Počká dokud se proces neukončí
                    //proc.Close(); //Zavře proces
                    #endregion
                }
            }
        }

        public void updateCommand(int countOfParts, List<string> commandParts)
        {
            string installedVersion = ""; //Nainstalovaná verze
            string latestVersion = ""; //Aktuální verze
            writeLine("Chcecking latest version");
            using (StreamReader sr = new StreamReader(@"etc\ver.txt"))
            {
                installedVersion = sr.ReadToEnd();
            }
            System.Net.WebClient klient = new System.Net.WebClient();
            const string _latestVersionLink = "https://drive.google.com/uc?export=download&id=0B74Wcp7zbbuhaVJOVDFqWGtHQVk"; //Odkaz na textový soubor s aktuální verzí programu
            const string _latestProgramLink = "https://drive.google.com/uc?export=download&id=0B74Wcp7zbbuheURLVUtxbWVscGs"; //Odkaz na patch programu
            if (countOfParts == 1) //"update"
            {
                klient.DownloadFile(_latestVersionLink, @"etc\new_ver.txt"); //Stáhne aktuální verzi programu
                using (StreamReader sr = new StreamReader(@"etc\new_ver.txt"))
                {
                    latestVersion = sr.ReadToEnd();
                }
                if (installedVersion == latestVersion)
                {
                    writeLine("The latest version is installed");
                    System.IO.File.Delete(@"etc\new_ver.txt");
                }
                else
                {
                    writeLine("New version available: " + latestVersion);
                    writeLine("To install the latest version type update -i"); //Nainstalovat nejnovější verzi lze pomocí argumentu -i
                }
            }
            else if (commandParts[1] == "-i" || commandParts[1] == "--install") //Instalace ("-i")
            {
                klient.DownloadFile(_latestVersionLink, @"etc\new_ver.txt"); //Stáhne aktuální verzi programu
                using (StreamReader sr = new StreamReader(@"etc\new_ver.txt"))
                {
                    latestVersion = sr.ReadToEnd();
                }
                if (installedVersion == latestVersion)
                {
                    writeLine("The latest version is installed");
                    System.IO.File.Delete(@"etc\new_ver.txt");
                }
                else
                {
                    writeLine("Downloading latest version: " + latestVersion);
                    klient.DownloadFile(_latestProgramLink, @"latest_patch.zip"); //Stáhne aktuální patch programu
                    writeLine("Successfully downloaded");
                    writeLine("Going to restart and install the new version");
                    Thread.Sleep(1000);
                    Process.Start("launcher.exe"); //Spustí launcher, který nainstaluje novou verzi programu
                    System.Windows.Forms.Application.Exit(); //Ukončí se
                }
            }
            else
            {
                writeLine("Invalid arguments");
            }
        }

        public string changeDirectory(int countOfParts, List<string> commandParts, string currentDir)
        {
            //cd command
            if (countOfParts == 1) //"cd"
            {
                return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); //Jít do domovského adresáře                
            }
            else if (commandParts[1] == "..") //"cd .."
            {
                try //Jít do nadřazeného adresáře
                {
                    return currentDir = System.IO.Directory.GetParent(currentDir).FullName;

                }
                catch //Pokud rodičovský adresář (adresář nad) neexistuje
                {
                    writeLine("cd: Cannot access parent directory");
                    return currentDir;
                }
            }
            else //"cd název" nebo "cd název/název/název" nebo "cd C:/název/název/název"
            {
                #region Cesta_s_mezerou
                //Pokud obsahuje zadaná cesta k adresáři mezeru nebo zpětné lomítko, musí se odstranit tak, aby dávali (pokud možno) platý adresář
                string patchWithGap = unGap(countOfParts, commandParts);
                #endregion

                if (System.IO.Directory.Exists(commandParts[1])) //"cd C:/název/název/název"
                {
                    return commandParts[1];
                }
                else if (System.IO.Directory.Exists(patchWithGap)) //"cd C:/Program\ Files" - zpětné lomítko značí na dalším znaku mezeru
                {
                    return patchWithGap;
                }
                else if (System.IO.Directory.Exists(currentDir + "/" + patchWithGap)) //"cd Program\ Files/" - zpětné lomítko značí na dalším znaku mezeru
                {
                    return currentDir + "/" + patchWithGap;
                }
                else //"cd název" nebo "cd název/název"
                {
                    if (System.IO.Directory.Exists(currentDir + "/" + commandParts[1]))
                    {
                        return currentDir + "/" + commandParts[1];
                    }
                    writeLine("cd: No such directory");
                    return currentDir;
                }
            }

        }

        public static string unGap(int countOfParts, List<string> commandParts)
        {
            //Pokud obsahuje zadaná cesta k adresáři mezeru nebo zpětné lomítko, musí se odstranit tak, aby dávali (pokud možno) platý adresář
            //Například pole obsahující prvky "[0]cd [1]C:/Program\ [2]Files" převede na výstupní řetězec tvaru "C:/Program Files"
            string patchWithGap = ""; //Cesta s mezerou
            for (int i = 1; i < countOfParts; i++) //Do řetězce přijdou všechny prvky z pole kromě prvního, kterým je příkaz "cd"
            {
                patchWithGap += commandParts[i] + " ";
            }
            string[] splitGap = patchWithGap.Trim().Split(new Char[] { '\\' }); //Rozdělení podle zpětných lomítek
            patchWithGap = "";
            foreach (string x in splitGap) //Foreach podle zpětných lomítek
            {
                patchWithGap += x;
            }
            return patchWithGap;
        }

        public static string rights()
        {
            //Získání informací o právech uživatele
            WindowsIdentity currentIdentity = WindowsIdentity.GetCurrent();
            WindowsPrincipal currentUser = new WindowsPrincipal(currentIdentity);

            if (currentUser.IsInRole(WindowsBuiltInRole.Administrator))
                return "#"; //Administrátor
            else
                return "$"; //Obyčejný uživatel
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            //Při jakékoli změně velikosti Formu se přizpůsobí velikost richtextboxu - AKTUÁLNĚ NAHRAZENO SPOLEHLIVĚJŠÍM ANCHOREM
            //PŮVODNÍ KÓD:
            //richTextBoxOutput.Size = this.Size;
            //richTextBoxOutput.Width = richTextBoxOutput.Width - 15;
            //richTextBoxOutput.Height = richTextBoxOutput.Height - 61;
        }

        private void richTextOutput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                #region KeyUp
                //historie příkazů - součást příkazu history (Basic Command)
                protect(false);
                upCount++; //Sčítání počtu stisků klávesy Up      
                if (upCount < 0) //Pokud je hodnota v mínusu (pokud uživatek vícekrát stiskne Down), přenastaví se zpět na 1
                    upCount = 1;
                e.Handled = true;
                if (File.Exists(@"etc\history.txt") == true) //Pokud soubor se záznamy existuje
                {
                    try
                    {
                        StreamReader sr = new StreamReader(@"etc\history.txt"); //Soubor s předešlými příkazy

                        List<string> commandsBefore = new List<string>(); //List pro načtení předešlých příkazů
                        string commandBefore = sr.ReadLine(); //Přečte první řádek
                        while (commandBefore != null) //Pokračuje dokud jsou v souboru řádky
                        {
                            commandsBefore.Add(commandBefore); //Přidá do listu
                            commandBefore = sr.ReadLine(); //Přečte další řádek
                        }
                        sr.Close(); //Uzavře soubor
                        string[] commandsBeforeArray = commandsBefore.ToArray(); //List se převede na pole
                        int totalLines = richTextBoxOutput.Lines.Length; //Získání počtu řádků v richtextboxu
                        string textForReplace = richTextBoxOutput.Lines[totalLines - 1].Remove(0, prompt.Length + 1); //Získání textu, který se musí před vložením nového textu (posledního příkazu) odstranit

                        if (textForReplace.Length > 0 && upCount <= commandsBeforeArray.Length) //Pokud je nechtěný text na místě, kam se má vepsat dřívější příkaz && v poli je dostatek položek (aby se uživatel nedožadoval starších, které už v souboru nejsou)
                            richTextBoxOutput.Text = richTextBoxOutput.Text.Remove(richTextBoxOutput.Text.Length - textForReplace.Length); //Odstranění nechtěného textu
                        protect(true); //Ochrana textu před uživatelem
                        if (upCount <= commandsBeforeArray.Length && upCount > 0) //V poli je dostatek položek (aby se uživatel nedožadoval starších, které už v souboru nejsou)
                            richTextBoxOutput.AppendText(commandsBefore[commandsBefore.ToArray().Length - upCount]); //Vepsání příkazu
                        setCurcor(); //Nastavení kurzoru na konec textu
                    }
                    catch
                    {
                        //Pokud uživatel při asynchronním přesměrování nepočká na všechen výstup a použije tuto funkci, ta hodí výjimku. Tímto se tím zabrání a zároveň se při async funkce deaktivuje.
                    }
                }
                #endregion
            }
            else if (e.KeyCode == Keys.Down)
            {
                #region KeyDown
                //historie příkazů - součást příkazu history (Basic Command)
                protect(false);
                e.Handled = true;
                if (File.Exists(@"etc\history.txt") == true) //Pokud soubor se záznamy existuje
                {
                    StreamReader sr = new StreamReader(@"etc\history.txt"); //Soubor s předešlými příkazy

                    List<string> commandsBefore = new List<string>(); //List pro načtení předešlých příkazů
                    string commandBefore = sr.ReadLine(); //Přečte první řádek
                    while (commandBefore != null) //Pokračuje dokud jsou v souboru řádky
                    {
                        commandsBefore.Add(commandBefore); //Přidá do listu
                        commandBefore = sr.ReadLine(); //Přečte další řádek
                    }
                    sr.Close(); //Uzavře soubor
                    string[] commandsBeforeArray = commandsBefore.ToArray(); //List se převede na pole
                    int totalLines = richTextBoxOutput.Lines.Length; //Získání počtu řádků v richtextboxu
                    string textForReplace = richTextBoxOutput.Lines[totalLines - 1].Remove(0, prompt.Length + 1); //Získání textu, který se musí před vložením nového textu (posledního příkazu) odstranit

                    if (upCount > commandsBeforeArray.Length) //Když je hodnota vyšší než počet prvků v poli, vrátí jí zpět na poslední prvek v poli, aby mohl vypsat o 1 "nižší" prvek (příkaz)
                        upCount = commandsBeforeArray.Length;

                    upCount--; //Odečte od počtu stisků klávesy Up

                    if (textForReplace.Length > 0 && upCount <= commandsBeforeArray.Length) //Pokud je nechtěný text na místě, kam se má vepsat dřívější příkaz && v poli je dostatek položek (aby se uživatel nedožadoval starších, které už v souboru nejsou)
                        richTextBoxOutput.Text = richTextBoxOutput.Text.Remove(richTextBoxOutput.Text.Length - textForReplace.Length); //Odstranění nechtěného textu
                    protect(true); //Ochrana textu před uživatelem
                    if (upCount <= commandsBeforeArray.Length && upCount > 0) //V poli je dostatek položek (aby se uživatel nedožadoval starších, které už v souboru nejsou)
                        richTextBoxOutput.AppendText(commandsBefore[commandsBefore.ToArray().Length - upCount]); //Vepsání příkazu
                    setCurcor(); //Nastavení kurzoru na konec textu
                }
                #endregion
            }
            else if (e.KeyCode == Keys.Enter)
            {
                try
                {
                    if (!proc.HasExited) //Proces beží
                    {
                        e.Handled = false; //Povolí se klávesa Enter
                    }
                    else //Proces již běžel, ale aktuálně žádný neběží
                        e.Handled = true; //Klávesa Enter bude potlačena
                }
                catch
                {
                    //Pokud proces ještě nikdy neběžel
                    e.Handled = true; //Klávesa Enter bude potlačena
                }
            }
            else if (e.Control && e.KeyCode == Keys.C) //Ctrl + C ukončí proces spuštěný v shellu (pouze u async)
            {
                e.Handled = true;
                try
                {
                    proc.Kill();
                    
                }
                catch
                {
                    //Pokud není žádný proces spuštěn
                }
            }
            else if (e.Control && e.KeyCode == Keys.V)
            {
                e.Handled = true;

            }

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists("installer.exe"))
            {
                try
                {
                    Process installer = new Process();
                    installer.StartInfo.FileName = "installer.exe";
                    installer.Start();
                    installer.WaitForExit();
                    installer.Close();
                    File.Delete("installer.exe");
                }
                catch
                {
                }
            }
            //Terminál defaultně začíná v domovském adresáři. Pokud je domovský adresář dohledatelný, nahradí se znakem "~". V opačném případě je terminál donucen začít v alternativním adresáři. 
            if (System.IO.Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)))
                currentDir = "~";
            else
                currentDir = "C:/";
            prompt = Environment.UserName.ToLower() + "@" + Environment.MachineName.ToLower() + ":" + currentDir + rights();

            //Načtení nastavení
            #region Loading Settings
            try
            {
                StreamReader sr = new StreamReader(@"etc\conf.txt");
                richTextBoxOutput.Font = new Font(sr.ReadLine(), 10); //Načte a nastaví font
                richTextBoxOutput.BackColor = Color.FromArgb(Int32.Parse(sr.ReadLine())); //Načte a nastaví barvu pozadí
                richTextBoxOutput.ForeColor = Color.FromArgb(Int32.Parse(sr.ReadLine())); //Načte a nastaví barvu textu
                if (sr.ReadLine() == "true") //Načte se nastavení průhlednosti richtextboxu
                    TransparencyKey = richTextBoxOutput.BackColor;
                this.Opacity = Convert.ToDouble(sr.ReadLine()); //Načte se nastavení průhledosti formu
                if (sr.ReadLine() == "true")
                    asyncRedirection = true;
                if (sr.ReadLine() == "true")
                    useShellRedirectionBool = true;
                sr.Close();
            }
            catch (Exception err)
            {
                MessageBox.Show("Error has occurred while loading settings: " + err.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            #endregion
                        
            richTextBoxOutput.Text = prompt + " "; //Vypsání promptu při prvním spuštění
            setTitle("Gnomnik"); //Změnit titulek
            protect(true);
            setCurcor();
            this.richTextBoxOutput.HideSelection = false; //Pokud uživatel označí text a poté klikne jinam, text zůstane označený
            this.richTextBoxOutput.Focus(); //Hned po spuštění bude richtextboxOutput očekávat vstup
        }

        private void appendWord(string wordToComplete, string possibleWord)
        {
            richTextBoxOutput.AppendText(possibleWord.Remove(0, wordToComplete.Length));
        }

        private void richTextBoxOutput_KeyPress(object sender, KeyPressEventArgs e)
        {
            bool asfv = true;

            try
            {
                if (!proc.HasExited)
                {
                    asfv = false;
                    if (e.KeyChar == (char)13)
                    {
                        proc.StandardInput.WriteLine();                        
                    }
                    else if (e.KeyChar == (char)8)
                    {
                        SendKeys.Send("{BS}");
                    }
                    else
                        proc.StandardInput.Write(e.KeyChar.ToString());
                }
            }
            catch
            {
            }

            if (e.KeyChar == (char)13 && asfv == true) //Stisk Enteru
            {
                upCount = 0;
                //e.Handled = true;
                protect(false); //V metodě proces() se děje úprava textu, která vyžaduje Protected = false;
                setCurcor();
                work();
                try //Při příkazu Clear, kde se celý richtextBox vymaže, nezbyde ani jedna řádka a tudíž následující podmínka míří mimo range a kvůli tomu padá. Díky tomuto ošetření se prompt vypíše jako by se nic nestalo.
                {
                    if (string.IsNullOrEmpty(richTextBoxOutput.Lines[richTextBoxOutput.Lines.Length - 1]) == true) //Pokud je poslední řádka richtextBoxu prázdná, znamená to, že již byla dříve zalomena a lze tedy rovnou vypsat nový prompt
                        richTextBoxOutput.AppendText(prompt + " "); //Vypíše nový prompt
                    else
                        richTextBoxOutput.AppendText("\n" + prompt + " "); //Zalomí řádek a vypíše nový prompt
                }
                catch
                {
                    richTextBoxOutput.AppendText(prompt + " "); //Vypíše nový prompt
                }
                protect(true); //Aby uživatel nemohl smazat prompt a předchozí text
                setCurcor();
            }
            else if (e.KeyChar == (char)Keys.Tab) //Doplnění slova (adresáře/souboru)
            {
                #region Doplnění_tabulátorem
                count++;
                e.Handled = true; //Zakáže klasickou funkci tabulátoru
                int totalLines = richTextBoxOutput.Lines.Length; //Získání počtu řádků v richtextboxu

                /* ********************PŘEHLED PROMĚNNÝCH********************
                 * workingDir - Adresář, ve kterém se uživatel aktuálně nachází
                 * wordToComplete - Poslední slovo v příkazu - U relativní cesty oddělené mezerou, u absolutní cesty oddělené lomítkem
                 * possibleWord - List slov, které jsou vhodná pro doplnění
                 * begunCommand - Příkaz tak, jak ho uživatel zadal */

                string wordToComplete = "";
                try
                {
                    wordToComplete = richTextBoxOutput.Lines[totalLines - 1].Remove(0, prompt.Length + 1); //Odstranění promptu od nově zadaného commandu
                }
                catch
                {
                    //Ošetření proti "blbosti" uživatele   
                }
                string begunCommand = wordToComplete;
                string[] split = wordToComplete.Split(new Char[] { ' ' }); //Rozdělení commandu podle mezer
                foreach (string item in split)
                {
                    wordToComplete = item; //V řetězci zůstane poslední část, která se má doplnit
                }
                List<string> possibleWord = new List<string>();
                string workingDir = currentDir;
                if (workingDir == "~") //Pokud se workingDir rovná znaku domovského adresáře, je nutné ho nahradit názvem (cestou) domovského adresáře
                {
                    workingDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile); //Získá se domovský adresář uživatele a nahradí se tím řeťezec currentDir
                    workingDir = reverseSlash(workingDir); //Obrácení lomítek
                }

                DirectoryInfo dir = new DirectoryInfo(workingDir); //Relativní cesta
                try
                {
                    //Pokud se nejedná o absolutní cestu, cesta neobsahuje lomítka. Tudíž nemůže fungovat následující řádek, program pokračuje výjimkou, kde je relativní cesta
                    DirectoryInfo dir2 = new DirectoryInfo(wordToComplete.Remove(wordToComplete.LastIndexOf('/'), wordToComplete.Length - wordToComplete.LastIndexOf('/')) + "/"); //Absolutní cesta - odtranění zbytku řetězce od posledního lomítka. Např: "C:/Users/Luk" -> "C:/Users/"
                    //Proměnné se musí přenastavit na cesty s absolutní cestou
                    workingDir = wordToComplete.Remove(wordToComplete.LastIndexOf('/'), wordToComplete.Length - wordToComplete.LastIndexOf('/')) + "/";
                    wordToComplete = wordToComplete.Remove(0, wordToComplete.LastIndexOf('/') + 1);

                    findOut(dir2, wordToComplete, possibleWord, begunCommand); //Prohledá adresáře a soubory s absolutní cestou
                }
                catch
                {
                    findOut(dir, wordToComplete, possibleWord, begunCommand); //Prohledá adresáře a soubory s relativní cestou
                }


                if (count >= 2)
                    count = 0;
                #endregion
            }
        }

        private void findOut(DirectoryInfo dir2, string wordToComplete, List<string> possibleWord, string begunCommand) //Metoda doplňování - prohledává adresáře
        {
            foreach (DirectoryInfo d in dir2.GetDirectories()) //Adresáře
            {
                if (d.Name.StartsWith(wordToComplete)) //Pokud adresář začíná stejně jako uživatel napsal
                    possibleWord.Add(d.Name);
            }
            foreach (FileInfo f in dir2.GetFiles()) //Soubory
            {
                if (f.Name.StartsWith(wordToComplete)) //Pokud soubor začíná stejně jako uživatel napsal
                    possibleWord.Add(f.Name);
            }
            if (possibleWord.ToArray().Length == 1) //Pokud je pouze jeden kandidát pro doplnění, není co řešit, okamžitě se doplní
            {
                appendWord(wordToComplete, possibleWord[0]);
                count = 0;
            }
            else if (possibleWord.ToArray().Length >= 2 && possibleWord.ToArray().Length < 50 && count >= 2) //Pokud je více možností na doplnění && Počet možností nepřesahuje 50 && Tabulátor stisknut podruhé (vícekrát)
            {
                richTextBoxOutput.AppendText("\n");
                for (int i = 0; i < possibleWord.ToArray().Length; i++)
                    richTextBoxOutput.AppendText(possibleWord[i] + "/  ");
                richTextBoxOutput.AppendText("\n" + prompt + " ");
                protect(true);
                richTextBoxOutput.AppendText(begunCommand);
                setCurcor();
            }
            else if (possibleWord.ToArray().Length >= 50 && count >= 2) //Pokud je více jak 50 možných adresářů na doplnění && Tabulátor stisknut vícekrát
            {
                richTextBoxOutput.AppendText("\nMore than 50 possibilities. Be specific");
                richTextBoxOutput.AppendText("\n" + prompt + " ");
                protect(true);
                richTextBoxOutput.AppendText(begunCommand);
                setCurcor();
            }
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Vyhladávání
            Form3 f3 = new Form3(this);
            f3.Show();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(@"Gnomnik is an application that simulates the behavior of the Linux shell.
Copyright (C) 2016 Lukáš Anděl

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for more details.

You should have received a copy of the GNU General Public License along with this program.  If not, see <http://www.gnu.org/licenses/>.

More about the program: http://gnomnik.sweb.cz/
", "About",MessageBoxButtons.OK,MessageBoxIcon.Asterisk);

        }
        
        private void setTitleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form2 f2 = new Form2(this);
            f2.ShowDialog();
        }

        private void clearScrollbackToolStripMenuItem_Click(object sender, EventArgs e)
        {
            protect(false);
            richTextBoxOutput.Text = prompt + " ";
            setCurcor();
            protect(true);
            setCurcor();
        }

        public void protect(bool x)
        {
            richTextBoxOutput.SelectAll();
            richTextBoxOutput.SelectionProtected = x;
        }

        public void setCurcor()
        {
            richTextBoxOutput.Select(richTextBoxOutput.Text.Length, 0);
        }

        private void closeWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        private void openTerminalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("gnomnik.exe");
        }

        private void contentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("CZ Manuál.pdf");
            }
            catch
            {
                MessageBox.Show("Cannot find manual, check instalation directory or web www.gnomnik.sweb.cz");
            }
        }

        private void fullscreenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (fullscreenToolStripMenuItem.Text == "Fullscreen")
            {
                WindowState = FormWindowState.Maximized;
                fullscreenToolStripMenuItem.Text = "Restore Down";
            }
            else if (fullscreenToolStripMenuItem.Text == "Restore Down")
            {
                WindowState = FormWindowState.Normal;
                fullscreenToolStripMenuItem.Text = "Fullscreen";
            }
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBoxOutput.Paste();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (richTextBoxOutput.SelectionLength > 0)
            {
                richTextBoxOutput.Copy();
            }
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            richTextBoxOutput.SelectAll();
        }

        private void preferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form4 f4 = new Form4(this);
            f4.Show();
        }

        private void richTextBoxOutput_TextChanged(object sender, EventArgs e)
        {
            
        }

        private void featuredToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(@"What's new?

- Fixed icon bug
- Fixed some grammar mistakes
- Web update (Check: www.gnomnik.sweb.cz)", "Featured", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }

        private void utilitiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Form5 f5 = new Form5(this);
            f5.Show();
        }
    }
}