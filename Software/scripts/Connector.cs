using Godot;
using System;
using System.Collections.Generic;
using System.IO.Ports;

// currently green traffic light is slow, original uses same code.... Why?
// ALways followed by a Node Resycn => Delay?


public partial class Connector : Node{
	//========================================
	//=====  GODOT INSPECTOR
	//========================================
	[Export] public Godot.Timer timer_ack;
	[Export] public Godot.Timer timer_sync_packages;


	//========================================
	//=====  CONSTANTS
	//========================================
	private const string TARGET_PORT_NAME = "COM5";
	private const int TARGET_PORT_BAUDRATE = 115200;

	private const int MAX_ACK_TRIES = 1000;

	private const float SYNC_PACK_DOWNTIME = 1f;
	private const float TELEFPHONE_RING_DURATION = 6f; // I think this does not work properly, but it is not used anyway
	private const float OPEN_SPARKASTEN_AFTER_STOPPTANZ_DELAY = 2.5f;
	private const float OPEN_DOOR_AFTER_GAME_FINISH_DELAY = 7f;
	
	public enum COMMANDS{
		SYSTEM_INITIALIZE,
		SYSTEM_SOFT_RESET,
		SYSTEM_HARD_RESET,
		DRINKS_SOLVE,
		DRINKS_STOP_OPENING,
		STOPPTANZ_FORCE_RESET,
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
		SEPAREE_RED,
		SEPAREE_GREEN,
		SEPAREE_BLUE,
		SEPAREE_WHITE,
		SEPAREE_LIGHTS_OFF,
		SEPAREE_FINAL_SEQUENCE,
		SEPAREE_OPEN,
		SEPAREE_STOP_OPENING,
	}

	private readonly Dictionary<COMMANDS,string> PACKS = new Dictionary<COMMANDS, string>
	{
		{COMMANDS.SYSTEM_INITIALIZE, 		"00,00,01"},
		{COMMANDS.SYSTEM_SOFT_RESET, 		"00,00,02"},
		{COMMANDS.SYSTEM_HARD_RESET, 		"00,00,01"},
		{COMMANDS.DRINKS_SOLVE, 			"13,01,00"},
		{COMMANDS.DRINKS_STOP_OPENING, 		"13,00,00"},
		{COMMANDS.STOPPTANZ_FORCE_RESET, 	"02,00,00"},
		{COMMANDS.STOPPTANZ_INITIALIZE, 	"02,00,03"},
		{COMMANDS.STOPPTANZ_DANCE, 			"02,00,01"},
		{COMMANDS.STOPPTANZ_STOP, 			"02,00,02"},
		{COMMANDS.STOPPTANZ_SOLVE, 			"02,01,00"},
		{COMMANDS.SPARKASTEN_OPEN, 			"03,00,01"},
		{COMMANDS.SPARKASTEN_STOP_OPENING, 	"03,00,00"},
		{COMMANDS.TELEFON_RING,				"21,00,03"},
		{COMMANDS.TELEPHONE_STOP_RINGING, 	"21,00,01"}, // does not stop the ringing...
		{COMMANDS.WASSERHAHN_ENABLE,		"05,00,01"},
		{COMMANDS.SEXDUNGEON_OPEN, 			"05,00,02"},
		{COMMANDS.SCHICHTPLAN_OPEN, 		"05,00,09"},
		{COMMANDS.SCHICHTPLAN_STOP_OPENING,	"05,00,00"},
		{COMMANDS.SEPAREE_RED, 				"01,00,01"},
		{COMMANDS.SEPAREE_GREEN, 			"01,00,02"},
		{COMMANDS.SEPAREE_BLUE, 			"01,00,03"},
		{COMMANDS.SEPAREE_WHITE, 			"01,00,06"},
		{COMMANDS.SEPAREE_LIGHTS_OFF, 		"01,00,00"},
		{COMMANDS.SEPAREE_FINAL_SEQUENCE,	"01,01,09"},
		{COMMANDS.SEPAREE_OPEN, 			"01,00,07"},
	}; 

	
	private Dictionary<int,string> SYNC_PACKS = new Dictionary<int, string>
	{
		{0, "01,00,00"},
		{1, "02,00,00"},
		{2, "03,00,00"},
		// {3, "04,00,00"},
		{3, "05,00,00"},
		{4, "13,00,00"},
		{5, "21,00,00"},
		{6, "29,00,02"},	
	};


