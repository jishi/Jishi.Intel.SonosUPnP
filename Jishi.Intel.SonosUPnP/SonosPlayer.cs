using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using OpenSource.UPnP;

namespace Jishi.Intel.SonosUPnP
{
	public class SonosPlayer
	{
		private UPnPDevice device;
		private UPnPDevice mediaRenderer;
		private UPnPService avTransport;
		private UPnPDevice mediaServer;
		private UPnPService renderingControl;
		private UPnPService contentDirectory;
		private PlayerState currentState = new PlayerState();
		private Timer positionTimer;

		public string Name { get; set; }
		public string UUID { get; set; }

		public event Action<SonosPlayer> StateChanged;

		private void SubscribeToEvents()
		{
			AVTransport.Subscribe( 600, ( service, subscribeok ) =>
				{
					if ( !subscribeok )
						return;

					var lastChangeStateVariable = service.GetStateVariableObject( "LastChange" );
					lastChangeStateVariable.OnModified += ChangeTriggered;
				} );
		}

		private void ChangeTriggered( UPnPStateVariable sender, object value )
		{
			Console.WriteLine( "LastChange from {0}", UUID );
			var newState = sender.Value;
			Console.WriteLine( newState );
			ParseChangeXML( (string) newState );
		}

		private void ParseChangeXML( string newState )
		{
			var xEvent = XElement.Parse( newState );
			XNamespace ns = "urn:schemas-upnp-org:metadata-1-0/AVT/";

			var instance = xEvent.Element( ns + "InstanceID" );

			// We can receive other types of change events here.
			if ( instance.Element( ns + "TransportState" ) == null )
			{
				return;
			}

			var preliminaryState = new PlayerState
				{
					TransportState = instance.Element( ns + "TransportState" ).Attribute( "val" ).Value,
					NumberOfTracks = instance.Element( ns + "NumberOfTracks" ).Attribute( "val" ).Value,
					CurrentTrack = instance.Element( ns + "CurrentTrack" ).Attribute( "val" ).Value,
					CurrentTrackDuration =
						ParseDuration( instance.Element( ns + "CurrentTrackDuration" ).Attribute( "val" ).Value ),
					CurrentTrackMetaData = instance.Element( ns + "CurrentTrackMetaData" ).Attribute( "val" ).Value
				};

			currentState = preliminaryState;

			// every time we have got a state change, do a PositionInfo
			try
			{
				var positionInfo = GetPositionInfo();
				CurrentState.RelTime = positionInfo.RelTime;
			}
			catch ( Exception )
			{
				// void
			}

			CurrentState.LastStateChange = DateTime.Now;

			if ( StateChanged != null )
				StateChanged.Invoke( this );
		}

		private TimeSpan ParseDuration( string value )
		{
			if ( string.IsNullOrEmpty( value ) )
				return TimeSpan.FromSeconds( 0 );
			return TimeSpan.Parse( value );
		}

		public UPnPSmartControlPoint ControlPoint { get; set; }

		public UPnPDevice Device { get; set; }

		public UPnPDevice MediaRenderer
		{
			get
			{
				if ( mediaRenderer != null )
					return mediaRenderer;
				if ( Device == null )
					return null;
				mediaRenderer =
					Device.EmbeddedDevices.FirstOrDefault( d => d.DeviceURN == "urn:schemas-upnp-org:device:MediaRenderer:1" );
				return mediaRenderer;
			}
		}

		public UPnPDevice MediaServer
		{
			get
			{
				if ( mediaServer != null )
					return mediaServer;
				if ( Device == null )
					return null;
				mediaServer =
					Device.EmbeddedDevices.FirstOrDefault( d => d.DeviceURN == "urn:schemas-upnp-org:device:MediaServer:1" );
				return mediaServer;
			}
		}

		public UPnPService RenderingControl
		{
			get
			{
				if ( renderingControl != null )
					return renderingControl;
				if ( MediaRenderer == null )
					return null;
				renderingControl = MediaRenderer.GetService( "urn:upnp-org:serviceId:RenderingControl" );
				return renderingControl;
			}
		}

		public UPnPService AVTransport
		{
			get
			{
				if ( avTransport != null )
					return avTransport;
				if ( MediaRenderer == null )
					return null;
				avTransport = MediaRenderer.GetService( "urn:upnp-org:serviceId:AVTransport" );
				return avTransport;
			}
		}

