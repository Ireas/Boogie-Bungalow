
using Godot;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;


// Logger writes messages to local file and Godots Console.  
public class Logger{
    //====================  CONSTANTS
    public enum LogSeverity{
        VERBOSE,
        WARNING,
        ERROR,
    }

    private int NUMBER_OF_LOGS_SAVED = 10; // number of previous sessions which are saved until override

    // store logfile in application data folder (Windows: %Appdata%/Roaming/Boogie-Bungalow)
	string PATH_LOGFILE = Path.Combine(
		System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), 
		"Boogie-Bungalow/log_"
	);

    string PATH_EXPORT = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);


    //====================  VARIABLES
    private bool busy;
    private Queue<(string,LogSeverity)> _buffer = new Queue<(string,LogSeverity)>(); 



    //====================  CONSTRUCTOR
    public Logger()
    {
        // rename every logfile and override the oldest one
        for(int i=NUMBER_OF_LOGS_SAVED-1; i>=0; i--)
        {
            try
            {
                File.Move(PATH_LOGFILE+i+".txt", PATH_LOGFILE+(i+1)+".txt", true);
            }
            catch(Exception _){} // ignore errors if file does not exist yet
        }

        // create new Logfile and add date and time to new files
        File.AppendAllText(PATH_LOGFILE+"0.txt", DateTime.Now.ToString("dd.MM.yyyy hh:mm:ss")+"\n\n");
    }



    //====================  LOGGING
    // log a message directly or store in temporary buffer to negate conflicts
    public void Log(string message, LogSeverity severity)
    {
        if(!busy)
        {
            WriteToFile( (message, severity) );
        }
        else
        {
            _buffer.Enqueue( (message, severity) );
        }
    }


    // format the string depending on severity to make debugging easier
    private string _formatString(string message, LogSeverity severity)
    {
        switch(severity)
        {
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
    private void WriteToFile((string,LogSeverity) input)
    {
        busy = true;

        // format message nicely
        string message = _formatString(input.Item1, input.Item2);
        
        // print to Godot console
        GD.Print(message);

        // write to local file
	    File.AppendAllText(PATH_LOGFILE+"0.txt", message +"\n");

        // search for next message in buffer
        if(_buffer.Any())
        {
            WriteToFile(_buffer.Dequeue());
        }
        else
        {
            busy = false;
        }
    }


    // export file to Desktop
    public void Export()
    {
        File.Copy(PATH_LOGFILE+"0.txt", PATH_EXPORT+"/log_"+DateTime.Now.ToString("dd.MM.yyyy.hh.mm")+".txt");
    }
}