using BuggieBungalow_Pult.Properties;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace BuggieBungalow_Pult
{
    public partial class MainScreen : Form
    {
        string[] inputData_buffer;

        int prevPktCount = 0;
        //music
        static string MUSIC_ROOT = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        struct Tracks
        {
            public const string MUSIC_ATMO1 = "atmo1.mp3";
            public const string MUSIC_ATMO2 = "atmo2.mp3";
            public const string MUSIC_SEPAREE = "separee.mp3";
            public const string MUSIC_FINALE = "finale.mp3";
            public const string MUSIC_STOPPTANZ = "stopptanz.mp3";
        }
        public string prevSong { get; set; }
        public string nextSong { get; set; }



        IWavePlayer waveOutDevice = new WaveOut();
        AudioFileReader audioFileReader;
        private FadeInOutSampleProvider fadeInOut;

        DateTime MusicDelayed_StartTime;


        DateTime SepareeSolvedTime = DateTime.MinValue;
        DateTime PultStartTime = DateTime.Now;
        int JukeboxCommandCnt = 2;
        int RingingCnt = 2;

        public Timer Clock = new Timer();
        public Timer MusicChangeViewer = new Timer();
        public Timer BackgroundWorker = new Timer();
        private int Init_counter = 0;
        public Timer Initializator = new Timer();
        private int AccumulatedLostFlags_MasterNode = 0;
        bool sync = false;
        public GameSession Session { get; set; }
        private BindingSource BindingSource = new BindingSource();
        List<Label> RemoteStates;
        List<Button> ButtonsDisabled = new List<Button>();
        private SerialPort ArduinoCOM;

        private bool isPaused = false;

        bool FirstSepareeClick = true;

        PictureBox[] RangePics = new PictureBox[7];

        int ConsecutiveNetworkResetRequests = 0;
        DateTime LastNetResetRequest = DateTime.MinValue;

        DateTime LastButtonActivation = DateTime.MinValue;
        public MainScreen()
        {
            InitializeComponent();

            //initialize a Session object
            Session = new GameSession();

            // set the data bindings source to the variables here (i think this is by default though)
            this.BindingSource.DataSource = this;
            dataGridRiddles.DataSource = Session.Riddles;

            // setup the Clock (used for the Game Timer) to tick every second and to run the 'clock_tick' function
            Clock.Tick += Clock_Tick;
            Clock.Interval = 1000;

            //clock to send initialization messages without overwhelming the arduino

            BackgroundWorker.Tick += BackgroundWorker_Tick;
            BackgroundWorker.Interval = 1000;

            MusicChangeViewer.Tick += MusicChangeViewer_Tick;
            MusicChangeViewer.Interval = 250;

            MusicDelayed_StartTime = DateTime.MinValue;

            RangePics[0] = GM_range_4Drinks;
            RangePics[1] = GM_range_jukebox;
            RangePics[2] = GM_range_Stopptanz;
            RangePics[3] = GM_range_sparkatsen;
            RangePics[4] = GM_range_wasserhahn;
            RangePics[5] = GM_range_workSchedules;
            RangePics[6] = GM_range_Separee;

            foreach (PictureBox pic in RangePics)
            {
                pic.BackgroundImage = Resources.signal_lost;
                pic.BackgroundImageLayout = ImageLayout.Zoom;
            }

            dataGridRiddles.ColumnDataPropertyNameChanged += DataGridRiddles_ColumnDataPropertyNameChanged;
            //fill serialPorts Available
            COMportsList.DataSource = SerialPort.GetPortNames();

            //set light off as default value
            comboBox_separeePreFinalColor.SelectedIndex = 4;


            //data bindings
            SessionRunningTime.DataBindings.Add(new Binding("Text", this.BindingSource, "Session.RunningTime", true, DataSourceUpdateMode.OnPropertyChanged));
            masterNodeHealth.DataBindings.Add(new Binding("Text", this.BindingSource, "Session.RTdelayCOMmaster", true, DataSourceUpdateMode.OnValidation));
            label_nextSong.DataBindings.Add(new Binding("Text", this.BindingSource, "nextSong", true, DataSourceUpdateMode.OnPropertyChanged));

            lastPacket_label.DataBindings.Add(new Binding("Text", this.BindingSource, "Session.LatestPacket", true, DataSourceUpdateMode.OnPropertyChanged));
            CorruptPacket_label.DataBindings.Add(new Binding("Text", this.BindingSource, "Session.CorruptPackets", true, DataSourceUpdateMode.OnPropertyChanged));
            TotalPacket_label.DataBindings.Add(new Binding("Text", this.BindingSource, "Session.TotalPackets", true, DataSourceUpdateMode.OnPropertyChanged));
            COMLog.DataBindings.Add(new Binding("Text", this.BindingSource, "Session.COMlog", true, DataSourceUpdateMode.OnPropertyChanged));
            NetChannel_label.DataBindings.Add(new Binding("Text", this.BindingSource, "Session.NetChannel", true, DataSourceUpdateMode.OnPropertyChanged));
            SelfRepairs_label.DataBindings.Add(new Binding("Text", this.BindingSource, "Session.NetSelfRepairs", true, DataSourceUpdateMode.OnPropertyChanged));
            NodeResync_label.DataBindings.Add(new Binding("Text", this.BindingSource, "Session.NodeResyncs", true, DataSourceUpdateMode.OnPropertyChanged));
            PacketsSent_label.DataBindings.Add(new Binding("Text", this.BindingSource, "Session.PacketsSent", true, DataSourceUpdateMode.OnPropertyChanged));
        }


        private void MainScreen_Shown(object sender, EventArgs e)
        {
            //check that all music files are there
            bool MusicFileMissing =
                File.Exists(Path.Combine(MUSIC_ROOT, Tracks.MUSIC_ATMO1)) &&
                File.Exists(Path.Combine(MUSIC_ROOT, Tracks.MUSIC_ATMO2)) &&
                File.Exists(Path.Combine(MUSIC_ROOT, Tracks.MUSIC_FINALE)) &&
                File.Exists(Path.Combine(MUSIC_ROOT, Tracks.MUSIC_SEPAREE)) &&
                File.Exists(Path.Combine(MUSIC_ROOT, Tracks.MUSIC_STOPPTANZ));

            if (!MusicFileMissing)
            {
                //not all the music tracks are there. Notify it
                DialogResult result1 = MessageBox.Show("Not all the music tracks were found. The session cannot start.\n\n" + "" +
                    "The folder expected is: " + MUSIC_ROOT + "" +
                    "\n\nThe files inside that folder must be named (exactly):\n" +
                    "\n" + Tracks.MUSIC_ATMO1 +
                    "\n" + Tracks.MUSIC_ATMO2 +
                    "\n" + Tracks.MUSIC_FINALE +
                    "\n" + Tracks.MUSIC_STOPPTANZ +
                    "\n" + Tracks.MUSIC_SEPAREE +
                    "\n\nThe app will close..."
                    , "Error in the music tracks", MessageBoxButtons.OK);
                Application.Exit();

            }
            else
            {
                prevSong = "-";
                nextSong = Tracks.MUSIC_ATMO1;
                audioFileReader = new AudioFileReader(Path.Combine(MUSIC_ROOT, Tracks.MUSIC_ATMO1));
                fadeInOut = new FadeInOutSampleProvider(audioFileReader);
                waveOutDevice.Init(fadeInOut);
            }

            BackgroundWorker.Start();

        }

        private void ConnectingMode()
        {
            foreach (Riddle riddle in Session.Riddles)
                riddle.AVGdelayCOM = uint.MinValue;
        }

        private void SendPacket(string NodeID, bool solved, int state)
        {
            string message = NodeID + "," + (solved ? "01" : "00") + ",0" + state.ToString();
            Session.COMlog += DateTime.Now.TimeOfDay.ToString() + ": SYNC Packet sent to Master Node: " + message + "\r\n";
            SendCommand(message);
            Session.PacketsSent++;
        }

        private void StartInitialization()
        {
            Session.COMlog += DateTime.Now.TimeOfDay.ToString() + ": Pult launched the Sync" + "\r\n";
            sync = true;
            ConnectingMode();

        }

        private void BackgroundWorker_Tick(object sender, EventArgs e)
        {

            this.BindingSource.ResetBindings(false); //update data bindings so the interface shows the new values
            updateGrid();

            label_nextSong_delay.Text = MusicDelayed_StartTime == DateTime.MinValue ? "0 s" : Math.Round((MusicDelayed_StartTime - DateTime.Now).TotalSeconds).ToString() + " s";

            if (MusicDelayed_StartTime != DateTime.MinValue && nextSong == Tracks.MUSIC_ATMO1 && MusicDelayed_StartTime - DateTime.Now <= TimeSpan.FromSeconds(0))
            {
                PlaySong(Tracks.MUSIC_ATMO1, true);
                MusicDelayed_StartTime = DateTime.MinValue;
            }

            //music state
            label_PlaybackState.Text = waveOutDevice.PlaybackState == PlaybackState.Playing ? "Spielen" : "Pausiert";
            label_PlaybackState.ForeColor = waveOutDevice.PlaybackState == PlaybackState.Playing ? Color.DarkGreen : Color.DarkRed;
            label_track.Text = audioFileReader.FileName.Split('\\')[audioFileReader.FileName.Split('\\').Length - 1].Trim();


            //when separee is solved, I must launch the moneyrain after some seconds
            if (!SepareeSolvedTime.Equals(DateTime.MinValue))
            {
                debuglabel.Text = ", countdown: " + ((double)MoneyGunDelay.Value - ((DateTime.Now - SepareeSolvedTime).TotalSeconds)).ToString();
                //is not minValue so it has been set already
                if (DateTime.Now - SepareeSolvedTime >= TimeSpan.FromSeconds((double)MoneyGunDelay.Value))
                {
                    SepareeSolvedTime = DateTime.MinValue; //so it does not keep launching
                    if (checkBox_moneyGun.Checked)
                    {
                        SendCommand("01,01,08"); // set state to solved with moneyGun
                    }
                    else
                    {
                        SendCommand("01,01,09"); // set state to solved without moneyGun
                    }
                }
            }

            if (sync)
            {
                BackgroundWorker.Interval = 500;
                SendPacket(Session.DecimalIDs[Init_counter], Session.Riddles[Init_counter].Solved, Session.Riddles[Init_counter].state);
                Init_counter++;

                if (Init_counter == 8)
                {
                    Init_counter = 0;
                    sync = false;

                }
            }
            else
            {
                BackgroundWorker.Interval = 1000;
            }

            if (!Session.SerialConnectionActive)
            {
                AccumulatedLostFlags_MasterNode++;
                if (AccumulatedLostFlags_MasterNode > 3)
                {
                    foreach (Riddle riddle in Session.Riddles)
                        riddle.AVGdelayCOM = uint.MinValue;

                    masterNodeHealth.Text = "LOST CONNECTION";
                    masterNodeHealth.ForeColor = Color.Red;
                    Session.SerialConnectionInitialized = false;
                    foreach (PictureBox pic in RangePics)
                    {
                        pic.BackgroundImage = Resources.signal_lost;
                    }
                    //Session.SerialConnectionActive = false;
                }
            }
            else
            {
                AccumulatedLostFlags_MasterNode = 0;
            }

        }

        private void DataGridRiddles_ColumnDataPropertyNameChanged(object sender, DataGridViewColumnEventArgs e)
        {

        }

        private void updateGrid()
        {
            dataGridRiddles.Invoke(new Action(() => { dataGridRiddles.Refresh(); }));
            int[] range = new int[8];

            for (int row = 0; row < dataGridRiddles.Rows.Count; row++)
            {
                try
                {
                    int output = int.Parse(dataGridRiddles.Rows[row].Cells[0].Value.ToString());
                    bool badConnection = output == 0 || output > 3000;
                    bool weakConnection = output > 1000;
                    bool goodConnection = !badConnection && !weakConnection;
                    dataGridRiddles.Rows[row].Cells[0].Style.BackColor = badConnection ? Color.Red : (weakConnection ? Color.Yellow : Color.Green);
                    range[row] = badConnection ? 0 : (weakConnection ? 1 : 2);

                }
                catch
                {
                    dataGridRiddles.Rows[row].Cells[0].Style.BackColor = Color.Orange;
                }
            }
            RangePics[0].BackgroundImage = range[5] == 0 ? Resources.signal_bad : (range[5] == 1 ? Resources.signal_medium : Resources.signal_good);
            RangePics[1].BackgroundImage = range[3] == 0 ? Resources.signal_bad : (range[3] == 1 ? Resources.signal_medium : Resources.signal_good);
            RangePics[2].BackgroundImage = range[1] == 0 ? Resources.signal_bad : (range[1] == 1 ? Resources.signal_medium : Resources.signal_good);
            RangePics[3].BackgroundImage = range[2] == 0 ? Resources.signal_bad : (range[2] == 1 ? Resources.signal_medium : Resources.signal_good);
            RangePics[4].BackgroundImage = range[4] == 0 ? Resources.signal_bad : (range[4] == 1 ? Resources.signal_medium : Resources.signal_good);
            RangePics[5].BackgroundImage = range[4] == 0 ? Resources.signal_bad : (range[4] == 1 ? Resources.signal_medium : Resources.signal_good);
            RangePics[6].BackgroundImage = range[0] == 0 ? Resources.signal_bad : (range[0] == 1 ? Resources.signal_medium : Resources.signal_good);

            delay_4drinks.Text = dataGridRiddles.Rows[5].Cells[0].Value + " ms";
            delay_jukebox.Text = dataGridRiddles.Rows[3].Cells[0].Value + " ms";
            delay_stoptanz.Text = dataGridRiddles.Rows[1].Cells[0].Value + " ms";
            delay_sparkatchen.Text = dataGridRiddles.Rows[2].Cells[0].Value + " ms";
            delay_wasserhahn.Text = dataGridRiddles.Rows[4].Cells[0].Value + " ms";
            delay_schichtplan.Text = dataGridRiddles.Rows[4].Cells[0].Value + " ms";
            delay_separee.Text = dataGridRiddles.Rows[0].Cells[0].Value + " ms";
        }

        private void Clock_Tick(object sender, EventArgs e)
        {
            // set the time passed, as the current time minus the time the session started. 
            // the 'truncate' functions are to remove the milliseconds and round them to the closest second (so you do not have hr:min:sec.millis)
            if (Session.HasStarted)
                Session.RunningTime = Session.RunningTime.Add(TimeSpan.FromSeconds(1)); //Truncate(DateTime.Now, TimeSpan.TicksPerSecond).Subtract(Truncate(Session.StartTime, TimeSpan.TicksPerSecond));

            if (prevPktCount == Session.TotalPackets)
            {
                Session.SerialConnectionActive = false;
                prevPktCount = Session.TotalPackets;
            }


        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void label1_Click_1(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!Session.HasStarted)
            {
                if (ArduinoCOM != null && ArduinoCOM.IsOpen)
                {
                    DialogResult result1 = MessageBox.Show("The session is not started yet, so a reset has no effect. \n\nDid you want to reset the Electronics?", "Electronics reset confirmation", MessageBoxButtons.YesNo);

                    if (result1 == DialogResult.Yes)
                    {
                        SendCommand("00,00,01");
                    }
                }
                return;

            }

            DialogResult result = MessageBox.Show("Are you sure that you want to finish the current session? \n\n Use only when the group has finished!", "Reset confirmation", MessageBoxButtons.YesNo);

            if (result == DialogResult.No)
                return;


            Session = new GameSession();
            Clock.Enabled = false;
            startButton.Enabled = true;
            //dataGridRiddles.DataSource = null;
            SendCommand("00,00,01");

            foreach (Button b in ButtonsDisabled)
            {
                b.Enabled = true;
                b.Text = "Solve";

            }

            //groupBoxElectronicsSettings.Enabled = true;
            //groupBoxPayerSettings.Enabled = true;

            this.BindingSource.ResetBindings(false);
            MessageBox.Show("The Game (and Electronics) are ready for the next session :).", "Reset confirmation", MessageBoxButtons.OK);

        }

        private void startButton_Click(object sender, EventArgs e)
        {
            if (ArduinoCOM == null)
            {
                MessageBox.Show("You MUST select and test a COM port before starting a session: \n\n 1 - In Setup, select the appropiate COM port. \n\n 2 - Click on 'Test COM' to initialize (and test) the connection with the Master remote.", "COM not initialized", MessageBoxButtons.OK);
                return;
            }

            Button button = sender as Button;


            button.Enabled = false;
            Clock.Enabled = true;

            Session.StartTime = DateTime.Now;
            Session.HasStarted = true;

            //Session.NumberOfPlayers = numberOfPlayers.SelectedItem != null ? numberOfPlayers.SelectedItem as string : "Not defined";

            //just in case the service team played with the lights of the separee or any other state, reset it at the beggining of the game
            //ArduinoCOM.WriteLine("01,00,00" ); // set the separee to unsolved and state OFF

            this.BindingSource.ResetBindings(false);
        }

        private void ArduinoCOM_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            

            try
            {
                if (ArduinoCOM.BytesToRead < 28)
                    return;

                string data = ArduinoCOM.ReadLine();
                Session.TotalPackets++;

                Session.LatestPacket = data;
                Session.LastPacketLength = data.Length;
                Session.LatestPacket.Remove(Session.LastPacketLength - 1);

                processBuffer(data.Split(','));
            }
            catch
            {
                Session.CorruptPackets++;
            }


        }

        private void processBuffer(string[] data)
        {
            //the information coming is a string with 29 numbers separated by coma, being:
            // [0] millis of master node

            // then, for each riddle (order 1,2,3,4,5,12,22)... So, 7 times:
            // [1] id
            // [2] millis
            // [3] solved
            // [4] state

            Session.SerialConnectionActive = true;

            if (!uint.TryParse(data[0].Substring(1), out uint output))
            {
                Session.CorruptPackets++;
                return;
            }
                

            if (Session.SerialConnectionActive == false)
                ButtonCOM.Text = "Restart COM\n(Connected)";

            if (!ButtonCOM.Enabled)
                ButtonCOM.Enabled = true;

            if (output >= 11 && output <= 17)
            {
                Session.COMlog += DateTime.Now.TimeOfDay.ToString() + ": Laggy connection with Node " + (output - 10).ToString() + " detected. It was forced to auto-restart.\r\n";
                return;
            }
            else if (output >= 70 && output <= 120)
            {
                Session.COMlog += DateTime.Now.TimeOfDay.ToString() + ": The Master Node set the Network channel to " + output.ToString() + "\r\n";
                Session.NetChannel = (int)output;
                return;
            }
            else if (output == 1)
            {
                Session.SerialConnectionInitialized = true;
                Session.COMlog += DateTime.Now.TimeOfDay.ToString() + ": The Master node requested Sync." + "\r\n";
                StartInitialization();
                return;
            }
            else if (output == 2)
            {
                Session.NetSelfRepairs++;
                return;
            }
            else if (output == 3)
            {
                //Session.COMlog += DateTime.Now.TimeOfDay.ToString() + ": The Master detected lag in the network and cleaned up everything." + "\r\n";
                Session.NetSelfRepairs++;
                if (DateTime.Now.Subtract(LastNetResetRequest) < TimeSpan.FromMinutes(1))
                {
                    ConsecutiveNetworkResetRequests++;
                    if (ConsecutiveNetworkResetRequests >= 2)
                    {
                        // MessageBox.Show("The Remotes Network keeps having issues (again, do not panic!). Please, do a 'Electronics Reset' whenever is fine to loose the remotes for 30 seconds.", "Electronics reset required", MessageBoxButtons.OK);
                    }
                    else
                    {
                        //MessageBox.Show("The Remotes Network has some issues (do not panic!). Please, do a 'Network Reset' whenever is fine to lose the remotes for 15 seconds.\n\nThe system will recover the current game state itself during the process.", "Network reset required", MessageBoxButtons.OK);
                    }
                }
                else
                {
                    ConsecutiveNetworkResetRequests = 0;

                }

                LastNetResetRequest = DateTime.Now;
                return;
            }
            else if (output == 4)
            {
                Session.COMlog += DateTime.Now.TimeOfDay.ToString() + ": The Master node received a Network reset request. Launching..." + "\r\n";
                return;
            }
            else if (output == 5)
            {
                Session.NodeResyncs++;
                return;
            }
            else if (output == 6)
            {
                Session.COMlog += DateTime.Now.TimeOfDay.ToString() + ": The Master node reported that the Sync was successful. Current Session recovered." + "\r\n";
                return;
            }
            else if (output == 7)
            {
                Session.COMlog += DateTime.Now.TimeOfDay.ToString() + ": The Master node reported that the Sync was not successful. State set to 'New Game'" + "\r\n";
                return;
            }
            else if (output == 8)
            {
                Session.COMlog += DateTime.Now.TimeOfDay.ToString() + ": The Master node received a reset request. Launching..." + "\r\n";
                return;
            }
            else if (output > 3000)
            {

                Session.RTdelayCOMmaster = output;

                int aux = 0;
                for (int i = 1; i <= 4*7; i += 4)
                {
                    aux = i / 4;
                    Session.Riddles[aux].AVGdelayCOM = uint.Parse(data[i + 1]);
                    bool newSolved = (float.Parse(data[i + 2]) == 1) ? true : false;
                    int newState = int.Parse(data[i + 3]);

                    switch (aux)
                    {
                        case 0:
                            // SEPAREE NODE
                            if (Session.Riddles[aux].state == 0 && (newState > 0 && newState < 4)) //if it was off and now is one of the 3 colors then it just went on
                            {
                                //if (checkBox_manualControl.Checked) PlaySong(Tracks.MUSIC_SEPAREE, false);



                            }

                            if (!Session.Riddles[aux].Solved && newSolved) // if it has been just solved, get the time to launch the money rain
                            {
                                //if (checkBox_manualControl.Checked) PlaySong(Tracks.MUSIC_FINALE, false);
                                //SepareeSolvedTime = DateTime.Now;
                            }


                            if (newState == 7)//the door command reached the node so change it to make it stop opening
                            {
                                SendCommand("01,00,00");
                            }

                            break;

                        case 1:
                            // STOPPTANZ

                            //When the node reports that it has solved itself, change the music and open the sparkatsen
                            if (!Session.Riddles[aux].Solved && newSolved)//if it is solved just now
                            {
                                //if (checkBox_manualControl.Checked) PlaySong(Tracks.MUSIC_ATMO2, false);
                                //ArduinoCOM.WriteLine("03,00,01"); //open the sparkatsen
                            }

                            if (Session.Riddles[aux].state == 0 && newState == 3) //it started the initialization, so launch the song
                            {
                                //if (checkBox_manualControl.Checked) PlaySong(Tracks.MUSIC_STOPPTANZ, false);
                            }

                            break;

                        case 2:
                            // SPARKATSEN

                            //turn off the open request
                            if (newState == 3) //the riddle received the open command
                            {
                                SendCommand("03,00,00"); //stop asking to open compartment. 0 is ignored to just use it as joker
                            }

                            break;

                        case 3:
                            // JUKEBOX

                            break;

                        case 4:
                            // WASSERHAN / WORK SCHEDULES
                            //turn off the open request
                            if (newState == 8 || newState == 9) //the riddle received the open command
                            {
                                SendCommand("05,00,00"); //stop asking to open compartment. 0 is ignored to just use it as joker
                            }
                            break;

                        case 5:
                            // 4 DRINKS
                            if (newState == 1) //the riddle received the open command
                            {
                                SendCommand("13,00,00"); //stop asking to open compartment. 0 is ignored to just use it as joker

                            }
                            break;

                        case 6:
                            // TELEPHONE
                            if (newState > 2)//the phone received the ring request so stop asking
                            {
                                SendCommand("21,00,00");
                            }
                            break;

                        case 7:
                            // SECRET CORRIDOR
                            if (Session.Riddles[aux].state == 0)//it is off, so ask it to get pink which is the default
                            {
                                SendCommand("29,00,02");
                            }
                            break;
                    }
                    Session.Riddles[aux].Solved = newSolved;
                    Session.Riddles[aux].state = newState;

                }

            }
            else
            {
                Session.COMlog += DateTime.Now.TimeOfDay.ToString() + ": Unknown notification from Master node (Code " + output.ToString() + ")\r\n";

            }

        }

        private void SendCommand(string command)
        {
            ArduinoCOM.WriteLine(">" + command);
        }

        private void groupBox15_Enter(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void GameMasterLaunchedRiddle(object sender, EventArgs e)
        {
            if (Session.HasStarted)
            {

                if (isPaused)
                {
                    DialogResult result = MessageBox.Show("The sesion is Paused! Are you sure that you want to launch it?", "Confirm", MessageBoxButtons.YesNo);

                    if (result == DialogResult.No)
                        return;

                }

                Button buttonPressed = sender as Button;

                LaunchRoutineForButton(buttonPressed);

                this.BindingSource.ResetBindings(false);
            }
            else
            {
                DialogResult result = MessageBox.Show("You did not start the session yet. Start it and try again", "Session not started", MessageBoxButtons.OK);
            }
        }

        private void LaunchRoutineForButton_ForServiceTeam(object sender, EventArgs e)
        {
            if (ArduinoCOM == null)
            {
                MessageBox.Show("You MUST select and test a COM port before sending these commands: \n\n 1 - Select the appropiate COM port (Refresh if needed). \n\n 2 - Click on 'Test COM' to initialize (and test) the connection with the Master remote.", "COM not initialized", MessageBoxButtons.OK);
                return;
            }

            if (DateTime.Compare(DateTime.Now, LastButtonActivation.AddSeconds(1)) == -1)
            {
                MessageBox.Show("Sie sättigen die Kommunikation. Bitte haben Sie Geduld und betätigen Sie die Tasten nicht so oft.", "Gesättigtes Netz", MessageBoxButtons.OK);
                return;
            }
            LastButtonActivation = DateTime.Now;

            Button pressedButton = sender as Button;

            switch (pressedButton.Tag)
            {
                case "preG_4drinks_door":
                    SendCommand("13,00,01");
                    break;


                case "preG_Sparkastchen_door":
                    SendCommand("03,00,03");
                    break;

                case "preG_wasserhahn_door":
                    SendCommand("05,00,08");
                    break;

                case "preG_worksch_door":
                    SendCommand("05,00,09");
                    break;

                //Serparee buttons
                case "preG_Separee_r":
                    SendCommand("01,00,01");
                    break;

                case "preG_Separee_g":
                    SendCommand("01,00,02");
                    break;

                case "preG_Separee_b":
                    SendCommand("01,00,03");
                    break;

                case "preG_Separee_y":
                    SendCommand("01,00,05");
                    break;

                case "preG_Separee_w":
                    SendCommand("01,00,06");
                    break;

                case "preG_Separee_off":
                    SendCommand("01,00,00");
                    break;

                case "preG_Separee_door":
                    SendCommand("01,00,07");
                    break;


                //Secret corridor buttons
                case "preG_SecretC_r":
                    SendCommand("29,00,01");
                    break;

                case "preG_SecretC_p":
                    SendCommand("29,00,02");
                    break;

                case "preG_SecretC_y":
                    SendCommand("29,00,03");
                    break;

                case "preG_SecretC_w":
                    SendCommand("29,00,05");
                    break;

                case "preG_SecretC_off":
                    SendCommand("29,00,00");
                    break;
            }
        }

        private async void LaunchRoutineForButton(Button button)
        {
            if (DateTime.Compare(DateTime.Now, LastButtonActivation.AddSeconds(1)) == -1)
            {
                MessageBox.Show("Sie sättigen die Kommunikation. Bitte haben Sie Geduld und betätigen Sie die Tasten nicht so oft..", "Gesättigtes Netz", MessageBoxButtons.OK);
                return;
            }
            LastButtonActivation = DateTime.Now;

            switch (button.Tag)
            {

                // ----------------- RIDDLES BUTTONS ----------------- //

                case "wasserhahn_enable":
                    SendCommand("05,00,01");
                    break;

                case "wasserhahn_solve":
                    SendCommand("05,00,02");
                    break;

                case "4Drinks":
                    SendCommand("13,01,00");
                    break;

                case "Jukebox_Activate":
                    SendCommand("04,00,01");
                    break;

                case "Jukebox_Fix":
                    SendCommand("04,00,01");
                    break;

                case "Jukebox_SolveSong":
                    SendCommand("04,00,01");
                    break;


                case "TrafficLight_Start":
                    if (Session.Riddles[1].AVGdelayCOM != 0)
                    {
                        SendCommand("02,00,03");
                        if (checkBox_manualControl.Checked) PlaySong(Tracks.MUSIC_STOPPTANZ, false);
                    }
                    break;

                case "TrafficLight_Stop":
                    if (Session.Riddles[1].AVGdelayCOM != 0)
                    {
                        SendCommand("02,00,02");
                        Pause(false);
                    }
                    else
                    {

                    }
                    break;

                case "TrafficLight_Dance":
                    if (Session.Riddles[1].AVGdelayCOM != 0)
                    {
                        SendCommand("02,00,01");
                        Resume(false);
                    }
                    else
                    {

                    }
                    break;

                case "TrafficLight_Solve":
                    if (Session.Riddles[1].AVGdelayCOM != 0)
                    {
                        SendCommand("02,01,00");
                        if (checkBox_manualControl.Checked) PlaySong(Tracks.MUSIC_ATMO2, false);
                        await Task.Delay(300);
                        SendCommand("03,00,01");
                    }
                    break;

                case "WorkingSchedules":
                    SendCommand("05,00,03");
                    break;

                case "Separee_solve":
                    if (Session.Riddles[0].AVGdelayCOM != 0)
                    {
                        SepareeSolvedTime = DateTime.Now;

                        switch (comboBox_separeePreFinalColor.SelectedIndex)
                        {
                            case 0:
                                SendCommand("01,01,06");
                                break;
                            case 1:
                                SendCommand("01,01,01");
                                break;
                            case 2:
                                SendCommand("01,01,02");
                                break;
                            case 3:
                                SendCommand("01,01,03");
                                break;
                            case 4:
                                SendCommand("01,01,00");
                                break;
                        }
                        if (checkBox_manualControl.Checked) PlaySong(Tracks.MUSIC_FINALE, false);
                    }
                    break;

                case "Separee_r":
                    if (checkBox_manualControl.Checked && FirstSepareeClick) { PlaySong(Tracks.MUSIC_SEPAREE, false); }
                    FirstSepareeClick = false;
                    SendCommand("01,00,01");
                    break;

                case "Separee_g":
                    if (checkBox_manualControl.Checked && FirstSepareeClick) { PlaySong(Tracks.MUSIC_SEPAREE, false); }
                    FirstSepareeClick = false;
                    SendCommand("01,00,02");
                    break;

                case "Separee_b":
                    if (checkBox_manualControl.Checked && FirstSepareeClick) { PlaySong(Tracks.MUSIC_SEPAREE, false); }
                    FirstSepareeClick = false;
                    SendCommand("01,00,03");
                    break;

                case "Telephone_ring":
                    SendCommand("21,00,0" + (RingingCnt.ToString()));
                    RingingCnt++;
                    if (RingingCnt == 10)
                        RingingCnt = 2;
                    break;

                case "Sparkastchen_start":
                    if (Session.Riddles[2].AVGdelayCOM != 0)
                    {
                        if (!Session.Riddles[1].Solved)
                        {
                            SendCommand("02,01,00");
                            if (checkBox_manualControl.Checked) PlaySong(Tracks.MUSIC_ATMO2, false);
                        }
                        else
                        {
                            SendCommand("03,00,01");
                        }
                    }
                    break;

                case "Sparkastchen_solve":
                    SendCommand("03,00,02");
                    break;


                    // ----------------- OTHER BUTTONS ----------------- //

                    //smoke has its own function because it has to be while it is pressed!


            }

            //Riddle riddle = Session.Riddles.Where(r => r.Name.Equals(button.Tag)).FirstOrDefault();

            // riddle.SolvedAt = Truncate(DateTime.Now, TimeSpan.TicksPerSecond).Subtract(Truncate(Session.StartTime, TimeSpan.TicksPerSecond));
            // riddle.Solved = true;
            // riddle.SolvedBy = "GameMaster";
        }

        public static DateTime Truncate(DateTime date, long resolution)
        {
            return new DateTime(date.Ticks - (date.Ticks % resolution), date.Kind);
        }

        private void groupBoxPayerSettings_Enter(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {

            if ((sender as Button).Text == "Connect")
            {
                if (COMportsList.SelectedItem == null)
                {
                    MessageBox.Show("Please, select a COM port to test. If no COM port is listed, try:\n\n 1 - Disconect the USB cable, and connect it back.\n 2 - Click on 'Refresh' to scan for new ports available.", "COM port not selected", MessageBoxButtons.OK);
                    Session.SerialConnectionActive = false;

                }
                else
                {
                    OpenCOM((sender as Button));
                }

            }
            else
            {
                if (ArduinoCOM != null && ArduinoCOM.IsOpen)
                {
                    DialogResult result = MessageBox.Show("Are you sure that you want to restart the COM? \n\nThis action will make the remotes to be OFFLINE for up to 30 seconds!", "Confirmation", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        ArduinoCOM.DataReceived -= ArduinoCOM_DataReceived;
                        OpenCOM((sender as Button));
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    OpenCOM((sender as Button));
                }
            }




            this.BindingSource.ResetBindings(false);

            //DialogResult result = MessageBox.Show("This is not implemented yet.", "Not implemented", MessageBoxButtons.OK);



        }

        private void OpenCOM(Button ConnectButton)
        {
            try
            {
                ConnectButton.Enabled = false;

                if (ArduinoCOM != null && ArduinoCOM.IsOpen)
                {
                    ArduinoCOM.Close();
                }


                Session.SerialConnectionInitialized = false;
                Session.SerialConnectionActive = false;
                ArduinoCOM = new SerialPort();
                ArduinoCOM.BaudRate = 115200;
                ArduinoCOM.PortName = COMportsList.SelectedItem as string;
                ArduinoCOM.Open();
                SendCommand("00,00,01");

                ButtonCOM.Text = "Waiting for Master's ACK...";
                DateTime startWaitingInitMessage = DateTime.Now;
                int timeout = 2000;
                while (DateTime.Now.Subtract(startWaitingInitMessage) < TimeSpan.FromMilliseconds(timeout))
                {
                    if (ArduinoCOM.BytesToRead > 0 && ArduinoCOM.ReadChar() == '8')
                    {
                        Session.SerialConnectionInitialized = true;
                        Session.COMlog += DateTime.Now.TimeOfDay.ToString() + ": ACK received from Master node. Waiting for Master to ask for SYNC. Please wait up to 30 seconds..." + "\r\n";
                        break;
                    }
                };

                if (Session.SerialConnectionInitialized)
                {

                    //MessageBox.Show("The COM test was successful.", "COM port test: OK", MessageBoxButtons.OK);
                    ButtonCOM.Text = "ACK Received. Starting COM...";
                    ArduinoCOM.DataReceived += ArduinoCOM_DataReceived;
                    ConnectButton.Enabled = true;
                }
                else
                {
                    if (ArduinoCOM != null && ArduinoCOM.IsOpen)
                    {
                        ArduinoCOM.Close();
                        ArduinoCOM.DataReceived -= ArduinoCOM_DataReceived;
                        Session.SerialConnectionInitialized = false;
                        Session.SerialConnectionActive = false;
                        ButtonCOM.Text = "Connect";
                        ConnectButton.Enabled = true;
                    }
                    ConnectButton.Enabled = true;
                    MessageBox.Show("The Master node did not answer.\n\nProbably, the " + COMportsList.SelectedItem as string + " is not correct. If you are sure, just try again!\n\n.", "Master Node answer timeout", MessageBoxButtons.OK);
                }
            }
            catch
            {
                if (ArduinoCOM != null && ArduinoCOM.IsOpen)
                {
                    ArduinoCOM.Close();
                    ArduinoCOM.DataReceived -= ArduinoCOM_DataReceived;
                    Session.SerialConnectionInitialized = false;
                    Session.SerialConnectionActive = false;
                    ButtonCOM.Text = "Connect";
                    ConnectButton.Enabled = true;
                }
                ConnectButton.Enabled = true;
                //MessageBox.Show("The COM test reported an error. This can be caused by: \n\n 1 - The COM port selected is not correct. \n\n 2 - You selected the port, but the Arduino was disconnected before testing :) \n 3 - The COM is occupied (Serial monitor open?)", "COM port test: ERROR", MessageBoxButtons.OK);
                MessageBox.Show("Unable to set up the COM port.\n\n The problem has been tried to be fixed, so try again in some seconds.", "Unable to set up COM", MessageBoxButtons.OK);
            }
        }

        private void refreshCOM_Click(object sender, EventArgs e)
        {
            Session.SerialConnectionActive = false;
            COMportsList.SelectedItem = null;
            COMportsList.DataSource = SerialPort.GetPortNames();
        }

        private void COMstate_TextChanged(object sender, EventArgs e)
        {
            (sender as Label).Text = Session.SerialConnectionActive ? "ON" : " OFF";
            (sender as Label).ForeColor = Session.SerialConnectionActive ? Color.Green : Color.Red;
        }

        private void button17_Click(object sender, EventArgs e)
        {

        }

        private void trafficLight_stop_Click(object sender, EventArgs e)
        {

        }

        private void DancingMoves_dance1_Click(object sender, EventArgs e)
        {
            if (!Session.HasStarted)
                return;

            SendCommand("01,00,01");
        }

        private void DancingMoves_dance2_Click(object sender, EventArgs e)
        {
            if (!Session.HasStarted)
                return;

            if (!ArduinoCOM.IsOpen) ArduinoCOM.Open();
            SendCommand("01,00,02");
        }

        private void DancingMoves_dance3_Click(object sender, EventArgs e)
        {
            if (!Session.HasStarted)
                return;

            if (!ArduinoCOM.IsOpen) ArduinoCOM.Open();
            SendCommand("01,00,03");
        }

        private void PauseSession_Click(object sender, EventArgs e)
        {
            if (!Session.HasStarted)
                return;

            if ((sender as Button).Text == "Pause")
            {
                Clock.Stop();
                isPaused = true;
                (sender as Button).Text = "Resume";
            }
            else
            {
                Clock.Start();
                isPaused = false;
                (sender as Button).Text = "Pause";
            }


        }

        private void stopMusic_Click(object sender, EventArgs e)
        {

        }



        private void button22_Click(object sender, EventArgs e)
        {
            GameMasterLaunchedRiddle(sender, e);
            if (!Session.HasStarted)
                return;

            trafficLight_dance.Enabled = false;
            trafficLight_stop.Enabled = false;
            groupBox10.BackColor = Color.LightGray;
        }


        private void button9_Click(object sender, EventArgs e)
        {

        }

        private void masterNodeHealth_TextChanged(object sender, EventArgs e)
        {
            int value;

            if (!int.TryParse((sender as Label).Text, out value))
                return;

            bool badConnection = value == 0 || value > 3000;
            bool weakConnection = value > 1500;
            bool goodConnection = !badConnection && !weakConnection;
            (sender as Label).ForeColor = badConnection ? Color.Red : (weakConnection ? Color.Yellow : Color.Green);
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label38_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {

            if (ArduinoCOM == null || !ArduinoCOM.IsOpen)
            {
                MessageBox.Show("There is no connection active with the electronics, so it does not make sense to reset it :).", "COM not set up", MessageBoxButtons.OK);
                return;
            }


            DialogResult result = MessageBox.Show("This will restart the network. The remotes can take up to 15 seconds to be back online. \nDo you want to proceed?", "Authorize Network Reset", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                Session.COMlog += DateTime.Now.TimeOfDay.ToString() + ": The Game Master requested an Electronics reset (Master restart)." + "\r\n";
                SendCommand("00,00,02");

            }
        }

        private void label13_Click(object sender, EventArgs e)
        {

        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void COMLog_TextChanged(object sender, EventArgs e)
        {
            COMLog.SelectionStart = COMLog.Text.Length;
            COMLog.ScrollToCaret();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (ArduinoCOM == null || !ArduinoCOM.IsOpen)
            {
                MessageBox.Show("There is no connection active with the electronics, so it does not make sense to reset it :).", "COM not set up", MessageBoxButtons.OK);
                return;
            }


            DialogResult result = MessageBox.Show("This will restart the Master node. Everything will restart. It will take around 20 seconds.\n\nPD: Everything will get back online automatically! No need to re-connect!. \n\nDo you want to proceed?", "Authorize Network Hard-Reset", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                Session.COMlog += DateTime.Now.TimeOfDay.ToString() + ": The Game Master requested an Electronics reset (Master restart)." + "\r\n";
                SendCommand("00,00,01");

            }

        }

        private void label42_Click(object sender, EventArgs e)
        {

        }

        private void label43_Click(object sender, EventArgs e)
        {

        }

        private void button29_Click(object sender, EventArgs e)
        {
            SendCommand("13,00,03");
        }

        private void Music_Atmo1_Click(object sender, EventArgs e)
        {
            PlaySong(Tracks.MUSIC_ATMO1, true);
        }

        private void Music_Stopptanz_Click(object sender, EventArgs e)
        {
            PlaySong(Tracks.MUSIC_STOPPTANZ, true);
        }

        private void Music_Atmo2_Click(object sender, EventArgs e)
        {
            PlaySong(Tracks.MUSIC_ATMO2, true);
        }

        private void Music_strip_Click(object sender, EventArgs e)
        {
            PlaySong(Tracks.MUSIC_SEPAREE, true);
        }

        private void Music_finale_Click(object sender, EventArgs e)
        {
            PlaySong(Tracks.MUSIC_FINALE, true);
        }

        private void MusicChangeViewer_Tick(object sender, EventArgs e)
        {
            progressBar_musicChange.PerformStep();
            //label_nextSong_delay.Text =(((double)progressBar_musicChange.Value / (double)progressBar_musicChange.Maximum) * (double)100).ToString() + '%';

            if (progressBar_musicChange.Value == progressBar_musicChange.Maximum)
            {
                MusicChangeViewer.Stop(); ;

            }
        }

        private void PlaySong(string song, bool forceChange)
        {
            if (waveOutDevice.PlaybackState == PlaybackState.Playing)
            {
                if (prevSong.Contains(song))
                {
                    //the song requested is already playing. If this change is not forced (i.e., clicked by GM), then ignore the change
                    //this ignore is done to avoid recovered nodes that might notify a change in state (like saying "i am initializing")
                    //but it was only the recovery process. This made that if the node was lost in the process, it would relaunch the song
                    if (!forceChange)
                    {
                        return;
                    }
                }


                if (checkBox_fadeEffect.Checked)
                {
                    fadeInOut.BeginFadeOut((double)((FadeEffect_delay.Value * 1000) / 2));

                    progressBar_musicChange.Maximum = (int)FadeEffect_delay.Value * 1000;
                    progressBar_musicChange.Step = progressBar_musicChange.Maximum / 10;
                    progressBar_musicChange.Value = 0;

                    MusicChangeViewer.Interval = progressBar_musicChange.Maximum / 10;
                    MusicChangeViewer.Start();
                }
                else
                {
                    Pause(false);
                }
                

                
            }
            nextSong = song;
            prevSong = audioFileReader.FileName.Split('\\')[audioFileReader.FileName.Split('\\').Length - 1].Split('.')[0];


            audioFileReader = new AudioFileReader(Path.Combine(MUSIC_ROOT, song));
            fadeInOut = new FadeInOutSampleProvider(audioFileReader);
            waveOutDevice.Init(fadeInOut);


            if (checkBox_fadeEffect.Checked)
            {
                fadeInOut.BeginFadeIn((double)((FadeEffect_delay.Value * 1000) / 2));
            }
            else
            {
                progressBar_musicChange.Value = progressBar_musicChange.Maximum;

            }
            waveOutDevice.Play();
        }

        private void playMusic_Click(object sender, EventArgs e)
        {
            Resume(checkBox_fadeEffect.Checked);
        }

        private async void Resume(bool FadeIn)
        {
            if(waveOutDevice.PlaybackState != PlaybackState.Playing && MusicDelayed_StartTime == DateTime.MinValue && checkBox_FadeAtStart.Checked && nextSong == Tracks.MUSIC_ATMO1)
            {
                double fadetime_sec = (double)(FadeAtStart_mins.Value * 60) + (double)(FadeAtStart_secs.Value);
                MusicDelayed_StartTime = DateTime.Now.AddSeconds(fadetime_sec);
            }
            else
            {
                if (FadeIn)
                {
                    double fadetime_sec = (double)FadeEffect_delay.Value;

                    fadeInOut.BeginFadeIn(fadetime_sec * 1000);
                    progressBar_musicChange.Maximum = (int)fadetime_sec * 1000;
                    progressBar_musicChange.Step = progressBar_musicChange.Maximum / 50;
                    progressBar_musicChange.Value = 0;

                    MusicChangeViewer.Interval = progressBar_musicChange.Maximum / 50;
                    MusicChangeViewer.Start();
                    await Task.Delay(250);
                    
                }
                waveOutDevice.Play();
            }
            
        }

        private async void Pause(bool FadeOut)
        {
            if (FadeOut)
            {
                fadeInOut.BeginFadeOut((double)FadeEffect_delay.Value * 1000);
                await Task.Delay(TimeSpan.FromMilliseconds((double)FadeEffect_delay.Value * 1000 + 1000));
            }
            waveOutDevice.Pause();
        }


        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void pauseMusic_Click(object sender, EventArgs e)
        {
            if (waveOutDevice.PlaybackState == PlaybackState.Playing)
            {
                Pause(checkBox_fadeEffect.Checked);
            }

        }
        private void button29_KeyUp(object sender, KeyEventArgs e)
        {
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void button_SendCustomPacket_Click(object sender, EventArgs e)
        {
            if (ArduinoCOM == null || !ArduinoCOM.IsOpen)
            {
                MessageBox.Show("The port is not open so the message was not sent. This feature is only for use by the technical staff; using it can make the network and nodes to crash.", "Port closed", MessageBoxButtons.OK);
                return;
            }

            bool correctFormat = Regex.IsMatch(CustomPacket.Text.Trim(), @"^[0-9][0-9],[0-9][0-9],[0-9][0-9]$");

            if (!correctFormat)
            {
                MessageBox.Show("The format is not correct. Check it and try again. \n\nFormat:\n\nAA,BB,CC\n\n- AA the node id\n- BB the solved state (00/01)\n- CC the riddle mode.", "Wrong format", MessageBoxButtons.OK);
                return;
            }
            SendCommand(CustomPacket.Text.Trim());
        }

        private void label26_Click(object sender, EventArgs e)
        {

        }

        private void FadeAtStart_secs_ValueChanged(object sender, EventArgs e)
        {
            if (FadeAtStart_secs.Value == 60)
            {
                if (FadeAtStart_mins.Value < FadeAtStart_mins.Maximum) 
                {
                    FadeAtStart_mins.Value = FadeAtStart_mins.Value + 1;

                    (sender as NumericUpDown).Value = 0;
                }
            }
            else if(FadeAtStart_secs.Value == -1)
            {
                if (FadeAtStart_mins.Value > FadeAtStart_mins.Minimum)
                {
                    FadeAtStart_mins.Value = FadeAtStart_mins.Value - 1;

                    (sender as NumericUpDown).Value = 59;
                }
            }

            FadeAtStart_mins.Value = FadeAtStart_mins.Value % FadeAtStart_mins.Maximum;
            FadeAtStart_secs.Value = FadeAtStart_secs.Value % (FadeAtStart_secs.Maximum-1);


            if (FadeAtStart_secs.Value < 0)
                FadeAtStart_secs.Value = 0;
        }

        private void groupBox6_Enter(object sender, EventArgs e)
        {

        }

        private void MainScreen_Load(object sender, EventArgs e)
        {

        }

        private void label48_Click(object sender, EventArgs e)
        {

        }

        private void label32_Click(object sender, EventArgs e)
        {

        }

        private void label24_Click(object sender, EventArgs e)
        {

        }
    }
}
