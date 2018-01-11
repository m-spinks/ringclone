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
using static RingCentral.CallLog;
using System.Data;
using System.Data.SqlClient;
using Dapper;

namespace RingClone.Portal.Api
{
	[Authorize]
    public class RingCentralController : ApiController
    {
        [HttpGet]
        public RingCentralConnectionModel AccountInfo()
        {
            var model = new RingCentralConnectionModel();
            model.CanLogin = false;

            var accountInfoGetter = new RingCentral.AccountInfo(User.Identity.RingCloneIdentity().RingCentralId);
            accountInfoGetter.Execute();
            if (accountInfoGetter.data != null)
            {
                model.Name = accountInfoGetter.data.name;
                model.Company = accountInfoGetter.data.contact.company;
                model.CanLogin = true;
				model.Email = "";
				if (accountInfoGetter.data.contact != null && !string.IsNullOrEmpty(accountInfoGetter.data.contact.email))
					model.Email = accountInfoGetter.data.contact.email;
            }
            return model;
        }
        [HttpGet]
        public RingCentralCallLogModel CallLog(string DateFrom = "", string DateTo = "", string extension = "", int perPage = 50, string navTo = "")
        {
            var model = new RingCentralCallLogModel();
            model.Calls = new List<RingCentralCallLogModel.Call>();
            var t = new RingCentral.CallLog(User.Identity.RingCloneIdentity().RingCentralId);
            DateTime d;
            if (DateTime.TryParse(DateFrom, out d))
                t.DateFrom(d);
            if (DateTime.TryParse(DateTo, out d))
                t.DateTo(d);
            t.ExtensionNumber(extension).PerPage(perPage).NavTo(navTo);
            t.WithRecording(null);
            t.Execute();

            if (t.data != null && t.data.records != null)
            {
                getTicketLogs(model, t.data.records.Select(x => x.id));
                getTransferRules(model);
                foreach (var rec in t.data.records)
                {
                    var time = DateTime.Parse(rec.startTime);
                    var diff = (DateTime.Now.ToUniversalTime() - time);
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

                    var newFile = new RingCentralCallLogModel.Call()
                    {
                        Result = rec.result,
                        Action = rec.action,
                        Direction = rec.direction,
                        FromLocation = rec.from != null ? rec.from.location : "",
                        FromName = rec.from != null ? rec.from.name : "",
                        FromNumber = rec.from != null ? rec.from.phoneNumber : "",
                        ToLocation = rec.to.location,
                        ToName = rec.to.name,
                        ToNumber = rec.to.phoneNumber,
                        Id = rec.id,
                        Time = time,
                        TimeLabel = time.ToString("ddd, MMMM d, yyyy"),
                        TimeSince = timeSince,
                        ContentUri = rec.recording != null ? rec.recording.contentUri : ""
                    };
                    getNumbers(newFile, rec);
                    generateRecommendedFileName(newFile, rec);
                    getPreviousTransferStatus(model, newFile, rec);
                    var serializer = new JavaScriptSerializer();
                    newFile.SerializedPacket = HttpUtility.HtmlEncode(serializer.Serialize(newFile));
                    generateCallLogDisplay(model,newFile, rec);
                    if (t.data.navigation != null)
                    {
                        model.Navigation = new Models.RingCentralCallLogModel.CallLogNavigation();
                        if (t.data.navigation.firstPage != null)
                            model.Navigation.FirstPage = t.data.navigation.firstPage.uri;
                        if (t.data.navigation.previousPage != null)
                            model.Navigation.PrevPage = t.data.navigation.previousPage.uri;
                        if (t.data.navigation.nextPage != null)
                            model.Navigation.NextPage = t.data.navigation.nextPage.uri;
                        if (t.data.navigation.lastPage != null)
                            model.Navigation.LastPage = t.data.navigation.lastPage.uri;
                    }
                    model.Calls.Add(newFile);

                    if (rec.recording == null || string.IsNullOrWhiteSpace(rec.recording.contentUri))
                    {
                    }
                }
            }
            return model;
        }
        [HttpGet]
        public RingCentralExtensionsModel Extensions(string navTo = "", int perPage = 10)
        {
            var model = new RingCentralExtensionsModel();
            var extGetter = new RingCentral.ExtensionsGetter(User.Identity.RingCloneIdentity().RingCentralId);
            if (!string.IsNullOrWhiteSpace(navTo))
            {
                extGetter.NavTo(navTo);
            }
            extGetter.PerPage(perPage);
            extGetter.Execute();
            if (extGetter.data != null)
            {
                model.extensions = new List<RingCentralExtensionsModel.Extension>();
                foreach(var extData in extGetter.data.records.Where(x => !string.IsNullOrWhiteSpace(x.extensionNumber)))
                {
                    var ext = new RingCentralExtensionsModel.Extension();
                    ext.Id = extData.id;
                    ext.Name = extData.name;
                    ext.ExtensionNumber = extData.extensionNumber;
                    if (extData.contact != null)
                    {
                        ext.Email = extData.contact.email;
                        ext.Firstname = extData.contact.firstName;
                        ext.Lastname = extData.contact.lastName;
                    }
                    model.extensions.Add(ext);
                }
                model.navigation = new Models.RingCentralExtensionsModel.Navigation();
                if (extGetter.data.navigation != null && extGetter.data.navigation.firstPage != null)
                    model.navigation.firstPage = extGetter.data.navigation.firstPage.uri;
                if (extGetter.data.navigation != null && extGetter.data.navigation.previousPage != null)
                    model.navigation.prevPage = extGetter.data.navigation.previousPage.uri;
                if (extGetter.data.navigation != null && extGetter.data.navigation.nextPage != null)
                    model.navigation.nextPage = extGetter.data.navigation.nextPage.uri;
                if (extGetter.data.navigation != null && extGetter.data.navigation.lastPage != null)
                    model.navigation.lastPage = extGetter.data.navigation.lastPage.uri;
            }
            return model;
        }

