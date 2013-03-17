using System.Collections.Generic;
using System.Xml.Linq;

namespace Jishi.Intel.SonosUPnP
{
	public class SonosItem
	{
		public virtual SonosTrack Track { get; set; }
		public virtual SonosDIDL DIDL { get; set; }

		public static IList<SonosItem> Parse(string xmlString)
		{
			var xml = XElement.Parse(xmlString);
			XNamespace ns = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";
			XNamespace dc = "http://purl.org/dc/elements/1.1/";
			XNamespace upnp = "urn:schemas-upnp-org:metadata-1-0/upnp/";
			XNamespace r = "urn:schemas-rinconnetworks-com:metadata-1-0/";

			var items = xml.Elements(ns + "item");

			var list = new List<SonosItem>();

			foreach (var item in items)
			{
				var track = new SonosTrack();
				track.Uri = (string) item.Element(ns + "res");
				track.MetaData = (string) item.Element(r + "resMD");


				// fix didl if exist
				var didl = new SonosDIDL();
				didl.AlbumArtURI = (string) item.Element(upnp + "albumArtURI");
				didl.Artist = (string) item.Element(dc + "creator");
				didl.Title = (string) item.Element(dc + "title");
				didl.Description = (string) item.Element( r + "description" );

				list.Add(new SonosItem
					         {
						         Track = track,
						         DIDL = didl
					         });
			}

			return list;
		}
	}
}