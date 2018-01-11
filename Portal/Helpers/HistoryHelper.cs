using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Criterion;
using RingClone.Portal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Helpers
{
    public class HistoryHelper
    {

        public static HistoryModel GenerateHistory(string username, int pageSize = 50, int page = 1)
        {
			var model = new HistoryModel();
			model.TransferBatches = new List<HistoryModel.TransferBatch>();
            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    var accountCrit = session.CreateCriteria<AccountRec>();
                    accountCrit.Add(Expression.Eq("RingCentralId", username));
                    var accounts = accountCrit.List<AccountRec>();
                    if (accounts.Any())
                    {
                        var account = accounts.First();
                        var batchCrit = session.CreateCriteria<TransferBatch>();
                        batchCrit.Add(Expression.Eq("AccountId", account.AccountId));
						batchCrit.AddOrder(Order.Desc("TransferBatchId"));
                        var startIndex = (page - 1) * pageSize;
                        var batches = batchCrit.List<TransferBatch>();
                        model.TotalBatches = batches.Count();
                        batches = batches.Skip(startIndex).Take(pageSize).ToList();
						foreach (var batch in batches) 
						{
							var batchStatus = new HistoryModel.TransferBatch();
							model.TransferBatches.Add(batchStatus);
							batchStatus.TransferBatchId = batch.TransferBatchId;
                            batchStatus.LogNumber = batch.LogNumber.ToString("00");
                            batchStatus.CreateDate = batch.CreateDate;
                            batchStatus.TotalTickets = batch.Tickets.Count;
                            batchStatus.Title = "<span class='log-number'>" + batchStatus.LogNumber + "</span> <span class='create-date'>" + batchStatus.CreateDate.ToString("MM/dd/yyyy") + "</span> <span class='create-time'>" + batchStatus.CreateDate.ToString("hh:mmtt") + "</span><span class='total-tickets'>" + batchStatus.TotalTickets + (batchStatus.TotalTickets == 1 ? " file" : " files") + "</span>";
                            //batchStatus.StatusText = "Placed in queue on " + batch.CreateDate.ToString("MM/dd/yyyy at hh:mmtt");
							batchStatus.StatusText = "Placed in queue";
							batchStatus.StatusCss = "queued";
                            if (batch.ProcessingInd)
                            {
                                batchStatus.StartDate = batch.CreateDate;
                                batchStatus.StatusText = "Processing";
                                batchStatus.StatusCss = "processing";
                            }
                            else if (batch.CompleteDate.HasValue)
                            {
                                batchStatus.StartDate = batch.CreateDate;
                                batchStatus.StatusText = "Success";
                                batchStatus.StatusCss = "success";
                            }
                            else if (batch.ErrorInd)
                            {
                                batchStatus.StartDate = batch.CreateDate;
                                batchStatus.StatusText = "Error";
                                batchStatus.StatusCss = "error";
                            }
						}
                    }
                }
            }
            return model;
        }

        #region Database Models

        private class AccountRec
        {
            public virtual int AccountId { get; set; }
            public virtual string RingCentralId { get; set; }
        }
        private class AccountRecMap : ClassMap<AccountRec>
        {
            public AccountRecMap()
            {
                Table("T_ACCOUNT");
                Id(x => x.AccountId).Column("AccountId");
                Map(x => x.RingCentralId);
            }
        }
        private class TransferBatch
        {
            public virtual int TransferBatchId { get; set; }
            public virtual int TransferRuleId { get; set; }
            public virtual int AccountId { get; set; }
            public virtual bool QueuedInd { get; set; }
            public virtual bool ProcessingInd { get; set; }
            public virtual DateTime CreateDate { get; set; }
			public virtual int LogNumber { get; set; }
            public virtual bool ErrorInd { get; set; }
            public virtual DateTime? CompleteDate { get; set; }
            public virtual IList<Ticket> Tickets { get; set; }
        }
        private class TransferBatchMap : ClassMap<TransferBatch>
        {
            public TransferBatchMap()
            {
                Table("T_TRANSFERBATCH");
                Id(x => x.TransferBatchId).Column("TransferBatchId");
                Map(x => x.TransferRuleId);
                Map(x => x.AccountId);
                Map(x => x.QueuedInd);
                Map(x => x.ProcessingInd);
				Map(x => x.CreateDate);
                Map(x => x.LogNumber);
                Map(x => x.ErrorInd);
                Map(x => x.CompleteDate);
                HasMany(x => x.Tickets).KeyColumn("TransferBatchId").Inverse();
            }
        }

        private class Ticket
        {
            public virtual int TicketId { get; set; }
            public virtual int TransferBatchId { get; set; }
            public virtual TransferBatch TransferBatch { get; set; }
        }
        private class TicketMap : ClassMap<Ticket>
        {
            public TicketMap()
            {
                Table("T_TICKET");
                Id(x => x.TicketId).Column("TicketId");
                Map(x => x.TransferBatchId);
                References(x => x.TransferBatch).Column("TransferBatchId");
            }
        }
        #endregion

    }
}