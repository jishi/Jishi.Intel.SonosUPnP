using System;

namespace Jishi.Intel.SonosUPnP
{
	public class PlayerInfo
	{
		public string TrackURI { get; set; }
		public uint TrackIndex { get; set; }
		public string TrackMetaData { get; set; }
		public TimeSpan RelTime { get; set; }
		public TimeSpan TrackDuration { get; set; }
	}
}