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
    public class TransferBatchStatusHelper
    {

        public static TransferBatchStatusModel GenerateStatus(string username, int transferBatchId)
        {
            var model = new TransferBatchStatusModel();
            model.TransferBatchId = transferBatchId;
            model.Tickets = new List<TransferBatchStatusModel.Ticket>();
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
                        batchCrit.Add(Expression.Eq("TransferBatchId", transferBatchId));
                        var batch = batchCrit.UniqueResult<TransferBatch>();
						model.QueueMessage = "Placed in queue on " + batch.CreateDate.ToString("MM/dd/yyyy hh:mmtt");
						model.StatusText = "Queued";
						model.StatusCss = "queued";
						model.ProcessStartMessage = "...";
						model.ProcessStopMessage = "...";
                        if (batch != null && batch.Tickets != null)
                        {
							model.StatusIcon = "/images/batch-queued.svg";
                            if (batch.ProcessingInd)
                            {
                                model.ProcessStartMessage = "Process started on " + batch.CreateDate.ToString("MM/dd/yyyy hh:mmtt");
                                model.StatusText = "Processing";
                                model.StatusCss = "processing";
                                model.StatusIcon = "/images/spinner-x-large.gif";
                            }
                            else if (batch.CompleteDate.HasValue)
                            {
                                model.ProcessStartMessage = "Process started on " + batch.CreateDate.ToString("MM/dd/yyyy hh:mmtt");
                                model.StatusText = "Success";
                                model.StatusCss = "success";
                                model.StatusIcon = "/images/batch-success.svg";
                                model.ProcessStopMessage = "Process completed on " + batch.CompleteDate.Value.ToString("MM/dd/yyyy hh:mmtt");
                            }
                            else if (batch.QueuedInd)
                            {
                                model.ProcessStartMessage = "Process started on " + batch.CreateDate.ToString("MM/dd/yyyy hh:mmtt");
                            }
                            else if (batch.ErrorInd)
                            {
                                model.ProcessStartMessage = "Process started on " + batch.CreateDate.ToString("MM/dd/yyyy hh:mmtt");
                                model.StatusText = "Error";
                                model.StatusCss = "error";
                                model.StatusIcon = "/images/batch-error.svg";
                            }
                            foreach (var ticket in batch.Tickets)
                            {
                                var t = new TransferBatchStatusModel.Ticket();
                                model.Tickets.Add(t);
                                t.Title = ticket.SaveAsFileName;
								t.TicketId = ticket.TicketId;
								if (model.StatusText == "Queued")
								{
									t.StatusText = "Queued";
									t.StatusCss = "queued";
									t.StatusIcon = "/images/ticket-queued.svg";
								}
								else
								{
									t.StatusText = "Pending";
									t.StatusCss = "pending";
									t.StatusIcon = "/images/ticket-pending.svg";
								}
                                if (ticket.ProcessingInd)
                                {
                                    t.StatusText = "Processing";
                                    t.StatusCss = "processing";
                                    t.StatusIcon = "/images/spinner-small.gif";
                                }
                                else if (ticket.CompleteDate.HasValue)
                                {
                                    t.StatusText = "Success";
                                    t.StatusCss = "success";
                                    t.StatusIcon = "/images/ticket-success.svg";
                                }
                                else if (ticket.ErrorInd)
                                {
                                    t.StatusText = "Error";
                                    t.StatusCss = "error";
                                    t.StatusIcon = "/images/ticket-error.svg";
                                }
                            }
                        }
                    }
                }
            }
            return model;
        }

        #region "Database Models"

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
                Map(x => x.ErrorInd);
                Map(x => x.CompleteDate);
                HasMany(x => x.Tickets).KeyColumn("TransferBatchId").Inverse();
            }
        }
        private class Ticket
        {
            public virtual int TicketId { get; set; }
            public virtual int TransferBatchId { get; set; }
            public virtual DateTime CreateDate { get; set; }
			public virtual string InitiatedBy { get; set; }
			public virtual string Destination { get; set; }
			public virtual int DestinationBoxAccountId { get; set; }
			public virtual int DestinationGoogleAccountId { get; set; }
			public virtual int DestinationFtpAccountId { get; set; }
			public virtual string DestinationFolderId { get; set; }
			public virtual string DestinationFolderLabel { get; set; }
			public virtual string SaveAsFileName { get; set; }
            public virtual string CallId { get; set; }
            public virtual bool ProcessingInd { get; set; }
            public virtual bool ErrorInd { get; set; }
            public virtual DateTime? CompleteDate { get; set; }
            public virtual TransferBatch TransferBatch { get; set; }
        }
        private class TicketMap : ClassMap<Ticket>
        {
            public TicketMap()
            {
                Table("T_TICKET");
                Id(x => x.TicketId).Column("TicketId");
                Map(x => x.TransferBatchId);
                Map(x => x.CreateDate);
				Map(x => x.InitiatedBy);
				Map(x => x.Destination);
				Map(x => x.DestinationBoxAccountId);
				Map(x => x.DestinationGoogleAccountId);
				Map(x => x.DestinationFtpAccountId);
				Map(x => x.DestinationFolderId);
				Map(x => x.DestinationFolderLabel);
				Map(x => x.SaveAsFileName);
                Map(x => x.CallId);
                Map(x => x.ProcessingInd);
                Map(x => x.ErrorInd);
                Map(x => x.CompleteDate);
                References(x => x.TransferBatch).Column("TransferBatchId");
            }
        }

        #endregion

    }
}