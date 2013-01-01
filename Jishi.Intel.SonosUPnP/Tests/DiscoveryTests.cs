using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Machine.Specifications;


namespace Jishi.Intel.SonosUPnP.Tests
{
	public class DiscoveryBase
	{
		Establish context = () =>
			{
				d = new SonosDiscovery();
				d.StartScan();
				Thread.Sleep(5000);
			};


		protected static SonosDiscovery d;
	}

	public class simple_discovery : DiscoveryBase
	{
		Because of = () => { };

		It should_have_zones = () => { d.Zones.Count.ShouldBeGreaterThan(0); };
		It should_have_players = () => { d.Players.Count.ShouldBeGreaterThan(0); };
	}

	public class simple_play : DiscoveryBase
	{
		Because of = () =>
			{
				player = d.Players.First(p => p.Name == "Kitchen");
				uri = "x-sonos-spotify:" + Uri.EscapeDataString("spotify:track:5UnX1hCXQzmUzw2dn3L9nY") + "?sid=9&flags=0";
				var didl =
					string.Format(
						@"<DIDL-Lite xmlns:dc=""http://purl.org/dc/elements/1.1/"" xmlns:upnp=""urn:schemas-upnp-org:metadata-1-0/upnp/"" xmlns:r=""urn:schemas-rinconnetworks-com:metadata-1-0/"" xmlns=""urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/""><item id=""00030000{0}"" parentID=""-1"" restricted=""true""><dc:title>{1}</dc:title><upnp:class>object.item.audioItem.musicTrack</upnp:class><desc id=""cdudn"" nameSpace=""urn:schemas-rinconnetworks-com:metadata-1-0/"">SA_RINCON2311_jishi</desc></item></DIDL-Lite>",
						Uri.EscapeDataString("spotify:track:5UnX1hCXQzmUzw2dn3L9nY"),
						"Foo"
						);

				Console.WriteLine("playing on " + player.Name);
				player.SetAVTransportURI(new SonosTrack {Uri = uri, MetaData = didl});
				Thread.Sleep(1000);
				player.Play();
			};

		It should_be_playing = () => { player.CurrentStatus.ShouldEqual(PlayerStatus.Playing); };
		It should_be_correct_track = () => { player.CurrentTrack.Uri.ShouldEqual(uri); };
		It should_have_players = () => { d.Players.Count.ShouldBeGreaterThan(0); };
		private static SonosPlayer player;
		private static string uri;
	}

	public class simple_seek : DiscoveryBase
	{
		Because of = () =>
		{
			player = d.Players.First( p => p.Name == "Kitchen" );
			uri = "x-sonos-spotify:" + Uri.EscapeDataString( "spotify:track:5UnX1hCXQzmUzw2dn3L9nY" ) + "?sid=9&flags=0";
			var didl =
				string.Format(
					@"<DIDL-Lite xmlns:dc=""http://purl.org/dc/elements/1.1/"" xmlns:upnp=""urn:schemas-upnp-org:metadata-1-0/upnp/"" xmlns:r=""urn:schemas-rinconnetworks-com:metadata-1-0/"" xmlns=""urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/""><item id=""00030000{0}"" parentID=""-1"" restricted=""true""><dc:title>{1}</dc:title><upnp:class>object.item.audioItem.musicTrack</upnp:class><desc id=""cdudn"" nameSpace=""urn:schemas-rinconnetworks-com:metadata-1-0/"">SA_RINCON2311_jishi</desc></item></DIDL-Lite>",
					Uri.EscapeDataString( "spotify:track:5UnX1hCXQzmUzw2dn3L9nY" ),
					"Foo"
					);

			Console.WriteLine( "playing on " + player.Name );
			var trackPosition = player.Enqueue( new SonosTrack { Uri = uri, MetaData = didl } );
			player.Seek(trackPosition);

		};

		It should_be_playing = () => { player.CurrentStatus.ShouldEqual( PlayerStatus.Playing ); };
		It should_be_correct_track = () => { player.CurrentTrack.Uri.ShouldEqual( uri ); };
		It should_have_players = () => { d.Players.Count.ShouldBeGreaterThan( 0 ); };
		private static SonosPlayer player;
		private static string uri;
	}

	public class pause : DiscoveryBase
	{
		Because of = () =>
		{
			player = d.Players.First( p => p.Name == "Kitchen" );
			player.Pause();

		};

		It should_have_players = () => {  };
		private static SonosPlayer player;
		private static string uri;
	}
}