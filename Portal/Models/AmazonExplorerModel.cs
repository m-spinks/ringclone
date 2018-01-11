using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Models
{
	public class AmazonExplorerModel
	{
		public int AmazonAccountId { get; set; }
        public string Path { get; set; }
        public string FolderName { get; set; }
        public string OwnerName { get; set; }
        public string BucketName { get; set; }
        public string Key { get; set; }
        public bool CanUseFolder { get; set; }
        public string ErrorMessage { get; set; }

        public List<AmazonFolder> ChildFolders { get; set; }
        public List<AmazonFile> ChildFiles { get; set; }
        public List<Breadcrumb> Breadcrumbs { get; set; }
        public class AmazonFolder
        {
            public string FolderId { get; set; }
            public string FolderName { get; set; }
            public string FolderPath { get; set; }
            public string FolderType { get; set; }
            public string OwnerName { get; set; }
            public string BucketName { get; set; }
            public string Key { get; set; }
            public bool CanUseFolder { get; set; }
        }
        public class AmazonFile
		{
            public string FileId { get; set; }
            public string FileName { get; set; }
            public string FilePath { get; set; }
        }
        public class Breadcrumb
		{
			public string FolderId { get; set; }
			public string FolderName { get; set; }
            public string FolderPath { get; set; }
            public string FolderType { get; set; }
        }
    }
}