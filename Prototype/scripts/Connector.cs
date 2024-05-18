using Godot;
using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Ports;
using System.Reflection;


public partial class Connector : Node{
	//==========  GODOT INSPECTOR NODES
	[Export] public Godot.Timer timer_ack;
	[Export] public Godot.Timer timer_sync_packages;



	//==========  CONSTANTS
	public float SYNC_PACK_DELAY = 0.5f;
   
	// store logfile as application data (Windows: %Appdata%/Roaming/Boogie-Bungalow)
	string PATH_LOGFILE = Path.Combine(
		System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), 
		"Boogie-Bungalow/logfile.txt"
	);
	
	public enum COMMANDS{
		SYSTEM_INITIALIZE,
		DRINKS_SOLVE,
		DRINKS_STOP_OPENING,
		STOPPTANZ_INITIALIZE,
		STOPPTANZ_DANCE,
		STOPPTANZ_STOP,
		STOPPTANZ_SOLVE,
		SPARKASTEN_OPEN,		
		SPARKASTEN_STOP_OPENING,
		WASSERHAHN_ENABLE,
		SEXDUNGEON_OPEN,
		SCHICHTPLAN_OPEN,
		SCHICHTPLAN_STOP_OPENING,
		SEPAREE_ROT,
		SEPAREE_GRUEN,
		SEPAREE_BLAU,
		SEPAREE_WHITE,
		SEPAREE_OPEN,
		SEPAREE_STOP_OPENING,
		TELEPHONE_STOP_RINGING,
	}

	private readonly Dictionary<COMMANDS,string> PACKS = new Dictionary<COMMANDS, string>{
		{COMMANDS.SYSTEM_INITIALIZE, 		"00,00,01"},
		{COMMANDS.DRINKS_SOLVE, 			"13,01,00"},
		{COMMANDS.DRINKS_STOP_OPENING, 		"13,00,00"},
		{COMMANDS.STOPPTANZ_INITIALIZE, 	"02,00,03"},
		{COMMANDS.STOPPTANZ_DANCE, 			"02,00,01"},
		{COMMANDS.STOPPTANZ_STOP, 			"02,00,02"},
		{COMMANDS.STOPPTANZ_SOLVE, 			"02,01,00"},
		{COMMANDS.SPARKASTEN_OPEN, 			"03,00,01"},
		{COMMANDS.SPARKASTEN_STOP_OPENING, 	"03,00,00"},
		{COMMANDS.WASSERHAHN_ENABLE,		"05,00,01"},
		{COMMANDS.SEXDUNGEON_OPEN, 			"05,00,02"},
		{COMMANDS.SCHICHTPLAN_OPEN, 		"05,00,09"},
		{COMMANDS.SCHICHTPLAN_STOP_OPENING,	"05,00,00"},
		{COMMANDS.SEPAREE_ROT, 				"01,00,01"},
		{COMMANDS.SEPAREE_GRUEN, 			"01,00,02"},
		{COMMANDS.SEPAREE_BLAU, 			"01,00,03"},
		{COMMANDS.SEPAREE_WHITE, 			"01,00,06"},
		{COMMANDS.SEPAREE_OPEN, 			"01,01,09"},
		{COMMANDS.SEPAREE_STOP_OPENING, 	"01,00,00"},
		{COMMANDS.TELEPHONE_STOP_RINGING, 	"21,00,00"},
	}; 

	
	private Dictionary<int,string> SYNC_PACKS = new Dictionary<int, string>{
		{0, "01,00,00"},
		{1, "02,00,00"},
		{2, "03,00,00"},
		{3, "04,00,00"},
		{4, "05,00,00"},
		{5, "13,00,00"},
		{6, "21,00,00"},
		{7, "29,00,02"},	
	};

	//==========  VARIABLES
	private bool syncing = false;
	private SerialPort _arduinoMaster;
	private Node _globals;


	//==========  PREPARATION
	//>> on program start call godots ready function automatically 
	public override void _Ready(){
		File.Delete(PATH_LOGFILE);
		Log("Hello from C# to Godot :)");
		Log("Also hello from PC!");
		Log("");

		try{
			_globals = GetNode<Node>("/root/Globals");
		}
		catch(Exception e){
			Log("exception occured: " + e.Message);
		}

		
		_globals.CallDeferred("update_riddles", 
			-1,1,-1,
			-1,-1,-1,
			-1,-1,-1,
			-1,-1,-1,
			-1,-1,-1,
			-1,-1,-1,
			-1,-1,-1
		);
	}

	//>> display all port names to the GD console.
	public void SearchAllComs(){
		string[] ports = SerialPort.GetPortNames();
		Log("== PORT LIST ==");
		Log("The following serial ports were found:");
		foreach(string port in ports){
			Log("  " + port);
		}
		Log("");
	}

	//>> prints to console and logs (multithreading not supported yet)
	private void Log(string message){
		GD.Print(message);
		File.AppendAllText(PATH_LOGFILE, message+"\n");
	}

	//>> sends pack to arduino master
	private void SendCommand(COMMANDS _command){
		Log(">>>> sending command: " + _command);
		try{
			_arduinoMaster.WriteLine(">" + PACKS[_command]);
		}
		catch(Exception e){
			Log("exception occured: " + e.Message);
		}
	}

	//>> initializes serial port 
	private bool InitializeArduinoMaster(){
		_arduinoMaster = new SerialPort();
		_arduinoMaster.PortName = "COM5";
		_arduinoMaster.BaudRate = 115200;
		
		Log("==  OPEN PORT  ==");
		Log("PortName:" + _arduinoMaster.PortName + " - BaudRate:" + _arduinoMaster.BaudRate);
		Log("trying to open port...");
		try{
			_arduinoMaster.Open();
			Log("targeted Port opened successfully!");
		}
		catch(Exception e){
			Log("exception occured: " + e.Message);
			return false;
		}

		Log("");
		return true;
	}

	//>> initialize serial port with arduino hardware
	private bool SynchroniseHardware(){
		Log("==  INITIIALIZE SYSTEM  ==");
		
		try{
			SendCommand(COMMANDS.SYSTEM_INITIALIZE);

			Log(">>>while loop for confirmation");
			bool _ackRecieved = false;
			timer_ack.Start();

			while(!_ackRecieved && timer_ack.TimeLeft>0){
				if(_arduinoMaster.BytesToRead>0 && _arduinoMaster.ReadChar()=='8'){
					Log("  recieved characater 56 (which equals '8')");
					Log("  ACK received from Master... Now Waiting for Master to ask for SYNC");
					Log("");
					_ackRecieved = true;
					_arduinoMaster.DataReceived+= ProcessRecievedData;
				}
				else{
					Log(".");
				}
			}
		}
		catch(Exception e){
			Log("exception occured: " + e.Message);
			return false;
		}

		return true;
	}

	private void SendSyncPackage(int i){
		Log(">>>> sending pack: " + SYNC_PACKS[i]);
		_arduinoMaster.WriteLine(">" + SYNC_PACKS[i]);
	}


	//==========  START THE CONTROLLER
	public void Start(){
		//>> open serial port to arduino master
		bool _initialisationSuccessfully = InitializeArduinoMaster();
		if(!_initialisationSuccessfully){
			return;
		}

		//>> synchronise with hardware 
		bool _synchroniseSuccesfully = SynchroniseHardware();
		if(!_synchroniseSuccesfully){
			return;
		}


		return;
	}


	//==========  PROCESS INCOMING DATA
	private void ProcessRecievedData(object sender, SerialDataReceivedEventArgs args){
		Log("==  PROCESSING INPUT  ==");
		Log("data recieved!");

		try{
			if(_arduinoMaster.BytesToRead<28){
				Log("but <28 bytes included :(");
				return;
			}

			string data = _arduinoMaster.ReadLine();
			Log(">>got package: \"" + data + "\"");


			string[] data_split = data.Split(',');
			processBuffer(data_split);
		}
		catch(Exception e){
			Log("exception occured: " + e.Message);
			Log("+1 currupt package");
		}

		Log("");
		return;
	}

	private void processBuffer(string[] data){
		//the information coming is a string with 29 numbers separated by coma, being:
		// [0] millis of master node
		// then, for each riddle (order 1,2,3,-,5,13,21)... So, 7 times:
		// [1] id
		// [2] millis
		// [3] solved
		// [4] state

		Log("  processing package:");
		uint transformed_data_0;
		if(!uint.TryParse(data[0].Substring(1), out transformed_data_0)){
			Log("  first bit was not understood, returning: " + transformed_data_0);
			return;
		}
		Log("  successfully found substring " + transformed_data_0 + " in "  + data[0]);

		
		if(transformed_data_0>=11 && transformed_data_0<=17){
			Log("  laggy connection with node " + (transformed_data_0-10).ToString() + " detected. It was forced to auto-restart");
			return;
		}
		else if(transformed_data_0>=70 && transformed_data_0<=120){
			Log("  master Node set the Network channel to " + transformed_data_0.ToString());
			//Session.NetChannel = (int)output;
			return;
		}
		else if(transformed_data_0==1){
			// Session.SerialConnectionInitialized = true;
			Log("  master node requested Sync.");
			if(syncing){
				Log("  already syncing.");
				return;
			}
			Log("  starting syncing.");
			syncing = true;
			
			for(int i=0; i<7; i++){
				Log(">>>SYNC " + i);
				if(timer_sync_packages.TimeLeft<=0.0){
					timer_sync_packages.CallDeferred("start", SYNC_PACK_DELAY);
				}
				Log("  SYNC waiting started (1 second)");
				while(timer_sync_packages.TimeLeft>0){}
				Log("  SEND SYNC " + i);
				SendSyncPackage(i);
			}
			
			return;
		}
		else if(transformed_data_0==2){
			// Session.SerialConnectionInitialized = true;
			Log("  master node reported netself-repairs.");
			return;
		}
		else if(transformed_data_0==3){
			Log("  master node reported netself-repairs (lag detected and cleaned).");
			return;
		}
		else if(transformed_data_0==4){
			Log("  master node reported recieved a network reset request.");
			return;
		}
		else if(transformed_data_0==5){
			Log("  master node reported node resync.");
			return;  
		}
		else if(transformed_data_0==6){
			Log("  master node reported that sync was successful.");
			return;
		}
		else if(transformed_data_0==7){
			Log("  master node reported that sync failed.");
			Log("  try again...");
			
			for(int i=0; i<7; i++){
				Log(">>>SYNC " + i);
				if(timer_sync_packages.TimeLeft<=0.0){
					timer_sync_packages.CallDeferred("start", SYNC_PACK_DELAY);
				}
				Log("  SYNC waiting started (1 second)");
				while(timer_sync_packages.TimeLeft>0){}
				Log("  SEND SYNC " + i);
				SendSyncPackage(i);
			}

			return;
		}
		else if(transformed_data_0==8){
			Log("  master node recieved a reset request.");
			return;
		}
		else{
			Log("  something else was requested: " + transformed_data_0);
		}

		if(transformed_data_0<=3000){
			Log("  idk what to do with >8 and <=3000");
			return;
		}
		
		Log("  riddle information! >3000");
		int index;

		int DrinksPing = -1;
		int DrinksSolved = -1;
		int DrinksState = -1;
		int StopptanzPing = -1;
		int StopptanzSolved = -1;
		int StopptanzState = -1;
		int SparkastenPing = -1;
		int SparkastenSolved = -1;
		int SparkastenState = -1;
		int TelefonPing = -1;
		int TelefonSolved = -1;
		int TelefonState = -1;
		int SexdungeonSolved = -1;
		int SexdungeonState = -1;
		int SexdungeonPing = -1;
		int SchichtplanPing = -1;
		int SchichtplanSolved = -1;
		int SchichtplanState = -1;
		int SepareePing = -1;
		int SepareeSolved = -1;
		int SepareeState = -1;

		for(int i=1; i<=4*7; i+=4){
			index = i/4; //0,1,2,3,4,5,6
			uint delay = uint.Parse(data[i + 1]);
			bool newSolved = (float.Parse(data[i + 2]) == 1) ? true : false;
			int newState = int.Parse(data[i + 3]);
			Log("    current index: " + index + "-> " + delay + " " + newSolved + " " + newState);
			switch(index){
				case 0:
					Log("    thats Separee!");
					SepareePing = (int)delay;
					SepareeSolved = newSolved ? 1 : 0;
					SepareeState = newState;
					if(color_index!=-1 && newState==0){
						ResetPreviousColor();
					}
					//if(newState==7){
					//	SendCommand(COMMANDS.SEPAREE_STOP_OPENING);
					//}
					break;
				case 1:
					Log("    thats Stoptanz!");
					StopptanzPing = (int)delay;
					StopptanzSolved = newSolved ? 1 : 0;
					StopptanzState = newState;
					break;
				case 2:
					Log("    thats Sparkasten!");
					SparkastenPing = (int)delay;
					SparkastenSolved = newSolved ? 1 : 0;
					SparkastenState = newState;
					if(newState==3){
						SendCommand(COMMANDS.SPARKASTEN_STOP_OPENING);
					}
					break;
				case 3:
					Log("    thats Jukebox!???");
					break;
				case 4:
					Log("    thats Arbeitsplan!");
					SchichtplanPing = (int)delay;
					SchichtplanSolved = newSolved ? 1 : 0;
					SchichtplanState = newState;
					if(newState==2){
						SendCommand(COMMANDS.SCHICHTPLAN_STOP_OPENING);
					}
					break;
				case 5:
					Log("    thats 4 Drinks!");
					DrinksPing = (int)delay;
					DrinksSolved = newSolved ? 1 : 0;
					DrinksState = newState;
					if(newSolved){
						SendCommand(COMMANDS.DRINKS_STOP_OPENING);
					}
					break;
				case 6:
					Log("    thats Telephone!");
					TelefonPing = (int)delay;
					TelefonSolved = newSolved ? 1 : 0;
					TelefonState = newState;
					if(newState>2){
						SendCommand(COMMANDS.TELEPHONE_STOP_RINGING);
					}
					break;
				case 7:
					Log("    thats Sexdungeon!");
					SexdungeonPing = (int)delay;
					SexdungeonSolved = newSolved ? 1 : 0;
					SexdungeonState = newState;
					break;
				default:
					Log("    thats undefined Behaviour!");
					break;
			}
		}

		_globals.CallDeferred("update_riddles", 
			DrinksPing, DrinksSolved, DrinksState,
			StopptanzPing, StopptanzSolved, StopptanzState,
			SparkastenPing, SparkastenSolved, SparkastenState,
			TelefonPing, TelefonSolved, TelefonState,
			SexdungeonPing, SexdungeonSolved, SexdungeonState,
			SchichtplanPing, SchichtplanSolved, SchichtplanState,
			SepareePing, SepareeSolved, SepareeState
		);
		Log("");
	}


	private void Disconnect(){
		Log("==  DISCONNECTING  ==");
		Log("closing coms");
		try{
			if(_arduinoMaster==null || !_arduinoMaster.IsOpen){
				Log("targeted port not open");
				return;
			}	

			_arduinoMaster.Close();
			Log("target port closed successfully");
		}
		catch(Exception e){
			Log("Expection " + e);
		}
		Log("");
		return;
	}


	private int color_index = -1;

	private void ResetPreviousColor(){
		switch(color_index){
			case 0:
				SendCommand(COMMANDS.SEPAREE_ROT);
				break;
			case 1:
				SendCommand(COMMANDS.SEPAREE_GRUEN);
				break;
			case 2:
				SendCommand(COMMANDS.SEPAREE_BLAU);
				break;
			default:
				Log("    thats also undefined Behaviour!");
				break;
				
			}
	}

	//==========  BUTTONS
	public void DinksSolve(){SendCommand(COMMANDS.DRINKS_SOLVE);}
	public void StopptanzInit(){SendCommand(COMMANDS.STOPPTANZ_INITIALIZE);}
	public void StopptanzDance(){SendCommand(COMMANDS.STOPPTANZ_DANCE);}
	public void StopptanzStop(){SendCommand(COMMANDS.STOPPTANZ_STOP);}

	public async void StopptanzSolve(){
		SendCommand(COMMANDS.STOPPTANZ_SOLVE);
    	await ToSignal(GetTree().CreateTimer(1), "timeout");
		SendCommand(COMMANDS.SPARKASTEN_OPEN);
	}

	public void SparkastenOpen(){SendCommand(COMMANDS.SPARKASTEN_OPEN);}
	public void WasserhahnEnable(){SendCommand(COMMANDS.WASSERHAHN_ENABLE);}
	public void WasserhahnOpen(){SendCommand(COMMANDS.SEXDUNGEON_OPEN);}
	public void SchichtplanOpen(){SendCommand(COMMANDS.SCHICHTPLAN_OPEN);}
	
	public void SepareeRot(){
		color_index = 0;
		SendCommand(COMMANDS.SEPAREE_ROT);
	}
	public void SepareeGruen(){
		color_index = 1; 
		SendCommand(COMMANDS.SEPAREE_GRUEN);
	}
	public void SepareeBlau(){
		color_index = 2;
		SendCommand(COMMANDS.SEPAREE_BLAU);
	}
	public void SepareeWhite(){SendCommand(COMMANDS.SEPAREE_WHITE);}
	public async void SepareeOpen(){
    	await ToSignal(GetTree().CreateTimer(10), "timeout");
		SendCommand(COMMANDS.SEPAREE_OPEN);
	}
}
