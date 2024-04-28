using System;
using System.Collections.Generic;


namespace BuggieBungalow_Pult
{
	public class GameSession
	{
		public bool HasStarted { get; set; }
		public DateTime StartTime { get; set; }
		public List<Riddle> Riddles { get; set; }
        public DateTime EndTime { get; set; }
		public TimeSpan RunningTime { get; set; }
		public string NumberOfPlayers { get; set; }
		public bool SerialConnectionActive { get; set; }
		public bool SerialConnectionInitialized{ get; set; }




		//statistics for nerds
		public int PacketsSent { get; set; }
		public int TotalPackets { get; set; }
		public int CorruptPackets { get; set; }
		public int LastPacketLength { get; set; }
		public string LatestPacket { get; set; }
		public string COMlog { get; set; }

		public int NetChannel { get; set; }
		public int NetSelfRepairs { get; set; }
		public int NodeResyncs { get; set; }


		public uint[] Last2Millis { get; set; }
        public uint RTdelayCOMmaster
		{
            get
            {
                if (Last2Millis[0] != 0 && Last2Millis[1] != 0 && (Last2Millis[0]>Last2Millis[1]))
                    return Last2Millis[0] - Last2Millis[1];
                else
                    return 0;
            }
            set
            {
				Last2Millis[1] = Last2Millis[0];
				Last2Millis[0] = value;

            }
        }


		public string[] DecimalIDs { get; set; }
		public GameSession()
		{
            Last2Millis = new uint[2];
            HasStarted = false;
			Riddles = new List<Riddle>()
			{
				new Riddle("Separee", "01",0),
				new Riddle("Traffic Light", "02",0),
				new Riddle("Sparkastchen", "03",0),
				new Riddle("Jukebox", "04",0),
				new Riddle("WasserHahn / WorkSchedules", "05",0),
				new Riddle("4-Drinks", "015",0),
				new Riddle("Telephone", "025",0),
				new Riddle("Secret Corridor Light", "035",2), //init to pink
			};

			DecimalIDs = new string[8] { "01", "02", "03", "04", "05", "13", "21", "29" };

		}
	}
}
