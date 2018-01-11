using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using RingClone.Portal.Models;
using NHibernate;
using RingClone.Portal.Helpers;
using NHibernate.Criterion;
using FluentNHibernate.Mapping;
using System.IO;
using System.Web.Script.Serialization;
using System.Collections.Specialized;
using System.Web;

namespace RingClone.Portal.Api
{
    public class RingCentralExplorerController : ApiController
    {
		[HttpGet]
		public RingCentralExplorerModel get(string folderId)
		{
			var model = new RingCentralExplorerModel();
			model.FolderId = folderId;
			model.ChildFolders = new List<RingCentralExplorerModel.RingCentralFolder>();
			model.ChildFiles = new List<RingCentralExplorerModel.RingCentralFile>();
			var t = new RingCentral.MessageStore(User.Identity.Name);
			foreach (var rec in t.data.records)
			{
				var newFile = new RingCentralExplorerModel.RingCentralFile()
				{
					FileId = rec.id,
					FileName = rec.creationTime
				};
				model.ChildFiles.Add(newFile);
			}
			return model;
		}
	}
}
