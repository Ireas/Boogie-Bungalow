using Godot;
using System;
using System.IO.Ports;


public partial class Connector : Node{
	[Export]
	public Timer timer;
	private SerialPort _targetPort;
	
	
	public override void _Ready(){
		GD.Print("Hello from C# to Godot :)");
		GD.Print("");
	}

	// display all port names to the GD console.
	private void _on_search_coms_button_down(){
		string[] ports = SerialPort.GetPortNames();
		GD.Print("== PORT LIST ==");
		GD.Print("The following serial ports were found:");
		foreach(string port in ports){
			GD.Print("  " + port);
		}
		GD.Print("");
	}

	

	// APROACH EVENT-DRIVEN
	// try connect to the COM5.
	private void _on_connect_com_5_button_down(){
		_targetPort = new SerialPort();
		_targetPort.PortName = "COM5";
		_targetPort.BaudRate = 115200;
		_targetPort.DataReceived += new SerialDataReceivedEventHandler(WaitForACK);

		GD.Print("== OPEN PORT ==");
		GD.Print("created serialPort:");
		GD.Print("PortName:" + _targetPort.PortName + ", BaudRate:" + _targetPort.BaudRate);
		GD.Print("Port.DateRecieved: WaitForACK");
		try{
			GD.Print("trying to open port...");
			_targetPort.Open();
			GD.Print("targeted Port opened successfully!");
			GD.Print("");

			GD.Print("trying to send initial \"00,00,01\" command...");
			_targetPort.WriteLine(">" + "00,00,01");
			GD.Print("command send successfully!");
			GD.Print("");
		}
		catch(Exception e){
			GD.Print("exception occured: " + e.Message);
		}
	}

	
	// APROACH PRIMITIVE WHILE
	private void _on_connect_com_5_while_button_down(){
		_targetPort = new SerialPort();
		_targetPort.PortName = "COM5";
		_targetPort.BaudRate = 115200;

		GD.Print("== OPEN PORT ==");
		GD.Print("created serialPort:");
		GD.Print("PortName:" + _targetPort.PortName + ", BaudRate:" + _targetPort.BaudRate);
		GD.Print("Port.DateRecieved:");
		try{
			GD.Print("trying to open port...");
			_targetPort.Open();
			GD.Print("targeted Port opened successfully!");
			GD.Print("");

			GD.Print("trying to send initial \"00,00,01\" command...");
			_targetPort.WriteLine(">" + "00,00,01");
			GD.Print("command send successfully!");
			GD.Print("");

			GD.Print(">>>while loop for ACK");
			bool ACKrecieved = false;
			timer.Start();
			while(!ACKrecieved && timer.TimeLeft>0){
				if(_targetPort.BytesToRead>0 && _targetPort.ReadChar()=='8'){
					GD.Print("  recieved the following '8': " + _targetPort.ReadChar() + "  (should be 56, right?)");
					GD.Print("  ACK received from Master... Now Waiting for Master to ask for SYNC");
					GD.Print("");
					ACKrecieved = true;
					_targetPort.DataReceived+= ProcessRecievedData;
				}
				else{
					GD.Print("  ACK not yet received");
				}
			}

			if(!ACKrecieved){
				GD.Print("ACK wait timeout");
				return;
			}

			
		}
		catch(Exception e){
			GD.Print("exception occured: " + e.Message);
		}
	}

	private void WaitForACK(object sender, SerialDataReceivedEventArgs args){
		GD.Print(">>>>ACK-TEST");
		if(_targetPort.BytesToRead==0){
			GD.Print("  no bytes to read from targeted port!");
			return;
		}

		int _readChar = _targetPort.ReadChar(); // Quesce la fuck? (int)'8'=56 => 56=='8' means
		if(_readChar!='8'){
			GD.Print("  unexpected char recieved: " + _readChar);
			return;
		}

		GD.Print("  expected ACK recieved:" + _readChar + "  (should be 56 because 56=='8')");
		GD.Print("");
		GD.Print("== PROCESS DATA ==");
		GD.Print("unsubscribe from WaitForACK");
		_targetPort.DataReceived-= WaitForACK;
		GD.Print("subscribe to ProcessRecievedData");
		_targetPort.DataReceived+= ProcessRecievedData;
		GD.Print("");
	}