        private void getCallsFromCallLog(RingCentralCallLogModel model)
        {
            var pages = new List<RingCentral.CallLog>();
            var curPage = 1;
            var perPage = 500;
            var t = new RingCentral.CallLog(User.Identity.RingCloneIdentity().RingCentralId);
            if (model.DateFrom.HasValue)
                t.DateFrom(model.DateFrom.Value);
            if (model.DateTo.HasValue)
                t.DateTo(model.DateTo.Value);
            t.Page(curPage);
            t.PerPage(perPage);
            t.Execute();
            pages.Add(t);
            while (t.data != null && t.data.navigation != null && t.data.navigation.nextPage != null && !string.IsNullOrWhiteSpace(t.data.navigation.nextPage.uri))
            {
                t = new RingCentral.CallLog(User.Identity.RingCloneIdentity().RingCentralId);
                if (model.DateFrom.HasValue)
                    t.DateFrom(model.DateFrom.Value);
                if (model.DateTo.HasValue)
                    t.DateTo(model.DateTo.Value);
                t.Page(++curPage);
                t.PerPage(perPage);
                t.Execute();
                pages.Add(t);
            }
            foreach (var page in pages)
            {
                foreach (var rec in page.data.records)
                {
                    if (rec.recording != null && !string.IsNullOrWhiteSpace(rec.recording.contentUri) && !model.Calls.Any(x => x.Id == rec.id))
                    {
                        var time = DateTime.Parse(rec.startTime);
                        var diff = (DateTime.Now.ToUniversalTime() - time);
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

                        var newFile = new RingCentralCallLogModel.Call()
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
                        generateRecommendedFileName(newFile, rec);
                        getPreviousTransferStatus(model, newFile, rec);
                        var serializer = new JavaScriptSerializer();
                        newFile.SerializedPacket = HttpUtility.HtmlEncode(serializer.Serialize(newFile));
                        model.Calls.Add(newFile);
                    }
                }
            }
        }
        private void getCallsFromCallLogForExtension(RingCentralCallLogModel model, string extensionId)
        {
        }

