using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OMtoSMConverter
{
    public partial class Form2 : Form
    {
        public Form1 parent;
        private bool ready;
        public Form2()
        {
            InitializeComponent();
            ready = false;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            hsFrom.Items.Clear();
            hsTo.Items.Clear();
            hsFrom.Items.Add("Drag and drop a single osu file containing the keysound data here!");
            hsFrom.Items.Add("Alternatively, drop all the osu files involved, and only one difficulty named 'Key*' will be used as the keysound data.");
            hsFrom.Items.Add("Press 'c' to return to conversion.");
            hsTo.Items.Add("Drag and drop the destination osu files here!");
        }

        //File read and access
        private void fileDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }
        private void hsDragDrop(object sender, DragEventArgs e)
        {
            // Copied from fileBeg_DragDrop
            //Activate the window to focus it on front
            this.Activate();

            //Get the filenames of the dropped stuff
            object data = e.Data.GetData(DataFormats.FileDrop);
            string[] allDroppedFiles = (string[])data;

            //Search all directories for files and remove all directories
            HashSet<string> allFoundFiles = parent.foundFiles(allDroppedFiles);
            if (sender.Equals(hsFrom))
            {
                hsFrom.Items.Clear();
                hsTo.Items.Clear();
                if (allFoundFiles.Count == 1)
                {
                    hsFrom.Items.Add(allFoundFiles.First());
                }
                else
                {
                    foreach (string file in allFoundFiles)
                    {
                        if (file.Contains("[Key") && hsFrom.Items.Count < 1) hsFrom.Items.Add(file);
                        else
                        {
                            if (file.Substring(file.Length-4) == ".osu")
                            hsTo.Items.Add(file);
                        }
                    }
                }
            }
            else if (sender.Equals(hsTo))
            {
                hsTo.Items.Clear();
                foreach (string file in allFoundFiles) hsTo.Items.Add(file);
            }
            if ((hsFrom.Items.Count == 1) && (hsTo.Items.Count >= 1)) ready = true;
            doButton.Text = "Do";
        }

        //Usability
        private void hsKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 'c')
            {
                parent.Show();
                this.Close();
            }
        }
        private void hsClose(object sender, FormClosedEventArgs e)
        {
            parent.Close();
        }

        //Actually doing stuff
        private void doHSCopy(object sender, EventArgs e)
        {
            if (ready)
            {
                Beatmap source = Beatmap.getRawOsuFile((string)hsFrom.Items[0]);
                HashSet<Beatmap> outmaps = new HashSet<Beatmap>();
                foreach (string dest in hsTo.Items)
                {
                    int lastSlash = dest.LastIndexOf("\\");
                    string folder = dest.Substring(0, lastSlash);
                    string filename = dest.Remove(0, lastSlash);
                    /* make backup in different diff name
                    //First make a copy in BAK folder
                    string bakfolder = folder + "\\BAK" ;
                    Directory.CreateDirectory(bakfolder);
                    File.Copy(dest, bakfolder + filename+"b", true);
                    */
                    Beatmap toMap = Beatmap.getRawOsuFile((string)dest);
                    toMap.backup(folder);
                    toMap.copyHS(source);
                    toMap.writeOut(folder);
                    
                }
            }
            else
            {
                doButton.Text = "You haven't shown me all the files yet!";
            }
        }


    }
}
