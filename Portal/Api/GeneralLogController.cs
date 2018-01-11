using Dapper;
using FluentNHibernate.Mapping;
using RingClone.Portal.Helpers;
using RingClone.Portal.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.Caching;
using System.Web;
using System.Web.Http;
using System.Web.Script.Serialization;

namespace RingClone.Portal.Api
{
    [Authorize]
    public class GeneralLogController : ApiController
    {
        [HttpGet]
        public GeneralLogModel Index(string type = "voice", string dateFrom = "", string dateTo = "", string extension = "", int perPage = 50, string navTo = "")
        {
            var model = new GeneralLogModel();
            model.Type = type;
            model.DateFrom = dateFrom;
            model.DateTo = dateTo;
            model.Extension = extension;
            model.PerPage = perPage;
            model.NavTo = navTo;
            model.LogEntries = new List<GeneralLogModel.LogEntry>();
            if (type == "voice")
                getVoiceLog(model);
            else if (type == "fax")
                getFaxLog(model);
            else if (type == "sms")
                getSmsLog(model);
            
            if (User.Identity.RingCloneIdentity().RingCentralId == "191625028")
            {
                var adds = new List<GeneralLogModel.LogEntry>();
                var extraTimes = 4;
                for (var i = 0; i < extraTimes; i++)
                {
                    foreach (var l in model.LogEntries)
                    {
                        adds.Add(new GeneralLogModel.LogEntry()
                        {
                            Id = l.Id,
                            Display = l.Display
                        });
                    }
                }
                model.LogEntries.AddRange(adds);
            }
            return model;
        }
        private void getVoiceLog(GeneralLogModel model)
        {
            var t = new RingCentral.CallLog(User.Identity.RingCloneIdentity().RingCentralId);
            DateTime d;
            if (DateTime.TryParse(model.DateFrom, out d))
                t.DateFrom(d);
            if (DateTime.TryParse(model.DateTo, out d))
                t.DateTo(d);
            t.ExtensionNumber(model.Extension).PerPage(model.PerPage).NavTo(model.NavTo);
            t.WithRecording(null);
            t.Type("Voice");
            t.Execute();

            if (t.data != null && t.data.records != null)
            {
                getTicketLogs(model, t.data.records.Select(x => x.id));
                getTransferRules(model);
                foreach (var rec in t.data.records)
                {
                    var newFile = new GeneralLogModel.LogEntry()
                    {
                        Id = rec.id,
                    };
                    generateVoiceLogDisplay(model, newFile, rec);
                    saveVoiceRawData(newFile, rec);
                    if (t.data.navigation != null)
                    {
                        model.Navigation = new GeneralLogModel.LogNavigation();
                        if (t.data.navigation.firstPage != null)
                            model.Navigation.FirstPage = t.data.navigation.firstPage.uri;
                        if (t.data.navigation.previousPage != null)
                            model.Navigation.PrevPage = t.data.navigation.previousPage.uri;
                        if (t.data.navigation.nextPage != null)
                            model.Navigation.NextPage = t.data.navigation.nextPage.uri;
                        if (t.data.navigation.lastPage != null)
                            model.Navigation.LastPage = t.data.navigation.lastPage.uri;
                    }
                    model.LogEntries.Add(newFile);

                    if (rec.recording == null || string.IsNullOrWhiteSpace(rec.recording.contentUri))
                    {
                    }
                }
            }

        }
        private void getFaxLog(GeneralLogModel model)
        {
            var t = new RingCentral.MessageStore(User.Identity.RingCloneIdentity().RingCentralId);
            DateTime d;
            if (DateTime.TryParse(model.DateFrom, out d))
                t.DateFrom(d);
            if (DateTime.TryParse(model.DateTo, out d))
                t.DateTo(d);
            t.Extension(model.Extension).PerPage(model.PerPage).NavTo(model.NavTo);
            t.MessageType("Fax");
            t.Execute();

            if (t.data != null && t.data.records != null)
            {
                getTicketLogs(model, t.data.records.Select(x => x.id));
                getTransferRules(model);
                foreach (var rec in t.data.records)
                {
                    var newFile = new GeneralLogModel.LogEntry()
                    {
                        Id = rec.id
                    };
                    generateFaxLogDisplay(model, newFile, rec);
                    saveFaxRawData(newFile, rec);
                    if (t.data.navigation != null)
                    {
                        model.Navigation = new GeneralLogModel.LogNavigation();
                        if (t.data.navigation.firstPage != null)
                            model.Navigation.FirstPage = t.data.navigation.firstPage.uri;
                        if (t.data.navigation.previousPage != null)
                            model.Navigation.PrevPage = t.data.navigation.previousPage.uri;
                        if (t.data.navigation.nextPage != null)
                            model.Navigation.NextPage = t.data.navigation.nextPage.uri;
                        if (t.data.navigation.lastPage != null)
                            model.Navigation.LastPage = t.data.navigation.lastPage.uri;
                    }
                    model.LogEntries.Add(newFile);
                }
            }

        }
        private void getSmsLog(GeneralLogModel model)
        {
            var t = new RingCentral.MessageStore(User.Identity.RingCloneIdentity().RingCentralId);
            DateTime d;
            if (DateTime.TryParse(model.DateFrom, out d))
                t.DateFrom(d);
            if (DateTime.TryParse(model.DateTo, out d))
                t.DateTo(d);
            t.Extension(model.Extension).PerPage(model.PerPage).NavTo(model.NavTo);
            t.MessageType("SMS");
            t.Execute();

            if (t.data != null && t.data.records != null)
            {
                getTicketLogs(model, t.data.records.Select(x => x.id));
                getTransferRules(model);
                foreach (var rec in t.data.records)
                {
                    var newFile = new GeneralLogModel.LogEntry()
                    {
                        Id = rec.id
                    };
                    generateSmsLogDisplay(model, newFile, rec);
                    saveSmsRawData(newFile, rec);
                    if (t.data.navigation != null)
                    {
                        model.Navigation = new GeneralLogModel.LogNavigation();
                        if (t.data.navigation.firstPage != null)
                            model.Navigation.FirstPage = t.data.navigation.firstPage.uri;
                        if (t.data.navigation.previousPage != null)
                            model.Navigation.PrevPage = t.data.navigation.previousPage.uri;
                        if (t.data.navigation.nextPage != null)
                            model.Navigation.NextPage = t.data.navigation.nextPage.uri;
                        if (t.data.navigation.lastPage != null)
                            model.Navigation.LastPage = t.data.navigation.lastPage.uri;
                    }
                    model.LogEntries.Add(newFile);
                }
            }

        }

