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
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using FluentNHibernate.Mapping;
using NHibernate.Criterion;
using System.IO;
using System.Web.Script.Serialization;
using System.Web;

namespace TicketGenerator
{
	public class WorkerRole : RoleEntryPoint
	{
		private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		private readonly ManualResetEvent runCompleteEvent = new ManualResetEvent(false);

		public override void Run()
		{
			Trace.TraceInformation("TicketGenerator is running");

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

			Trace.TraceInformation("TicketGenerator has been started");

			return result;
		}

		public override void OnStop()
		{
			Trace.TraceInformation("TicketGenerator is stopping");

			this.cancellationTokenSource.Cancel();
			this.runCompleteEvent.WaitOne();

			base.OnStop();

			Trace.TraceInformation("TicketGenerator has stopped");
		}

		private Task RunAsync(CancellationToken cancellationToken)
		{
			return Task.Factory.StartNew(() =>
			{
				// TODO: Replace the following with your own logic.
				while (!cancellationToken.IsCancellationRequested)
				{
					//Trace.TraceInformation("Working");

					//var model = new TicketGeneratorModel();
					//model.Now = System.DateTime.Now;
					//getBatchesToAdd(model);
					//getTickets(model);
					//saveBatchesAndTickets(model);

					//Thread.Sleep(1000);
				}
			});
		}

		private void getTickets(TicketGeneratorModel model)
		{
			if (model == null || model.BatchesToRun == null || !model.BatchesToRun.Any())
			{
				foreach (var batch in model.BatchesToRun)
				{
					var calls = getCallsFromCallLog(batch);
					foreach (var call in calls)
					{
						var ticket = new TicketGeneratorModel.Ticket()
						{
							CreateDate = DateTime.Now,
							Destination = batch.Rule.Destination,
							DestinationBoxAccountId = batch.Rule.DestinationBoxAccountId,
							DestinationGoogleAccountId = batch.Rule.DestinationGoogleAccountId,
							DestinationFolderId = batch.Rule.DestinationFolderId,
							DestinationFolderLabel = batch.Rule.DestinationFolderLabel,
							PutInDatedSubfolder = batch.Rule.PutInDatedSubfolder,
							SaveAsFileName = call.FileName,
							CallAction = call.Action,
							CallDirection = call.Direction,
							CallFromLocation = call.FromLocation != null ? call.FromLocation : "",
							CallFromName = call.FromName,
							CallFromNumber = call.FromNumber,
							CallId = call.Id,
							CallResult = call.Result,
							CallTime = call.Time,
							InitiatedBy = "system",
							ContentUri = call.ContentUri
						};
					}

				}
			}
		}

