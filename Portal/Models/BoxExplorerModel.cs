using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
	public class BoxExplorerModel
	{
		public int BoxAccountId { get; set; }
		public string FolderId { get; set; }
		public string FolderName { get; set; }

		[JsonIgnore]
		public string AccessToken { get; set; }
		public List<BoxFolder> ChildFolders { get; set; }
		public List<BoxFile> ChildFiles { get; set; }
		public List<Breadcrumb> Breadcrumbs { get; set; }
		public class BoxFolder
		{
			public string FolderId { get; set; }
			public string FolderName { get; set; }
		}
		public class BoxFile
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