using System.Collections.Generic;
using System.Xml.Linq;

namespace Jishi.Intel.SonosUPnP
{
	public class SonosDIDL
	{
		public string AlbumArtURI { get; set; }
		public string Title { get; set; }
		public string Artist { get; set; }
		public string Album { get; set; }
		public string Uri { get; set; }
		public string Description { get; set; }

		public static IList<SonosDIDL> Parse(string xml)
		{
			var didl = XElement.Parse(xml);
			return Parse(didl);
		}

		public static IList<SonosDIDL> Parse(XElement didl)
		{
			XNamespace ns = "urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/";
			XNamespace dc = "http://purl.org/dc/elements/1.1/";
			XNamespace upnp = "urn:schemas-upnp-org:metadata-1-0/upnp/";
			XNamespace r = "urn:schemas-rinconnetworks-com:metadata-1-0/";

			var items = didl.Elements(ns + "item");

			var list = new List<SonosDIDL>();

			foreach (var item in items)
			{
				var response = new SonosDIDL();
				response.AlbumArtURI = item.Element(upnp + "albumArtURI").Value;
				response.Artist = item.Element(dc + "creator").Value;
				response.Title = item.Element(dc + "title").Value;
				list.Add(response);
			}

			return list;
		}
	}
}