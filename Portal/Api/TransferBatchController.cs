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
    public class TransferBatchController : ApiController
    {
        [HttpGet]
        public TransferBatchStatusModel Status(int id)
        {
            var model = TransferBatchStatusHelper.GenerateStatus(User.Identity.RingCloneIdentity().RingCentralId, id);
            return model;
        }
    }
}