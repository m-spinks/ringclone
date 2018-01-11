using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using NHibernate;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Mapping;
using NHibernate.Criterion;

namespace TicketProcessor
{
	public class WorkerRole : RoleEntryPoint
	{
		private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);
		private static bool IsRunning;

		public override void Run()
		{
			Trace.TraceInformation("RingClone ticket processor is running");

			try
			{
				this.RunAsync(this.cancellationTokenSource.Token).Wait();
			}
			finally
			{
				this.runCompleteEvent.Set();
			}
		}

		public override bool OnStart()
		{
			// Set the maximum number of concurrent connections
			ServicePointManager.DefaultConnectionLimit = 12;

			// For information on handling configuration changes
			// see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

			bool result = base.OnStart();

			Trace.TraceInformation("RingClone ticket processor has been started");

			return result;
		}

		public override void OnStop()
		{
			Trace.TraceInformation("RingClone ticket processor is stopping");

			this.cancellationTokenSource.Cancel();
			this.runCompleteEvent.WaitOne();

			base.OnStop();

			Trace.TraceInformation("RingClone ticket processor has stopped");
		}

		private Task RunAsync(CancellationToken cancellationToken)
		{
			return Task.Factory.StartNew(() =>
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					if (!IsRunning)
					{
						IsRunning = true;
						Trace.TraceInformation("Working");
						var model = new TicketProcessorModel();
						getBatchesToRun(model);
						foreach (var batch in model.BatchesToRun)
						{
							runBatch(batch);
						}
						IsRunning = false;
					}
					Thread.Sleep(1000);
				}
			});
		}

		public class TicketProcessorModel
		{
			public DateTime Now;
			public IList<TransferBatch> BatchesToRun;
            public IList<TicketLog> TicketLogs;
            public IList<TransferBatchLog> BatchLogs;
		}

        private void getBatchesToRun(TicketProcessorModel model)
        {
			using (ISessionFactory sessionFactory = CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenStatelessSession())
				{
					var batchCrit = session.CreateCriteria<TransferBatch>();
					batchCrit.Add(Expression.Eq("QueuedInd", true));
					batchCrit.Add(Expression.Eq("ProcessingInd", false));
					batchCrit.Add(Expression.Eq("DeletedInd", false));
					model.BatchesToRun = batchCrit.List<TransferBatch>();
				}
			}
        }
		private void runBatch(TransferBatch batch)
		{
			TransferBatchLog batchLog;
			using (ISessionFactory sessionFactory = CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenStatelessSession())
				{
					using (var transaction = session.BeginTransaction())
					{
						batch.ProcessingInd = true;
						batch.QueuedInd = false;
						session.Update(batch);
						batchLog = new TransferBatchLog();
						batchLog.TransferBatchLogStartDate = DateTime.Now;
						batchLog.TransferBatchId = batch.TransferBatchId;
						batchLog.Message = "";
						session.Insert(batchLog);
						transaction.Commit();
					}
					var ticketCrit = session.CreateCriteria<Ticket>();
					ticketCrit.Add(Expression.Eq("TransferBatchId", batch.TransferBatchId));
					ticketCrit.Add(Expression.Eq("ProcessingInd", false));
					ticketCrit.Add(Expression.Eq("DeletedInd", false));
					batch.Tickets = ticketCrit.List<Ticket>();
				}
			}
			bool ticketErr = false;
			bool batchErr = false;
			foreach (var ticket in batch.Tickets)
			{
				doTransfer(batch, ticket, ref ticketErr);
				if (ticketErr)
					batchErr = true;
			}
			using (ISessionFactory sessionFactory = CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenStatelessSession())
				{
					using (var transaction = session.BeginTransaction())
					{
						batchLog.TransferBatchLogStopDate = DateTime.Now;
						batchLog.ErrorInd = batchErr;
						batch.ProcessingInd = false;
						session.Update(batch);
						session.Update(batchLog);
						transaction.Commit();
					}
				}
			}
		}
        private void doTransfer(TransferBatch batch, Ticket ticket, ref bool err)
        {
			err = true;
			var errMessage = "";
			TicketLog ticketLog;
			using (ISessionFactory sessionFactory = CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenStatelessSession())
				{
					using (var transaction = session.BeginTransaction())
					{
						ticket.ProcessingInd = true;
						ticketLog = new TicketLog();
						ticketLog.TicketId = ticket.TicketId;
						ticketLog.TicketLogStartDate = DateTime.Now;
						ticketLog.TransferBatchId = ticket.TransferBatchId;
						ticketLog.Message = "";
						ticketLog.LogText = "";
						session.Update(ticket);
						session.Insert(ticketLog);
						transaction.Commit();
					}
				}
			}
	
			// DO THE TRANSFER HERE
			AccountRec account = null;
			BoxAccountRec boxAccount = null;
			try
			{
				LogWriteLine(ticketLog, "Looking up your RingCentral credentials", LogTextStatus.None);
				using (ISessionFactory sessionFactory = CreateSessionFactory())
				{
					using (var session = sessionFactory.OpenSession())
					{
						var accountCrit = session.CreateCriteria<AccountRec>();
						accountCrit.Add(Expression.Eq("AccountId", batch.AccountId));
						var accounts = accountCrit.List<AccountRec>();
						if (accounts.Any())
						{
							account = accounts.First();
							LogWriteLine(ticketLog, "RingCentral credentials found", LogTextStatus.None);
						}
						else
						{
							LogWriteLine(ticketLog, "RingCentral credentials not found", LogTextStatus.Danger);
							errMessage = "RingCentral credentials not found";
						}
					}
				}
				if (account != null)
				{
					if (ticket.Destination == "box")
					{
						LogWriteLine(ticketLog, "Looking up your Box credentials", LogTextStatus.None);
						using (ISessionFactory sessionFactory = CreateSessionFactory())
						{
							using (var session = sessionFactory.OpenSession())
							{
								var boxAccountCrit = session.CreateCriteria<BoxAccountRec>();
								boxAccountCrit.Add(Expression.Eq("AccountId", account.AccountId));
								boxAccountCrit.Add(Expression.Eq("BoxAccountId", ticket.DestinationBoxAccountId));
								var boxAccounts = boxAccountCrit.List<BoxAccountRec>();
								if (boxAccounts.Any())
								{
									boxAccount = boxAccounts.First();
									LogWriteLine(ticketLog, "Box credentials found", LogTextStatus.None);
								}
								else
								{
									LogWriteLine(ticketLog, "Box credentials not found", LogTextStatus.Danger);
									errMessage = "Box credentials not found";
								}
							}
						}
					}

					if (boxAccount != null)
					{
						LogWriteLine(ticketLog, "Validating RingCentral credentials", LogTextStatus.None);
						var accountInfoGetter = new RingCentral.AccountInfo(account.RingCentralUsername);
						accountInfoGetter.Execute();
						if (accountInfoGetter.data != null && !string.IsNullOrWhiteSpace(accountInfoGetter.data.id))
						{
							LogWriteLine(ticketLog, "RingCentral credentials validated", LogTextStatus.None);
							LogWriteLine(ticketLog, "Validating Box credentials", LogTextStatus.None);
							var boxAccountInfoGetter = new Box.AuthChecker(account.RingCentralUsername, ticket.DestinationBoxAccountId);
							boxAccountInfoGetter.Execute();
							if (boxAccountInfoGetter.IsAuthenticated)
							{
								LogWriteLine(ticketLog, "Box credentials validated", LogTextStatus.None);
								LogWriteLine(ticketLog, "Retrieving recording from RingCentral", LogTextStatus.None);
								var callRecordingGetter = new RingCentral.MessageContent(account.RingCentralUsername, ticket.CallId);
								callRecordingGetter.Execute();
								if (callRecordingGetter.data != null && callRecordingGetter.data.Length > 0)
								{
									LogWriteLine(ticketLog, "Recording retrieved (" + callRecordingGetter.data.Length + " bytes found)", LogTextStatus.None);
									LogWriteLine(ticketLog, "Transferring recording to Box", LogTextStatus.None);
									var boxUploader = new Box.BoxUpload(account.RingCentralUsername, ticket.DestinationBoxAccountId);
									boxUploader.FolderId = ticket.DestinationFolderId;
									boxUploader.FileName = ticket.SaveAsFileName;
									boxUploader.FileData = callRecordingGetter.data;
									boxUploader.Execute();
									if (boxUploader.ResultException == null)
									{
										LogWriteLine(ticketLog, "Recording transferred to Box", LogTextStatus.Success);
										err = false;
									}
									else
									{
										LogWriteLine(ticketLog, "Unable to transfer recording to Box", LogTextStatus.Danger);
										errMessage = "Unable to transfer recording to Box - " + boxUploader.ResultException.Message;
										if (boxUploader.ResultException != null)
											errMessage = "Unable to transfer recording to Box - " + boxUploader.ResultException.Message;
										else
											errMessage = "Unable to transfer recording to Box";
									}
								}
								else
								{
									LogWriteLine(ticketLog, "Unable to retrieve call", LogTextStatus.Danger);
									if (callRecordingGetter.ResultException != null)
										errMessage = "Unable to retrieve call - " + callRecordingGetter.ResultException.Message;
									else
										errMessage = "Unable to retrieve call";
								}
							}
							else
							{
								LogWriteLine(ticketLog, "Box credentials failed validation", LogTextStatus.Danger);
								errMessage = "Box credentials failed validation";
							}
						}
					}
					else
					{
						LogWriteLine(ticketLog, "RingCentral credentials failed validation", LogTextStatus.Danger);
						errMessage = "RingCentral credentials failed validation";
					}
				}
			}
			catch (Exception ex)
			{
				errMessage = ex.Message;
			}
			Thread.Sleep(9000);

			using (ISessionFactory sessionFactory = CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenStatelessSession())
				{
					using (var transaction = session.BeginTransaction())
					{
						ticket.ProcessingInd = false;
						if (err)
						{
							ticketLog.ErrorInd = true;
							ticketLog.Message = errMessage;
						}
						ticketLog.TicketLogStopDate = DateTime.Now;
						session.Update(ticket);
						session.Update(ticketLog);
						transaction.Commit();
					}
				}
			}
		}

		private void LogWriteLine(TicketLog ticketLog, string text, LogTextStatus status)
		{
			using (ISessionFactory sessionFactory = CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenStatelessSession())
				{
					var classes = "line";
					if (status != LogTextStatus.None)
					{
						if (status == LogTextStatus.Danger)
							classes += " danger";
						else if (status == LogTextStatus.Info)
							classes += " info";
						else if (status == LogTextStatus.Success)
							classes += " success";
						else if (status == LogTextStatus.Warning)
							classes += " warning";
					}
					ticketLog.LogText += "<div class='" + classes + "'><span class='time'>" + DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss:ff tt") + "</span><span class='text'>" + text + "</span></div>";
					if (ticketLog.TicketLogId > 0)
					{
						using (var transaction = session.BeginTransaction())
						{
							session.Update(ticketLog);
							transaction.Commit();
						}
					}
				}
			}
		}

		public enum LogTextStatus
		{
			None,
			Danger,
			Warning,
			Info,
			Success
		}

		#region NHibernate
		public static ISessionFactory CreateSessionFactory()
		{
			string connString = ConnectionStringHelper.ConnectionString;
			return Fluently.Configure()
				.Database(MsSqlConfiguration.MsSql2008
					.ShowSql()
					.ConnectionString(connString)
				)
				.Cache(c => c
					.UseQueryCache())
				.Mappings(m => m.FluentMappings.AddFromAssemblyOf<WorkerRole>())
				.BuildSessionFactory();
		}
		#endregion

		#region Database Models


		public class AccountRec
		{
			public virtual int AccountId { get; set; }
			public virtual string RingCentralUsername { get; set; }
			public virtual string RingCentralExtension { get; set; }
			public virtual int RingCentralTokenId { get; set; }
		}
		private class AccountRecMap : ClassMap<AccountRec>
		{
			public AccountRecMap()
			{
				Table("T_ACCOUNT");
				Id(x => x.AccountId).Column("AccountId");
				Map(x => x.RingCentralUsername);
				Map(x => x.RingCentralTokenId);
			}
		}

		public class BoxAccountRec
		{
			public virtual int BoxAccountId { get; set; }
			public virtual int BoxTokenId { get; set; }
			public virtual int AccountId { get; set; }
			public virtual string BoxAccountName { get; set; }
			public virtual bool DeletedInd { get; set; }
			public virtual bool ActiveInd { get; set; }
		}
		private class BoxAccountRecMap : ClassMap<BoxAccountRec>
		{
			public BoxAccountRecMap()
			{
				Table("T_BOXACCOUNT");
				Id(x => x.BoxAccountId);
				Map(x => x.BoxTokenId);
				Map(x => x.BoxAccountName);
				Map(x => x.AccountId);
				Map(x => x.DeletedInd);
				Map(x => x.ActiveInd);
			}
		}
		
		public class TransferBatch
		{
			public virtual int TransferBatchId { get; set; }
			public virtual int TransferRuleId { get; set; }
			public virtual int AccountId { get; set; }
			public virtual bool QueuedInd { get; set; }
			public virtual bool ProcessingInd { get; set; }
			public virtual bool DeletedInd { get; set; }
			public virtual DateTime CreateDate { get; set; }
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
				Map(x => x.DeletedInd);
				Map(x => x.CreateDate);
			}
		}

		public class Ticket
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
            public virtual DateTime? CallTime { get; set; }
            public virtual string CallFromNumber { get; set; }
            public virtual string CallFromName { get; set; }
            public virtual string CallFromLocation { get; set; }
            public virtual string CallDirection { get; set; }
            public virtual string CallAction { get; set; }
            public virtual string CallResult { get; set; }
			public virtual bool ProcessingInd { get; set; }
			public virtual bool DeletedInd { get; set; }
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
                Map(x => x.CallTime);
                Map(x => x.CallFromNumber);
                Map(x => x.CallFromName);
                Map(x => x.CallFromLocation);
                Map(x => x.CallDirection);
                Map(x => x.CallAction);
                Map(x => x.CallResult);
				Map(x => x.ProcessingInd);
				Map(x => x.DeletedInd);
            }
        }

		public class TransferBatchLog
        {
            public virtual int TransferBatchLogId { get; set; }
            public virtual int TransferBatchId { get; set; }
			public virtual DateTime? TransferBatchLogStartDate { get; set; }
			public virtual DateTime? TransferBatchLogStopDate { get; set; }
			public virtual bool ErrorInd { get; set; }
            public virtual string Message { get; set; }
        }
        private class TransferBatchLogMap : ClassMap<TransferBatchLog>
        {
            public TransferBatchLogMap()
            {
                Table("T_TRANSFERBATCHLOG");
                Id(x => x.TransferBatchLogId).Column("TransferBatchLogId");
                Map(x => x.TransferBatchId);
				Map(x => x.TransferBatchLogStartDate);
				Map(x => x.TransferBatchLogStopDate);
				Map(x => x.ErrorInd);
                Map(x => x.Message);
            }
        }

		public class TicketLog
        {
            public virtual int TicketLogId { get; set; }
            public virtual int TicketId { get; set; }
            public virtual int TransferBatchId { get; set; }
			public virtual DateTime? TicketLogStartDate { get; set; }
			public virtual DateTime? TicketLogStopDate { get; set; }
			public virtual bool ErrorInd { get; set; }
			public virtual string Message { get; set; }
			public virtual string LogText { get; set; }
		}
        private class TicketLogMap : ClassMap<TicketLog>
        {
            public TicketLogMap()
            {
                Table("T_TICKETLOG");
                Id(x => x.TicketLogId).Column("TicketLogId");
                Map(x => x.TicketId);
                Map(x => x.TransferBatchId);
				Map(x => x.TicketLogStartDate);
				Map(x => x.TicketLogStopDate);
				Map(x => x.ErrorInd);
				Map(x => x.Message);
				Map(x => x.LogText);
			}
        }

		#endregion
		
	}
}