        private void getMessages(RingCentralCallLogModel model)
        {
            if (model.Extensions != null && model.Extensions.Any())
            {
                foreach (var ext in model.Extensions)
                {
                    getMessagesForExtension(model, ext);
                }
            }
            else
            {
                getMessagesForExtension(model, "");
            }
        }
        private void getTicketLogs(RingCentralCallLogModel model, IEnumerable<string> callIds)
        {
            model.TicketLogs = new List<Models.RingCentralCallLogModel.TicketLog>();
            using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
            {
                var accounts = db.Query<Account>("SELECT * FROM T_ACCOUNT WHERE RINGCENTRALID = @ringCentralId", new { ringCentralId = User.Identity.RingCloneIdentity().RingCentralId });
                if (accounts.Any())
                {
                    var account = accounts.First();
                    model.TicketLogs = db.Query<RingCentralCallLogModel.TicketLog>("SELECT T_TICKETLOG.ErrorInd,T_TICKET.DeletedInd,T_TICKETLOG.TicketLogStopDate,T_TICKET.CallId,T_TRANSFERBATCH.AccountId " +
                        "FROM T_TICKETLOG " +
                        "INNER JOIN T_TICKET ON T_TICKETLOG.TICKETID = T_TICKET.TICKETID " +
                        "INNER JOIN T_TRANSFERBATCH ON T_TICKET.TRANSFERBATCHID = T_TRANSFERBATCH.TRANSFERBATCHID " +
                        "WHERE T_TICKET.CALLID IN @callIds AND T_TRANSFERBATCH.AccountId = @accountId", new { callIds = callIds, accountId = account.AccountId });
                }

            }
        }
        private void getTransferRules(RingCentralCallLogModel model)
        {
            model.TransferRules = new List<Models.RingCentralCallLogModel.TransferRule>();
            using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
            {
                var accounts = db.Query<Account>("SELECT * FROM T_ACCOUNT WHERE RINGCENTRALID = @ringCentralId", new { ringCentralId = User.Identity.RingCloneIdentity().RingCentralId });
                if (accounts.Any())
                {
                    var account = accounts.First();
                    model.TransferRules = db.Query<RingCentralCallLogModel.TransferRule>("SELECT * FROM T_TRANSFERRULE WHERE DELETEDIND=0 AND ACTIVEIND=1 AND  ACCOUNTID = @accountId ", new { accountId = account.AccountId });
                }

            }
        }
        private void getMessagesForExtension(RingCentralCallLogModel model, string extensionId)
        {
            var pages = new List<RingCentral.MessageStore>();
            var t = new RingCentral.MessageStore(User.Identity.RingCloneIdentity().RingCentralId);
            var curPage = 1;
            var perPage = 500;
            if (!string.IsNullOrEmpty(extensionId))
                t.Extension(extensionId);
            if (model.DateFrom.HasValue)
                t.DateFrom(model.DateFrom.Value);
            if (model.DateTo.HasValue)
                t.DateTo(model.DateTo.Value);
            t.Page(curPage);
            t.PerPage(perPage);
            t.Execute();
            pages.Add(t);
            while (t.data != null && t.data.navigation != null && t.data.navigation.nextPage != null && !string.IsNullOrWhiteSpace(t.data.navigation.nextPage.uri))
            {
                t = new RingCentral.MessageStore(User.Identity.RingCloneIdentity().RingCentralId);
                if (!string.IsNullOrEmpty(extensionId))
                    t.Extension(extensionId);
                if (model.DateFrom.HasValue)
                    t.DateFrom(model.DateFrom.Value);
                if (model.DateTo.HasValue)
                    t.DateTo(model.DateTo.Value);
                t.Page(++curPage);
                t.PerPage(perPage);
                t.Execute();
                pages.Add(t);
            }
            foreach (var page in pages)
            {
                foreach (var rec in page.data.records)
                {
                    if (rec.attachments != null && rec.attachments.Any(x => x.type == "AudioRecording") && !model.Calls.Any(x => x.Id == rec.id))
                    {
                        DateTime time;
                        if (DateTime.TryParse(rec.creationTime, out time))
                        {
                            var diff = (DateTime.Now.ToUniversalTime() - time);
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

                            var recording = rec.attachments.First(x => x.type == "AudioRecording");

                            var newFile = new RingCentralCallLogModel.Call()
                            {
                                Result = rec.type,
                                Action = "Missed Call",
                                Direction = rec.direction,
                                FromLocation = rec.from.location,
                                FromName = rec.from.name,
                                FromNumber = rec.from.phoneNumber,
                                ToLocation = (rec.to == null || rec.to.Any() ? "" : rec.to.First().location),
                                ToName = rec.to == null || rec.to.Any() ? "" : rec.to.First().phoneNumber,
                                ToNumber = rec.to == null || rec.to.Any() ? "" : rec.to.First().phoneNumber,
                                Id = rec.id,
                                Time = time,
                                TimeLabel = time.ToString("ddd, MMMM d, yyyy"),
                                TimeSince = timeSince,
                                ContentUri = recording.uri
                            };
                            getNumbers(newFile, rec);
                            generateRecommendedFileName(newFile, rec);
                            getPreviousTransferStatus(newFile, rec);
                            var serializer = new JavaScriptSerializer();
                            newFile.SerializedPacket = HttpUtility.HtmlEncode(serializer.Serialize(newFile));
                            model.Calls.Add(newFile);
                        }
                    }
                }
            }
        }
		private void getNumbers(RingCentralCallLogModel.Call call, RingCentral.CallLog.CallLogData.Record rec)
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
		private void getNumbers(RingCentralCallLogModel.Call call, RingCentral.MessageStore.MessageStoreData.Record rec)
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
		private void generateRecommendedFileName(RingCentralCallLogModel.Call call, RingCentral.CallLog.CallLogData.Record rec)
		{
			call.RecommendedFileName = "";
			DateTime time;
			if (DateTime.TryParse(rec.startTime, out time))
			{
				call.RecommendedFileName += time.ToString("yyyyMMdd_HHmm");
			}
			call.RecommendedFileName += "_" + call.FromNumber.Replace(" ", "");
			if (!string.IsNullOrEmpty(call.ToNumber))
				call.RecommendedFileName += "_" + call.ToNumber.Replace(" ", "");
			call.RecommendedFileName += "_" + call.Direction + "_" + call.Result.Replace(" ", "-").Replace("Call-connected", "RecordedCall") + ".mp3";
		}
        private void generateRecommendedFileName(RingCentralCallLogModel.Call call, RingCentral.MessageStore.MessageStoreData.Record rec)
        {
            call.RecommendedFileName = "";
            DateTime time;
            if (DateTime.TryParse(rec.creationTime, out time))
            {
                call.RecommendedFileName += time.ToString("yyyyMMdd_HHmm");
            }
            call.RecommendedFileName += "_" + call.FromNumber.Replace(" ", "");
            if (!string.IsNullOrEmpty(call.ToNumber))
                call.RecommendedFileName += "_" + call.ToNumber.Replace(" ", "");
            call.RecommendedFileName += "_" + call.Direction + "_" + call.Result.Replace(" ", "-").Replace("Call-connected", "RecordedCall") + ".mp3";
        }
        private void generateCallLogDisplay(RingCentralCallLogModel model, RingCentralCallLogModel.Call call, RingCentral.CallLog.CallLogData.Record rec)
        {
            call.Display = new Models.RingCentralCallLogModel.CallDisplay();
            call.Display.Type = rec.type;
            if (rec.direction == "inbound")
            {
                call.Display.PhoneNumber = "From: " + call.FromNumber;
                call.Display.Name = !string.IsNullOrWhiteSpace(call.FromName) ? call.FromName : "Unknown";
            }
            else
            {
                call.Display.PhoneNumber = "To: " + call.ToNumber;
                call.Display.Name = !string.IsNullOrWhiteSpace(call.ToName) ? call.ToName : "Unknown";
            }
            call.Display.Date = call.Time.ToString("ddd MM/dd/yyyy hh:mm tt");
            call.Display.Action = call.Action;
            call.Display.Result = call.Result;
            var hours = Math.Floor((double)rec.duration / 60 / 60);
            var minutes = Math.Floor((double)rec.duration / 60);
            var seconds = rec.duration - (hours * 60 * 60) - (minutes * 60);
            call.Display.Length = hours.ToString("0") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00");
            if (string.IsNullOrWhiteSpace(call.ContentUri))
            {
                call.Display.RowClass = "no-content";
                call.Display.ArchiveTooltip = "No content associated with this log entry.";
                call.Display.ArchiveIcon = "no-content.svg";
                call.Display.ArchiveStatus = "No content";
            }
            else
            {
                call.Display.RowClass = "has-content";
                if (model.TicketLogs.Any(x => x.CallId == call.Id && x.DeletedInd == false && x.ErrorInd == false && x.TicketLogStopDate.HasValue))
                {
                    var ticketLog = model.TicketLogs.Last(x => x.CallId == call.Id && x.DeletedInd == false && x.ErrorInd == false && x.TicketLogStopDate.HasValue);
                    call.Display.ArchiveStatus = "Archived";
                    call.Display.RowClass += " archived";
                    call.Display.ArchiveTooltip = "Archived on " + ticketLog.TicketLogStopDate.Value.ToString("ddd MM/dd/yyyy") + " at " + ticketLog.TicketLogStopDate.Value.ToString("hh:mm tt") + ".";
                    call.Display.ArchiveIcon = "archived.svg";
                }
                else if (model.TicketLogs.Any(x => x.CallId == call.Id && x.ErrorInd == true))
                {
                    call.Display.ArchiveStatus = "Error";
                    call.Display.RowClass += " error";
                    call.Display.ArchiveIcon = "error.svg";
                    var transferRule = model.TransferRules.First();
                    var timeForSchedule = 0;
                    if (transferRule.Frequency == "Every day" && int.TryParse(transferRule.TimeOfDay, out timeForSchedule))
                    {
                        timeForSchedule = timeForSchedule / 100;
                        var timeForNow = int.Parse(DateTime.Now.ToUniversalTime().ToString("HH"));
                        if (timeForNow <= timeForSchedule)
                        {
                            var dateForSchedule = new DateTime(DateTime.Now.ToUniversalTime().Year, DateTime.Now.ToUniversalTime().Month, DateTime.Now.ToUniversalTime().Day);
                            dateForSchedule = dateForSchedule.AddHours(timeForSchedule / 100);
                            call.Display.ArchiveTooltip = "An error occurred when archiving this file.\nAnother backup is scheduled for archival on " + dateForSchedule.ToString("ddd MM/dd/yyyy") + " at " + dateForSchedule.ToString("hh:mm tt");
                        }
                        else
                        {
                            var dateForSchedule = new DateTime(DateTime.Now.ToUniversalTime().Year, DateTime.Now.ToUniversalTime().Month, DateTime.Now.ToUniversalTime().Day);
                            dateForSchedule = dateForSchedule.AddDays(1).AddHours(timeForSchedule);
                            call.Display.ArchiveTooltip = "An error occurred when archiving this file.\nAnother backup is scheduled for archival on " + dateForSchedule.ToString("ddd MM/dd/yyyy") + " at " + dateForSchedule.ToString("hh:mm tt");
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(call.ContentUri) && model.TransferRules != null && model.TransferRules.Any())
                {
                    var transferRule = model.TransferRules.First();
                    call.Display.ArchiveStatus = "Scheduled";
                    call.Display.RowClass += " scheduled";
                    call.Display.ArchiveIcon = "scheduled.svg";
                    var timeForSchedule = 0;
                    if (transferRule.Frequency == "Every day" && int.TryParse(transferRule.TimeOfDay, out timeForSchedule))
                    {
                        timeForSchedule = timeForSchedule / 100;
                        var timeForNow = int.Parse(DateTime.Now.ToUniversalTime().ToString("HH"));
                        if (timeForNow <= timeForSchedule)
                        {
                            var dateForSchedule = new DateTime(DateTime.Now.ToUniversalTime().Year, DateTime.Now.ToUniversalTime().Month, DateTime.Now.ToUniversalTime().Day);
                            dateForSchedule = dateForSchedule.AddHours(timeForSchedule / 100);
                            call.Display.ArchiveTooltip = "Scheduled for archival on " + dateForSchedule.ToString("ddd MM/dd/yyyy") + " at " + dateForSchedule.ToString("hh:mm tt");
                        }
                        else
                        {
                            var dateForSchedule = new DateTime(DateTime.Now.ToUniversalTime().Year, DateTime.Now.ToUniversalTime().Month, DateTime.Now.ToUniversalTime().Day);
                            dateForSchedule = dateForSchedule.AddDays(1).AddHours(timeForSchedule);
                            call.Display.ArchiveTooltip = "Scheduled for archival on " + dateForSchedule.ToString("ddd MM/dd/yyyy") + " at " + dateForSchedule.ToString("hh:mm tt");
                        }
                    }
                    else
                    {
                        call.Display.ArchiveTooltip = "Scheduled for archival";
                    }
                }
                else
                {
                    call.Display.ArchiveStatus = "Not archived";
                    call.Display.ArchiveTooltip = "This content has not been archived.";
                    call.Display.ArchiveIcon = "not-archived.svg";
                    call.Display.RowClass += " not-archived";
                }
            }
        }
        private void getPreviousTransferStatus(RingCentralCallLogModel model, RingCentralCallLogModel.Call call, RingCentral.CallLog.CallLogData.Record rec)
		{
            if (model.TicketLogs.Any(x => x.CallId == call.Id && x.DeletedInd == false && x.ErrorInd == false && x.TicketLogStopDate.HasValue))
            {
                var log = model.TicketLogs.First(x => x.CallId == call.Id);
                call.Transferred = true;
                call.TransferredOn = "<span class='date'>" + log.TicketLogStopDate.Value.ToString("MMM d, yyyy") + " </span><span class='time'>" + log.TicketLogStopDate.Value.ToString("hh:mm:ss tt") + "</span>";
            }
        }
		private void getPreviousTransferStatus(RingCentralCallLogModel.Call call, RingCentral.MessageStore.MessageStoreData.Record rec)
		{
			using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenSession())
				{
					var accountCrit = session.CreateCriteria<Account>();
					accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
					var accounts = accountCrit.List<Account>();
					if (accounts.Any())
					{
						var account = accounts.First();
						foreach (var batch in account.TransferBatches)
						{
							foreach (var ticket in batch.Tickets.Where(x => !x.DeletedInd && x.CallId == call.Id))
							{
								foreach (var log in ticket.Logs.Where(x => !x.ErrorInd && x.TicketLogStopDate.HasValue))
								{
									call.Transferred = true;
									call.TransferredOn = "<span class='date'>" + log.TicketLogStopDate.Value.ToString("MMM d, yyyy") + " </span><span class='time'>" + log.TicketLogStopDate.Value.ToString("hh:mm:ss tt") + "</span>";
								}
							}
						}
					}
				}
				}
			}
        private void getExtensions(RingCentralCallLogModel model)
        {
            model.Extensions = new List<string>();
            var t = new RingCentral.ExtensionsGetter(User.Identity.RingCloneIdentity().RingCentralId);
            t.Execute();
            foreach (var rec in t.data.records)
            {
                if (!string.IsNullOrEmpty(rec.id))
                {
                    model.Extensions.Add(rec.id);
                }
            }
		}

#region Database Models
		private class Account
		{
			public virtual int AccountId { get; set; }
			public virtual string RingCentralId { get; set; }
			public virtual IList<TransferBatch> TransferBatches { get; set; }
		}
		private class AccountMap : ClassMap<Account>
		{
			public AccountMap()
			{
				Table("T_ACCOUNT");
				Id(x => x.AccountId).Column("AccountId");
				Map(x => x.RingCentralId);
				HasMany(x => x.TransferBatches).KeyColumn("AccountId").Inverse();
			}
		}
        private class TransferRule {
            public int TransferRuleId { get; set; }
            public int AccountId { get; set; }
            public bool DeletedInd { get; set; }
            public bool ActiveInd { get; set; }
        }

