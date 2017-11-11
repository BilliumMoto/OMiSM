using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

/* A quick description of the algorithm
 * 
 * OM -> SM:
 * Timing points will be treated as the beginning of the measure always. 
 * The BPM will change a lot more than needed, but we can make up for this by using a higher quantization.
 * Notes will be placed according to their location in the BPM space.
 */

/* TODO
 * Bug with #fairy dancing in lake
 * Standardize header data
 * Proper notification of inability to copy files
 * Copy mp3s asynchronously (createSingleSMFile())
 * Figure out what's causing the lag
 * Completely fails when there are more than five diffs of the same keycount in the same file
 * 
 * 
 * DONE
 * Multiple directory support
 * Multiple mp3s/files at a time support.
 * Standardize the sm Start time across all difficulties.
 */

namespace OMtoSMConverter
{
    public partial class Form1 : Form
    {
        //Initializations
        public Form1()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            InitializeComponent();
            beginInstructions();

        }

        //Stuff that happens
        public void processOsuFiles(IEnumerable<string> filepaths)
        {
            //
            //<summary>
            //This program begins the process of converting the files after dropping various files of different kinds into the program.
            //</summary>

            //Initialize a set of all available mp3 files and to hold files after initial parsint
            Dictionary<string, HashSet<Beatmap>> allParses = new Dictionary<string, HashSet<Beatmap>>();
            int count = 0;

            //Parse all dropped files that are osu files
            foreach (var filepath in filepaths)
            {
                if (filepath.Split('.').Last() == "osu")
                {
                    Beatmap newParse = Beatmap.getRawOsuFile(filepath);
                    count++;

                    //Then add it to the dictionary for later use
                    if (allParses.ContainsKey(newParse.audioPath)) allParses[newParse.audioPath].Add(newParse);
                    else
                    {
                        HashSet<Beatmap> newSet = new HashSet<Beatmap>();
                        newSet.Add(newParse);
                        allParses.Add(newParse.audioPath, newSet);
                    }
                }
            }

            //Inform user of progress
            boxInform(string.Format("{0} total osu files found.", count),
                string.Format("{0} potential Stepmania files may be created.", allParses.Keys.Count()));


            //Then we can treat each mp3 as its own smFile
            foreach (string audioFile in allParses.Keys)
            {
                boxInform("Now processing:", audioFile);
                createSingleSMFile(allParses[audioFile]);
            }
            boxInform("All conversions complete!");
        }
        public void createSingleSMFile(IEnumerable<Beatmap> diffs)
        {
            //Create a new smFile ready to use
            smFile file = new smFile();

            //First, add raw diffs to smFile
            foreach (Beatmap diff in diffs)
            {
                //Goes ahead and standardizes the starting point of all difficulties.
                file.Add(diff);

            }
            //Figure out where to put the diffs. Also kills all non-mania maps.
            file.AutoSortDiffs();

            //Checks and breaks if there is no files
            if (file.AllDiffs.Count == 0)
            {
                boxInform("No osu!mania beatmaps found!");
                return;
            }

            //Inform user of progress
            string progress = file.AllDiffs.Count().ToString() + " osu!mania beatmaps determined.";
            boxInform(progress);

            //Convert all diffs
            foreach (Beatmap diff in file.AllDiffs)
            {
                boxInform("Converting: " + diff.oMetadata["Version"]);
                if (diff.smKeyType() != "")
                {
                    file.hasSubstamce = true;
                    Thread converterThread = new Thread(() => diff.fillSMfromOSU());
                    converterThread.Start();
                    while (converterThread.IsAlive);
                }
                else
                {
                    boxInform("Unsupported key count, beatmap not converted.");
                }
            }

            //Write to file
            boxInform("Exporting the Stepmania files, copying the mp3s might take a while...");
            file.SetHeaderData();
            file.writeEverythingToFolder(this);

            //Thread fileCopyThread = new Thread(() => file.writeEverythingToFolder());
            //fileCopyThread.Start();

            //Inform of Completion
            boxInform("File complete!");
        }


