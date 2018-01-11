using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
	public class RingCentralExplorerModel
	{
		public string FolderId { get; set; }
		public string FolderName { get; set; }

		[JsonIgnore]
		public string AccessToken { get; set; }
		public List<RingCentralFolder> ChildFolders { get; set; }
		public List<RingCentralFile> ChildFiles { get; set; }
		public List<Breadcrumb> Breadcrumbs { get; set; }
		public class RingCentralFolder
		{
			public string FolderId { get; set; }
			public string FolderName { get; set; }
		}
		public class RingCentralFile
		{
			public string FileId { get; set; }
			public string FileName { get; set; }
		}
		public class Breadcrumb
		{
			public string FolderId { get; set; }
			public string FolderName { get; set; }
		}
	}
}