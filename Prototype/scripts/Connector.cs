using Godot;
using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Ports;
using System.Reflection;


// sopptanz green slow/fail
// no reset but if rätselstrom is out, test?
//help buttons same size would be nice
// and also include helper hlfe and helpfer feedback button lol
//debug just new fenster with full status


public partial class Connector : Node{
	//====================  GODOT INSPECTOR NODES
	[Export] public Godot.Timer timer_ack;
	[Export] public Godot.Timer timer_sync_packages;



	//====================  CONSTANTS
	private const float SYNC_PACK_DOWNTIME = 0.5f;
	private const float ACK_TIMEOUT_MAX = 20f;
	private const float OPEN_SPARKASTEN_AFTER_STOPPTANZ_DELAY = 2.5f;
	private const float OPEN_DOOR_AFTER_GAME_FINISH = 8f;
	
	public enum COMMANDS{
		SYSTEM_INITIALIZE,
		SYSTEM_SOFT_RESET,
		SYSTEM_HARD_RESET,
		DRINKS_SOLVE,
		DRINKS_STOP_OPENING,
		STOPPTANZ_INITIALIZE,
		STOPPTANZ_DANCE,
		STOPPTANZ_STOP,
		STOPPTANZ_SOLVE,
		SPARKASTEN_OPEN,		
		SPARKASTEN_STOP_OPENING,
		TELEFON_RING,
		TELEPHONE_STOP_RINGING,
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
	}