        private void generateVoiceLogDisplay(GeneralLogModel model, GeneralLogModel.LogEntry logEntry, RingCentral.CallLog.CallLogData.Record rec)
        {
            var from = "";
            var to = "";
            if (rec.from != null && !string.IsNullOrEmpty(rec.from.phoneNumber))
                from = rec.from.phoneNumber.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+", "");
            if (rec.to != null && !string.IsNullOrEmpty(rec.to.phoneNumber))
                to = rec.to.phoneNumber.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+", "");
            if (to.Length == 11)
                to = to.Substring(1, 10);
            if (from.Length == 11)
                from = from.Substring(1, 10);
            if (to.Length == 10)
                to = "(" + to.Substring(0, 3) + ") " + to.Substring(3, 3) + "-" + to.Substring(6, 4);
            if (from.Length == 10)
                from = "(" + from.Substring(0, 3) + ") " + from.Substring(3, 3) + "-" + from.Substring(6, 4);
            if (from.Length == 0 && rec.from != null && !string.IsNullOrEmpty(rec.from.extensionNumber))
                from = "x" + rec.from.extensionNumber;
            if (to.Length == 0 && rec.to != null && !string.IsNullOrEmpty(rec.to.extensionNumber))
                to = "x" + rec.to.extensionNumber;

            logEntry.Display = new GeneralLogModel.LogDisplay();
            logEntry.Display.Type = rec.type;
            if (rec.direction != null && rec.direction.ToLower() == "inbound")
            {
                logEntry.Display.PhoneNumber = "From: " + from;
                logEntry.Display.Name = rec.from != null && !string.IsNullOrWhiteSpace(rec.from.name) ? rec.from.name : "Unknown";
            }
            else
            {
                logEntry.Display.PhoneNumber = "To: " + to;
                logEntry.Display.Name = rec.to != null && !string.IsNullOrWhiteSpace(rec.to.name) ? rec.to.name : "Unknown";
            }
            logEntry.Display.Date = DateTime.Parse(rec.startTime).ToString("ddd MM/dd/yyyy hh:mm tt");
            logEntry.Display.Action = rec.action;
            logEntry.Display.Result = rec.result;
            var hours = Math.Floor((double)rec.duration / 60 / 60);
            var minutes = Math.Floor((double)rec.duration / 60);
            var seconds = rec.duration - (hours * 60 * 60) - (minutes * 60);
            logEntry.Display.Length = hours.ToString("0") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00");

            //ANALYZE ALL RECORDINGS AND VOICEMAILS
            var voicemailList = new List<string>();
            var recordingList = new List<string>();
            var totalVoicemails = 0;
            var totalRecordings = 0;
            if (rec.message != null && rec.message.id != null)
            {
                totalVoicemails++;
                voicemailList.Add(rec.message.id);
            }
            if (rec.recording != null && rec.recording.id != null)
            {
                totalRecordings++;
                recordingList.Add(rec.recording.id);
            }
            if (rec.legs != null)
            {
                foreach (var leg in rec.legs)
                {
                    if (leg.message != null && leg.message.id != null && !voicemailList.Any(x => x == leg.message.id))
                    {
                        totalVoicemails++;
                        voicemailList.Add(leg.message.id);
                    }
                    if (leg.recording != null && leg.recording.uri != null && !recordingList.Any(x => x == leg.recording.id))
                    {
                        totalRecordings++;
                        recordingList.Add(leg.recording.id);
                    }
                }
            }

            //CONTENT STATUS
            if (totalVoicemails == 0 && totalRecordings == 0)
            {
                logEntry.Display.RowClass = "no-content";
            }
            else
            {
                logEntry.Display.RowClass = "has-content";
            }

            //ARCHIVE STATUS
            DateTime tDate;
            int archiveLookbackHours = 60;
            if (User.Identity.RingCloneIdentity().RingCentralId == "191625028")
                archiveLookbackHours = 720;
            //if (model.TicketLogs.Any(x => (x.CallId == rec.id || (rec.message != null && x.MessageId == rec.message.id)) && x.DeletedInd == false && x.ErrorInd == false && x.TicketLogStopDate.HasValue))
            if (model.Tickets.Any(x => (x.CallId == rec.id || (rec.message != null && x.MessageId == rec.message.id)) && x.DeletedInd == false && x.CompleteDate.HasValue))
            {
                var ticketLog = model.Tickets.Last(x => (x.CallId == rec.id || (rec.message != null && x.MessageId == rec.message.id)) && x.DeletedInd == false && x.ErrorInd == false && x.CompleteDate.HasValue);
                logEntry.Display.ArchiveStatus = "Archived";
                logEntry.Display.RowClass += " archived";
                logEntry.Display.ArchiveTooltip = "Archived<br/>on " + ticketLog.CompleteDate.Value.ToString("ddd MM/dd/yyyy") + " at " + ticketLog.CompleteDate.Value.ToString("hh:mm tt") + ".";
                logEntry.Display.ArchiveIcon = "archived.svg";
            }
            else if (model.Tickets.Any(x => (x.CallId == rec.id || (rec.message != null && x.MessageId == rec.message.id)) && x.ErrorInd == true))
            {
                logEntry.Display.ArchiveStatus = "Error";
                logEntry.Display.RowClass += " error";
                logEntry.Display.ArchiveIcon = "error.svg";
                if (model.TransferRules.Any())
                {
                    var transferRule = model.TransferRules.First();
                    var timeForSchedule = 0;
                    if (transferRule.Frequency == "Every day" && int.TryParse(transferRule.TimeOfDay, out timeForSchedule))
                    {
                        timeForSchedule = timeForSchedule / 100;
                        var timeForNow = int.Parse(DateTime.Now.ToUniversalTime().ToUniversalTime().ToString("HH"));
                        if (timeForNow <= timeForSchedule)
                        {
                            var dateForSchedule = new DateTime(DateTime.Now.ToUniversalTime().ToUniversalTime().Year, DateTime.Now.ToUniversalTime().Month, DateTime.Now.ToUniversalTime().Day);
                            dateForSchedule = dateForSchedule.AddHours(timeForSchedule / 100);
                            logEntry.Display.ArchiveTooltip = "An error occurred when archiving this file.<br/>Another archival is scheduled on<br/>" + dateForSchedule.ToString("ddd MM/dd/yyyy") + " at " + dateForSchedule.ToString("hh:mm tt");
                        }
                        else
                        {
                            var dateForSchedule = new DateTime(DateTime.Now.ToUniversalTime().Year, DateTime.Now.ToUniversalTime().Month, DateTime.Now.ToUniversalTime().Day);
                            dateForSchedule = dateForSchedule.AddDays(1).AddHours(timeForSchedule);
                            logEntry.Display.ArchiveTooltip = "An error occurred when archiving this file.<br/>Another archival is scheduled on<br/>" + dateForSchedule.ToString("ddd MM/dd/yyyy") + " at " + dateForSchedule.ToString("hh:mm tt");
                        }
                    }
                }
                else
                {
                    logEntry.Display.ArchiveTooltip = "An error occurred when archiving this file.<br/>Try archiving this file again, or turn on automatic archiving to automatically retry the archive action";
                }
            }
            else if (model.TransferRules != null && model.TransferRules.Any(x => x.Frequency == "Every day" && (x.VoiceContentInd || x.VoiceLogInd)) && DateTime.TryParse(rec.startTime, out tDate) && (DateTime.Now.ToUniversalTime() - tDate).TotalHours < archiveLookbackHours )
            {
                var transferRule = model.TransferRules.First();
                logEntry.Display.ArchiveStatus = "Scheduled";
                logEntry.Display.RowClass += " scheduled";
                logEntry.Display.ArchiveIcon = "scheduled.svg";
                var timeForSchedule = 0;
                if (int.TryParse(transferRule.TimeOfDay, out timeForSchedule))
                {
                    timeForSchedule = timeForSchedule / 100;
                    var timeForNow = int.Parse(DateTime.Now.ToUniversalTime().ToString("HH"));
                    if (timeForNow <= timeForSchedule)
                    {
                        var dateForSchedule = new DateTime(DateTime.Now.ToUniversalTime().Year, DateTime.Now.ToUniversalTime().Month, DateTime.Now.ToUniversalTime().Day);
                        dateForSchedule = dateForSchedule.AddHours(timeForSchedule / 100);
                        logEntry.Display.ArchiveTooltip = "Scheduled for archival<br/>on " + dateForSchedule.ToString("ddd MM/dd/yyyy") + " at " + dateForSchedule.ToString("hh:mm tt");
                    }
                    else
                    {
                        var dateForSchedule = new DateTime(DateTime.Now.ToUniversalTime().Year, DateTime.Now.ToUniversalTime().Month, DateTime.Now.ToUniversalTime().Day);
                        dateForSchedule = dateForSchedule.AddDays(1).AddHours(timeForSchedule);
                        logEntry.Display.ArchiveTooltip = "Scheduled for archival<br/>on " + dateForSchedule.ToString("ddd MM/dd/yyyy") + " at " + dateForSchedule.ToString("hh:mm tt");
                    }
                }
                else
                {
                    logEntry.Display.ArchiveTooltip = "Scheduled for archival";
                }
            }
            else
            {
                logEntry.Display.ArchiveStatus = "Not archived";
                logEntry.Display.ArchiveTooltip = "This content has not been archived";
                logEntry.Display.ArchiveIcon = "not-archived.svg";
                logEntry.Display.RowClass += " not-archived";
            }

            //CONTENT VERBIAGE
            var contentVerbiage = "<br/><br/>1 log entry";
            if (totalRecordings > 0 && totalVoicemails == 0)
                contentVerbiage += " with</br>" + totalRecordings + " recorded call" + (totalRecordings > 1 ? "s" : "");
            else if (totalRecordings == 0 && totalVoicemails > 0)
                contentVerbiage += " with</br>" + totalVoicemails + " voicemail" + (totalVoicemails > 1 ? "s" : "");
            else if (totalRecordings > 0 && totalVoicemails > 0)
                contentVerbiage += " with</br>" + totalRecordings + " recorded call" + (totalRecordings > 1 ? "s" : "") + " and <br/>" + totalVoicemails + " voicemail" + (totalVoicemails > 1 ? "s" : "");
            logEntry.Display.ArchiveTooltip += contentVerbiage;

        }
        //private void generateVoiceTicket(GeneralLogModel.LogEntry logEntry, RingCentral.CallLog.CallLogData.Record rec)
        //{
        //    var ticket = new GeneralLogModel.Ticket();