		private void getBatchesToAdd(TicketGeneratorModel model)
		{

			if (model.BatchesToRun == null)
				model.BatchesToRun = new List<TicketGeneratorModel.TransferBatch>();

			using (ISessionFactory sessionFactory = CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenSession())
				{
					//GET ALL RULES THAT NEED TO BE RUN THIS HOUR
					var ruleCrit = session.CreateCriteria<TransferRuleRec>();
					var ruleDisjunction = new Disjunction();
					//ACTIVE AND NOT DELETED
					ruleCrit.Add(Expression.Eq("DeletedInd", false));
					ruleCrit.Add(Expression.Eq("ActiveInd", true));

					var rules = ruleCrit.List<TransferRuleRec>();

					if (rules.Any())
					{
						foreach (var ruleRec in rules)
						{
							bool isDayToRun = false;
							bool isTimeToRun = false;
							//CONVERT EACH RULE TO UTC, BASED ON THE CLIENT'S TIMEZONE
							string clientTimeZoneId = "Central Standard Time"; //TODO: OVERRIDE WITH CLIENT'S SELECTION
							DateTime clientNow = convertFromUtc(clientTimeZoneId, model.Now);
							var clientDaysInThisMonth = DateTime.DaysInMonth(clientNow.Year, clientNow.Month);
							//MONTHLY RULES
							if (ruleRec.Frequency == "First day of each month" && clientNow.Day == 1)
								isDayToRun = true;
							if (ruleRec.Frequency == "Last day of each month" && clientNow.Day == clientDaysInThisMonth)
								isDayToRun = true;
							if (ruleRec.Frequency == "Middle of each month (15th)" && clientNow.Day == 15)
								isDayToRun = true;
							//WEEKLY RULES
							var clientDayOfWeek = clientNow.ToString("dddd");
							if (ruleRec.Frequency == "Every " + clientDayOfWeek)
								isDayToRun = true;
							//DAILY RULE
							if (ruleRec.Frequency == "Every day")
								isDayToRun = true;
							//TIME OF DAY
							var clientTimeOfDayForRightNow = clientNow.ToString("HH00"); // "0000", "0300", "1600", etc;
							if (ruleRec.TimeOfDay == clientTimeOfDayForRightNow)
								isTimeToRun = true;

							if (isDayToRun && isTimeToRun)
							{
								//HAS THIS RULE BEEN RUN THIS HOUR?
								var batchCrit = session.CreateCriteria<TransferBatchRec>();
								batchCrit.Add(Expression.Eq("TransferRuleId", ruleRec.TransferRuleId));
								var startOfHour = new DateTime(model.Now.Year, model.Now.Month, model.Now.Day, model.Now.Hour, 0, 0, 0);
								var endOfHour = new DateTime(model.Now.Year, model.Now.Month, model.Now.Day, model.Now.Hour, 59, 59, 999);
								batchCrit.Add(Expression.Ge("CreateDate", startOfHour));
								batchCrit.Add(Expression.Le("CreateDate", endOfHour));
								var batches = batchCrit.List<TransferBatchRec>();
								//IF NOT, ADD IT TO THE LIST
								if (!batches.Any())
								{
									var rule = new TicketGeneratorModel.Rule();
									rule.AccountId = ruleRec.AccountId;
									rule.ActiveInd = ruleRec.ActiveInd;
									rule.DayOf = ruleRec.DayOf;
									rule.DeletedInd = ruleRec.DeletedInd;
									rule.Destination = ruleRec.Destination;
									rule.Frequency = ruleRec.Frequency;
									rule.Source = ruleRec.Source;
									rule.TimeOfDay = ruleRec.TimeOfDay;
									rule.TransferRuleId = ruleRec.TransferRuleId;
									var newBatch = new TicketGeneratorModel.TransferBatch();
									newBatch.AccountId = ruleRec.AccountId;
									newBatch.Destination = ruleRec.Destination;
									newBatch.Frequency = ruleRec.Frequency;
									newBatch.Source = ruleRec.Source;
									newBatch.TimeOfDay = ruleRec.TimeOfDay;
									newBatch.Rule = rule;
									model.BatchesToRun.Add(newBatch);
								}
							}
						}
					}
				}
			}
		}

		private void saveBatchesAndTickets(TicketGeneratorModel model)
		{
			if (model.BatchesToRun.Any())
			{
				foreach (var batch in model.BatchesToRun)
				{
					using (ISessionFactory sessionFactory = CreateSessionFactory())
					{
						using (var session = sessionFactory.OpenSession())
						{
							var newBatchRec = new TransferBatchRec();
							newBatchRec.AccountId = batch.AccountId;
							newBatchRec.CreateDate = System.DateTime.Now;
							newBatchRec.TransferRuleId = batch.Rule.TransferRuleId;
							using (var transaction = session.BeginTransaction())
							{
								session.SaveOrUpdate(newBatchRec);
								transaction.Commit();
							}
							using (var transaction = session.BeginTransaction())
							{
								foreach (var ticket in batch.Tickets)
								{
									var newTicketRec = new TicketRec();
									newTicketRec.CreateDate = DateTime.Now;
									newTicketRec.Destination = ticket.Destination;
									newTicketRec.DestinationBoxAccountId = ticket.DestinationBoxAccountId;
									newTicketRec.DestinationGoogleAccountId = ticket.DestinationGoogleAccountId;
									newTicketRec.DestinationFolderId = ticket.DestinationFolderId;
									newTicketRec.DestinationFolderLabel = ticket.DestinationFolderLabel;
									newTicketRec.SaveAsFileName = ticket.SaveAsFileName;
									newTicketRec.CallAction = ticket.CallAction;
									newTicketRec.CallDirection = ticket.CallDirection;
									newTicketRec.CallFromLocation = ticket.CallFromLocation;
									newTicketRec.CallFromName = ticket.CallFromName;
									newTicketRec.CallFromNumber = ticket.CallFromNumber;
									newTicketRec.CallId = ticket.CallId;
									newTicketRec.CallResult = ticket.CallResult;
									newTicketRec.CallTime = ticket.CallTime;
									newTicketRec.InitiatedBy = ticket.InitiatedBy;
									newTicketRec.ContentUri = ticket.ContentUri;
									newTicketRec.TransferBatchId = ticket.TransferBatchId;
									session.SaveOrUpdate(newTicketRec);
								}
								transaction.Commit();
							}
						}
					}
				}
			}
		}