	private readonly Dictionary<COMMANDS,string> PACKS = new Dictionary<COMMANDS, string>{
		{COMMANDS.SYSTEM_INITIALIZE, 		"00,00,01"},
		{COMMANDS.SYSTEM_SOFT_RESET, 		"00,00,02"},
		{COMMANDS.SYSTEM_HARD_RESET, 		"00,00,01"},
		{COMMANDS.DRINKS_SOLVE, 			"13,01,00"},
		{COMMANDS.DRINKS_STOP_OPENING, 		"13,00,00"},
		{COMMANDS.STOPPTANZ_INITIALIZE, 	"02,00,03"},
		{COMMANDS.STOPPTANZ_DANCE, 			"02,00,01"},
		{COMMANDS.STOPPTANZ_STOP, 			"02,00,02"},
		{COMMANDS.STOPPTANZ_SOLVE, 			"02,01,00"},
		{COMMANDS.SPARKASTEN_OPEN, 			"03,00,01"},
		{COMMANDS.SPARKASTEN_STOP_OPENING, 	"03,00,00"},
		{COMMANDS.TELEFON_RING,				"21,00,03"}, //unused
		{COMMANDS.TELEPHONE_STOP_RINGING, 	"21,00,00"}, //unused
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



	//====================  VARIABLES
	private bool syncing = false;
	private SerialPort _arduinoMaster;
	private Node _eventBus;
	private Logger _logger;
	private int color_index = -1;
	private bool final_sequence_started = false;


	//====================  PREPARATION
	//>> on program start call godots ready function automatically 
	public override void _Ready(){
		_logger = new Logger();

		_logger.Log("Accessing Autoloads", Logger.LogSeverity.VERBOSE);
		try{
			_eventBus = GetNode<Node>("/root/EventBus");
		}
		catch(Exception e){
			_logger.Log(e.Message, Logger.LogSeverity.ERROR);
		}

		
		_eventBus.CallDeferred("update_riddles", 
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
		_logger.Log("The following serial ports were found", Logger.LogSeverity.VERBOSE);
		foreach(string port in ports){
			_logger.Log(" >"+port, Logger.LogSeverity.VERBOSE);
		}
	}

	//>> sends pack to arduino master
	private void SendCommand(COMMANDS _command){
		_logger.Log("Sending Command: " + _command, Logger.LogSeverity.VERBOSE);
		try{
			_arduinoMaster.WriteLine(">" + PACKS[_command]);
		}
		catch(Exception e){
			_logger.Log(e.Message, Logger.LogSeverity.ERROR);
		}
	}

	//>> initializes serial port 
	private bool InitializeArduinoMaster(){
		_arduinoMaster = new SerialPort();
		_arduinoMaster.PortName = "COM5";
		_arduinoMaster.BaudRate = 115200;
		
		_logger.Log("Open Port:" + _arduinoMaster.PortName + " - BaudRate:" + _arduinoMaster.BaudRate, Logger.LogSeverity.VERBOSE);
		try{
			_arduinoMaster.Open();
		}
		catch(Exception e){
			_logger.Log(e.Message, Logger.LogSeverity.ERROR);
			return false;
		}

		return true;
	}

	//>> initialize serial port with arduino hardware
	private bool SynchroniseHardware(){
		_logger.Log("Initializing System", Logger.LogSeverity.VERBOSE);
		try{
			SendCommand(COMMANDS.SYSTEM_INITIALIZE);

			_logger.Log("While Loop for ACK", Logger.LogSeverity.VERBOSE);
			bool _ackRecieved = false;
			timer_ack.Start(ACK_TIMEOUT_MAX);

			while(!_ackRecieved && timer_ack.TimeLeft>0){
				if(_arduinoMaster.BytesToRead>0 && _arduinoMaster.ReadChar()==56){
					_logger.Log("ACK Revieced!", Logger.LogSeverity.VERBOSE);
					_ackRecieved = true;
					_arduinoMaster.DataReceived+= ProcessRecievedData;
				}
			}
		}
		catch(Exception e){
			_logger.Log(e.Message, Logger.LogSeverity.ERROR);
			return false;
		}

		return true;
	}

	private void SendSyncPackage(int i){
		_logger.Log("Sending Command: " + SYNC_PACKS[i], Logger.LogSeverity.VERBOSE);
		_arduinoMaster.WriteLine(">" + SYNC_PACKS[i]);
		_eventBus.CallDeferred("emit_sync_package_send");
	}



	//====================  COMMUNICATION CONTROL
	public void Start(){
		_logger.Log("Sart Initialization", Logger.LogSeverity.VERBOSE);
		//>> open serial port to arduino master
		bool _initialisationSuccessfully = InitializeArduinoMaster();
		if(!_initialisationSuccessfully){
			return;
		}

		//>> synchronise with hardware 
		_logger.Log("Sart Synchronization", Logger.LogSeverity.VERBOSE);
		bool _synchroniseSuccesfully = SynchroniseHardware();
		if(!_synchroniseSuccesfully){
			return;
		}


		return;
	}

	
	private void Disconnect(){
		_logger.Log("Closing Port", Logger.LogSeverity.VERBOSE);
		try{
			if(_arduinoMaster==null || !_arduinoMaster.IsOpen){
				_logger.Log("Port is not Open", Logger.LogSeverity.WARNING);
				return;
			}	

			_arduinoMaster.Close();
			_logger.Log("Port Closed", Logger.LogSeverity.VERBOSE);
		}
		catch(Exception e){
			_logger.Log(e.Message, Logger.LogSeverity.ERROR);
		}
		return;
	}



	//====================  PROCESS INCOMING DATA
	private void ProcessRecievedData(object sender, SerialDataReceivedEventArgs args){
		try{
			if(_arduinoMaster.BytesToRead<28){
				//_logger.Log("Pack Contains <28 Bytes", Logger.LogSeverity.WARNING); ignore spam
				return;
			}

			string data = _arduinoMaster.ReadLine();
			_logger.Log("Pack Recieved: " + data, Logger.LogSeverity.VERBOSE);


			string[] data_split = data.Split(',');
			processBuffer(data_split);
		}
		catch(Exception e){
			_logger.Log(e.Message, Logger.LogSeverity.WARNING);
		}

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

		uint transformed_data_0;
		if(!uint.TryParse(data[0].Substring(1), out transformed_data_0)){
			_logger.Log("Ignoring Pack (First Byte not Understood)", Logger.LogSeverity.WARNING);
			return;
		}

		
		if(transformed_data_0>=11 && transformed_data_0<=17){
			_logger.Log("Laggy Connection with Node " + (transformed_data_0-10).ToString() + " (Auto-Restart is forced)", Logger.LogSeverity.WARNING);
			return;
		}
		else if(transformed_data_0>=70 && transformed_data_0<=120){
			_logger.Log("Network Channel set to " + transformed_data_0.ToString(), Logger.LogSeverity.WARNING);
			_eventBus.CallDeferred("emit_sync_successful"); //this will be called every time even if 6 is not send
			return;
		}
		else if(transformed_data_0==1){
			// Session.SerialConnectionInitialized = true;
			_logger.Log("Master Node Requested Sync", Logger.LogSeverity.VERBOSE);
			if(syncing){
				_logger.Log("Already Syncing", Logger.LogSeverity.WARNING);
				return;
			}
			syncing = true;
			
			for(int i=0; i<7; i++){
				if(timer_sync_packages.TimeLeft<=0.0){
					timer_sync_packages.CallDeferred("start", SYNC_PACK_DOWNTIME);
				}
				while(timer_sync_packages.TimeLeft>0){}
				SendSyncPackage(i);
			}
			
			return;
		}
		else if(transformed_data_0==2 || transformed_data_0==3){
			_logger.Log("Master Node Netself-Rapair", Logger.LogSeverity.WARNING);
			return;
		}
		else if(transformed_data_0==4){
			_logger.Log("Master Node Recieved a Network Reset Request", Logger.LogSeverity.WARNING);
			return;
		}
		else if(transformed_data_0==5){
			_logger.Log("Master Node Resynced Node", Logger.LogSeverity.WARNING);
			return;  
		}
		else if(transformed_data_0==6){
			_logger.Log("Master Node Sync Successful", Logger.LogSeverity.VERBOSE);
			_eventBus.CallDeferred("emit_sync_successful");
			return;
		}
		else if(transformed_data_0==7){
			_logger.Log("Master Node Sync Failed", Logger.LogSeverity.WARNING);
			_logger.Log("trying again", Logger.LogSeverity.WARNING);
			
			for(int i=0; i<7; i++){
				if(timer_sync_packages.TimeLeft<=0.0){
					timer_sync_packages.CallDeferred("start", SYNC_PACK_DOWNTIME);
				}
				while(timer_sync_packages.TimeLeft>0){}
				SendSyncPackage(i);
			}

			return;
		}
		else if(transformed_data_0==8){
			_logger.Log("Master Node Recieved a Reset Request", Logger.LogSeverity.VERBOSE);
			return;
		}
		else if(transformed_data_0<=3000){
			_logger.Log("Undefined Behaviour for " + transformed_data_0.ToString(), Logger.LogSeverity.WARNING);
			return;
		}
		else{
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
				switch(index){
					case 0: //Separee
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
					case 1: //Stopptanz
						StopptanzPing = (int)delay;
						StopptanzSolved = newSolved ? 1 : 0;
						StopptanzState = newState;
						break;
					case 2: //Sparkasten
						SparkastenPing = (int)delay;
						SparkastenSolved = newSolved ? 1 : 0;
						SparkastenState = newState;
						if(newState>0){ //sparkasten is open then reset so it can be opened again
							SendCommand(COMMANDS.SPARKASTEN_STOP_OPENING);
						}
						break;
					case 3: //Jukebox
						break;
					case 4: //Schichtplan
						SchichtplanPing = (int)delay;
						SchichtplanSolved = newSolved ? 1 : 0;
						SchichtplanState = newState;
						if(newState==9){
							SendCommand(COMMANDS.SCHICHTPLAN_STOP_OPENING);
						}
						break;
					case 5: //Drinks
						DrinksPing = (int)delay;
						DrinksSolved = newSolved ? 1 : 0;
						DrinksState = newState;
						if(newSolved){
							SendCommand(COMMANDS.DRINKS_STOP_OPENING);
						}
						break;
					case 6: //Telefon
						TelefonPing = (int)delay;
						TelefonSolved = newSolved ? 1 : 0;
						TelefonState = newState;
						break;
					default: //Undefined
						_logger.Log("Undefined Behaviour for " + index, Logger.LogSeverity.WARNING);
						break;
				}
			}

			//update riddles in Godot
			_eventBus.CallDeferred("update_riddles", 
				DrinksPing, DrinksSolved, DrinksState,
				StopptanzPing, StopptanzSolved, StopptanzState,
				SparkastenPing, SparkastenSolved, SparkastenState,
				TelefonPing, TelefonSolved, TelefonState,
				SexdungeonPing, SexdungeonSolved, SexdungeonState,
				SchichtplanPing, SchichtplanSolved, SchichtplanState,
				SepareePing, SepareeSolved, SepareeState
			);
		}
	}

	// if lights go out randomly, reset them to previous color
	private void ResetPreviousColor(){
		if(final_sequence_started)
			return;
		
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
				_logger.Log("Undefined Separee Color " + color_index, Logger.LogSeverity.WARNING);
				break;
				
			}
	}