		public UPnPService ContentDirectory
		{
			get
			{
				if ( contentDirectory != null )
					return contentDirectory;
				if ( MediaRenderer == null )
					return null;
				contentDirectory = MediaRenderer.GetService( "urn:upnp-org:serviceId:ContentDirectory" );
				return contentDirectory;
			}
		}

		public PlayerState CurrentState
		{
			get { return currentState; }
		}

		public PlayerStatus CurrentStatus
		{
			get { return GetPlayerStatus(); }
		}

		public PlayerInfo PlayerInfo
		{
			get { return GetPositionInfo(); }
		}

		public MediaInfo MediaInfo
		{
			get { return GetMediaInfo(); }
		}

		public SonosTrack CurrentTrack
		{
			get { throw new NotImplementedException(); }
		}

		public Uri BaseUrl
		{
			get { return Device.BaseURL; }
		}

		public void SetDevice( UPnPDevice playerDevice )
		{
			Device = playerDevice;
			// Subscribe to LastChange event
			SubscribeToEvents();

			// Start a timer that polls for PositionInfo
			//StartPolling();
		}

		public void StartPolling()
		{
			if ( positionTimer != null )
			{
				return;
			}

			if ( CurrentStatus != PlayerStatus.Playing )
			{
				return;
			}

			positionTimer = new Timer( UpdateState, null, TimeSpan.FromSeconds( 0 ), TimeSpan.FromSeconds( 30 ) );
		}

		private void UpdateState( object state )
		{
			var positionInfo = GetPositionInfo();
			CurrentState.RelTime = positionInfo.RelTime;
			CurrentState.LastStateChange = DateTime.Now;

			if ( StateChanged != null )
				StateChanged.Invoke( this );
		}

		public PlayerStatus GetPlayerStatus()
		{
			if ( AVTransport == null )
			{
				return PlayerStatus.Stopped;
			}
			var arguments = new UPnPArgument[4];
			arguments[0] = new UPnPArgument( "InstanceID", 0u );
			arguments[1] = new UPnPArgument( "CurrentTransportState", "" );
			arguments[2] = new UPnPArgument( "CurrentTransportStatus", "" );
			arguments[3] = new UPnPArgument( "CurrentSpeed", "" );

			try
			{
				AVTransport.InvokeSync( "GetTransportInfo", arguments );
			}
			catch ( UPnPInvokeException ex )
			{
				return PlayerStatus.Stopped;
			}

			PlayerStatus status;

			switch ( (string) arguments[1].DataValue )
			{
				case "PLAYING":
					return PlayerStatus.Playing;
					break;
				case "PAUSED":
					return PlayerStatus.Paused;
					break;
				default:
					return PlayerStatus.Stopped;
			}
		}

		public MediaInfo GetMediaInfo()
		{
			var arguments = new UPnPArgument[10];
			arguments[0] = new UPnPArgument( "InstanceID", 0u );
			arguments[1] = new UPnPArgument( "NrTracks", 0u );
			arguments[2] = new UPnPArgument( "MediaDuration", null );
			arguments[3] = new UPnPArgument( "CurrentURI", null );
			arguments[4] = new UPnPArgument( "CurrentURIMetaData", null );
			arguments[5] = new UPnPArgument( "NextURI", null );
			arguments[6] = new UPnPArgument( "NextURIMetaData", null );
			arguments[7] = new UPnPArgument( "PlayMedium", null );
			arguments[8] = new UPnPArgument( "RecordMedium", null );
			arguments[9] = new UPnPArgument( "WriteStatus", null );
			AVTransport.InvokeSync( "GetMediaInfo", arguments );

			return new MediaInfo
				{
					NrOfTracks = (uint) arguments[1].DataValue
				};
		}