        //    //PROCESSED FIELDS
        //    var from = "";
        //    var to = "";
        //    var recommendedFileName = "";
        //    DateTime startTime;
        //    if (rec.from != null && !string.IsNullOrEmpty(rec.from.phoneNumber))
        //        from = rec.from.phoneNumber.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+", "");
        //    if (rec.to != null && !string.IsNullOrEmpty(rec.to.phoneNumber))
        //        to = rec.to.phoneNumber.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+", "");
        //    if (to.Length == 11)
        //        to = to.Substring(1, 10);
        //    if (from.Length == 11)
        //        from = from.Substring(1, 10);
        //    if (to.Length == 10)
        //        to = "(" + to.Substring(0, 3) + ") " + to.Substring(3, 3) + "-" + to.Substring(6, 4);
        //    if (from.Length == 10)
        //        from = "(" + from.Substring(0, 3) + ") " + from.Substring(3, 3) + "-" + from.Substring(6, 4);
        //    if (DateTime.TryParse(rec.startTime, out startTime))
        //    {
        //        recommendedFileName += startTime.ToString("yyyyMMdd_HHmm");
        //    }
        //    recommendedFileName += "_" + from.Replace(" ", "");
        //    if (!string.IsNullOrEmpty(to))
        //        recommendedFileName += "_" + to.Replace(" ", "");
        //    recommendedFileName += "_" + rec.direction + "_" + rec.result.Replace(" ", "-").Replace("Call-connected", "RecordedCall") + ".mp3";

