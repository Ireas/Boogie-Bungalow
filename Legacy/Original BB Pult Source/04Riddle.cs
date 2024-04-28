Nameusing System;
using System.Linq;

namespace BuggieBungalow_Pult
{

	public class Riddle
	{
        public uint AVGdelayCOM { get; set; }
        public string Name { get; set; } 
        public bool Solved { get; set; }
        public string SolvedBy { get; set; }
        public TimeSpan SolvedAt { get; set; }

        public int state { get; set; }

        public string NetworkNodeName { get; set; }

        public Riddle(string Name, string RemoteNodeName, int state)
		{
            this.Name = Name;
            this.NetworkNodeName = RemoteNodeName;
            this.state = state;
		}
	}

}
