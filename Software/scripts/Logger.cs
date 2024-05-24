
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

    private int _amountLogHistorySets = 9; // Stores a set of three log files per session

    // store logfile as application data (Windows: %Appdata%/Roaming/Boogie-Bungalow)
	string PATH_LOGFILE_VERBOSE = Path.Combine(
		System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), 
		"Boogie-Bungalow/logfile_verbose_"
	);
    string PATH_LOGFILE_WARNING = Path.Combine(
		System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), 
		"Boogie-Bungalow/logfile_warning_"
	);
    string PATH_LOGFILE_ERROR = Path.Combine(
		System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), 
		"Boogie-Bungalow/logfile_error_"
	);



    //====================  VARIABLES
    private bool busy;
    private Queue<(string,LogSeverity)> _buffer = new Queue<(string,LogSeverity)>(); 



    //====================  CONSTRUCTOR
    public Logger(){
        //rename every logfile and override the oldest one
        for(int i=_amountLogHistorySets-1; i>=0; i--){
            try{
                File.Move(PATH_LOGFILE_VERBOSE + i + ".txt", PATH_LOGFILE_VERBOSE + (i+1) + ".txt", true);
                File.Move(PATH_LOGFILE_WARNING + i + ".txt", PATH_LOGFILE_WARNING + (i+1) + ".txt", true);
                File.Move(PATH_LOGFILE_ERROR + i + ".txt", PATH_LOGFILE_ERROR + (i+1) + ".txt", true);
            }
            catch(Exception _){} //ignore errors if file does not exist yet
        }

        // add date and time to all files
        File.AppendAllText(PATH_LOGFILE_VERBOSE+"0.txt", DateTime.Now.ToString()+"\n");
		File.AppendAllText(PATH_LOGFILE_WARNING+"0.txt", DateTime.Now.ToString()+"\n");
		File.AppendAllText(PATH_LOGFILE_ERROR+"0.txt", DateTime.Now.ToString()+"\n");
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
		        File.AppendAllText(PATH_LOGFILE_VERBOSE+"0.txt", message +"\n");
                break;
            case(LogSeverity.WARNING):
		        File.AppendAllText(PATH_LOGFILE_VERBOSE+"0.txt", message +"\n");
		        File.AppendAllText(PATH_LOGFILE_WARNING+"0.txt", message +"\n");
                break;
            case(LogSeverity.ERROR):
		        File.AppendAllText(PATH_LOGFILE_VERBOSE+"0.txt", message +"\n");
		        File.AppendAllText(PATH_LOGFILE_WARNING+"0.txt", message +"\n");
		        File.AppendAllText(PATH_LOGFILE_ERROR+"0.txt", message +"\n");
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