	//====================  BUTTONS
	public void DinksSolve(){SendCommand(COMMANDS.DRINKS_SOLVE);}
	public void StopptanzInit(){SendCommand(COMMANDS.STOPPTANZ_INITIALIZE);}
	public void StopptanzDance(){SendCommand(COMMANDS.STOPPTANZ_DANCE);}
	public void StopptanzStop(){SendCommand(COMMANDS.STOPPTANZ_STOP);}

	public async void StopptanzSolve(){
		SendCommand(COMMANDS.STOPPTANZ_SOLVE);
    	await ToSignal(GetTree().CreateTimer(OPEN_SPARKASTEN_AFTER_STOPPTANZ_DELAY), "timeout");
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
		ResetPreviousColor();
		final_sequence_started = true;
		await ToSignal(GetTree().CreateTimer(OPEN_DOOR_AFTER_GAME_FINISH), "timeout");
		SendCommand(COMMANDS.SEPAREE_OPEN);
	}

	public void SystemSoftReset(){
		_logger.Log("Requst Soft Reset!", Logger.LogSeverity.WARNING);
		SendCommand(COMMANDS.SYSTEM_SOFT_RESET);
	}
	public void SystemHardReset(){
		_logger.Log("Requst Hard Reset!", Logger.LogSeverity.WARNING);
		SendCommand(COMMANDS.SYSTEM_HARD_RESET);
	}
}