		private DateTime convertFromUtc(string timeZoneId, DateTime dateTime)
		{
			TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
			return TimeZoneInfo.ConvertTimeFromUtc(dateTime.ToUniversalTime(), timeZone);
		}

		private List<Call> getCallsFromCallLog(TicketGeneratorModel.TransferBatch batch)
		{
			List<Call> calls = new List<Call>();
			Account account = null;
			using (ISessionFactory sessionFactory = CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenSession())
				{
					var accountCrit = session.CreateCriteria<Account>();
					accountCrit.Add(Expression.Eq("AccountId", batch.AccountId));
					var accounts = accountCrit.List<Account>();
					if (accounts.Any())
					{
						account = accounts.First();
					}
				}
			}

			if (account != null)
			{
				var t = new RingCentral.CallLog(account.RingCentralUsername);
				var dateTo = DateTime.Now.AddHours(12);
				var dateFrom = dateTo.AddHours(-64);
				t.DateFrom(dateFrom);
				t.DateTo(dateTo);
				t.Execute();
				if (t.data != null && t.data.records != null)
				{
					foreach (var rec in t.data.records)
					{
						if (rec.recording != null && !string.IsNullOrWhiteSpace(rec.recording.contentUri))
						{
							var time = DateTime.Parse(rec.startTime);
							var diff = (DateTime.Now - time);
							var timeSince = "";
							if (diff.Days > 0)
								timeSince = diff.Days + " days ago";
							else if (diff.Hours == 1)
								timeSince = diff.Hours + " hour ago";
							else if (diff.Hours > 0)
								timeSince = diff.Hours + " hours ago";
							else if (diff.Minutes > 3)
								timeSince = diff.Minutes + " minutes ago";
							else
								timeSince = "just now";

							var newFile = new Call()
							{
								Result = rec.result,
								Action = rec.action,
								Direction = rec.direction,
								FromLocation = rec.from.location,
								FromName = rec.from.name,
								FromNumber = rec.from.phoneNumber,
								ToLocation = rec.to.location,
								ToName = rec.to.name,
								ToNumber = rec.to.phoneNumber,
								Id = rec.id,
								Time = time,
								TimeLabel = time.ToString("ddd, MMMM d, yyyy"),
								TimeSince = timeSince,
								ContentUri = rec.recording.contentUri
							};
							getNumbers(newFile, rec);
							generateFileName(newFile, rec);
							getPreviousTransferStatus(newFile, rec, batch.AccountId);
							var serializer = new JavaScriptSerializer();
							newFile.SerializedPacket = HttpUtility.HtmlEncode(serializer.Serialize(newFile));
							calls.Add(newFile);
						}
					}
				}
			}
			return calls;
		}

		public class TicketGeneratorModel
		{
			public DateTime Now;
			public List<TransferBatch> BatchesToRun;
			public class Rule
			{
				public int TransferRuleId;
				public int AccountId;
				public int FtpAccountId;
				public string Source;
				public string Destination;
				public int DestinationBoxAccountId;
				public int DestinationGoogleAccountId;
				public int DestinationFtpAccountId;
				public string DestinationFolderId;
				public string DestinationFolderLabel;
				public bool PutInDatedSubfolder;
				public string Frequency;
				public string DayOf;
				public string TimeOfDay;
				public bool DeletedInd;
				public bool ActiveInd;
			}
			public class TransferBatch
			{
				public int TransferBatchId;
				public int AccountId;
				public string Source;
				public string Destination;
				public string Frequency;
				public string TimeOfDay;
				public List<Ticket> Tickets;
				public Rule Rule;
				
			}
			public class Ticket
			{
				public int TicketId;
				public int TransferBatchId;
				public DateTime CreateDate;
				public string InitiatedBy;
				public string Destination;
				public int DestinationBoxAccountId;
				public int DestinationGoogleAccountId;
				public int DestinationFtpAccountId;
				public string DestinationFolderId;
				public string DestinationFolderLabel;
				public bool PutInDatedSubfolder;
				public string SaveAsFileName;
				public string CallId;
				public DateTime? CallTime;
				public string CallFromNumber;
				public string CallFromName;
				public string CallFromLocation;
				public string CallDirection;
				public string CallAction;
				public string CallResult;
				public string ContentUri;
				public bool ProcessingInd;
			}
		}

		public class Call
		{
			public string Id;
			public DateTime Time;
			public string TimeLabel;
			public string TimeSince;
			public string FromNumber;
			public string FromName;
			public string FromLocation;
			public string ToNumber;
			public string ToName;
			public string ToLocation;
			public string Numbers;
			public string Direction;
			public string Action;
			public string Result;
			public string FileName;
			public string SerializedPacket;
			public bool Transferred;
			public string TransferredOn;
			public string ContentUri;
		}

		private void getNumbers(Call call, RingCentral.CallLog.CallLogData.Record rec)
		{
			var from = "";
			var to = "";
			var numbers = "";
			if (!string.IsNullOrEmpty(call.ToNumber))
				to = call.ToNumber.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+", "");
			if (!string.IsNullOrEmpty(call.FromNumber))
				from = call.FromNumber.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+", "");
			if (to.Length == 11)
				to = to.Substring(1, 10);
			if (from.Length == 11)
				from = from.Substring(1, 10);
			if (to.Length == 10)
				to = "(" + to.Substring(0, 3) + ") " + to.Substring(3, 3) + "-" + to.Substring(6, 4);
			if (from.Length == 10)
				from = "(" + from.Substring(0, 3) + ") " + from.Substring(3, 3) + "-" + from.Substring(6, 4);
			call.FromNumber = from;
			call.ToNumber = to;
			if (from.Any())
				if (to.Any())
					numbers = "From " + from + " To " + to;
				else
					numbers = "From " + from;
			else
				if (to.Any())
				numbers = "To " + to;
			call.Numbers = numbers;
		}
		private void getNumbers(Call call, RingCentral.MessageStore.MessageStoreData.Record rec)
		{
			var from = "";
			var to = "";
			var numbers = "";
			if (!string.IsNullOrEmpty(call.ToNumber))
				to = call.ToNumber.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+", "");
			if (!string.IsNullOrEmpty(call.FromNumber))
				from = call.FromNumber.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+", "");
			if (to.Length == 11)
				to = to.Substring(1, 10);
			if (from.Length == 11)
				from = from.Substring(1, 10);
			if (to.Length == 10)
				to = "(" + to.Substring(0, 3) + ") " + to.Substring(3, 3) + "-" + to.Substring(6, 4);
			if (from.Length == 10)
				from = "(" + from.Substring(0, 3) + ") " + from.Substring(3, 3) + "-" + from.Substring(6, 4);
			call.FromNumber = from;
			call.ToNumber = to;
			if (from.Any())
				if (to.Any())
					numbers = "From " + from + " To " + to;
				else
					numbers = "From " + from;
			else
				if (to.Any())
				numbers = "To " + to;
			call.Numbers = numbers;
		}
		private void generateFileName(Call call, RingCentral.CallLog.CallLogData.Record rec)
		{
			call.FileName = "";
			DateTime time;
			if (DateTime.TryParse(rec.startTime, out time))
			{
				call.FileName += time.ToString("yyyyMMdd_HHmm");
			}
			call.FileName += "_" + call.FromNumber.Replace(" ", "");
			if (!string.IsNullOrEmpty(call.ToNumber))
				call.FileName += "_" + call.ToNumber.Replace(" ", "");
			call.FileName += "_" + call.Direction + "_" + call.Result.Replace(" ", "-").Replace("Call-connected", "RecordedCall") + ".mp3";
		}
		private void generateFileName(Call call, RingCentral.MessageStore.MessageStoreData.Record rec)
		{
			call.FileName = "";
			DateTime time;
			if (DateTime.TryParse(rec.creationTime, out time))
			{
				call.FileName += time.ToString("yyyyMMdd_HHmm");
			}
			call.FileName += "_" + call.FromNumber.Replace(" ", "");
			if (!string.IsNullOrEmpty(call.ToNumber))
				call.FileName += "_" + call.ToNumber.Replace(" ", "");
			call.FileName += "_" + call.Direction + "_" + call.Result.Replace(" ", "-").Replace("Call-connected", "RecordedCall") + ".mp3";
		}
		private void getPreviousTransferStatus(Call call, RingCentral.CallLog.CallLogData.Record rec, int accountId)
		{
			using (ISessionFactory sessionFactory = CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenSession())
				{
					var accountCrit = session.CreateCriteria<Account>();
					accountCrit.Add(Expression.Eq("AccountId", accountId));
					var accounts = accountCrit.List<Account>();
					if (accounts.Any())
					{
						var account = accounts.First();
						var ticketCrit = session.CreateCriteria<TicketRec>();
						ticketCrit.Add(Expression.Eq("CallId", rec.id));
						ticketCrit.Add(Expression.Eq("DeletedInd", false));
						var tickets = ticketCrit.List<TicketRec>();
						foreach (var ticket in tickets)
						{
							var ticketLogCrit = session.CreateCriteria<TicketLogRec>();
							ticketLogCrit.Add(Expression.Eq("TicketId", ticket.TicketId));
							var ticketLogs = ticketLogCrit.List<TicketLogRec>();
							foreach (var log in ticketLogs.Where(x => !x.ErrorInd && x.TicketLogStopDate.HasValue))
							{
								call.Transferred = true;
								call.TransferredOn = "<span class='date'>" + log.TicketLogStopDate.Value.ToString("MMM d, yyyy") + " </span><span class='time'>" + log.TicketLogStopDate.Value.ToString("hh:mm:ss tt") + "</span>";
							}
						}
					}
				}
			}
		}
		private void getPreviousTransferStatus(Call call, RingCentral.MessageStore.MessageStoreData.Record rec, int accountId)
		{
			using (ISessionFactory sessionFactory = CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenSession())
				{
					var accountCrit = session.CreateCriteria<Account>();
					accountCrit.Add(Expression.Eq("AccountId", accountId));
					var accounts = accountCrit.List<Account>();
					if (accounts.Any())
					{
						var account = accounts.First();
						var ticketCrit = session.CreateCriteria<TicketRec>();
						ticketCrit.Add(Expression.Eq("CallId", rec.id));
						ticketCrit.Add(Expression.Eq("DeletedInd", false));
						var tickets = ticketCrit.List<TicketRec>();
						foreach (var ticket in tickets)
						{
							var ticketLogCrit = session.CreateCriteria<TicketLogRec>();
							ticketLogCrit.Add(Expression.Eq("TicketId", ticket.TicketId));
							var ticketLogs = ticketLogCrit.List<TicketLogRec>();
							foreach (var log in ticketLogs.Where(x => !x.ErrorInd && x.TicketLogStopDate.HasValue))
							{
								call.Transferred = true;
								call.TransferredOn = "<span class='date'>" + log.TicketLogStopDate.Value.ToString("MMM d, yyyy") + " </span><span class='time'>" + log.TicketLogStopDate.Value.ToString("hh:mm:ss tt") + "</span>";
							}
						}
					}
				}
			}
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

		private class Account
		{
			public virtual int AccountId { get; set; }
			public virtual string RingCentralUsername { get; set; }
		}
		private class AccountMap : ClassMap<Account>
		{
			public AccountMap()
			{
				Table("T_ACCOUNT");
				Id(x => x.AccountId).Column("AccountId");
				Map(x => x.RingCentralUsername);
			}
		}
		private class TicketRec
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
			public virtual string PutInDatedSubfolder { get; set; }
			public virtual string SaveAsFileName { get; set; }
			public virtual string CallId { get; set; }
			public virtual DateTime? CallTime { get; set; }
			public virtual string CallFromNumber { get; set; }
			public virtual string CallFromName { get; set; }
			public virtual string CallFromLocation { get; set; }
			public virtual string CallDirection { get; set; }
			public virtual string CallAction { get; set; }
			public virtual string CallResult { get; set; }
			public virtual string ContentUri { get; set; }
			public virtual bool ProcessingInd { get; set; }
		}
		private class TicketRecMap : ClassMap<TicketRec>
		{
			public TicketRecMap()
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
				Map(x => x.PutInDatedSubfolder);
				Map(x => x.SaveAsFileName);
				Map(x => x.CallId);
				Map(x => x.CallTime);
				Map(x => x.CallFromNumber);
				Map(x => x.CallFromName);
				Map(x => x.CallFromLocation);
				Map(x => x.CallDirection);
				Map(x => x.CallAction);
				Map(x => x.CallResult);
				Map(x => x.ContentUri);
				Map(x => x.ProcessingInd);
			}
		}

		public class TransferRuleRec
		{
			public virtual int TransferRuleId { get; set; }
			public virtual int AccountId { get; set; }
			public virtual int FtpAccountId { get; set; }
			public virtual string Source { get; set; }
			public virtual string Destination { get; set; }
			public virtual int DestinationBoxAccountId { get; set; }
			public virtual int DestinationGoogleAccountId { get; set; }
			public virtual int DestinationFtpAccountId { get; set; }
			public virtual string DestinationFolderId { get; set; }
			public virtual string DestinationFolderLabel { get; set; }
			public virtual bool PutInDatedSubFolder { get; set; }
			public virtual string Frequency { get; set; }
			public virtual string DayOf { get; set; }
			public virtual string TimeOfDay { get; set; }
			public virtual bool DeletedInd { get; set; }
			public virtual bool ActiveInd { get; set; }
		}
		private class TransferRuleRecMap : ClassMap<TransferRuleRec>
		{
			public TransferRuleRecMap()
			{
				Table("T_TRANSFERRULE");
				Id(x => x.TransferRuleId).Column("TransferRuleId");
				Map(x => x.FtpAccountId);
				Map(x => x.AccountId);
				Map(x => x.Source);
				Map(x => x.Destination);
				Map(x => x.DestinationBoxAccountId);
				Map(x => x.DestinationGoogleAccountId);
				Map(x => x.DestinationFtpAccountId);
				Map(x => x.DestinationFolderId);
				Map(x => x.DestinationFolderLabel);
				Map(x => x.PutInDatedSubFolder);
				Map(x => x.Frequency);
				Map(x => x.DayOf);
				Map(x => x.TimeOfDay);
				Map(x => x.DeletedInd);
				Map(x => x.ActiveInd);
			}
		}

		public class TransferBatchRec
		{
			public virtual int TransferBatchId { get; set; }
			public virtual int TransferRuleId { get; set; }
			public virtual int AccountId { get; set; }
			public virtual DateTime CreateDate { get; set; }
		}
		private class TransferBatchRecMap : ClassMap<TransferBatchRec>
		{
			public TransferBatchRecMap()
			{
				Table("T_TRANSFERBATCH");
				Id(x => x.TransferBatchId).Column("TransferBatchId");
				Map(x => x.TransferRuleId);
				Map(x => x.AccountId);
				Map(x => x.CreateDate);
			}
		}
		private class TicketLogRec
		{
			public virtual int TicketLogId { get; set; }
			public virtual int TicketId { get; set; }
			public virtual int TransferBatchId { get; set; }
			public virtual DateTime? TicketLogStartDate { get; set; }
			public virtual DateTime? TicketLogStopDate { get; set; }
			public virtual bool ErrorInd { get; set; }
		}
		private class TicketLogRecMap : ClassMap<TicketLogRec>
		{
			public TicketLogRecMap()
			{
				Table("T_TICKETLOG");
				Id(x => x.TicketLogId).Column("TicketLogId");
				Map(x => x.TicketId);
				Map(x => x.TransferBatchId);
				Map(x => x.TicketLogStartDate);
				Map(x => x.TicketLogStopDate);
				Map(x => x.ErrorInd);
			}
		}

		#endregion
	}
}