        private class TransferBatch
		{
			public virtual int TransferBatchId { get; set; }
			public virtual int AccountId { get; set; }
			public virtual DateTime CreateDate { get; set; }
			public virtual Account Account { get; set; }
			public virtual IList<Ticket> Tickets { get; set; }
		}
		private class TransferBatchMap : ClassMap<TransferBatch>
		{
			public TransferBatchMap()
			{
				Table("T_TRANSFERBATCH");
				Id(x => x.TransferBatchId).Column("TransferBatchId");
				Map(x => x.AccountId);
				Map(x => x.CreateDate);
				References(x => x.Account).Column("AccountId");
				HasMany(x => x.Tickets).KeyColumn("TransferBatchId").Inverse();
			}
		}
		private class Ticket
		{
			public virtual int TicketId { get; set; }
			public virtual int TransferBatchId { get; set; }
			public virtual DateTime CreateDate { get; set; }
			public virtual string CallId { get; set; }
			public virtual bool ProcessingInd { get; set; }
			public virtual bool DeletedInd { get; set; }
			public virtual TransferBatch TransferBatch { get; set; }
			public virtual IList<TicketLog> Logs { get; set; }
		}
		private class TicketMap : ClassMap<Ticket>
		{
			public TicketMap()
			{
				Table("T_TICKET");
				Id(x => x.TicketId).Column("TicketId");
				Map(x => x.TransferBatchId);
				Map(x => x.CreateDate);
				Map(x => x.CallId);
				Map(x => x.ProcessingInd);
				Map(x => x.DeletedInd);
				HasMany(x => x.Logs).KeyColumn("TicketId").Inverse();
				References(x => x.TransferBatch).Column("TransferBatchId");
			}
		}
		private class TicketLog
		{
			public virtual int TicketLogId { get; set; }
			public virtual int TicketId { get; set; }
			public virtual int TransferBatchId { get; set; }
			public virtual DateTime? TicketLogStartDate { get; set; }
			public virtual DateTime? TicketLogStopDate { get; set; }
			public virtual bool ErrorInd { get; set; }
			public virtual Ticket Ticket { get; set; }
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
				References(x => x.Ticket).Column("TicketId");
			}
		}
#endregion
	}
}