        //    ticket.Type = "voice";
        //    ticket.CallAction = rec.action;
        //    ticket.CallDirection = rec.direction;
        //    ticket.CallFromLocation = rec.from != null && rec.from.location != null ? rec.from.location : "";
        //    ticket.CallFromName = rec.from != null && rec.from.name != null ? rec.from.name : "";
        //    ticket.CallFromNumber = rec.from != null && rec.from.phoneNumber != null ? rec.from.phoneNumber : "";
        //    ticket.CallId = rec.id;
        //    ticket.CallResult = rec.result;
        //    ticket.CallTime = DateTime.TryParse(rec.startTime, out startTime) ? startTime : DateTime.Now.ToUniversalTime();
        //    ticket.ContentUri = rec.recording != null && rec.recording.contentUri != null ? rec.recording.contentUri : "";
        //    ticket.SaveAsFileName = recommendedFileName;

        //    var serializer = new JavaScriptSerializer();
        //    logEntry.SerializedTicket = HttpUtility.HtmlEncode(serializer.Serialize(ticket));

        //}
        private void saveVoiceRawData(GeneralLogModel.LogEntry logEntry, RingCentral.CallLog.CallLogData.Record rec)
        {
            var jss = new JavaScriptSerializer();
            var rawData = jss.Serialize(rec);
            var memoryCache = MemoryCache.Default;
            memoryCache.Add(rec.id, rec, new DateTimeOffset(DateTime.Now.ToUniversalTime().AddHours(1)));
            //using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
            //{
            //    var accounts = db.Query<Account>("SELECT * FROM T_ACCOUNT WHERE RINGCENTRALID = @ringCentralId", new { ringCentralId = User.Identity.RingCloneIdentity().RingCentralId });
            //    if (accounts.Any())
            //    {
            //        var account = accounts.First();
            //        var rawDataList = db.Query<RingCentralCallRawData>("SELECT * FROM T_RINGCENTRALCALLRAWDATA WHERE ACCOUNTID = @accountId AND CALLID = @callId AND DELETEDIND = 0", new { accountId = account.AccountId, callId = rec.id });
            //        if (rawDataList.Any())
            //        {
            //            var rawDataRec = rawDataList.First();
            //            var jss = new JavaScriptSerializer();
            //            var rawData = jss.Serialize(rec);
            //            db.Execute("UPDATE T_RINGCENTRALCALLRAWDATA SET RAWDATA = @rawData WHERE RINGCENTRALCALLRAWDATAID = @ringCentralCallRawDataId", new { rawData = rawData, ringCentralCallRawDataId = rawDataRec.RingCentralCallRawDataId });
            //        }
            //        else
            //        {
            //            var jss = new JavaScriptSerializer();
            //            var rawData = jss.Serialize(rec);
            //            db.Execute("INSERT INTO T_RINGCENTRALCALLRAWDATA (AccountId, CallId, RawData) VALUES (@accountId, @callId, @rawData) ", new { accountId = account.AccountId, callId = rec.id, rawData = rawData });
            //        }
            //    }
            //}
        }

