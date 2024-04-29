using Godot;
using System;
using System.IO.Ports;


public partial class test : Node{
	
	private SerialPort _port;

	
	public override void _Ready(){
        GD.Print("Hello from C# to Godot :)");
		// Get a list of serial port names.
	}

	public void _on_button_down(){
        
        string[] ports = SerialPort.GetPortNames();
		GD.Print("The following serial ports were found:");
		// Display each port name to the console.
        foreach(string port in ports){
			GD.Print(port);
        }
		
        GD.Print();

		_port = new SerialPort();
		GD.Print("Currently using default:");
        GD.Print(_port.PortName);
	}
}
