using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Box.Models
{
	public class BoxFolderModel
	{
		public string access_token { get; set; }
		public string refresh_token { get; set; }
		public string id { get; set; }
		public string name { get; set; }
		public string description { get; set; }
		public PathCollection path_collection { get; set; }
		public ItemCollection item_collection { get; set; }
		public class PathCollection
		{
			public int total_Count { get; set; }
			public List<Entry> entries { get; set; }

		}
		public class ItemCollection
		{
			public int total_Count { get; set; }
			public List<Entry> entries { get; set; }

		}
		public class Entry
		{
			public string type { get; set; }
			public string id { get; set; }
			public string sequence_id { get; set; }
			public string name { get; set; }
		}
	}
}
