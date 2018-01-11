using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
	public class GoogleExplorerModel
	{
		public int GoogleAccountId { get; set; }
		public string FolderId { get; set; }
		public string FolderName { get; set; }

		[JsonIgnore]
		public string AccessToken { get; set; }
		public List<GoogleFolder> ChildFolders { get; set; }
		public List<GoogleFile> ChildFiles { get; set; }
		public List<Breadcrumb> Breadcrumbs { get; set; }
		public class GoogleFolder
		{
			public string FolderId { get; set; }
			public string FolderName { get; set; }
		}
		public class GoogleFile
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