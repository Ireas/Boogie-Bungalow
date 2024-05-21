
using Godot;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;


// Logger writes messages to local file and Godots Console if needed.  
public class Logger{
    //====================  CONSTANTS
    public enum LogSeverity{
        VERBOSE,
        WARNING,
        ERROR,
    }

    // store logfile as application data (Windows: %Appdata%/Roaming/Boogie-Bungalow)
	string PATH_LOGFILE_VERBOSE = Path.Combine(
		System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), 
		"Boogie-Bungalow/logfile_verbose.txt"
	);
    string PATH_LOGFILE_WARNING = Path.Combine(
		System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), 
		"Boogie-Bungalow/logfile_warning.txt"
	);
    string PATH_LOGFILE_ERROR = Path.Combine(
		System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), 
		"Boogie-Bungalow/logfile_error.txt"
	);



    //====================  VARIABLES
    private bool busy;
    private Queue<(string,LogSeverity)> _buffer = new Queue<(string,LogSeverity)>(); 



    //====================  CONSTRUCTOR
    public Logger(){
        //reset files by deleting them
		File.Delete(PATH_LOGFILE_VERBOSE);
		File.Delete(PATH_LOGFILE_WARNING);
		File.Delete(PATH_LOGFILE_ERROR);
    }



    //====================  LOGGING
    // Log a message directly or store in temporary buffer to negate conflicts
    public void Log(string message, LogSeverity severity){
        if(!busy){
            WriteToFile( (message,severity) );
        }
        else{
            _buffer.Enqueue( (message, severity) );
        }
    }

    // format the string depending on severity to make debugging easier
    private string _formatString(string message, LogSeverity severity){
        switch(severity){
            case(LogSeverity.VERBOSE):
		        message = "  >[V]: "+ message;
                break;
            case(LogSeverity.WARNING):
		        message = " >>[W]: "+ message;
                break;
            case(LogSeverity.ERROR):
		        message = ">>>[E]: "+ message;
                break;
            default:
                break;
        }
        
        return message;
    }

    // write formatted string to file and search for next entry
    private void WriteToFile((string,LogSeverity) input){
        busy = true;

        // format message nicely
        string message = _formatString(input.Item1, input.Item2);
        
        // print to Godot console
        GD.Print(message);

        // write to local file
        switch(input.Item2){
            case(LogSeverity.VERBOSE):
		        File.AppendAllText(PATH_LOGFILE_VERBOSE, message +"\n");
                break;
            case(LogSeverity.WARNING):
		        File.AppendAllText(PATH_LOGFILE_VERBOSE, message +"\n");
		        File.AppendAllText(PATH_LOGFILE_WARNING, message +"\n");
                break;
            case(LogSeverity.ERROR):
		        File.AppendAllText(PATH_LOGFILE_VERBOSE, message +"\n");
		        File.AppendAllText(PATH_LOGFILE_WARNING, message +"\n");
		        File.AppendAllText(PATH_LOGFILE_ERROR, message +"\n");
                break;
            default:
                break;
        }

        // search for next message in buffer
        if(_buffer.Any()){
            WriteToFile(_buffer.Dequeue());
        }
        else{
            busy = false;
        }
    }
}