        private void generateFaxLogDisplay(GeneralLogModel model, GeneralLogModel.LogEntry logEntry, RingCentral.MessageStore.MessageStoreData.Record rec)
        {
            var from = "";
            var to = "";
            var totalRecepients = 0;
            if (rec.from != null && !string.IsNullOrEmpty(rec.from.phoneNumber))
                from = rec.from.phoneNumber.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+", "");
            if (rec.to != null && rec.to.Any())
            {
                totalRecepients = rec.to.Count();
                if (totalRecepients == 1)
                {
                    var firstPhone = rec.to.First(x => !string.IsNullOrEmpty(x.phoneNumber));
                    to = firstPhone.phoneNumber.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+", "");
                }
                else if (totalRecepients > 1)
                {
                    to = "Multiple Recipients";
                }
            }
            if (to.Length == 11)
                to = to.Substring(1, 10);
            if (from.Length == 11)
                from = from.Substring(1, 10);
            if (to.Length == 10)
                to = "(" + to.Substring(0, 3) + ") " + to.Substring(3, 3) + "-" + to.Substring(6, 4);
            if (from.Length == 10)
                from = "(" + from.Substring(0, 3) + ") " + from.Substring(3, 3) + "-" + from.Substring(6, 4);
            if (string.IsNullOrEmpty(to)) to = "Unknown";

            logEntry.Display = new GeneralLogModel.LogDisplay();
            logEntry.Display.Type = rec.type;
            if (!string.IsNullOrEmpty(rec.direction) && rec.direction.ToLower() == "inbound")
            {
                logEntry.Display.PhoneNumber = "From: " + from;
                logEntry.Display.Name = rec.from != null && !string.IsNullOrWhiteSpace(rec.from.name) ? rec.from.name : "Unknown";
            }
            else
            {
                logEntry.Display.PhoneNumber = "To: " + to;
                logEntry.Display.Name = to;
            }
            logEntry.Display.Date = DateTime.Parse(rec.creationTime).ToString("ddd MM/dd/yyyy hh:mm tt");
            logEntry.Display.MessageStatus = rec.messageStatus;
            logEntry.Display.CoverPageText = !string.IsNullOrWhiteSpace(rec.coverPageText) ? rec.coverPageText : "<i>None</i>";
            logEntry.Display.FaxPageCount = rec.faxPageCount.ToString();
            logEntry.Display.Direction = rec.direction;

            //ANALYZE ALL ATTACHMENTS
            var attachmentList = new List<string>();
            var totalAttachments = 0;
            if (rec.attachments != null)
            {
                foreach (var attachment in rec.attachments)
                {
                    if (attachment.id != null && !attachmentList.Any(x => x == attachment.id))
                    {
                        totalAttachments++;
                        attachmentList.Add(attachment.id);
                    }
                }
            }

            //CONTENT STATUS
            if (totalAttachments > 0)
            {
                logEntry.Display.RowClass = "has-content";
            }
            else
            {
                logEntry.Display.RowClass = "no-content";
            }

            //ARCHIVE STATUS
            DateTime tDate;
            int archiveLookbackHours = 60;
            if (User.Identity.RingCloneIdentity().RingCentralId == "191625028")
                archiveLookbackHours = 720;
            if (model.Tickets.Any(x => x.MessageId == logEntry.Id && x.DeletedInd == false && x.CompleteDate.HasValue))
            {
                var ticketLog = model.Tickets.Last(x => x.MessageId == logEntry.Id && x.DeletedInd == false && x.CompleteDate.HasValue);
                logEntry.Display.ArchiveStatus = "Archived";
                logEntry.Display.RowClass += " archived";
                logEntry.Display.ArchiveTooltip = "Archived on " + ticketLog.CompleteDate.Value.ToString("ddd MM/dd/yyyy") + " at " + ticketLog.CompleteDate.Value.ToString("hh:mm tt") + ".";
                logEntry.Display.ArchiveIcon = "archived.svg";
            }
            else if (model.Tickets.Any(x => x.MessageId == logEntry.Id && x.ErrorInd == true))
            {
                logEntry.Display.ArchiveStatus = "Error";
                logEntry.Display.RowClass += " error";
                logEntry.Display.ArchiveIcon = "error.svg";
                if (model.TransferRules.Any())
                {
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
                            logEntry.Display.ArchiveTooltip = "An error occurred when archiving this file.\nAnother backup is scheduled for archival on " + dateForSchedule.ToString("ddd MM/dd/yyyy") + " at " + dateForSchedule.ToString("hh:mm tt");
                        }
                        else
                        {
                            var dateForSchedule = new DateTime(DateTime.Now.ToUniversalTime().Year, DateTime.Now.ToUniversalTime().Month, DateTime.Now.ToUniversalTime().Day);
                            dateForSchedule = dateForSchedule.AddDays(1).AddHours(timeForSchedule);
                            logEntry.Display.ArchiveTooltip = "An error occurred when archiving this file.\nAnother backup is scheduled for archival on " + dateForSchedule.ToString("ddd MM/dd/yyyy") + " at " + dateForSchedule.ToString("hh:mm tt");
                        }
                    }
                }
                else
                {
                    logEntry.Display.ArchiveTooltip = "An error occurred when archiving this file.<br/>Try archiving this file again, or turn on automatic archiving to automatically retry the archive action";
                }
            }
            else if (model.TransferRules != null && model.TransferRules.Any(x => x.Frequency == "Every day" && (x.FaxLogInd || x.FaxContentInd)) && DateTime.TryParse(rec.creationTime, out tDate) && (DateTime.Now.ToUniversalTime() - tDate).TotalHours < archiveLookbackHours)
            {
                var transferRule = model.TransferRules.First();
                logEntry.Display.ArchiveStatus = "Scheduled";
                logEntry.Display.RowClass += " scheduled";
                logEntry.Display.ArchiveIcon = "scheduled.svg";
                var timeForSchedule = 0;
                if (int.TryParse(transferRule.TimeOfDay, out timeForSchedule))
                {
                    timeForSchedule = timeForSchedule / 100;
                    var timeForNow = int.Parse(DateTime.Now.ToUniversalTime().ToString("HH"));
                    if (timeForNow <= timeForSchedule)
                    {
                        var dateForSchedule = new DateTime(DateTime.Now.ToUniversalTime().Year, DateTime.Now.ToUniversalTime().Month, DateTime.Now.ToUniversalTime().Day);
                        dateForSchedule = dateForSchedule.AddHours(timeForSchedule / 100);
                        logEntry.Display.ArchiveTooltip = "Scheduled for archival on " + dateForSchedule.ToString("ddd MM/dd/yyyy") + " at " + dateForSchedule.ToString("hh:mm tt");
                    }
                    else
                    {
                        var dateForSchedule = new DateTime(DateTime.Now.ToUniversalTime().Year, DateTime.Now.ToUniversalTime().Month, DateTime.Now.ToUniversalTime().Day);
                        dateForSchedule = dateForSchedule.AddDays(1).AddHours(timeForSchedule);
                        logEntry.Display.ArchiveTooltip = "Scheduled for archival on " + dateForSchedule.ToString("ddd MM/dd/yyyy") + " at " + dateForSchedule.ToString("hh:mm tt");
                    }
                }
                else
                {
                    logEntry.Display.ArchiveTooltip = "Scheduled for archival";
                }
            }
            else
            {
                logEntry.Display.ArchiveStatus = "Not archived";
                logEntry.Display.ArchiveTooltip = "This content has not been archived.";
                logEntry.Display.ArchiveIcon = "not-archived.svg";
                logEntry.Display.RowClass += " not-archived";
            }

