using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Criterion;
using RingClone.Portal.Helpers;
using RingClone.Portal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace RingClone.Portal.Api
{
    [Authorize]
    public class HistoryController : ApiController
    {
        [HttpGet]
        public HistoryModel Index(int pageSize = 30)
        {
            var model = HistoryHelper.GenerateHistory(User.Identity.RingCloneIdentity().RingCentralId, pageSize);
            return model;
        }
    }
}