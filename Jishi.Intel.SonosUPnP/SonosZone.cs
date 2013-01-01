using System.Collections.Generic;
using System.Linq;

namespace Jishi.Intel.SonosUPnP
{
	public class SonosZone
	{
		private string CoordinatorUUID;
		private IList<SonosPlayer> players = new List<SonosPlayer>();

		public SonosZone(string coordinator)
		{
			CoordinatorUUID = coordinator;
		}

		public void AddPlayer(SonosPlayer player)
		{
			if (player.UUID == CoordinatorUUID)
			{
				Coordinator = player;
			}

			players.Add(player);
		}

		public SonosPlayer Coordinator { get; set; }

		public IList<SonosPlayer> Players
		{
			get { return players; }
			set { players = value; }
		}

		public string Name
		{
			get { return string.Join(" + ", players.Select(p => p.Name)); }
		}
	}
}