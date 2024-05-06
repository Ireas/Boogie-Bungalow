using Godot;
using System;
using System.IO;
using System.Collections.Generic;
using System.IO.Ports;


public partial class Connector : Node{
	[Export]
	public Timer timer;
	[Export]
	public Timer timer_sync_packages;
	private SerialPort _targetPort;
    
	string path_logfile = Path.Combine(
		System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), 
		"Boogie-Bungalow/logfile.txt"
	);
	
	
	public override void _Ready(){
		File.Delete(path_logfile);
		Log("Hello from C# to Godot :)");
		Log("Also hello from PC!");
		Log("");
	}

	// display all port names to the GD console.
	private void _on_search_coms_button_down(){
		string[] ports = SerialPort.GetPortNames();
		Log("== PORT LIST ==");
		Log("The following serial ports were found:");
		foreach(string port in ports){
			Log("  " + port);
		}
		Log("");
	}

	
	private void _on_connect_com_5_button_down(){
		_targetPort = new SerialPort();
		_targetPort.PortName = "COM5";
		_targetPort.BaudRate = 115200;

		Log("== OPEN PORT ==");
		Log("created serialPort:");
		Log("PortName:" + _targetPort.PortName + ", BaudRate:" + _targetPort.BaudRate);
		try{
			Log("trying to open port...");
			_targetPort.Open();
			Log("targeted Port opened successfully!");
			Log("");

			Log("trying to send initial \"00,00,01\" command...");
			_targetPort.WriteLine(">" + "00,00,01");
			Log("command send successfully!");
			Log("");

			Log(">>>while loop for ACK");
			bool ACKrecieved = false;
			timer.Start();
			while(!ACKrecieved && timer.TimeLeft>0){
				if(_targetPort.BytesToRead>0 && _targetPort.ReadChar()=='8'){
					Log("  recieved characater 56 (which equals '8')");
					Log("  ACK received from Master... Now Waiting for Master to ask for SYNC");
					Log("");
					ACKrecieved = true;
					_targetPort.DataReceived+= ProcessRecievedData;
				}
				else{
					Log("  ACK not yet received");
				}
			}

			if(!ACKrecieved){
				Log("ACK wait timeout");
				return;
			}
		}
		catch(Exception e){
			Log("exception occured: " + e.Message);
		}
	}

	bool deferred_activated = false;
	private void _on_connect_com_5_deferred_button_down(){
		deferred_activated  = true;
		_on_connect_com_5_button_down();
	}

	private void ProcessRecievedData(object sender, SerialDataReceivedEventArgs args){
		Log(">>>>data recieved!");

		try{
			if(_targetPort.BytesToRead==0){
				Log("  no bytes to read from targeted port!");
				return;
			}
			if(_targetPort.BytesToRead<28){
				Log("  unexpected char recieved! " + _targetPort.ReadChar());
				return;
			}

			string data = _targetPort.ReadLine();
			Log("  got package: \"" + data + "\"");
			string[] data_split = data.Split(',');
			foreach(string entry in data_split){
				Log("    |" + entry);
			}
			// Session.LatestPacket = data;
			// Session.LastPacketLength = data.Length;
			// Session.LatestPacket.Remove(Session.LastPacketLength - 1);

			processBuffer(data_split);
		}
		catch(Exception e){
			Log("exception occured: " + e.Message);
			Log("+1 currupt package");
		}
		Log("");
	}


	private void processBuffer(string[] data){
		//the information coming is a string with 29 numbers separated by coma, being:
		// [0] millis of master node

		// then, for each riddle (order 1,2,3,4,5,12,22)... So, 7 times:
		// [1] id
		// [2] millis
		// [3] solved
		// [4] state

		Log("  processing package:");
		uint transformed_data_0;
		if(!uint.TryParse(data[0].Substring(1), out transformed_data_0)){
			Log("  first bit was not understood: " + transformed_data_0);
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
			Log("  starting syncing.");
			
			for(int i=0; i<7; i++){
				Log(">>>SYNC " + i);
				
				Log("  CallDeferred? => " + deferred_activated);
				if(deferred_activated)
					timer_sync_packages.CallDeferred("start", 1); //expected godots pascal case in call deferred
				else
					timer_sync_packages.Start(1);

				Log("  SYNC waiting started");
				while(timer.TimeLeft>0){
					Log("    waiting..." + timer.TimeLeft);
				}
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
			Log("  master node reported that sync was successful. Current session recovered.");
			return;
		}
		else if(transformed_data_0==7){
			Log("  master node reported that sync failed. new game state.");
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
		for(int i=1; i<=4*7; i+=4){
			index = i/4; //0,1,2,3,4,5,6,7
			uint delay = uint.Parse(data[i + 1]);
			bool newSolved = (float.Parse(data[i + 2]) == 1) ? true : false;
			int newState = int.Parse(data[i + 3]);
			Log("    current index: " + index + "-> " + delay + " " + newSolved + " " + newState);
			switch(index){
				case 0:
					Log("    thats Separee!");
					break;
				case 1:
					Log("    thats Stoptanz!");
					break;
				case 2:
					Log("    thats Sparkaestchen!");
					break;
				case 3:
					Log("    thats Jukebox!");
					break;
				case 4:
					Log("    thats Wasserhahn!");
					break;
				case 5:
					Log("    thats 4 Drinks!");
					if(newState==1){
						Log("Sending: " + "13,00,00" + " (= make it stop open door)");
						_targetPort.WriteLine(">	" + "13,00,00"); //stop asking to open compartment. 0 is ignored to just use it as joker
					}
					break;
				case 6:
					Log("    thats Telephone!");
					break;
				case 7:
					Log("    thats Sexdungeon!");
					break;
				default:
					Log("    thats undefined Behaviour!");
					break;
			}
		}
		Log("");
	}


	private Dictionary<int,string> MESSAGE_TEMPLATES = new Dictionary<int, string>{
		{0,"01,00,00"},
		{1,"02,00,00"},
		{2,"03,00,00"},
		{3,"04,00,00"},
		{4,"05,00,00"},
		{5,"13,00,00"},
		{6,"21,00,00"},
		{7,"29,00,02"},	
	};

	private void SendSyncPackage(int i){
		string message = MESSAGE_TEMPLATES[i];
		Log("    Sending: " + message);
		_targetPort.WriteLine(">" + message);
	}


	private void _on_send_package_button_down(){
		Log("Sending: " + "13,01,00" + " (= 4Drinks solved)");
		_targetPort.WriteLine(">" + "13,01,00");
		return;
	}


	private void _on_disconnect_com_button_down(){
		Log("== CLOSE COM ==");
		if(_targetPort==null || !_targetPort.IsOpen){
			Log("targeted port not open");
			return;
		}
		Log("target port will now close...");
		_targetPort.Close();
		Log("target port closed successfully");
		Log("");
	}

	private void Log(string message){
		GD.Print(message);
		File.AppendAllText(path_logfile, message+"\n");
	}
}
