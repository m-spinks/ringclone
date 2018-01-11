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
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;
using System.Web.Script.Serialization;

namespace RingClone.Portal.Api
{
    public class ArchiveSearchController : ApiController
    {
        [HttpGet]
        public ArchiveSearchModel Index(string type = "voice", string dateFrom = "", string dateTo = "", string extension = "", string name = "", int perPage = 50, int pageNumber = 1)
        {
            var model = new ArchiveSearchModel();
            model.Type = type;
            model.DateFrom = dateFrom;
            model.DateTo = dateTo;
            model.Extension = extension;
            model.Name = name;
            model.PerPage = perPage;
            model.PageNumber = pageNumber;
            if (model.PageNumber < 1)
                model.PageNumber = 1;
            model.ArchiveEntries = new List<ArchiveSearchModel.ArchiveEntry>();
            if (type == "voice")
                getVoiceArchives(model);
            if (type == "sms")
                getTextArchives(model);
            return model;
        }
        private void getVoiceArchives(ArchiveSearchModel model)
        {
            if (model.ArchiveEntries == null)
                model.ArchiveEntries = new List<ArchiveSearchModel.ArchiveEntry>();
            if (model.Navigation == null)
                model.Navigation = new ArchiveSearchModel.ArchiveNavigation();
            IEnumerable<IndexRec> dbRecs = null;
            IEnumerable<IndexCallerRec> dbCallerRecs = null;
            IEnumerable<IndexFileRec> dbFileRecs = null;
            DateTime dateFrom;
            DateTime dateTo;
            DateTime.TryParse(model.DateFrom, out dateFrom);
            DateTime.TryParse(model.DateTo, out dateTo);

            if (!string.IsNullOrWhiteSpace(model.Name) && !string.IsNullOrWhiteSpace(model.Extension))
            {
                using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                {
                    var accountRec = db.Query<AccountRec>("SELECT TOP 1 * FROM T_ACCOUNT WHERE RingCentralId=@ringCentralId ORDER BY AccountId", new { ringCentralId = User.Identity.RingCloneIdentity().RingCentralId }).First();
                    var mainQueryParams = new { ownerId = accountRec.RingCentralOwnerId, type = "voice", dateFrom = dateFrom, dateTo = dateTo, name = model.Name, offset = model.PerPage * (model.PageNumber - 1), perPage = model.PerPage, extension = model.Extension };
                    dbRecs = db.Query<IndexRec>("IndexSearchWithNameAndExtension", mainQueryParams, commandType: CommandType.StoredProcedure);
                    if (dbRecs.Count() == 0)
                        model.TotalRecords = 0;
                    else
                        model.TotalRecords = dbRecs.First().TotalRecs;
                    dbCallerRecs = db.Query<IndexCallerRec>("SELECT * FROM T_INDEXCALLER WHERE IndexId IN @indexRecs", new { indexRecs = dbRecs.Select(x => x.IndexId) });
                    dbFileRecs = db.Query<IndexFileRec>("SELECT * FROM T_INDEXFILE WHERE IndexId IN @indexRecs", new { indexRecs = dbRecs.Select(x => x.IndexId) });
                }
            }
            else if (!string.IsNullOrWhiteSpace(model.Name))
            {
                using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                {
                    var accountRec = db.Query<AccountRec>("SELECT TOP 1 * FROM T_ACCOUNT WHERE RingCentralId=@ringCentralId ORDER BY AccountId", new { ringCentralId = User.Identity.RingCloneIdentity().RingCentralId }).First();
                    var mainQueryParams = new { ownerId = accountRec.RingCentralOwnerId, type = "voice", dateFrom = dateFrom, dateTo = dateTo, name = model.Name, offset = model.PerPage * (model.PageNumber - 1), perPage = model.PerPage };
                    dbRecs = db.Query<IndexRec>("IndexSearchWithName", mainQueryParams, commandType: CommandType.StoredProcedure);
                    if (dbRecs.Count() == 0)
                        model.TotalRecords = 0;
                    else
                        model.TotalRecords = dbRecs.First().TotalRecs;
                    dbCallerRecs = db.Query<IndexCallerRec>("SELECT * FROM T_INDEXCALLER WHERE IndexId IN @indexRecs", new { indexRecs = dbRecs.Select(x => x.IndexId) });
                    dbFileRecs = db.Query<IndexFileRec>("SELECT * FROM T_INDEXFILE WHERE IndexId IN @indexRecs", new { indexRecs = dbRecs.Select(x => x.IndexId) });
                }
            }
            else if (!string.IsNullOrWhiteSpace(model.Extension))
            {
                using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                {
                    var accountRec = db.Query<AccountRec>("SELECT TOP 1 * FROM T_ACCOUNT WHERE RingCentralId=@ringCentralId ORDER BY AccountId", new { ringCentralId = User.Identity.RingCloneIdentity().RingCentralId }).First();
                    var mainQueryParams = new { ownerId = accountRec.RingCentralOwnerId, type = "voice", dateFrom = dateFrom, dateTo = dateTo, offset = model.PerPage * (model.PageNumber - 1), perPage = model.PerPage, extension = model.Extension };
                    dbRecs = db.Query<IndexRec>("IndexSearchWithExtension", mainQueryParams, commandType: CommandType.StoredProcedure);
                    if (dbRecs.Count() == 0)
                        model.TotalRecords = 0;
                    else
                        model.TotalRecords = dbRecs.First().TotalRecs;
                    dbCallerRecs = db.Query<IndexCallerRec>("SELECT * FROM T_INDEXCALLER WHERE IndexId IN @indexRecs", new { indexRecs = dbRecs.Select(x => x.IndexId) });
                    dbFileRecs = db.Query<IndexFileRec>("SELECT * FROM T_INDEXFILE WHERE IndexId IN @indexRecs", new { indexRecs = dbRecs.Select(x => x.IndexId) });
                }
            }
            else
            {
                using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                {
                    var accountRec = db.Query<AccountRec>("SELECT TOP 1 * FROM T_ACCOUNT WHERE RingCentralId=@ringCentralId ORDER BY AccountId", new { ringCentralId = User.Identity.RingCloneIdentity().RingCentralId }).First();
                    var mainQueryParams = new { ownerId = accountRec.RingCentralOwnerId, type = "voice", dateFrom = dateFrom, dateTo = dateTo, offset = model.PerPage * (model.PageNumber - 1), perPage = model.PerPage };
                    dbRecs = db.Query<IndexRec>("IndexSearch", mainQueryParams, commandType: CommandType.StoredProcedure);
                    if (dbRecs.Count() == 0)
                        model.TotalRecords = 0;
                    else
                        model.TotalRecords = dbRecs.First().TotalRecs;
                    dbCallerRecs = db.Query<IndexCallerRec>("SELECT * FROM T_INDEXCALLER WHERE IndexId IN @indexRecs", new { indexRecs = dbRecs.Select(x => x.IndexId) });
                    dbFileRecs = db.Query<IndexFileRec>("SELECT * FROM T_INDEXFILE WHERE IndexId IN @indexRecs", new { indexRecs = dbRecs.Select(x => x.IndexId) });
                }
            }
            model.TotalPages = (int)Math.Ceiling(((double)model.TotalRecords / (double)model.PerPage));
            if (dbRecs != null && dbRecs.Any())
            {
                foreach (var dbRec in dbRecs)
                {
                    var entry = new ArchiveSearchModel.ArchiveEntry();
                    var callers = dbCallerRecs.Where(x => x.IndexId == dbRec.IndexId).Select(x => new CallerItem() { IndexId = x.IndexId, ExtensionNumber = x.ExtensionNumber, Location = x.Location, Name = x.Name, PhoneNumber = x.PhoneNumber, ToInd = x.ToInd }).ToList();
                    var fromCallers = callers.Where(x => !x.ToInd).ToList();
                    var toCallers = callers.Where(x => x.ToInd).ToList();
                    entry.Id = dbRec.CallId;
                    entry.Display = new ArchiveSearchModel.ArchiveDisplay();
                    entry.Display.Type = dbRec.Type;
                    entry.Display.Action = dbRec.Action;
                    entry.Display.Date = dbRec.CallTime.ToString();
                    entry.Display.Direction = dbRec.Direction;
                    entry.Display.Date = dbRec.CallTime.ToString("ddd MM/dd/yyyy hh:mm tt");
                    entry.Display.Result = dbRec.Result;
                    var hours = dbRec.Duration / 60 / 60;
                    var minutes = dbRec.Duration / 60;
                    var seconds = dbRec.Duration - (hours * 60 * 60) - (minutes * 60);
                    entry.Display.Length = hours.ToString("0") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00");
                    
                    if (!string.IsNullOrEmpty(dbRec.Direction) && dbRec.Direction.ToLower() == "inbound" && fromCallers.Any())
                    {
                        fromCallers.First().Show = true;
                    }
                    else if (toCallers.Any())
                    {
                        toCallers.First().Show = true;
                    }

                    if (!string.IsNullOrWhiteSpace(model.Name) || !string.IsNullOrWhiteSpace(model.Extension))
                    {
                        foreach (var caller in callers)
                        {
                            if (!string.IsNullOrWhiteSpace(model.Name))
                            {
                                if (!string.IsNullOrWhiteSpace(caller.Name) && hasMatch(caller.Name, model.Name))
                                {
                                    caller.Name = applyMatch(caller.Name, model.Name);
                                    caller.Show = true;
                                }
                                if (!string.IsNullOrWhiteSpace(caller.PhoneNumber) && hasMatch(caller.PhoneNumber, model.Name))
                                {
                                    caller.PhoneNumber = applyMatch(caller.PhoneNumber, model.Name);
                                    caller.Show = true;
                                }
                                if (!string.IsNullOrWhiteSpace(caller.ExtensionNumber) && hasMatch(caller.ExtensionNumber, model.Name))
                                {
                                    caller.ExtensionNumber = applyMatch(caller.ExtensionNumber, model.Name);
                                    caller.Show = true;
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(model.Extension))
                            {
                                if (!string.IsNullOrWhiteSpace(caller.ExtensionNumber) && hasMatch(caller.ExtensionNumber, model.Extension))
                                {
                                    caller.ExtensionNumber = applyMatch(caller.ExtensionNumber, model.Extension);
                                    caller.Show = true;
                                }
                            }
                        }
                    }
                    foreach (var caller in callers.Where(x => x.Show))
                    {
                        if (!string.IsNullOrWhiteSpace(caller.PhoneNumber))
                            caller.PhoneNumber = formatPhoneNumber(caller.PhoneNumber);
                        if (!string.IsNullOrWhiteSpace(caller.ExtensionNumber))
                            caller.ExtensionNumber = formatPhoneNumber(caller.ExtensionNumber);
                    }

                    entry.Display.PhoneNumber = string.Join("<br/>", callers.Where(x => x.Show).Select(x => (x.ToInd ? "To: " : "From: ") + (string.IsNullOrWhiteSpace(x.PhoneNumber) ? x.ExtensionNumber : x.PhoneNumber)));
                    entry.Display.Name = string.Join("<br/>", callers.Where(x => x.Show).Select(x => string.IsNullOrWhiteSpace(x.Name) ? "Unknown" : x.Name));

                    //FILES
                    var totalFiles = 0;
                    var totalLogFiles = 0;
                    var totalContentFiles = 0;
                    string filesImages = "";
                    var fileSize1 = 30000;
                    var fileSize2 = 150000;
                    foreach (var f in dbFileRecs.Where(x => x.IndexId == dbRec.IndexId))
                    {
                        if (f.ContentInd)
                        {
                            totalContentFiles++;
                            var size = f.NumberOfBytes < fileSize1 ? "1" : f.NumberOfBytes < fileSize2 ? "2" : "3";
                            filesImages += "<span data-toggle='tooltip' data-placement='left' title='mp3 file'><img src='/images/files-content-" + size + ".svg' data-toggle='modal' data-target='#mp3Modal' data-fileid='" + f.IndexFileId + "' /></span>";
                        }
                        else
                        {
                            totalLogFiles++;
                            filesImages += "<span data-toggle='tooltip' data-placement='left' title='log file'><img src='/images/files-log.svg' data-toggle='modal' data-target='#logModal' data-fileid='" + f.IndexFileId + "' /></span>";
                        }
                        totalFiles++;
                    }
                    entry.Display.Files = filesImages;
                    if (totalFiles == 0)
                    {
                        filesImages += "<span data-toggle='tooltip' data-placement='left' title='No files'></span>";
                        entry.Display.FilesTooltip = "No files";
                    }
                    model.ArchiveEntries.Add(entry);
                }
            }
            if (model.PageNumber > model.TotalPages)
                model.PageNumber = model.TotalPages;
            if (model.PageNumber < model.TotalPages)
            {
                model.Navigation.NextPage = true;
                model.Navigation.LastPage = true;
            }
            if (model.PageNumber > 1)
            {
                model.Navigation.PrevPage = true;
                model.Navigation.FirstPage = true;
            }

        }

        private void getTextArchives(ArchiveSearchModel model)
        {
            if (model.ArchiveEntries == null)
                model.ArchiveEntries = new List<ArchiveSearchModel.ArchiveEntry>();
            if (model.Navigation == null)
                model.Navigation = new ArchiveSearchModel.ArchiveNavigation();
            IEnumerable<IndexRec> dbRecs = null;
            IEnumerable<IndexCallerRec> dbCallerRecs = null;
            IEnumerable<IndexFileAndFileInfoRec> dbFileRecs = null;
            DateTime dateFrom;
            DateTime dateTo;
            DateTime.TryParse(model.DateFrom, out dateFrom);
            DateTime.TryParse(model.DateTo, out dateTo);

            if (!string.IsNullOrWhiteSpace(model.Name) && !string.IsNullOrWhiteSpace(model.Extension))
            {
                using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                {
                    var accountRec = db.Query<AccountRec>("SELECT TOP 1 * FROM T_ACCOUNT WHERE RingCentralId=@ringCentralId ORDER BY AccountId", new { ringCentralId = User.Identity.RingCloneIdentity().RingCentralId }).First();
                    var mainQueryParams = new { ownerId = accountRec.RingCentralOwnerId, type = "sms", dateFrom = dateFrom, dateTo = dateTo, name = model.Name, offset = model.PerPage * (model.PageNumber - 1), perPage = model.PerPage, extension = model.Extension };
                    dbRecs = db.Query<IndexRec>("IndexSearchWithNameAndExtensionAndSubject", mainQueryParams, commandType: CommandType.StoredProcedure);
                    if (dbRecs.Count() == 0)
                        model.TotalRecords = 0;
                    else
                        model.TotalRecords = dbRecs.First().TotalRecs;
                    dbCallerRecs = db.Query<IndexCallerRec>("SELECT * FROM T_INDEXCALLER WHERE IndexId IN @indexRecs", new { indexRecs = dbRecs.Select(x => x.IndexId) });
                    dbFileRecs = db.Query<IndexFileAndFileInfoRec>("SELECT * FROM T_INDEXFILE INNER JOIN T_INDEXFILEINFO ON T_INDEXFILE.IndexFileInfoId = T_INDEXFILEINFO.IndexFileInfoId WHERE IndexId IN @indexRecs", new { indexRecs = dbRecs.Select(x => x.IndexId) });
                }
            }
            else if (!string.IsNullOrWhiteSpace(model.Name))
            {
                using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                {
                    var accountRec = db.Query<AccountRec>("SELECT TOP 1 * FROM T_ACCOUNT WHERE RingCentralId=@ringCentralId ORDER BY AccountId", new { ringCentralId = User.Identity.RingCloneIdentity().RingCentralId }).First();
                    var mainQueryParams = new { ownerId = accountRec.RingCentralOwnerId, type = "sms", dateFrom = dateFrom, dateTo = dateTo, name = model.Name, offset = model.PerPage * (model.PageNumber - 1), perPage = model.PerPage };
                    dbRecs = db.Query<IndexRec>("IndexSearchWithNameAndSubject", mainQueryParams, commandType: CommandType.StoredProcedure);
                    if (dbRecs.Count() == 0)
                        model.TotalRecords = 0;
                    else
                        model.TotalRecords = dbRecs.First().TotalRecs;
                    dbCallerRecs = db.Query<IndexCallerRec>("SELECT * FROM T_INDEXCALLER WHERE IndexId IN @indexRecs", new { indexRecs = dbRecs.Select(x => x.IndexId) });
                    dbFileRecs = db.Query<IndexFileAndFileInfoRec>("SELECT * FROM T_INDEXFILE INNER JOIN T_INDEXFILEINFO ON T_INDEXFILE.IndexFileInfoId = T_INDEXFILEINFO.IndexFileInfoId WHERE IndexId IN @indexRecs", new { indexRecs = dbRecs.Select(x => x.IndexId) });
                }
            }
            else if (!string.IsNullOrWhiteSpace(model.Extension))
            {
                using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                {
                    var accountRec = db.Query<AccountRec>("SELECT TOP 1 * FROM T_ACCOUNT WHERE RingCentralId=@ringCentralId ORDER BY AccountId", new { ringCentralId = User.Identity.RingCloneIdentity().RingCentralId }).First();
                    var mainQueryParams = new { ownerId = accountRec.RingCentralOwnerId, type = "sms", dateFrom = dateFrom, dateTo = dateTo, offset = model.PerPage * (model.PageNumber - 1), perPage = model.PerPage, extension = model.Extension };
                    dbRecs = db.Query<IndexRec>("IndexSearchWithExtension", mainQueryParams, commandType: CommandType.StoredProcedure);
                    if (dbRecs.Count() == 0)
                        model.TotalRecords = 0;
                    else
                        model.TotalRecords = dbRecs.First().TotalRecs;
                    dbCallerRecs = db.Query<IndexCallerRec>("SELECT * FROM T_INDEXCALLER WHERE IndexId IN @indexRecs", new { indexRecs = dbRecs.Select(x => x.IndexId) });
                    dbFileRecs = db.Query<IndexFileAndFileInfoRec>("SELECT * FROM T_INDEXFILE INNER JOIN T_INDEXFILEINFO ON T_INDEXFILE.IndexFileInfoId = T_INDEXFILEINFO.IndexFileInfoId WHERE IndexId IN @indexRecs", new { indexRecs = dbRecs.Select(x => x.IndexId) });
                }
            }
            else
            {
                using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                {
                    var accountRec = db.Query<AccountRec>("SELECT TOP 1 * FROM T_ACCOUNT WHERE RingCentralId=@ringCentralId ORDER BY AccountId", new { ringCentralId = User.Identity.RingCloneIdentity().RingCentralId }).First();
                    var mainQueryParams = new { ownerId = accountRec.RingCentralOwnerId, type = "sms", dateFrom = dateFrom, dateTo = dateTo, offset = model.PerPage * (model.PageNumber - 1), perPage = model.PerPage };
                    dbRecs = db.Query<IndexRec>("IndexSearch", mainQueryParams, commandType: CommandType.StoredProcedure);
                    if (dbRecs.Count() == 0)
                        model.TotalRecords = 0;
                    else
                        model.TotalRecords = dbRecs.First().TotalRecs;
                    dbCallerRecs = db.Query<IndexCallerRec>("SELECT * FROM T_INDEXCALLER WHERE IndexId IN @indexRecs", new { indexRecs = dbRecs.Select(x => x.IndexId) });
                    dbFileRecs = db.Query<IndexFileAndFileInfoRec>("SELECT * FROM T_INDEXFILE INNER JOIN T_INDEXFILEINFO ON T_INDEXFILE.IndexFileInfoId = T_INDEXFILEINFO.IndexFileInfoId WHERE IndexId IN @indexRecs", new { indexRecs = dbRecs.Select(x => x.IndexId) });
                }
            }
            model.TotalPages = (int)Math.Ceiling(((double)model.TotalRecords / (double)model.PerPage));
            if (dbRecs != null && dbRecs.Any())
            {
                foreach (var dbRec in dbRecs)
                {
                    var entry = new ArchiveSearchModel.ArchiveEntry();
                    var callers = dbCallerRecs.Where(x => x.IndexId == dbRec.IndexId).Select(x => new CallerItem() { IndexId = x.IndexId, ExtensionNumber = x.ExtensionNumber, Location = x.Location, Name = x.Name, PhoneNumber = x.PhoneNumber, ToInd = x.ToInd }).ToList();
                    var fromCallers = callers.Where(x => !x.ToInd).ToList();
                    var toCallers = callers.Where(x => x.ToInd).ToList();
                    entry.Id = dbRec.MessageId;
                    entry.Display = new ArchiveSearchModel.ArchiveDisplay();
                    entry.Display.Type = dbRec.Type;
                    entry.Display.Action = dbRec.Action;
                    entry.Display.Date = dbRec.CallTime.ToString();
                    entry.Display.Direction = dbRec.Direction;
                    entry.Display.Date = dbRec.CallTime.ToString("ddd MM/dd/yyyy hh:mm tt");
                    entry.Display.MessageStatus = dbRec.MessageStatus;
                    entry.Display.Subject = dbRec.Subject;
                    var hours = dbRec.Duration / 60 / 60;
                    var minutes = dbRec.Duration / 60;
                    var seconds = dbRec.Duration - (hours * 60 * 60) - (minutes * 60);
                    entry.Display.Length = hours.ToString("0") + ":" + minutes.ToString("00") + ":" + seconds.ToString("00");

                    if (!string.IsNullOrEmpty(dbRec.Direction) && dbRec.Direction.ToLower() == "inbound" && fromCallers.Any())
                    {
                        fromCallers.First().Show = true;
                    }
                    else if (toCallers.Any())
                    {
                        toCallers.First().Show = true;
                    }

                    if (!string.IsNullOrWhiteSpace(model.Name) || !string.IsNullOrWhiteSpace(model.Extension))
                    {
                        if (!string.IsNullOrWhiteSpace(model.Name) && hasMatch(entry.Display.Subject, model.Name))
                        {
                            entry.Display.Subject = applyMatch(entry.Display.Subject, model.Name);
                        }
                        foreach (var caller in callers)
                        {
                            if (!string.IsNullOrWhiteSpace(model.Name))
                            {
                                if (!string.IsNullOrWhiteSpace(caller.Name) && hasMatch(caller.Name, model.Name))
                                {
                                    caller.Name = applyMatch(caller.Name, model.Name);
                                    caller.Show = true;
                                }
                                if (!string.IsNullOrWhiteSpace(caller.PhoneNumber) && hasMatch(caller.PhoneNumber, model.Name))
                                {
                                    caller.PhoneNumber = applyMatch(caller.PhoneNumber, model.Name);
                                    caller.Show = true;
                                }
                                if (!string.IsNullOrWhiteSpace(caller.ExtensionNumber) && hasMatch(caller.ExtensionNumber, model.Name))
                                {
                                    caller.ExtensionNumber = applyMatch(caller.ExtensionNumber, model.Name);
                                    caller.Show = true;
                                }
                            }
                            if (!string.IsNullOrWhiteSpace(model.Extension))
                            {
                                if (!string.IsNullOrWhiteSpace(caller.ExtensionNumber) && hasMatch(caller.ExtensionNumber, model.Extension))
                                {
                                    caller.ExtensionNumber = applyMatch(caller.ExtensionNumber, model.Extension);
                                    caller.Show = true;
                                }
                            }
                        }
                    }
                    foreach (var caller in callers.Where(x => x.Show))
                    {
                        if (!string.IsNullOrWhiteSpace(caller.PhoneNumber))
                            caller.PhoneNumber = formatPhoneNumber(caller.PhoneNumber);
                        if (!string.IsNullOrWhiteSpace(caller.ExtensionNumber))
                            caller.ExtensionNumber = formatPhoneNumber(caller.ExtensionNumber);
                    }

                    entry.Display.PhoneNumber = string.Join("<br/>", callers.Where(x => x.Show).Select(x => (x.ToInd ? "To: " : "From: ") + (string.IsNullOrWhiteSpace(x.PhoneNumber) ? x.ExtensionNumber : x.PhoneNumber)));
                    entry.Display.Name = string.Join("<br/>", callers.Where(x => x.Show).Select(x => string.IsNullOrWhiteSpace(x.Name) ? "Unknown" : x.Name));

                    //FILES
                    var totalFiles = 0;
                    var totalLogFiles = 0;
                    var totalContentFiles = 0;
                    string filesImages = "";
                    var fileSize1 = 30000;
                    var fileSize2 = 150000;
                    foreach (var f in dbFileRecs.Where(x => x.IndexId == dbRec.IndexId))
                    {
                        if (f.ContentInd)
                        {
                            totalContentFiles++;
                            var size = f.NumberOfBytes < fileSize1 ? "1" : f.NumberOfBytes < fileSize2 ? "2" : "3";
                            if (f.Filename.EndsWith("jpg") || f.Filename.EndsWith("gif") || f.NumberOfBytes > 750)
                                filesImages += "<span data-toggle='tooltip' data-placement='left' title='image'><img src='/images/files-image.svg' data-toggle='modal' data-target='#imageModal' data-fileid='" + f.IndexFileId + "' /></span>";
                            else
                                filesImages += "<span data-toggle='tooltip' data-placement='left' title='text message'><img src='/images/files-sms.svg' data-toggle='modal' data-target='#smsModal' data-fileid='" + f.IndexFileId + "' /></span>";
                        }
                        else
                        {
                            totalLogFiles++;
                            filesImages += "<span data-toggle='tooltip' data-placement='left' title='log file'><img src='/images/files-log.svg' data-toggle='modal' data-target='#logModal' data-fileid='" + f.IndexFileId + "' /></span>";
                        }
                        totalFiles++;
                    }
                    entry.Display.Files = filesImages;
                    if (totalFiles == 0)
                    {
                        filesImages += "<span data-toggle='tooltip' data-placement='left' title='No files'></span>";
                        entry.Display.FilesTooltip = "No files";
                    }
                    model.ArchiveEntries.Add(entry);
                }
            }
            if (model.PageNumber > model.TotalPages)
                model.PageNumber = model.TotalPages;
            if (model.PageNumber < model.TotalPages)
            {
                model.Navigation.NextPage = true;
                model.Navigation.LastPage = true;
            }
            if (model.PageNumber > 1)
            {
                model.Navigation.PrevPage = true;
                model.Navigation.FirstPage = true;
            }

        }

        private bool hasMatch(string originalString, string stringToMatch)
        {
            if (originalString.ToLower().Contains(stringToMatch.ToLower()))
            {
                return true;
            }
            return false;
        }
        private string applyMatch(string originalString, string stringToMatch)
        {
            return Regex.Replace(originalString, stringToMatch, "<span class='match'>" + stringToMatch + "</span>", RegexOptions.IgnoreCase);
        }
        private string stripPhoneNumber(string phone)
        {
            return phone.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace("+", "");
        }
        private string formatPhoneNumber(string originalString)
        {
            var totalNumbers = 0;
            foreach (char ch in originalString)
                if (char.IsNumber(ch))
                    totalNumbers++;

            //if (originalString.Length == 11)
            //    originalString = originalString.Substring(1, 10);
            if (totalNumbers == 10)
            {
                var ix = 0;
                var newString = new StringBuilder();
                newString.Append('(');
                foreach (char ch in originalString)
                {
                    if (char.IsNumber(ch))
                    {
                        if (ix == 0)
                        {
                            newString.Append(ch);
                        }
                        else if (ix == 3)
                        {
                            newString.Append(')');
                            newString.Append(' ');
                            newString.Append(ch);
                        }
                        else if (ix == 6)
                        {
                            newString.Append('-');
                            newString.Append(ch);
                        }
                        else
                        {
                            newString.Append(ch);
                        }
                        ix++;
                    }
                    else
                    {
                        newString.Append(ch);
                    }
                }
                originalString = newString.ToString();
            }
            // 441865809044
            else if (totalNumbers == 12)
            {
                var ix = 0;
                var newString = new StringBuilder();
                foreach (char ch in originalString)
                {
                    if (char.IsNumber(ch))
                    {
                        if (ix == 2)
                        {
                            newString.Append(' ');
                            newString.Append('(');
                            newString.Append(ch);
                        }
                        else if (ix == 5)
                        {
                            newString.Append(')');
                            newString.Append(' ');
                            newString.Append(ch);
                        }
                        else if (ix == 8)
                        {
                            newString.Append('-');
                            newString.Append(ch);
                        }
                        else
                        {
                            newString.Append(ch);
                        }
                        ix++;
                    }
                    else
                    {
                        newString.Append(ch);
                    }
                }
                originalString = newString.ToString();
            }
            else if (totalNumbers < 6)
            {
                originalString = "x" + originalString;
            }
            //if (from.Length == 0 && !string.IsNullOrEmpty(fromCallers.First().ExtensionNumber))
            //    from = "x" + fromCallers.First().ExtensionNumber;
            return originalString;
        }

        private DateTime startOfDay(DateTime dateTime)
        {
            return new DateTime(
               dateTime.Year,
               dateTime.Month,
               dateTime.Day,
               0, 0, 0, 0);
        }

        private DateTime endOfDay(DateTime dateTime)
        {
            return new DateTime(
               dateTime.Year,
               dateTime.Month,
               dateTime.Day,
               23, 59, 59, 999);
        }

        public class CallerItem
        {
            public int IndexId;
            public string PhoneNumber;
            public string ExtensionNumber;
            public string Name;
            public string Location;
            public bool ToInd;
            public bool Show;
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

        #region Database Models
        private class AccountRec
        {
            public int AccountId;
            public string RingCentralId;
            public string RingCentralOwnerId;
            public bool DeletedInd;
            public bool ActiveInd;
            public bool CancelledInd;
        }
        public class IndexRec
        {
            public int IndexId;
            public int TicketId;
            public string Type;
            public string CallId;
            public string MessageId;
            public int IndexRawDataId;
            public int IndexMessageId;
            public bool CompleteInd;
            public bool DeletedInd;
            public string Direction;
            public string Action;
            public string Result;
            public int Duration;
            public DateTime CallTime;
            public int TotalRecs;
            public string Subject;
            public string CoverPageText;
            public string MessageStatus;
            public string FaxPageCount;
        }
        public class IndexRawDataRec
        {
            public int IndexRawDataId;
            public string RawData;
        }
        public class IndexCallerRec
        {
            public int IndexCallerId;
            public int IndexId;
            public string PhoneNumber;
            public string ExtensionNumber;
            public string Name;
            public string Location;
            public bool ToInd;
            public bool DeletedInd;
        }
        public class IndexMessageRec
        {
            public int IndexMessageId;
            public string Subject;
            public string CoverPageText;
            public string MessageStatus;
            public string FaxPageCount;
        }
        public class IndexFileRec
        {
            public int IndexFileId;
            public int IndexId;
            public bool LogInd;
            public bool ContentInd;
            public int IndexFileInfoId;
            public string Destination;
            public int DestinationAccountId;
            public string ContentUri;
            public int NumberOfBytes;
            public bool DeletedInd;
        }
        public class IndexFileInfoRec
        {
            public int IndexFileInfoId;
            public string Filename;
            public string FileId;
            public string Folder;
            public string BucketName;
            public string Prefix;
        }
        public class IndexFileAndFileInfoRec
        {
            public int IndexFileId;
            public int IndexId;
            public bool LogInd;
            public bool ContentInd;
            public int IndexFileInfoId;
            public string Destination;
            public int DestinationAccountId;
            public string ContentUri;
            public int NumberOfBytes;
            public bool DeletedInd;
            public string Filename;
            public string FileId;
            public string Folder;
            public string BucketName;
            public string Prefix;
        }
        #endregion
    }
}