	private enum SyncState
	{
		NOT_STARTED,
		CURRENTLY_SYNCING,
		CURRENTLY_SYNCING_AGAIN,
		SYNC_SUCCESSFUL,
		SYNC_FAILED,
	}



	//========================================
	//=====  VARIABLES
	//========================================
	private SerialPort _arduinoMaster;
	private Logger _logger;
	private int color_index = -1;
	private int _stopptanz_ping = 0;
	private bool final_sequence_started = false;
	private bool _do_not_reset_separee_lights = false;
	private SyncState _syncState = SyncState.NOT_STARTED;

	private Node _eventBus;
	private Node _gameManager;


	//========================================
	//=====  PREPARATION
	//========================================
	// on program start call godots ready function automatically 
	public override void _Ready()
	{
		// create new Logger with new logfile
		CreateNewLogger();


		// access Godots autoloads
		_logger.Log("Accessing Autoloads", Logger.LogSeverity.VERBOSE);
		try
		{
			_eventBus = GetNode<Node>("/root/EventBus");
			_gameManager = GetNode<Node>("/root/GameManager");
			_gameManager.Set("connector", this);
		}
		catch(Exception e)
		{
			_logger.Log(e.Message, Logger.LogSeverity.ERROR);
		}

		// set initial riddle information to invalid values
		_eventBus.CallDeferred("update_riddles", 
			-1,-1,-1,
			-1,-1,-1,
			-1,-1,-1,
			-1,-1,-1,
			-1,-1,-1,
			-1,-1,-1,
			-1,-1,-1
		);
	}


	// create new Logger with new logfile, called at start of each new game
	public void CreateNewLogger()
	{
		_logger = new Logger();
	}

	public void ExportCurrentLog()
	{
		_logger.Export();
	}

	// initializes serial port 
	private bool OpenMasterPort()
	{
		_arduinoMaster = new SerialPort();
		_arduinoMaster.PortName = TARGET_PORT_NAME;
		_arduinoMaster.BaudRate = TARGET_PORT_BAUDRATE;
		
		_logger.Log("Open Port:" + _arduinoMaster.PortName + " - BaudRate:" + _arduinoMaster.BaudRate, Logger.LogSeverity.VERBOSE);
		try
		{
			_arduinoMaster.Open();
		}
		catch(Exception e)
		{
			_logger.Log(e.Message, Logger.LogSeverity.ERROR);
			return false;
		}

		return true;
	}


	// initialize serial port with arduino hardware
	private bool WaitForMasterACK()
	{
		_logger.Log("Waiting for Master ACK", Logger.LogSeverity.VERBOSE);
		bool _ackRecieved = false;

		try
		{
			SendCommand(COMMANDS.SYSTEM_INITIALIZE);

			_logger.Log("While Loop for ACK started", Logger.LogSeverity.VERBOSE);
			

			timer_ack.CallDeferred("start", 0.2);
			
			for(int i=0; i<MAX_ACK_TRIES; i++)
			{
				_logger.Log("Waiting for ACK ("+i.ToString()+")", Logger.LogSeverity.VERBOSE);

				if(timer_ack.TimeLeft<=0.0)
				{
					timer_ack.CallDeferred("start", 0.2);
				}
				while(timer_ack.TimeLeft>0){}

				if(_arduinoMaster.BytesToRead>0 && _arduinoMaster.ReadChar()==56)
				{
					_logger.Log("ACK Revieced!", Logger.LogSeverity.VERBOSE);
					_ackRecieved = true;
					_arduinoMaster.DataReceived+= ProcessRecievedData;
					break;
				}
			}
		}
		catch(Exception e)
		{
			_logger.Log(e.Message, Logger.LogSeverity.ERROR);
			return false;
		}

		if(!_ackRecieved)
		{
			_logger.Log("No ACK recieved!", Logger.LogSeverity.WARNING);
			return false;
		}

		return true;
	}

	
	//========================================
	//=====  COMMUNICATION PROTOCOL
	//========================================
	// sends pack to arduino master
	private void SendCommand(COMMANDS _command)
	{
		_logger.Log(" " + DateTime.Now.ToString("hh:mm:ss") + " -> Sending Command: " + _command, Logger.LogSeverity.VERBOSE);

		try
		{
			_arduinoMaster.WriteLine(">" + PACKS[_command]);
		}
		catch(Exception e)
		{
			_logger.Log(e.Message, Logger.LogSeverity.ERROR);
		}
	}


