using Godot;
using System;
using System.IO.Ports;


public partial class Connector : Node{
	[Export]
	public Timer timer;
	private SerialPort _targetPort;
	
	
	public override void _Ready(){
        GD.Print("Hello from C# to Godot :)");
		// Get a list of serial port names.
	}

	// display all port names to the GD console.
	private void _on_search_coms_button_down(){
        string[] ports = SerialPort.GetPortNames();
		GD.Print("The following serial ports were found:");
        foreach(string port in ports){
			GD.Print(port);
        }
	}

	
	// try connect to the COM5.
	private void _on_connect_com_5_button_down(){
		_targetPort = new SerialPort();
		_targetPort.PortName = "COM5";
		_targetPort.BaudRate = 115200;

		GD.Print("created serialPort:");
        GD.Print("PortName:" + _targetPort.PortName + ", BaudRate:" + _targetPort.BaudRate);
		GD.Print("trying to open port...");
		try{
			_targetPort.Open();
			GD.Print("targeted Port opened successfully!");
		}
		catch(Exception e){
			GD.Print("exception occured: " + e.Message);
		}
	}

	private void _on_send_initial_command_button_down(){
		GD.Print("trying to send \"00,00,01\" command...");
		
		try{
        	_targetPort.WriteLine(">" + "00,00,01");
			GD.Print("command send successfully!");
			GD.Print("starting timer...");
			timer.Start();
		}
		catch(Exception e){
			GD.Print("exception occured: " + e.Message);
		}
	}

	private void _on_wait_for_ack_timeout(){
		if(_targetPort.BytesToRead==0){
			GD.Print("no bytes to read from targeted port!");
			return;
		}
		if(_targetPort.ReadChar()!='8'){
			GD.Print("unexpected char recieved!" + _targetPort.ReadChar());
			return;
		}

		GD.Print("expected ACK recieved!" + _targetPort.ReadChar());
		timer.Autostart = false;
		_targetPort.DataReceived += proccess_data_recieved;
	}

	private void proccess_data_recieved(object sender, SerialDataReceivedEventArgs args){
		GD.Print("data recieved!");
		GD.Print("trying to understand...");

		try{
			if(_targetPort.BytesToRead<28){
				GD.Print("no bytes to read from targeted port!");
				return;
			}
			if(_targetPort.BytesToRead<28){
				GD.Print("unexpected char recieved!" + _targetPort.ReadChar());
				return;
			}

			string data = _targetPort.ReadLine();
			GD.Print("package recieved:");
			string[] data_split = data.Split(',');
			foreach(string entry in data_split){
				GD.Print("> " + entry);
			}
			// Session.LatestPacket = data;
            // Session.LastPacketLength = data.Length;
            // Session.LatestPacket.Remove(Session.LastPacketLength - 1);

            	processBuffer(data.Split(','));
            }
		catch(Exception e){
			GD.Print("exception occured: " + e.Message);
			GD.Print("+1 currupt package");
		}
	}


	private void processBuffer(string[] data){
        //the information coming is a string with 29 numbers separated by coma, being:
        // [0] millis of master node

        // then, for each riddle (order 1,2,3,4,5,12,22)... So, 7 times:
        // [1] id
        // [2] millis
        // [3] solved
        // [4] state

		uint transformed_data_0 = 0;
		if(!uint.TryParse(data[0].Substring(1), out transformed_data_0)){
			GD.Print("first bit was not understood: " + transformed_data_0);
			return;
		}

		
		if(transformed_data_0>=11 && transformed_data_0<=17){
            GD.Print("laggy connection with node " + (transformed_data_0-10).ToString() + " detected. It was forced to auto-restart");
            return;
        }
        else if(transformed_data_0>=70 && transformed_data_0<=120){
			GD.Print("The Master Node set the Network channel to " + transformed_data_0.ToString());
			//Session.NetChannel = (int)output;
			return;
        }
        else if(transformed_data_0==1){
			// Session.SerialConnectionInitialized = true;
            GD.Print("The Master node requested Sync.");
            // StartInitialization();
            return;
        }
		else{
			GD.Print("something else was requested: " + transformed_data_0);
		}
	}

}