            //CONTENT VERBIAGE
            var contentVerbiage = "<br/><br/>1 log entry";
            if (totalAttachments > 0)
            {
                contentVerbiage += "<br/>with " + totalAttachments + " attachment" + (totalAttachments > 1 ? "s" : "");
            }
            logEntry.Display.ArchiveTooltip += contentVerbiage;

        }
        //private void generateFaxTicket(GeneralLogModel.LogEntry logEntry, RingCentral.MessageStore.MessageStoreData.Record rec)
        //{
        //    var ticket = new GeneralLogModel.Ticket();

        //    //PROCESSED FIELDS
        //    var from = "";
        //    var to = "";
        //    var recommendedFileName = "";
        //    var totalRecepients = 0;
        //    DateTime startTime;
        //    if (rec.from != null && !string.IsNullOrEmpty(rec.from.phoneNumber))
        //        from = rec.from.phoneNumber.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+", "");
        //    if (rec.to != null && rec.to.Any())
        //    {
        //        totalRecepients = rec.to.Count();
        //        if (totalRecepients == 1)
        //        {
        //            var firstPhone = rec.to.First(x => !string.IsNullOrEmpty(x.phoneNumber));
        //            to = firstPhone.phoneNumber.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+", "");
        //        }
        //        else if (totalRecepients > 1)
        //        {
        //            to = "MultipleRecepients";
        //        }
        //    }
        //    if (to.Length == 11)
        //        to = to.Substring(1, 10);
        //    if (from.Length == 11)
        //        from = from.Substring(1, 10);
        //    if (to.Length == 10)
        //        to = "(" + to.Substring(0, 3) + ") " + to.Substring(3, 3) + "-" + to.Substring(6, 4);
        //    if (from.Length == 10)
        //        from = "(" + from.Substring(0, 3) + ") " + from.Substring(3, 3) + "-" + from.Substring(6, 4);
        //    if (DateTime.TryParse(rec.creationTime, out startTime))
        //    {
        //        recommendedFileName += startTime.ToString("yyyyMMdd_HHmm");
        //    }
        //    recommendedFileName += "_" + from.Replace(" ", "");
        //    if (!string.IsNullOrEmpty(to))
        //        recommendedFileName += "_" + to.Replace(" ", "");
        //    recommendedFileName += "_" + rec.direction + "_" + rec.type + ".pdf";


        //    ticket.MessageId = rec.id;
        //    ticket.Type = "fax";
        //    ticket.CallDirection = rec.direction;
        //    ticket.CallFromLocation = rec.from != null && rec.from.location != null ? rec.from.location : "";
        //    ticket.CallFromName = rec.from != null && rec.from.name != null ? rec.from.name : "";
        //    ticket.CallFromNumber = rec.from != null && rec.from.phoneNumber != null ? rec.from.phoneNumber : "";
        //    ticket.CallId = rec.id;
        //    ticket.CallTime = DateTime.TryParse(rec.creationTime, out startTime) ? startTime : DateTime.Now.ToUniversalTime();
        //    if (rec.attachments != null && rec.attachments.Any())
        //    {
        //        ticket.ContentUri = rec.attachments.First().uri;
        //        ticket.ContentType = rec.attachments.First().contentType;
        //    }
        //    ticket.SaveAsFileName = recommendedFileName;

        //    var serializer = new JavaScriptSerializer();
        //    logEntry.SerializedTicket = HttpUtility.HtmlEncode(serializer.Serialize(ticket));

        //}
        private void saveFaxRawData(GeneralLogModel.LogEntry logEntry, RingCentral.MessageStore.MessageStoreData.Record rec)
        {
            var jss = new JavaScriptSerializer();
            var rawData = jss.Serialize(rec);
            var memoryCache = MemoryCache.Default;
            memoryCache.Add(rec.id, rec, new DateTimeOffset(DateTime.Now.ToUniversalTime().AddHours(1)));
        }

        private void generateSmsLogDisplay(GeneralLogModel model, GeneralLogModel.LogEntry logEntry, RingCentral.MessageStore.MessageStoreData.Record rec)
        {
            var from = "";
            var to = "";
            var totalRecepients = 0;
            if (rec.from != null && !string.IsNullOrEmpty(rec.from.phoneNumber))
                from = rec.from.phoneNumber.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+", "");
            if (rec.to != null && rec.to.Any())
            {
                totalRecepients = rec.to.Count();
                if (totalRecepients == 1)
                {
                    var firstPhone = rec.to.First(x => !string.IsNullOrEmpty(x.phoneNumber));
                    to = firstPhone.phoneNumber.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+", "");
                }
                else if (totalRecepients > 1)
                {
                    to = "Multiple recipients";
                }
            }
            if (to.Length == 11)
                to = to.Substring(1, 10);
            if (from.Length == 11)
                from = from.Substring(1, 10);
            if (to.Length == 10)
                to = "(" + to.Substring(0, 3) + ") " + to.Substring(3, 3) + "-" + to.Substring(6, 4);
            if (from.Length == 10)
                from = "(" + from.Substring(0, 3) + ") " + from.Substring(3, 3) + "-" + from.Substring(6, 4);
            if (string.IsNullOrEmpty(to)) to = "Unknown";

            logEntry.Display = new GeneralLogModel.LogDisplay();
            logEntry.Display.Type = rec.type;
            if (!string.IsNullOrEmpty(rec.direction) && rec.direction.ToLower() == "inbound")
            {
                logEntry.Display.PhoneNumber = "From: " + from;
                logEntry.Display.Name = rec.from != null && !string.IsNullOrWhiteSpace(rec.from.name) ? rec.from.name : "Unknown";
            }
            else
            {
                logEntry.Display.PhoneNumber = "To: " + to;
                logEntry.Display.Name = to;
            }
            logEntry.Display.Date = DateTime.Parse(rec.creationTime).ToString("ddd MM/dd/yyyy hh:mm tt");
            logEntry.Display.MessageStatus = rec.messageStatus;
            logEntry.Display.Subject = !string.IsNullOrWhiteSpace(rec.subject) ? rec.subject : "<i>None</i>";
            logEntry.Display.Direction = rec.direction;

            //ANALYZE ALL ATTACHMENTS
            var attachmentList = new List<string>();
            var totalAttachments = 0;
            if (rec.attachments != null)
            {
                foreach (var attachment in rec.attachments)
                {
                    if (attachment.id != null && !attachmentList.Any(x => x == attachment.id))
                    {
                        totalAttachments++;
                        attachmentList.Add(attachment.id);
                    }
                }
            }

            //CONTENT VERBIAGE
            if (totalAttachments > 0)
            {
                logEntry.Display.RowClass = "has-content";
            }
            else
            {
                logEntry.Display.RowClass = "no-content";
            }

            //ARCHIVE STATUS
            DateTime tDate;
            int archiveLookbackHours = 60;
            if (User.Identity.RingCloneIdentity().RingCentralId == "191625028")
                archiveLookbackHours = 720;
            if (model.Tickets.Any(x => x.MessageId == logEntry.Id && x.DeletedInd == false && x.CompleteDate.HasValue))
            {
                var ticketLog = model.Tickets.Last(x => x.MessageId == logEntry.Id && x.DeletedInd == false && x.CompleteDate.HasValue);
                logEntry.Display.ArchiveStatus = "Archived";
                logEntry.Display.RowClass += " archived";
                logEntry.Display.ArchiveTooltip = "Archived on " + ticketLog.CompleteDate.Value.ToString("ddd MM/dd/yyyy") + " at " + ticketLog.CompleteDate.Value.ToString("hh:mm tt") + ".";
                logEntry.Display.ArchiveIcon = "archived.svg";
            }
            else if (model.Tickets.Any(x => x.MessageId == logEntry.Id && x.ErrorInd == true))
            {
                logEntry.Display.ArchiveStatus = "Error";
                logEntry.Display.RowClass += " error";
                logEntry.Display.ArchiveIcon = "error.svg";
                if (model.TransferRules.Any())
                {
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
                            logEntry.Display.ArchiveTooltip = "An error occurred when archiving this file.\nAnother backup is scheduled for archival on " + dateForSchedule.ToString("ddd MM/dd/yyyy") + " at " + dateForSchedule.ToString("hh:mm tt");
                        }
                        else
                        {
                            var dateForSchedule = new DateTime(DateTime.Now.ToUniversalTime().Year, DateTime.Now.ToUniversalTime().Month, DateTime.Now.ToUniversalTime().Day);
                            dateForSchedule = dateForSchedule.AddDays(1).AddHours(timeForSchedule);
                            logEntry.Display.ArchiveTooltip = "An error occurred when archiving this file.\nAnother backup is scheduled for archival on " + dateForSchedule.ToString("ddd MM/dd/yyyy") + " at " + dateForSchedule.ToString("hh:mm tt");
                        }
                    }
                }
                else
                {
                    logEntry.Display.ArchiveTooltip = "An error occurred when archiving this file.<br/>Try archiving this file again, or turn on automatic archiving to automatically retry the archive action";
                }
            }
            else if (model.TransferRules != null && model.TransferRules.Any(x => x.Frequency == "Every day" && (x.SmsLogInd || x.SmsContentInd)) && DateTime.TryParse(rec.creationTime, out tDate) && (DateTime.Now.ToUniversalTime() - tDate).TotalHours < archiveLookbackHours)
            {
                var transferRule = model.TransferRules.First();
                logEntry.Display.ArchiveStatus = "Scheduled";
                logEntry.Display.RowClass += " scheduled";
                logEntry.Display.ArchiveIcon = "scheduled.svg";
                var timeForSchedule = 0;
                if (int.TryParse(transferRule.TimeOfDay, out timeForSchedule))
                {
                    timeForSchedule = timeForSchedule / 100;
                    var timeForNow = int.Parse(DateTime.Now.ToUniversalTime().ToString("HH"));
                    if (timeForNow <= timeForSchedule)
                    {
                        var dateForSchedule = new DateTime(DateTime.Now.ToUniversalTime().Year, DateTime.Now.ToUniversalTime().Month, DateTime.Now.ToUniversalTime().Day);
                        dateForSchedule = dateForSchedule.AddHours(timeForSchedule / 100);
                        logEntry.Display.ArchiveTooltip = "Scheduled for archival on " + dateForSchedule.ToString("ddd MM/dd/yyyy") + " at " + dateForSchedule.ToString("hh:mm tt");
                    }
                    else
                    {
                        var dateForSchedule = new DateTime(DateTime.Now.ToUniversalTime().Year, DateTime.Now.ToUniversalTime().Month, DateTime.Now.ToUniversalTime().Day);
                        dateForSchedule = dateForSchedule.AddDays(1).AddHours(timeForSchedule);
                        logEntry.Display.ArchiveTooltip = "Scheduled for archival on " + dateForSchedule.ToString("ddd MM/dd/yyyy") + " at " + dateForSchedule.ToString("hh:mm tt");
                    }
                }
                else
                {
                    logEntry.Display.ArchiveTooltip = "Scheduled for archival";
                }
            }
            else
            {
                logEntry.Display.ArchiveStatus = "Not archived";
                logEntry.Display.ArchiveTooltip = "This content has not been archived.";
                logEntry.Display.ArchiveIcon = "not-archived.svg";
                logEntry.Display.RowClass += " not-archived";
            }

            //CONTENT VERBIAGE
            var contentVerbiage = "<br/><br/>1 log entry";
            if (totalAttachments > 0)
            {
                contentVerbiage += "<br/>with " + totalAttachments + " attachment" + (totalAttachments > 1 ? "s" : "");
            }
            logEntry.Display.ArchiveTooltip += contentVerbiage;

        }
        //private void generateSmsTicket(GeneralLogModel.LogEntry logEntry, RingCentral.MessageStore.MessageStoreData.Record rec)
        //{
        //    var ticket = new GeneralLogModel.Ticket();

        //    //PROCESSED FIELDS
        //    var from = "";
        //    var to = "";
        //    var recommendedFileName = "";
        //    var totalRecepients = 0;
        //    DateTime startTime;
        //    if (rec.from != null && !string.IsNullOrEmpty(rec.from.phoneNumber))
        //        from = rec.from.phoneNumber.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+", "");
        //    if (rec.to != null && rec.to.Any())
        //    {
        //        totalRecepients = rec.to.Count();
        //        if (totalRecepients == 1)
        //        {
        //            var firstPhone = rec.to.First(x => !string.IsNullOrEmpty(x.phoneNumber));
        //            to = firstPhone.phoneNumber.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+", "");
        //        }
        //        else if (totalRecepients > 1)
        //        {
        //            to = "MultipleRecepients";
        //        }
        //    }
        //    if (to.Length == 11)
        //        to = to.Substring(1, 10);
        //    if (from.Length == 11)
        //        from = from.Substring(1, 10);
        //    if (to.Length == 10)
        //        to = "(" + to.Substring(0, 3) + ") " + to.Substring(3, 3) + "-" + to.Substring(6, 4);
        //    if (from.Length == 10)
        //        from = "(" + from.Substring(0, 3) + ") " + from.Substring(3, 3) + "-" + from.Substring(6, 4);
        //    if (DateTime.TryParse(rec.creationTime, out startTime))
        //    {
        //        recommendedFileName += startTime.ToString("yyyyMMdd_HHmm");
        //    }
        //    recommendedFileName += "_" + from.Replace(" ", "");
        //    if (!string.IsNullOrEmpty(to))
        //        recommendedFileName += "_" + to.Replace(" ", "");
        //    recommendedFileName += "_" + rec.direction + "_" + rec.type + ".pdf";


        //    ticket.MessageId = rec.id;
        //    ticket.Type = "sms";
        //    ticket.CallDirection = rec.direction;
        //    ticket.CallFromLocation = rec.from != null && rec.from.location != null ? rec.from.location : "";
        //    ticket.CallFromName = rec.from != null && rec.from.name != null ? rec.from.name : "";
        //    ticket.CallFromNumber = rec.from != null && rec.from.phoneNumber != null ? rec.from.phoneNumber : "";
        //    ticket.CallId = rec.id;
        //    ticket.CallTime = DateTime.TryParse(rec.creationTime, out startTime) ? startTime : DateTime.Now.ToUniversalTime();
        //    if (rec.attachments != null && rec.attachments.Any())
        //    {
        //        ticket.ContentUri = rec.attachments.First().uri;
        //        ticket.ContentType = rec.attachments.First().contentType;
        //    }
        //    ticket.SaveAsFileName = recommendedFileName;

        //    var serializer = new JavaScriptSerializer();
        //    logEntry.SerializedTicket = HttpUtility.HtmlEncode(serializer.Serialize(ticket));

        //}
        private void saveSmsRawData(GeneralLogModel.LogEntry logEntry, RingCentral.MessageStore.MessageStoreData.Record rec)
        {
            var jss = new JavaScriptSerializer();
            var rawData = jss.Serialize(rec);
            var memoryCache = MemoryCache.Default;
            memoryCache.Add(rec.id, rec, new DateTimeOffset(DateTime.Now.ToUniversalTime().AddHours(1)));
        }

        private void getTicketLogs(GeneralLogModel model, IEnumerable<string> ids)
        {
            model.Tickets = new List<GeneralLogModel.Ticket>();
            if (model.Type == "fax" || model.Type == "sms")
            {
                using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                {
                    var accounts = db.Query<Account>("SELECT * FROM T_ACCOUNT WHERE RINGCENTRALID = @ringCentralId", new { ringCentralId = User.Identity.RingCloneIdentity().RingCentralId });
                    if (accounts.Any())
                    {
                        var account = accounts.First();
                        model.Tickets = db.Query<GeneralLogModel.Ticket>("SELECT T_TICKET.ErrorInd,T_TICKET.DeletedInd,T_TICKET.CompleteDate,T_TICKET.CallId,T_TICKET.MessageId,T_TRANSFERBATCH.AccountId " +
                            "FROM T_TICKET " +
                            "INNER JOIN T_TRANSFERBATCH ON T_TICKET.TRANSFERBATCHID = T_TRANSFERBATCH.TRANSFERBATCHID " +
                            "WHERE T_TICKET.MESSAGEID IN @callIds AND T_TRANSFERBATCH.AccountId = @accountId", new { callIds = ids, accountId = account.AccountId });
                    }
                }
            }
            else
            {
                using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                {
                    var accounts = db.Query<Account>("SELECT * FROM T_ACCOUNT WHERE RINGCENTRALID = @ringCentralId", new { ringCentralId = User.Identity.RingCloneIdentity().RingCentralId });
                    if (accounts.Any())
                    {
                        var account = accounts.First();
                        model.Tickets = db.Query<GeneralLogModel.Ticket>("SELECT T_TICKET.ErrorInd,T_TICKET.DeletedInd,T_TICKET.CompleteDate,T_TICKET.CallId,T_TICKET.MessageId,T_TRANSFERBATCH.AccountId " +
                            "FROM T_TICKET " +
                            "INNER JOIN T_TRANSFERBATCH ON T_TICKET.TRANSFERBATCHID = T_TRANSFERBATCH.TRANSFERBATCHID " +
                            "WHERE T_TICKET.CALLID IN @callIds AND T_TRANSFERBATCH.AccountId = @accountId", new { callIds = ids, accountId = account.AccountId });
                    }
                }
            }
        }
        private void getTransferRules(GeneralLogModel model)
        {
            model.TransferRules = new List<GeneralLogModel.TransferRule>();
            using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
            {
                var accounts = db.Query<Account>("SELECT * FROM T_ACCOUNT WHERE RINGCENTRALID = @ringCentralId", new { ringCentralId = User.Identity.RingCloneIdentity().RingCentralId });
                if (accounts.Any())
                {
                    var account = accounts.First();
                    model.TransferRules = db.Query<GeneralLogModel.TransferRule>("SELECT * FROM T_TRANSFERRULE WHERE DELETEDIND=0 AND ACTIVEIND=1 AND  ACCOUNTID = @accountId ", new { accountId = account.AccountId });
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
        private class TransferRule
        {
            public int TransferRuleId { get; set; }
            public int AccountId { get; set; }
            public bool VoiceLogInd { get; set; }
            public bool VoiceContentInd { get; set; }
            public bool FaxLogInd { get; set; }
            public bool FaxContentInd { get; set; }
            public bool SmsLogInd { get; set; }
            public bool SmsContentInd { get; set; }
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