        //Events
        private void label1_Click(object sender, EventArgs e)
        {
            outBox.Items.Clear();
            outBox.Items.Add("This works");
            Console.WriteLine("weedless");
        }
        private void fileBeg_dragDrop(object sender, DragEventArgs e)
        {
            //Activate the window to focus it on front
            this.Activate();

            //Get the filenames of the dropped stuff
            object data = e.Data.GetData(DataFormats.FileDrop);
            string[] allDroppedFiles = (string[])data;
            boxInform(allDroppedFiles.Count().ToString() + " files dropped.");

            //Search all directories for files and remove all directories
            HashSet<string> allFoundFiles = foundFiles(allDroppedFiles);

            //Begin processing the files
            processOsuFiles(allFoundFiles);
        }
        private void fileBeg_dragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }
        private void WinKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Space) boxClear();
        }
        private void WinKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 'e') boxClear();
            if (e.KeyChar == 'h')
            {
                hsCopier();
            }
                
        }

        //GUI I/O
        public void boxInform(string progress, bool verbose=false)
        {
            outBox.Items.Add(progress);
            int visibleItems = outBox.ClientSize.Height / outBox.ItemHeight;
            outBox.TopIndex = Math.Max(outBox.Items.Count - visibleItems + 1, 0);
            outBox.Update();
        }
        public void boxInform(params string[] progresses)
        {
            foreach (string progress in progresses)
            {
                boxInform(progress);
            }
        }
        public void beginInstructions()
        {
            boxInform("Drag and drop a bunch of osu files and folders here!",
            "Press 'e' to clear this status box.",
            "Press 'h' to copy hitsounds");
        }
        private void boxClear()
        {
            outBox.Items.Clear();
            beginInstructions();
        }
        public void hsCopier()
        {
            Form2 hitsoundCopier = new Form2();
            hitsoundCopier.Show();
            hitsoundCopier.parent = this;
            this.Hide();
        }

        //File Handling
        public HashSet<string> foundFiles(string[] allDroppedFiles)
        {
            //Search all directories for files and remove all directories from the set
            HashSet<string> outFiles = new HashSet<string>();
            foreach (string path in allDroppedFiles)
            {
                if (File.Exists(path)) outFiles.Add(path);
                else if (Directory.Exists(path))
                {
                    string[] filenames = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                    outFiles.UnionWith(filenames);
                }
            }
            return outFiles;
        }
        public static string dirnameSanitize(string bad)
        {
            //HashSet<char> invalid = new HashSet<char>(Path.GetInvalidFileNameChars());
            return string.Join("_", bad.Split(Path.GetInvalidPathChars()));
        }
        public static string filenameSanitize(string bad)
        {
            //HashSet<char> invalid = new HashSet<char>(Path.GetInvalidFileNameChars());
            return string.Join("_", bad.Split(Path.GetInvalidFileNameChars()));
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void outBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
    public class smFile
    {
        public List<Beatmap> AllDiffs { get; set; }
        public Dictionary<smSetting,string> HeaderData { get; set; }
        public double startMS { get; set; }
        public string errorfile { get; set; }
        public bool hasSubstamce { get; set; }

        //Init
        public smFile()
        {
            AllDiffs = new List<Beatmap>();
            HeaderData = new Dictionary<smSetting, string>();
        }
        public smFile(Beatmap beatmap)
        {
            AllDiffs = new List<Beatmap> { beatmap };
            HeaderData = createHeaderData(beatmap);
            startMS = beatmap.smStartMS;
        }
        public smFile(List<Beatmap> beatmaps)
        {
            startMS = beatmaps.First().smStartMS;
            AllDiffs = new List<Beatmap>();
            foreach (Beatmap beatmap in beatmaps)
            {
                Add(beatmap);
            }
        }

        //Methods
        public void Add(Beatmap beatmap)
        {
            AllDiffs.Add(beatmap);
            if (AllDiffs.Count == 1) startMS = beatmap.smStartMS;
            if (beatmap.smStartMS < startMS) startMS = beatmap.smStartMS;
            standardizeStartTimes();


            /*
            if (HeaderData.Count() == 0)
                HeaderData = createHeaderData(beatmap);
                */
        }
        public void standardizeStartTimes()
        {
            //Force the starting point of all maps to be uniform
            foreach (Beatmap beatmap in AllDiffs)
            {
                beatmap.smStartMS = startMS;
            }
        }
        public void SetHeaderData(int index = 0)
        {
            HeaderData = createHeaderData(AllDiffs[index]);
        }

        //Helpers
        public static Dictionary<smSetting, string> createHeaderData(Beatmap beatmap)
        {
            Dictionary<smSetting, string> HeaderData = new Dictionary<smSetting, string>();
            string numForm = "0.######";
            HeaderData.Add(smSetting.TITLE, beatmap.oMetadata["Title"]);
            HeaderData.Add(smSetting.SUBTITLE, beatmap.oMetadata["Creator"] + " - " + beatmap.oMetadata["Version"]);
            HeaderData.Add(smSetting.ARTIST, beatmap.oMetadata["Artist"]);
            HeaderData.Add(smSetting.TITLETRANSLIT, "");
            HeaderData.Add(smSetting.SUBTITLETRANSLIT, "");
            HeaderData.Add(smSetting.ARTISTTRANSLIT, "");
            HeaderData.Add(smSetting.GENRE, beatmap.oMetadata["Tags"]);

            HeaderData.Add(smSetting.CREDIT, beatmap.oMetadata["Creator"]);
            HeaderData.Add(smSetting.BANNER, ""); //FIX THIS SOON? Graphics manipulation maybe? How does graphics even work?

            //FIX THIS IN A BIT, this is not proper at all as it is... I mean, it works?
            string BG;
            try
            {
                BG = beatmap.oEvents[1].parameters[2];
            }
            catch
            {
                BG = "";                
            }
            HeaderData.Add(smSetting.BACKGROUND, BG); 
            HeaderData.Add(smSetting.LYRICSPATH, ""); 
            HeaderData.Add(smSetting.CDTITLE, ""); //GODDAMMIT, maybe try 
            HeaderData.Add(smSetting.MUSIC, beatmap.oGeneral["AudioFilename"]);
            //See Image
            double MSPB = beatmap.oTimingPoints.First().MSPerBeat;
            double BPMeasure = beatmap.BeatsPerMeasure;

            //80 is what seems to be the difference of osu and stuff
            HeaderData.Add(smSetting.OFFSET, 
                ((((MSPB * BPMeasure) - (beatmap.smStartMS + 77 + MSPB * BPMeasure )) / 1000).ToString(numForm))); 
            HeaderData.Add(smSetting.SAMPLESTART, (
                (double.Parse(beatmap.oGeneral["PreviewTime"]) >= 0) 
                ? (double.Parse(beatmap.oGeneral["PreviewTime"]) / 1000).ToString(numForm)
                : "0")); //In case the mapper didn't specify a preview time.
            HeaderData.Add(smSetting.SAMPLELENGTH, "20.000000"); //This is completely arbitrary, try getting length of mp3?
            HeaderData.Add(smSetting.SELECTABLE, "YES");
            HeaderData.Add(smSetting.BPMS, beatmap.rawSMBPMs());
            HeaderData.Add(smSetting.STOPS, ""); //Because fuck stops
            HeaderData.Add(smSetting.BGCHANGES, "");
            HeaderData.Add(smSetting.KEYSOUNDS, ""); // Eventually...
            HeaderData.Add(smSetting.ATTACKS, "");
            return HeaderData;
        }
        public void AutoSortDiffs()
        {
            //Filter out all non-mania maps
            AllDiffs.RemoveAll(i => i.oGeneral["Mode"] != "3");

            //Create count of all key counts
            Dictionary<int, int> keycountCount = new Dictionary<int, int>();

            //Create something to help us track the diffs as they are assigned 
            Dictionary<int, int> diffsInd = new Dictionary<int, int>();

            //Initialize both dictionaries
            for (int i = 0; i < 11; i++)
            {
                //Because stuff is 0based aaaaa
                keycountCount[i] = -1;
                diffsInd[i] = -1;
            }

            foreach (Beatmap beatmap in AllDiffs)
            {
                keycountCount[beatmap.KeyCount] += 1;

                //4 is the challenge, we don't want any higher than that if it can be avoided...
                diffsInd[beatmap.KeyCount] = (keycountCount[beatmap.KeyCount] > 4) ? 5 : 4;
            }

            //Sort AllDiffs by number of notes, this should be an alright indicator of difficulty right?
            //We want the highest number of HO to come first in the list
            AllDiffs.Sort((b1, b2) => b2.oHitObjects.Count().CompareTo(b1.oHitObjects.Count()));

            //Assign each beatmap their indexes! Counting down!
            foreach (Beatmap beatmap in AllDiffs)
            {
                beatmap.smDiffInd = diffsInd[beatmap.KeyCount];

                keycountCount[beatmap.KeyCount] -= 1;

                //Still more than 4 maps left of the same keycount?, still goes in edit
                diffsInd[beatmap.KeyCount] = (keycountCount[beatmap.KeyCount] > 4) ? 5 : diffsInd[beatmap.KeyCount] - 1;
            }

            //Now sort the beatmaps by keycount
            AllDiffs.Sort((b1, b2) => b1.KeyCount.CompareTo(b2.KeyCount));

        }

        //Write Out
        public string getWholeFile()
        {
            string file = "//This file was created using BilliumMoto's Osu to SM file converter";
            foreach (smSetting HeaderItem in HeaderData.Keys)
            {
                file = string.Join("", file, 
                    Environment.NewLine,
                    "#",
                    HeaderItem.ToString(),
                    ":",
                    HeaderData[HeaderItem],
                    ";");
            }

            file = file + Environment.NewLine;

            foreach (Beatmap diff in AllDiffs)
            {
                if (diff.smKeyType() != "")
                {
                    file = file + Environment.NewLine + diff.smDiffHeader();
                    file = file + diff.rawSMNotes();
                }
            }
            return file;
        }
        public void writeEverythingToFolder(Form1 parent)
        {
            if (!hasSubstamce) return;
            //Use the name of the mp3 to separate same-folder different files
            //Oh and clean the unsafe strings
            string fileName = Form1.filenameSanitize(HeaderData[smSetting.TITLE]);
            string audioName = Form1.filenameSanitize(HeaderData[smSetting.MUSIC]);
            string creditName = Form1.filenameSanitize(HeaderData[smSetting.CREDIT]);

            //Make new folder
            string newFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\" +
                Form1.dirnameSanitize(
                    (fileName) +
                     " (" +
                    (audioName.Substring(0, audioName.Count() - 4)) +
                     ") [" +
                    (creditName) +
                     "]");

            string destPath = newFolder + "\\";

            Directory.CreateDirectory(newFolder);

            //Write SM file
            StreamWriter fileWrite = new StreamWriter(newFolder + "\\" + fileName + ".sm");
            fileWrite.Write(getWholeFile());
            fileWrite.Close();

            //Copy mp3 and background
            string sourcePath = AllDiffs[0].sourcePath + "\\";
            string bgName = HeaderData[smSetting.BACKGROUND];

            try
            {
                if (bgName != "") if (!File.Exists(destPath + bgName)) File.Copy(sourcePath + bgName, destPath + bgName, true);
                if (audioName != "") if (!File.Exists(destPath + audioName)) File.Copy(sourcePath + audioName, destPath + audioName, true);
            }
            catch
            {
                //Tell user somehow that files couldn't be copied over?
                parent.boxInform("Files could not be copied over!");
                return;
            }
        }
    }
    public enum smSetting
    {
        TITLE,
        SUBTITLE,
        ARTIST,
        TITLETRANSLIT,
        SUBTITLETRANSLIT,
        ARTISTTRANSLIT,
        GENRE,  
        CREDIT,
        BANNER,
        BACKGROUND,
        LYRICSPATH,
        CDTITLE,
        MUSIC,
        OFFSET,
        SAMPLESTART,
        SAMPLELENGTH,
        SELECTABLE,
        BPMS,
        STOPS,
        BGCHANGES,
        KEYSOUNDS,
        ATTACKS
            //Adding here means translations must be added in smFile.createHeaderData
    }
    public class Beatmap
    {
        //General Parts
        public double KeyCountRaw { get; set; }
        public int KeyCount { get; set; }
        public int BeatsPerMeasure { get; set; }
        public string sourcePath { get; set; }
        public string audioPath { get; set; }

        //Osu Parts
        public Dictionary<string, string> oGeneral { get; set; }
        public Dictionary<string, string> oEditor { get; set; }
        public Dictionary<string, string> oMetadata { get; set; }
        public Dictionary<string, string> oDifficulty { get; set; }
        public List<osuEvent> oEvents { get; set; }
        public List<osuTimingPoint> oTimingPoints { get; set; }
        public List<osuHitObject> oHitObjects { get; set; }
        public List<string> osuRaw { get; set; }

        //Stepmania Parts
        public List<smMeasure> smMeasures { get; set; }
        public Dictionary<double,double> smBPMs { get; set; }
        public double smStartMS { get; set; }
        public int smDiffInd { get; set; }
        public string smKeyType()
        {
            return smKeyNames[(int)KeyCount];
        }
        public string smDiffHeader()
        {
            smDiff diff = (smDiff)smDiffInd;
            string thingy = string.Join(Environment.NewLine,
                "//---------------" + smKeyType() + " - ----------------",
                "#NOTES:") + 
                string.Join(Environment.NewLine + "     ",
                "", 
                smKeyType() + ":",
                ":",
                diff.ToString() + ":",
                "1:",
                "",
                "0.000,0.000,0.000,0.000,0.000:"); //No clue what this last one is

        return thingy;
        }

        //Constants, a few checks are done based on whether or not strings in this array are empty ("")
        public static string[] smKeyNames = new string[] 
        {
            "", "", "", "",
            "dance-single",
            "pump-single",
            "dance-solo",
            "kb7-single",
            "dance-double",
            "", //Find pms header name
            "pump-double"
        };

        //Init
        public Beatmap()
        {
            oGeneral = new Dictionary<string, string>();
            oEditor = new Dictionary<string, string>();
            oMetadata = new Dictionary<string, string>();
            oDifficulty = new Dictionary<string, string>();
            oEvents = new List<osuEvent>();
            oTimingPoints = new List<osuTimingPoint>();
            oHitObjects = new List<osuHitObject>();
            osuRaw = new List<string>();
            smMeasures = new List<smMeasure>();
            BeatsPerMeasure = 4;

            //Replace
            smDiffInd = 4;
        }

        //Read in
        public static Beatmap getRawOsuFile(string filename)
        {
            //Variables we need
            StreamReader osuFile = new StreamReader(filename);
            Beatmap placeHold = new Beatmap();
            string line;
            string osuField = "";
            string[] setting = new string[2];
            placeHold.sourcePath = Path.GetDirectoryName(filename);

            //Loop Through file by line
            while ((line = osuFile.ReadLine()) != null)
            {
                //First get the raw line
                line.Trim();
                placeHold.osuRaw.Add(line);
                //Console.WriteLine(line);

                //Get settings and all osu data
                setting = Beatmap.ParseOsuSetting(line);
                if (line.Contains("[") && !line.Contains(":"))
                {
                    osuField = line;
                }
                else if (line == "") continue;
                else
                {
                    switch (osuField)
                    {
                        case "[General]":
                            placeHold.oGeneral.Add(setting[0], setting[1]);
                            break;
                        case "[Editor]":
                            placeHold.oEditor.Add(setting[0], setting[1]);
                            break;
                        case "[Metadata]":
                            placeHold.oMetadata.Add(setting[0], setting[1]);
                            break;
                        case "[Difficulty]":
                            placeHold.oDifficulty.Add(setting[0], setting[1]);
                            break;
                        case "[Events]":
                            placeHold.oEvents.Add(new osuEvent(line));
                            break;
                        case "[TimingPoints]":
                            osuTimingPoint TP = osuTimingPoint.Parse(line);
                            if (TP != null)
                                placeHold.oTimingPoints.Add(TP);
                            break;
                        case "[HitObjects]":
                            osuHitObject HO = osuHitObject.Parse(line);
                            if (HO != null)
                                placeHold.oHitObjects.Add(HO);
                            break;
                        default:
                            break;
                    }
                }
            }
            //File Read Done
            osuFile.Close();
            placeHold.KeyCountRaw = double.Parse(placeHold.oDifficulty["CircleSize"]);
            placeHold.KeyCount = (int)placeHold.KeyCountRaw;
            placeHold.audioPath = placeHold.sourcePath + "\\" + placeHold.oGeneral["AudioFilename"];
            placeHold.oTimingPoints.Sort((x1, x2) => x1.Time.CompareTo(x2.Time));
            placeHold.oHitObjects.Sort((x1, x2) => x1.Time.CompareTo(x2.Time));

            //In case the mapper is an idiot and makes the first timing point later in the beatmap
            //we need to make our first measure begins at/before the first note.
            //We will make it so it doesn't start too far behind the first note or else we can get weird stuff happening
            try
            {
                double realStartMS = placeHold.oTimingPoints.First().Time;
                while (realStartMS <= placeHold.oHitObjects.First().Time)
                {
                    realStartMS += placeHold.BeatsPerMeasure * placeHold.oTimingPoints.First().MSPerBeat;
                }
                while (realStartMS >= placeHold.oHitObjects.First().Time)
                {
                    realStartMS -= placeHold.BeatsPerMeasure * placeHold.oTimingPoints.First().MSPerBeat;
                }
                placeHold.smStartMS = realStartMS;
            }
            catch (InvalidOperationException)
            {
                //No hitobjects found... can't really do much about it
                
            }

            //We will need this value later
            //Lets try making the BPMs along with the notes
            //placeHold.smBPMs = getSMBPMsfromTP(placeHold.oTimingPoints, starttime: realStartMS);
            placeHold.smBPMs = new Dictionary<double, double>();
            return placeHold;
        }

        //Conversions
        public void fillSMfromOSU()
        {
            //Divide all hitobjects up into measures, keeping their LN ends stored for separate entry.
            Queue<osuHitObject> QueueHO = new Queue<osuHitObject>(oHitObjects);

            //We only want the uninherited timing points for now
            Queue<osuTimingPoint> QueueTP = new Queue<osuTimingPoint>(oTimingPoints.FindAll(p => p.IsTiming()));

            //Initialize stuff
            int currMeasureNum = -1;
            osuHitObject currHO = new osuHitObject();
            List<Tuple<int, int>> LNEnds = new List<Tuple<int, int>>();
            double currMeasureStart;
            double currMeasureEnd = smStartMS;
            double currFudgeMeasureEnd = currMeasureEnd - 1;

            //Make the first BPM
            osuTimingPoint currTP = QueueTP.Dequeue();
            smBPMs.Add(0, MSPBtoBPM(currTP.MSPerBeat));


            //We need to split osu up into measures here...
            //This outer while loop should loop once per measure.
            while (QueueHO.Count() > 0 || LNEnds.Count() > 0)
            {
                //We need to get a list of all HO in the measure, the list of all LNEnds in the measure
                //measureNumber, start/end/BPM, key count to make a Stepmania measure
                //Reinitialize everything
                List<osuHitObject> currMeasureHO = new List<osuHitObject>();
                List<Tuple<int, int>> currMeasureLNEnds = new List<Tuple<int, int>>();
                currMeasureNum++;
                currMeasureStart = currMeasureEnd;
                currMeasureEnd = currMeasureStart + (BeatsPerMeasure * currTP.MSPerBeat);

                //This fudge factor ensures that the first note of the next measure isn't cut off here
                currFudgeMeasureEnd = currMeasureEnd - (currTP.MSPerBeat / 192) * 4;

                //Check if there is a new TP in the way and adjust for it, and add to smBPMs
                //We don't use the fudge factor here since we're moving through a continuous time space
                if (QueueTP.Count() > 0)
                {
                    if (QueueTP.Peek().Time <= currMeasureEnd + 1)
                    {
                        currTP = QueueTP.Dequeue();

                        //Special BPM Considerations, code copied from original smBPMs retrival method
                        osuTimingPoint TPdequeue = currTP;
                        double gap = (TPdequeue.Time - currMeasureStart);
                        if (gap == 0) gap = TPdequeue.MSPerBeat * BeatsPerMeasure;

                        //Change the "current" measure
                        double gapBPM = MSPBtoBPM(gap / BeatsPerMeasure);
                        if (smBPMs.ContainsKey(currMeasureNum)) smBPMs[currMeasureNum] = gapBPM;
                        else smBPMs.Add(currMeasureNum, gapBPM);

                        //Create the next BPM point
                        smBPMs.Add(currMeasureNum + 1, MSPBtoBPM(TPdequeue.MSPerBeat));

                        currMeasureEnd = currTP.Time;
                        currFudgeMeasureEnd = currMeasureEnd - (currTP.MSPerBeat / 192) * 4;
                    }
                    //No TPs left? The currTP will reign until the end!
                }


                //We now have measure num, start/end/Key count. We will do without BPM for now.
                //We need the HO list and LNEnds
                if (QueueHO.Count() > 0)
                {
                    while (QueueHO.Peek().Time < currFudgeMeasureEnd)
                    {
                        currHO = QueueHO.Dequeue();
                        currMeasureHO.Add(currHO);

                        //Tuple in format <xpos, LNEndTime>
                        if (currHO.Addition.LNEnd != 0) LNEnds.Add(Tuple.Create(currHO.Xpos, currHO.Addition.LNEnd));
                        if (QueueHO.Count() == 0) break;
                    }
                }

                //We have HO list now and full queue of LNEnds. We get all LNEnds that end before measure end.
                foreach (Tuple<int, int> LNEnd in LNEnds)
                {
                    if (LNEnd.Item2 < currFudgeMeasureEnd) currMeasureLNEnds.Add(LNEnd);
                }
                //Remove all LNEnds that we took from the list
                foreach (Tuple<int, int> LNEnd in currMeasureLNEnds)
                {
                    LNEnds.Remove(LNEnd);
                }

                //We now have everything we need to make a measure.
                smMeasure ToBeAdded = new smMeasure();
                ToBeAdded.KeyCount = (int)KeyCount;
                ToBeAdded.StartTime = currMeasureStart;
                ToBeAdded.EndTime = currMeasureEnd;
                ToBeAdded.BeatsPerMeasure = BeatsPerMeasure;
                ToBeAdded.MeasureNum = currMeasureNum;
                ToBeAdded.fillMeasurefromOsu(currMeasureHO, currMeasureLNEnds);
                smMeasures.Add(ToBeAdded);
            }
        }

        //Copy Hitsounds/Keysounds
        public void copyHS(Beatmap source)
        {
            //Make sure time is ascending
            source.oHitObjects.Sort((a, b) => a.Time.CompareTo(b.Time));
            oHitObjects.Sort((a, b) => a.Time.CompareTo(b.Time));
            Queue<osuHitObject> sHO = new Queue<osuHitObject>(source.oHitObjects);
            Queue<osuHitObject> dHO = new Queue<osuHitObject>(oHitObjects);

            List<osuHitObject> toSB = new List<osuHitObject>();
            oHitObjects = copyHSrec(sHO, dHO, toSB);
            oHitObjects.Reverse();

            //Put these into SB
            foreach (osuHitObject sbSound in toSB)
            {
                oEvents.Add(new osuEvent(osuEventType.Sample,
                    sbSound.Time,
                    sbSound.Addition.Volume,
                    sbSound.Addition.KeySound));
            }

        }
        private List<osuHitObject> copyHSrec(Queue<osuHitObject> sHO, Queue<osuHitObject> dHO, List<osuHitObject> toSB)
        {
            //OK so recursion is really slow? Eh, it's aight
            //Returns the modified list of hit objects that the recursion has looped through, by pure virtue of time comparisons
            //First, the base cases
            if (sHO.Count == 0 || dHO.Count == 0)
            {
                // anything remaining in source means it goes into SB
                while (sHO.Count > 0)
                {
                    toSB.Add(sHO.Dequeue());
                }

                //remainder in dHO goes into list unmodified, return the remainder, must be reversed
                var hold = new List<osuHitObject>(dHO);
                hold.Reverse();
                return hold;
            }
            // next keysound hasn't been reached yet, need to remove note from dHO
            else if (sHO.Peek().Time > dHO.Peek().Time)
            {
                //The add method throws list into reverse
                osuHitObject rem = dHO.Dequeue();
                List<osuHitObject> hold = copyHSrec(sHO, dHO, toSB);
                hold.Add(rem);
                return hold;
            } 
            // keysound has been passed, need to put into SB
            else if (sHO.Peek().Time < dHO.Peek().Time)
            {
                toSB.Add(sHO.Dequeue());
                return copyHSrec(sHO, dHO, toSB);
            }
            // times match, we want to assign ks by column (xpos) and destination by randomly for the time
            else
            {
                int time = sHO.Peek().Time;
                // Find all stuff in source at this time
                List<osuHitObject> sourceKS = new List<osuHitObject>();
                while (sHO.Count > 0 && sHO.Peek().Time == time)
                {
                    sourceKS.Add(sHO.Dequeue());
                }
                //Order keysounds by xpos, convert to queue
                sourceKS.Sort((a, b) => a.Xpos.CompareTo(b.Xpos));
                Queue<osuHitObject> tKS = new Queue<osuHitObject>(sourceKS);

                // Find all stuff in dest at this time
                List<osuHitObject> tDest = new List<osuHitObject>();
                while (dHO.Count > 0 && dHO.Peek().Time == time)
                {
                    tDest.Add(dHO.Dequeue());
                }

                //random number generator
                Random rng = new Random();

                //container for done dest hs
                List<osuHitObject> copiedHO = new List<osuHitObject>();

                //go until source keysounds exhausted
                while (tKS.Count > 0)
                {
                    //find the source KS and the random note to copy it to, remove from both collections
                    osuHitObject ks = tKS.Dequeue();

                    //if there is still a destination to copy to
                    if (tDest.Count > 0)
                    {
                        int indHO = rng.Next(0, tDest.Count);
                        osuHitObject to = tDest[indHO];
                        tDest.RemoveAt(indHO);

                        //do the copy
                        to.ksCopy(ks);

                        //finalize and collect the completed copied HO
                        copiedHO.Add(to);
                    }
                    else
                    {
                        // no more destinations? current ks and rest of tKS goes into toSB
                        toSB.Add(ks);
                        toSB.AddRange(tKS);
                        tKS.Clear();
                    }
                }
                // Complete by emptying the dest HOs
                copiedHO.AddRange(tDest);
                
                // Finish recursive loop
                List<osuHitObject> hold = copyHSrec(sHO, dHO, toSB);
                hold.AddRange(copiedHO);
                return hold;
            }
        }

        //Helpers
        private static string[] ParseOsuSetting(string line)
        {
            string[] sets = new string[2];
            int x = line.IndexOf(":");
            x = (x >= 0) ? x : 0;
            sets[0] = line.Substring(0, x);
            try
            {
                sets[1] = line.Substring(x + 1).Trim();
            }
            catch
            {
                sets[1] = "";
            }
            return sets;
        }
        public static double MSPBtoBPM(double MSPB)
        {
            return (60000 / MSPB);
        }
        public void backup(string dest)
        {
            string old = oMetadata["Version"];
            oMetadata["Version"] = old + " backup";
            writeOut(dest);
            oMetadata["Version"] = old;
        }

        //This is the place to do BPM smoothing if we do any. Assumes BeatsPerMeasure = 4
        public static Dictionary<double,double> getSMBPMsfromTP(List<osuTimingPoint> allTPs, double starttime = 0)
        {
            //Initialize with the first and required BPM point
            //However, due to our algorithm, being the Beat #, the key always should be an integer.
            //The value in the dict is the BPM
            //Because BPMs and everything in SM is done in measures, our BPM metrics will need to be converted to Measures
            //MSPB = Milliseconds per beat // MSPM = Milliseconds per measure
            Dictionary<double, double> smBPMs = new Dictionary<double, double>();
            int BeatsPerMeasure = 4;//Should be 4?
            double MeasureNum = 0;
            double currMSPM = allTPs.First().MSPerBeat * BeatsPerMeasure;

            //We will sweep through the time space of the entire map, assigning BPMs according to not only the value of the timing point but also their location
            double currTPTime = starttime;
            double currTime = starttime;

            //Add the first BPM point
            smBPMs.Add(MeasureNum, MSPBtoBPM(currMSPM / BeatsPerMeasure));

            //Keep only true timing points and create a Queue
            Queue<osuTimingPoint> TPs = new Queue<osuTimingPoint>(allTPs.FindAll(p => p.IsTiming()));

            //Discard timing points before the beginning of our file, with a conservative 1ms buffer
            while (TPs.Peek().Time < starttime - 1) TPs.Dequeue();

            osuTimingPoint TPdequeue;

            while (TPs.Count > 0)
            {
                //If the beat ends before the next timing point, don't do anything. Just move up in the time space.
                //This comparison also uses a 1 ms buffer to account for stupid osu rounding
                if (currTime + currMSPM + 1 <= TPs.Peek().Time)
                {
                    MeasureNum++;
                    currTime += currMSPM;
                }

                //Otherwise, we will need to change the bpm of this measure so the whole measure will fit before the next one.
                //This requires changing the BPM of the previously determined measure to fit the gap perfectly as well as creating the next.
                else
                {
                    //Save this TP to be worked with
                    TPdequeue = TPs.Dequeue();

                    double gap = (TPdequeue.Time - currTime);
                    if (gap == 0) gap = TPdequeue.MSPerBeat * BeatsPerMeasure;

                    //Change the previous measure
                    double gapBPM = MSPBtoBPM(gap / BeatsPerMeasure);
                    if (smBPMs.ContainsKey(MeasureNum)) smBPMs[MeasureNum] = gapBPM;
                    else smBPMs.Add(MeasureNum, gapBPM);

                    //Create the next BPM point
                    currTime = TPdequeue.Time;
                    currMSPM = TPdequeue.MSPerBeat * BeatsPerMeasure;
                    MeasureNum++;
                    smBPMs.Add(MeasureNum, MSPBtoBPM(TPdequeue.MSPerBeat));
                }
            }

            //We're done!
            return smBPMs;
        }

        //Write Out
        public string rawSMNotes()
        {
            string rawNotes = Environment.NewLine;
            Queue<smMeasure> measureQueue = new Queue<smMeasure>(smMeasures);
            smMeasure measure;
            while (measureQueue.Count > 0) 
            {
                measure = measureQueue.Dequeue();
                //Comment beginning of measure 
                rawNotes = rawNotes + "  //  Measure " + measure.MeasureNum;
                foreach (int quant in measure.smNotes.Keys)
                {
                    rawNotes = rawNotes + Environment.NewLine + measure.smNotes[quant].makeRawNoteLine();
                }


                //Mark end of measure, unless it's the last measure.
                rawNotes = rawNotes + Environment.NewLine;
                if (measureQueue.Count > 0) rawNotes = rawNotes + ",";
                else rawNotes = rawNotes + ";";
            } 
            return rawNotes;
        }
        public string rawSMBPMs()
        {
            string rawBPMs = "";
            //this originally involved measurenum * BeatsperMeasure
            foreach (double beatnum in smBPMs.Keys)
            {
                if (rawBPMs != "") rawBPMs += Environment.NewLine + ",";
                rawBPMs += string.Join("", 
                    (beatnum * BeatsPerMeasure).ToString("0.######"), 
                    "=", 
                    smBPMs[beatnum].ToString());
            }

            return rawBPMs;
        }
        public string entireOsuFile()
        {
            string file = "osu file format v14";
            string nl = Environment.NewLine;
            file = file +
                nl + nl + "[General]"       + dictReduce(oGeneral)      +
                nl + nl + "[Editor]"        + dictReduce(oEditor)       +
                nl + nl + "[Metadata]"      + dictReduce(oMetadata)     +
                nl + nl + "[Difficulty]"    + dictReduce(oDifficulty)   +
                nl + nl + "[Events]"        + nl + string.Join(nl, oEvents.ConvertAll(a => a.ToString())) +
                nl + nl + "[TimingPoints]"  + nl + string.Join(nl, oTimingPoints.ConvertAll(a => a.write()))   +
                nl + nl + "[HitObjects]"    + nl + string.Join(nl, oHitObjects.ConvertAll(a => a.write()))
                ;

            return file;
        }
        public string dictReduce(Dictionary<string, string> dict)
        {
            return dictReduce(dict, ": ", Environment.NewLine);
        }
        public string dictReduce(Dictionary<string,string> dict, string pairDelim, string keyDelim)
        {
            string done = "";
            foreach (string key in dict.Keys)
            {
                done = done + keyDelim + key + pairDelim + dict[key];
            }
            return done;
        }
        public void writeOut(string folder)
        {
            string filename = oMetadata["ArtistUnicode"] + " - " +
                oMetadata["TitleUnicode"] + string.Format(" ({0}) [{1}].osu", oMetadata["Creator"], oMetadata["Version"]);

            StreamWriter fileWrite = new StreamWriter(folder + "\\" + filename);
            fileWrite.Write(entireOsuFile());
            fileWrite.Close();
        }

    }
    public enum osuEventType
    {
        _0,
        Sprite,
        Sample,
        Comment,
        File
    }
    public class osuEvent
    {
        public List<string> parameters { get; set; }
        public osuEventType type { get; set; }
        public string rawline { get; set; }

        public osuEvent(string rwline)
        {
            parameters = new List<string>();
            rawline = rwline;
            if (!rwline.Contains(","))
            {
                type = osuEventType.Comment;
                parameters.Add(rwline);
            }
            else
            {
                parameters.AddRange(rwline.Split(','));
                switch (parameters[0])
                {
                    case "0":
                        type = osuEventType._0;
                        break;
                    case "Sample":
                        type = osuEventType.Sample;
                        break;
                    case "Sprite":
                        type = osuEventType.Sprite;
                        break;
                    default:
                        break;
                }
                if (parameters[2].Contains("\"") )
                {
                    type = osuEventType.File;
                    parameters[2] = parameters[2].Replace("\"", "");

                }
            }
        }

        public osuEvent(osuEventType type, int time, int volume, string ksfile)
        {
            //Sample,24,0,"pispl_010.wav",60
            parameters = new List<string>();
            this.type = type;
            switch (type)
            {
                case osuEventType._0:
                    break;
                case osuEventType.Sprite:
                    break;
                case osuEventType.Sample:
                    parameters = new List<string>{
                        "Sample",
                        time.ToString(),
                        "0",
                        string.Format("\"{0}\"",ksfile)
                        , volume.ToString()
                    };
                    break;
                case osuEventType.Comment:
                    break;
                default:
                    break;
            }
        }
        public override string ToString()
        {
            return string.Join(",", parameters);
        }
    }
    public class osuTimingPoint
    {
        public double Time { get; set; }
        public double MSPerBeat { get; set; }
        public int TimeSig { get; set; }
        public int SType { get; set; }
        public int SSet { get; set; }
        public int Volume { get; set; }
        public int Inherited { get; set; }
        public int Kiai { get; set; }

        public static osuTimingPoint Parse(string line)
        {
            osuTimingPoint TP = new osuTimingPoint();
            try
            {
                string[] parser = line.Split(",".ToCharArray());
                //Console.WriteLine(line);
                if (parser.Length != 8)
                    return null;

                TP.Time = double.Parse(parser[0]);
                TP.MSPerBeat = double.Parse(parser[1]);
                TP.TimeSig = int.Parse(parser[2]);
                TP.SType = int.Parse(parser[3]);
                TP.SSet = int.Parse(parser[4]);
                TP.Volume = int.Parse(parser[5]);
                TP.Inherited = int.Parse(parser[6]);
                TP.Kiai = int.Parse(parser[7]);
                return TP;
            }
            catch
            {
                //return null;
                throw;
            }
        }
        public bool IsTiming()
        {
            return (Inherited == 1);
        }
        public bool IsInherited()
        {
            return (Inherited == 0);
        }
        public string write()
        {
            return string.Join(",", new string[]{
                Time.ToString(),
                MSPerBeat.ToString(),
                TimeSig.ToString(),
                SType.ToString(),
                SSet.ToString(),
                Volume.ToString(),
                Inherited.ToString(),
                Kiai.ToString()
            });
        }
    }
    public class osuHitObject
    {
        public int Xpos { get; set; }
        public int YPos { get; set; }
        public int Time { get; set; }
        public int Type { get; set; }
        public int HitSound { get; set; }
        public osuAddition Addition { get; set; }

        public osuHitObject()
        {
            Addition = new osuAddition();

        }

        public static osuHitObject Parse(string line)
        {
            osuHitObject HO = new osuHitObject();
            try
            {
                string[] parser = line.Split(",".ToCharArray());
                if (parser.Length != 6)
                    return null;

                HO.Xpos = int.Parse(parser[0]);
                HO.YPos = int.Parse(parser[1]);
                HO.Time = int.Parse(parser[2]);
                HO.Type = int.Parse(parser[3]);
                HO.HitSound = int.Parse(parser[4]);
                HO.Addition = osuAddition.Parse(parser[5], HO.Type);
                return HO;
            }
            catch (Exception)
            {
                Console.WriteLine("Error here");

                throw;
            }

        }
        public void ksCopy(osuHitObject ks)
        {
            //there is an actual wav? copy that
            if (ks.Addition.KeySound != "")
            {
                Addition.KeySound = ks.Addition.KeySound;
            }
            else
            {
                HitSound = ks.HitSound;
            }
            Addition.Volume = ks.Addition.Volume;

        }
        public string write()
        {
            return string.Join(",", new string[]
            {
                Xpos.ToString(),
                YPos.ToString(),
                Time.ToString(),
                Type.ToString(),
                HitSound.ToString(),
                Addition.write()
            });
        }
    }
    public class osuAddition
    {
        public string additionRaw { get; set; }
        public int typeRaw { get; set; }
        public int LNEnd { get; set; }
        public int Volume { get; set; }
        public string KeySound { get; set; }

        public static int typeDecider(int typeRaw)
        {
            //Questionable, will need changing if parsing non-mania files
            return (typeRaw < 16) ? 1 : typeRaw;
        }
        public static osuAddition Parse(string additionRawString, int type)
        {
            osuAddition OA = new osuAddition();
            OA.typeRaw = type;
            OA.additionRaw = additionRawString;
            type = typeDecider(type);
            string[] parser = additionRawString.Split(":".ToCharArray());
            switch (type)
            {
                case 1:
                    OA.Volume = int.Parse(parser[3]);
                    OA.KeySound = parser[4];
                    break;
                case 128:
                    OA.LNEnd = int.Parse(parser[0]);
                    OA.Volume = int.Parse(parser[4]);
                    OA.KeySound = parser[5];
                    break;
                default:
                    break;
            }

            return OA;
        }
        public string write()
        {
            int typew = typeDecider(typeRaw);
            switch (typew)
            {
                case 1:
                    return string.Join(":", new string[]
                    {
                        "0","0","0", Volume.ToString(), KeySound
                    });
                case 128:
                    return string.Join(":", new string[]
                    {
                        LNEnd.ToString(),
                        "0","0","0", Volume.ToString(), KeySound
                    });

                default:
                    return "";
            }
        }
    }
    public class smMeasure
    {
        //Constants
        public List<int> SMQuants ;

        //Basics
        public int MeasureNum { get; set; }
        public int KeyCount { get; set; }
        public int BeatsPerMeasure { get; set; }
        public int Quant { get; set; }
        //the int is the index of the line of the measure
        public Dictionary<int,smNote> smNotes { get; set; }

        //If two of the following three are assigned the third is automatically calculated
        private double iStartTime;
        public double StartTime
        {
            get { return iStartTime; }
            set
            {
                iStartTime = value;
                if (iEndTime != -1)
                {
                    iBPM = (60000 * 4 / (iEndTime - iStartTime));
                }
                else if (iBPM != -1)
                {
                    iEndTime = (60000 * 4 / iBPM) + iStartTime;
                }
            }
        }
        private double iEndTime;
        public double EndTime
        {
            get { return iEndTime; }
            set
            {
                iEndTime = value;
                if (iStartTime != -1)
                {
                    iBPM = (60000 * 4 / (iEndTime - iStartTime));
                }
                else if (iBPM != -1)
                {
                    iStartTime = iEndTime - (60000 * 4 / iBPM);
                }
            }
        }
        private double iBPM;
        public double BPM
        {
            get { return iBPM; }
            set
            {
                iBPM = value;
                if (iStartTime != -1)
                {
                    iEndTime = (60000 * 4 / iBPM) + iStartTime;
                }
                else if (iEndTime != -1)
                {
                    iStartTime = iEndTime - (60000 * 4 / iBPM);
                }
            }
        }

        public smMeasure()
        {
            smNotes = new Dictionary<int, smNote>();
            BeatsPerMeasure = 4;
            StartTime = -1;
            EndTime = -1;
            BPM = -1;
            SMQuants = new List<int> { 4, 8, 12, 16, 24, 32, 48, 64, 96, 192 };
        }

        public void fillMeasurefromOsu(List<osuHitObject> oHOinMeasure, List<Tuple<int,int>> LNEnds)
        {
            //We need two of the three optional parameters to work
            smNotes = new Dictionary<int, smNote>();
            //if (iStartTime != -1 && iEndTime >= 0 && iBPM >= 0)
            //Not sure how to deal with the variability of iStartTime here...
            if (iEndTime >= 0 && iBPM >= 0)
            {
                //First, get best quant of all notes and LNEnds in measure, call it timesList
                //Then, assign sm notes
                //Not the most clever, but it's easy
                //LNEnds tuple in format <xpos, LNEndTime>
                List<int> timesList = new List<int>();

                //LNEnds are treated as a separate entity and must be sorted
                LNEnds.Sort((p1, p2) => p1.Item2.CompareTo(p2.Item2));

                //Get all the times into the timesList
                timesList.AddRange(oHOinMeasure.Select(i => i.Time));
                timesList.AddRange(LNEnds.Select(i => i.Item2));
                timesList.Sort();

                //Fix all times to span 0 to 1 in timesListNorm
                List<double> timesListZeroed = new List<double>();
                foreach (int time in timesList)
                {
                    timesListZeroed.Add((time - StartTime));
                }

                //Get the best quantization
                Quant = BestQuant(timesListZeroed, EndTime - StartTime);
                double QuantTime = (EndTime - StartTime) / Quant;
                double currTime = StartTime;

                //Assign SM Notes to Quant
                Queue<osuHitObject> HOQuene = new Queue<osuHitObject>(oHOinMeasure);
                Queue<Tuple<int, int>> LNEndsQueue = new Queue<Tuple<int, int>>(LNEnds);

                //This does stuff?
                double precisionTemp = 2;
                for (int i = 0; i < Quant; i++)
                {
                    currTime = StartTime + i * QuantTime;
                    smNote note = new smNote(KeyCount);
                    if (HOQuene.Count > 0)
                    {
                        //This boolean is to see if we accidentally passed a note
                        bool notePassed = HOQuene.Peek().Time < currTime;
                        while (AlmostEqual(HOQuene.Peek().Time, currTime, precisionTemp) || notePassed)
                        {
                            osuHitObject HO = HOQuene.Dequeue();
                            if (HO.Type == 128)
                            {
                                note.OsuXtoNote(HO.Xpos, "2");
                            }
                            else
                            {
                                note.OsuXtoNote(HO.Xpos, "1");
                            }
                            if (!(HOQuene.Count > 0))
                            {
                                break;
                            }
                            notePassed = HOQuene.Peek().Time < currTime;
                        }
                    }
                    if (LNEndsQueue.Count > 0)
                    {
                        bool notePassed = LNEndsQueue.Peek().Item2 < currTime;
                        while (AlmostEqual(LNEndsQueue.Peek().Item2,currTime, precisionTemp) || notePassed)
                        {
                            note.OsuXtoNote(LNEndsQueue.Dequeue().Item1, "3");
                            if (!(LNEndsQueue.Count > 0)) break;
                            notePassed = LNEndsQueue.Peek().Item2 < currTime;
                        }
                    }
                    smNotes.Add(i, note);

                }
                if (HOQuene.Count > 0) Console.WriteLine("Leftover HOs detected at end of measure. Time : " + HOQuene.Peek().Time.ToString());
                if (LNEndsQueue.Count > 0) Console.WriteLine("Leftover LNEnds detected at end of measure. Time : " + LNEndsQueue.Peek().Item2.ToString());


                //Clever way : look for best quant as writing all timestamps for sm notes. 
                //Convert all timestamps to quant# after best quant is found
                //Actually neither is faster on immediate analysis. So we go with the easier.
            }
            else
            {
                Console.Write("Failed Measure Conversion: ");
                Console.WriteLine(MeasureNum);
            }


        }

        //Quantizing Tools
        public static bool AlmostEqual(double x1, double x2, double precision = 1)
        {
            return AlmostZero(x1 - x2, precision);
        }
        public static bool AlmostZero(double x, double precision = 1)
        {
            return (Math.Abs(x) < precision);
        }
        public int BestQuant(List<double> Times, double MeasureLength)
        {
            double testLength, precisionTemp;
            bool foundBest = false;
            foreach (int quant in SMQuants)
            {
                //Here we make a somewhat arbitrary decision, we'll see if it works later
                //The precision of osu beatmaps is 1ms, so we should keep this 1ms, right?
                //precision = 1.5 / quant;
                precisionTemp = 2;
                testLength = MeasureLength / quant;
                //if (Times.All(time => AlmostZero(time % testLength, precisionTemp)))
                //Clever, but too hard to debug.
                foundBest = true;
                foreach (double time in Times)
                {
                    double isitZero = time % testLength;
                    isitZero = (Math.Abs(isitZero) > Math.Abs(isitZero - testLength)) ? isitZero - testLength : isitZero;
                    foundBest &= AlmostZero(isitZero, precisionTemp);

                }

                if (foundBest)
                {
                    //Console.WriteLine(quant);
                    return quant;
                }
            }
            return SMQuants.Last();
        }
    }
    public class smNote
    {
        public string[] indNotes { get; set; }
        public int MeasureLineNum { get; set; }
        public int KeyCount { get; set; }

        public smNote(int keyCount)
        {
            KeyCount = keyCount;
            indNotes = new string[keyCount];
            for (int i = 0; i < keyCount; i++)
            {
                indNotes[i] = "0";
            }
        }

        public void OsuXtoNote(int osuX, string notetype)
        {
            int keyInd = (int)Math.Round((((((osuX / 512.0) * KeyCount * 2) + 1) / 2) - 1));
            indNotes[keyInd] = notetype;
        }
        public string makeRawNoteLine()
        {
            string rawLine = "";
            foreach (string note in indNotes)
            {
                rawLine = rawLine + note;
            }
            return rawLine;
        }
    }
    public enum smQuant //Never used?
    {
        _4, _8, _12, _16, _24, _32, _48, _64, _96, _192
    }
    public enum smDiff
    {
        Beginner,
        Easy,
        Medium,
        Hard,
        Challenge,
        Edit
    }
}