	private void ProcessRecievedData(object sender, SerialDataReceivedEventArgs args){
		GD.Print(">>>>data recieved!");

		try{
			if(_targetPort.BytesToRead==0){
				GD.Print("  no bytes to read from targeted port!");
				return;
			}
			if(_targetPort.BytesToRead<28){
				GD.Print("  unexpected char recieved! " + _targetPort.ReadChar());
				return;
			}

			string data = _targetPort.ReadLine();
			GD.Print("  got package: \"" + data + "\"");
			string[] data_split = data.Split(',');
			foreach(string entry in data_split){
				GD.Print("    |" + entry);
			GD.Print("");
			}
			// Session.LatestPacket = data;
			// Session.LastPacketLength = data.Length;
			// Session.LatestPacket.Remove(Session.LastPacketLength - 1);

			processBuffer(data_split);
		}
		catch(Exception e){
			GD.Print("exception occured: " + e.Message);
			GD.Print("+1 currupt package");
		}
		GD.Print("");
	}


	private void processBuffer(string[] data){
		//the information coming is a string with 29 numbers separated by coma, being:
		// [0] millis of master node

		// then, for each riddle (order 1,2,3,4,5,12,22)... So, 7 times:
		// [1] id
		// [2] millis
		// [3] solved
		// [4] state

		GD.Print("  processing package:");
		uint transformed_data_0;
		if(!uint.TryParse(data[0].Substring(1), out transformed_data_0)){
			GD.Print("  first bit was not understood: " + transformed_data_0);
			return;
		}
		GD.Print("  successfully found substring " + transformed_data_0 + " in "  + data[0]);

		
		if(transformed_data_0>=11 && transformed_data_0<=17){
			GD.Print("  laggy connection with node " + (transformed_data_0-10).ToString() + " detected. It was forced to auto-restart");
			return;
		}
		else if(transformed_data_0>=70 && transformed_data_0<=120){
			GD.Print("  master Node set the Network channel to " + transformed_data_0.ToString());
			//Session.NetChannel = (int)output;
			return;
		}
		else if(transformed_data_0==1){
			// Session.SerialConnectionInitialized = true;
			GD.Print("  master node requested Sync.");
			// StartInitialization();
			return;
		}
		else if(transformed_data_0==2){
			// Session.SerialConnectionInitialized = true;
			GD.Print("  master node reported netself-repairs.");
			// StartInitialization();
			return;
		}
		else if(transformed_data_0==3){
			GD.Print("  master node reported netself-repairs (lag detected and cleaned).");
			return;
		}
		else if(transformed_data_0==4){
			GD.Print("  master node reported recieved a network reset request.");
			return;
		}
		else if(transformed_data_0==5){
			GD.Print("  master node reported node resync.");
			return;  
		}
		else if(transformed_data_0==6){
			GD.Print("  master node reported that sync was successful. Current session recovered.");
			return;
		}
		else if(transformed_data_0==7){
			GD.Print("  master node reported that sync failed. new game state.");
			return;
		}
		else if(transformed_data_0==8){
			GD.Print("  master node recieved a reset request.");
			return;
		}
		else{
			GD.Print("  something else was requested: " + transformed_data_0);
		}

		if(transformed_data_0<=3000){
			GD.Print("  idk what to do with >8 and <=3000");
			return;
		}
		
		GD.Print("  riddle information! >3000");
		int index;
		for(int i=1; i<=4*7; i+=4){
			index = i/4; //0,1,2,3,4,5,6
			uint delay = uint.Parse(data[i + 1]);
			bool newSolved = (float.Parse(data[i + 2]) == 1) ? true : false;
			int newState = int.Parse(data[i + 3]);
			GD.Print("    current index: " + index + "-> " + delay + " " + newSolved + " " + newState);
			switch(index){
				case 0:
					GD.Print("    thats Separee!");
					break;
				case 1:
					GD.Print("    thats Stoptanz!");
					break;
				case 2:
					GD.Print("    thats Sparkaestchen!");
					break;
				case 3:
					GD.Print("    thats Jukebox!");
					break;
				case 4:
					GD.Print("    thats Wasserhahn!");
					break;
				case 5:
					GD.Print("    thats 4 Drinks!");
					break;
				case 6:
					GD.Print("    thats Telephone!");
					break;
				case 7:
					GD.Print("    thats Sexdungeon!");
					break;
				default:
					GD.Print("    thats undefined Behaviour!");
					break;
			}
		}
		GD.Print("");
	}


	private void _on_disconnect_com_button_down(){
		GD.Print("== CLOSE COM ==");
		if(_targetPort==null || !_targetPort.IsOpen){
			GD.Print("targeted port not open");
			return;
		}
		GD.Print("target port will now close...");
		_targetPort.Close();
		GD.Print("target port closed successfully");
		GD.Print("");
	}
}