		public PlayerInfo GetPositionInfo()
		{
			var arguments = new UPnPArgument[9];
			arguments[0] = new UPnPArgument( "InstanceID", 0u );
			arguments[1] = new UPnPArgument( "Track", 0u );
			arguments[2] = new UPnPArgument( "TrackDuration", null );
			arguments[3] = new UPnPArgument( "TrackMetaData", null );
			arguments[4] = new UPnPArgument( "TrackURI", null );
			arguments[5] = new UPnPArgument( "RelTime", null );
			arguments[6] = new UPnPArgument( "AbsTime", null );
			arguments[7] = new UPnPArgument( "RelCount", 0 );
			arguments[8] = new UPnPArgument( "AbsCount", 0 );
			AVTransport.InvokeSync( "GetPositionInfo", arguments );

			TimeSpan trackDuration;
			TimeSpan relTime;

			TimeSpan.TryParse( (string) arguments[2].DataValue, out trackDuration );
			TimeSpan.TryParse( (string) arguments[5].DataValue, out relTime );
			return new PlayerInfo
				{
					TrackIndex = (uint) arguments[1].DataValue,
					TrackMetaData = (string) arguments[3].DataValue,
					TrackURI = (string) arguments[4].DataValue,
					TrackDuration = trackDuration,
					RelTime = relTime
				};
		}

		public void SetAVTransportURI( SonosTrack track )
		{
			var arguments = new UPnPArgument[3];
			arguments[0] = new UPnPArgument( "InstanceID", 0u );
			arguments[1] = new UPnPArgument( "CurrentURI", track.Uri );
			arguments[2] = new UPnPArgument( "CurrentURIMetaData", track.MetaData );
			AVTransport.InvokeAsync( "SetAVTransportURI", arguments );
		}

		public void Play()
		{
			var arguments = new UPnPArgument[2];
			arguments[0] = new UPnPArgument( "InstanceID", 0u );
			arguments[1] = new UPnPArgument( "Speed", "1" );
			AVTransport.InvokeAsync( "Play", arguments );
		}

		public uint Enqueue( SonosTrack track, bool asNext = false )
		{
			var arguments = new UPnPArgument[8];
			arguments[0] = new UPnPArgument( "InstanceID", 0u );
			arguments[1] = new UPnPArgument( "EnqueuedURI", track.Uri );
			arguments[2] = new UPnPArgument( "EnqueuedURIMetaData", track.MetaData );
			arguments[3] = new UPnPArgument( "DesiredFirstTrackNumberEnqueued", 0u );
			arguments[4] = new UPnPArgument( "EnqueueAsNext", asNext );
			arguments[5] = new UPnPArgument( "FirstTrackNumberEnqueued", null );
			arguments[6] = new UPnPArgument( "NumTracksAdded", null );
			arguments[7] = new UPnPArgument( "NewQueueLength", null );
			AVTransport.InvokeSync( "AddURIToQueue", arguments );

			return (uint) arguments[5].DataValue;
		}

		public void Seek( uint position )
		{
			var arguments = new UPnPArgument[3];
			arguments[0] = new UPnPArgument( "InstanceID", 0u );
			arguments[1] = new UPnPArgument( "Unit", "TRACK_NR" );
			arguments[2] = new UPnPArgument( "Target", position.ToString() );
			AVTransport.InvokeAsync( "Seek", arguments );
		}

		public void Pause()
		{
			var arguments = new UPnPArgument[1];
			arguments[0] = new UPnPArgument( "InstanceID", 0u );
			AVTransport.InvokeAsync( "Pause", arguments );
		}

		public IList<SonosDIDL> GetQueue()
		{
			var arguments = new UPnPArgument[10];
			arguments[0] = new UPnPArgument( "ObjectID", "Q:0" );
			arguments[1] = new UPnPArgument( "BrowseFlag", "1" );
			arguments[2] = new UPnPArgument( "Filter", "" );
			arguments[3] = new UPnPArgument( "StartingIndex", 0u );
			arguments[4] = new UPnPArgument( "RequestedCount", 0u );
			arguments[5] = new UPnPArgument( "SortCriteria", "" );
			arguments[6] = new UPnPArgument( "Result", "" );
			arguments[7] = new UPnPArgument( "NumberReturned", 0u );
			arguments[8] = new UPnPArgument( "TotalMatches", 0u );
			arguments[9] = new UPnPArgument( "UpdateID", 0u );

			ContentDirectory.InvokeSync( "Browse", arguments );

			var result = arguments[6].DataValue as string;

			var didls = SonosDIDL.Parse(result);

			return didls;
		}
	}

	public class PlayerState
	{
		public string TransportState { get; set; }
		public string NumberOfTracks { get; set; }
		public string CurrentTrack { get; set; }
		public TimeSpan CurrentTrackDuration { get; set; }
		public string CurrentTrackMetaData { get; set; }
		public DateTime LastStateChange { get; set; }
		public TimeSpan RelTime { get; set; }
	}
}