	private void ProcessRecievedData(object sender, SerialDataReceivedEventArgs args){
		try
		{
			if(_arduinoMaster.BytesToRead<28)
			{
				//_logger.Log("Pack Contains <28 Bytes", Logger.LogSeverity.WARNING); ignore spam
				return;
			}

			string data = _arduinoMaster.ReadLine();
			_logger.Log(" " + DateTime.Now.ToString("hh:mm:ss") + " -> Pack Recieved: " + data, Logger.LogSeverity.VERBOSE);

			string[] data_split = data.Split(',');
			processBuffer(data_split);
		}
		catch(Exception e)
		{
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

		uint _first_int_in_pack;
		if(!uint.TryParse(data[0].Substring(1), out _first_int_in_pack))
		{
			_logger.Log("Ignoring Pack (First Byte not Understood)", Logger.LogSeverity.WARNING);
			return;
		}

		
		if(_first_int_in_pack>=11 && _first_int_in_pack<=17)
		{
			_logger.Log("Laggy Connection with Node " + (_first_int_in_pack-10).ToString() + " (Auto-Restart is forced)", Logger.LogSeverity.WARNING);
			return;
		}
		else if(_first_int_in_pack>=70 && _first_int_in_pack<=120)
		{
			_logger.Log("Network Channel set to " + _first_int_in_pack.ToString(), Logger.LogSeverity.WARNING);
			_eventBus.CallDeferred("emit_sync_successful"); //this is called once every time after sync, so success
			return;
		}
		else if(_first_int_in_pack==1)
		{
			_logger.Log("Master Node Requested Sync", Logger.LogSeverity.VERBOSE);

			if(_syncState!=SyncState.NOT_STARTED)
			{
				_logger.Log("Already Syncing", Logger.LogSeverity.WARNING);
				return;
			}

			_syncState = SyncState.CURRENTLY_SYNCING;
			
			for(int i=0; i<7; i++)
			{
				if(timer_sync_packages.TimeLeft<=0.0){
					timer_sync_packages.CallDeferred("start", SYNC_PACK_DOWNTIME);
				}
				while(timer_sync_packages.TimeLeft>0){}
				_arduinoMaster.WriteLine(">" + SYNC_PACKS[i]);
			}
			
			return;
		}
		else if(_first_int_in_pack==2 || _first_int_in_pack==3)
		{
			_logger.Log("Master Node Netself-Rapair", Logger.LogSeverity.WARNING);
			return;
		}
		else if(_first_int_in_pack==4)
		{
			_logger.Log("Master Node Recieved a Network Reset Request", Logger.LogSeverity.WARNING);
			return;
		}
		else if(_first_int_in_pack==5)
		{
			_logger.Log("Master Node Resynced Node", Logger.LogSeverity.WARNING);
			return;  
		}
		else if(_first_int_in_pack==6)
		{ // this is not recieved after every successful sync time
			_logger.Log("Master Node Sync Successful", Logger.LogSeverity.VERBOSE);
			_eventBus.CallDeferred("emit_sync_successful");
			return;
		}
		else if(_first_int_in_pack==7)
		{
			_logger.Log("Master Node Sync Failed", Logger.LogSeverity.WARNING);

			if(_syncState==SyncState.NOT_STARTED)
			{
				return;
			}
			else if(_syncState==SyncState.CURRENTLY_SYNCING)
			{
				_logger.Log("Trying Syncing Again", Logger.LogSeverity.WARNING);
				_eventBus.CallDeferred("emit_sync_restart"); //restart Godot Overlay
			}
			else if(_syncState==SyncState.CURRENTLY_SYNCING_AGAIN || _syncState==SyncState.SYNC_FAILED)
			{
				_logger.Log("Resync Already Failed", Logger.LogSeverity.WARNING);
				_syncState = SyncState.SYNC_FAILED;
				return;
			}

			_syncState = SyncState.CURRENTLY_SYNCING_AGAIN;
			

			for(int i=0; i<7; i++)
			{
				if(timer_sync_packages.TimeLeft<=0.0)
				{
					timer_sync_packages.CallDeferred("start", SYNC_PACK_DOWNTIME);
				}
				while(timer_sync_packages.TimeLeft>0){}
				_arduinoMaster.WriteLine(">" + SYNC_PACKS[i]);
			}
			return;
		}
		else if(_first_int_in_pack==8)
		{
			_logger.Log("Master Node Recieved a Reset Request", Logger.LogSeverity.VERBOSE);
			return;
		}
		else if(_first_int_in_pack<=3000)
		{
			_logger.Log("Undefined Behaviour for " + _first_int_in_pack.ToString(), Logger.LogSeverity.WARNING);
			return;
		}
		else
		{
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
						if(newState==0) // separee lights went out, reset it to previous color
						{
							ResetPreviousColor();
						}
						break;
					case 1: //Stopptanz
						StopptanzPing = (int)delay;
						StopptanzSolved = newSolved ? 1 : 0;
						StopptanzState = newState;
						_stopptanz_ping = StopptanzPing;
						break;
					case 2: //Sparkasten
						SparkastenPing = (int)delay;
						SparkastenSolved = newSolved ? 1 : 0;
						SparkastenState = newState;
						if(newState>0)//sparkasten is open then reset so it can be opened again
						{ 
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
	

	//========================================
	//=====  UTILITY
	//========================================
	// reset separee lights to previous color if they turn off randomly
	private void ResetPreviousColor()
	{
		// if final sequence is ongoing or light has been turned off by default
		if(final_sequence_started || _do_not_reset_separee_lights)
		{
			return;
		}
		
		// turn on last saved color
		switch(color_index)
		{
			case -1:
				SendCommand(COMMANDS.SEPAREE_WHITE);
				break;
			case 0:
				SendCommand(COMMANDS.SEPAREE_RED);
				break;
			case 1:
				SendCommand(COMMANDS.SEPAREE_GREEN);
				break;
			case 2:
				SendCommand(COMMANDS.SEPAREE_BLUE);
				break;
			default:
				_logger.Log("Undefined Separee Color " + color_index, Logger.LogSeverity.WARNING);
				SendCommand(COMMANDS.SEPAREE_WHITE);
				break;
		}
	}



	//========================================
	//=====  BUTTONS
	//========================================
	// Drinks
	public async void DinksSolve()
	{
		SendCommand(COMMANDS.DRINKS_SOLVE);
    	await ToSignal(GetTree().CreateTimer(1f), "timeout");
		SendCommand(COMMANDS.STOPPTANZ_FORCE_RESET);
	}
	

	// Stopptanz
	public void StopptanzInit()
	{
		SendCommand(COMMANDS.STOPPTANZ_INITIALIZE);
	}
	public void StopptanzDance()
	{
		if(_stopptanz_ping==0)
		{
			return;
		}
		SendCommand(COMMANDS.STOPPTANZ_DANCE);
	}
	public void StopptanzStop()
	{
		if(_stopptanz_ping==0)
		{
			return;
		}
		SendCommand(COMMANDS.STOPPTANZ_STOP);
	}
	public async void StopptanzSolve()
	{
		SendCommand(COMMANDS.STOPPTANZ_SOLVE);
    	await ToSignal(GetTree().CreateTimer(OPEN_SPARKASTEN_AFTER_STOPPTANZ_DELAY), "timeout");
		SendCommand(COMMANDS.SPARKASTEN_OPEN);
	}


	// Sparkasten
	public void SparkastenOpen()
	{
		SendCommand(COMMANDS.SPARKASTEN_OPEN);
	}


	// Telefon
	public async void TelefonRing()
	{
		SendCommand(COMMANDS.TELEFON_RING);
		await ToSignal(GetTree().CreateTimer(TELEFPHONE_RING_DURATION), "timeout");
		SendCommand(COMMANDS.TELEPHONE_STOP_RINGING);
	}


	// Wasserhahn
	public void WasserhahnEnable()
	{
		SendCommand(COMMANDS.WASSERHAHN_ENABLE);
	}
	public void WasserhahnOpen()
	{
		SendCommand(COMMANDS.SEXDUNGEON_OPEN);
	}


	// Schichtplan
	public void SchichtplanOpen()
	{
		SendCommand(COMMANDS.SCHICHTPLAN_OPEN);
	}


	// Separee
	public void SepareeRed()
	{
		_do_not_reset_separee_lights = false;
		color_index = 0;
		SendCommand(COMMANDS.SEPAREE_RED);
	}
	public void SepareeGreen()
	{
		_do_not_reset_separee_lights = false;
		color_index = 1; 
		SendCommand(COMMANDS.SEPAREE_GREEN);
	}
	public void SepareeBlue()
	{
		_do_not_reset_separee_lights = false;
		color_index = 2;
		SendCommand(COMMANDS.SEPAREE_BLUE);
	}
	public void SepareeWhite()
	{
		_do_not_reset_separee_lights = false;
		color_index = -1;
		SendCommand(COMMANDS.SEPAREE_WHITE);
	}
	public void SepareeLightsOff()
	{
		_do_not_reset_separee_lights = true;
		SendCommand(COMMANDS.SEPAREE_LIGHTS_OFF);
	}
	public void SepareeOpen()
	{
		SendCommand(COMMANDS.SEPAREE_OPEN);
	}
	public async void SepareeSolve()
	{
		ResetPreviousColor();
		final_sequence_started = true;
		await ToSignal(GetTree().CreateTimer(OPEN_DOOR_AFTER_GAME_FINISH_DELAY), "timeout");
		SendCommand(COMMANDS.SEPAREE_FINAL_SEQUENCE);
		final_sequence_started = false;
	}


	// System
	public void SystemSoftReset()
	{
		_logger.Log("Soft Reset Requested!", Logger.LogSeverity.WARNING);
		SendCommand(COMMANDS.SYSTEM_SOFT_RESET);
	}
	public void SystemHardReset()
	{
		_logger.Log("Hard Reset Requested!", Logger.LogSeverity.WARNING);
		SendCommand(COMMANDS.SYSTEM_HARD_RESET);
	}




	//========================================
	//=====  UNUSED
	//========================================
	// display all port names to the GD console.
	public void SearchAllComs()
	{
		string[] ports = SerialPort.GetPortNames();
		_logger.Log("The following serial ports were found", Logger.LogSeverity.VERBOSE);
		foreach(string port in ports){
			_logger.Log(" >"+port, Logger.LogSeverity.VERBOSE);
		}
	}


	// close current port if open
	private void Disconnect()
	{
		_logger.Log("Closing Port", Logger.LogSeverity.VERBOSE);

		try
		{
			if(_arduinoMaster==null || !_arduinoMaster.IsOpen)
			{
				_logger.Log("Port is not Open", Logger.LogSeverity.WARNING);
				return;
			}

			_arduinoMaster.Close();
			_logger.Log("Port Closed", Logger.LogSeverity.VERBOSE);
		}
		catch(Exception e)
		{
			_logger.Log(e.Message, Logger.LogSeverity.ERROR);
		}
		return;
	}
}
