using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Criterion;
using RingClone.Portal.Filters;
using RingClone.Portal.Helpers;
using RingClone.Portal.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace RingClone.Portal.Controllers
{
    [Register]
	[Authorize]
	public class AdHocTransferController : Controller
    {
        [NotCancelled]
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Log");
        }
        [NotCancelled]
        [HttpPost]
        public ActionResult Index(AdHocTransferModel postedData)
        {
			var model = (AdHocTransferModel)Session["AdHocTransferModel"];
			if (model == null)
			{
				model = new AdHocTransferModel();
			}
			if (postedData == null || postedData.LogEntries == null || !postedData.LogEntries.Any())
            {
                return View("SelectedNone");
            }
            else
            {
				model.LogEntries = postedData.LogEntries;
                model.Type = postedData.Type;
                //NOT YET
                //if (model.LogEntries.Count > model.AvailableTransfersLeft)
                //{
                //	return View("SelectedTooMany", model);
                //}
                Session["AdHocTransferModel"] = model;
                return RedirectToAction("SelectDestination", model);
            }
        }
        public ActionResult SelectDestination()
        {
            return View();
        }
        public ActionResult UseBox()
        {
            var model = (AdHocTransferModel)Session["AdHocTransferModel"];
            if (model == null || model.LogEntries == null)
                return RedirectToAction("Index");
            model.Destination = AdHocTransferModel.DestinationType.Box;

            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    var accountCrit = session.CreateCriteria<AccountRec>();
                    accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
                    var accounts = accountCrit.List<AccountRec>();
                    if (accounts.Any())
                    {
                        var account = accounts.First();
                        var boxAccountRec = session.CreateCriteria<BoxAccountRec>();
                        boxAccountRec.Add(Expression.Eq("AccountId", account.AccountId));
                        var boxAccounts = boxAccountRec.List<BoxAccountRec>();
                        if (boxAccounts.Any())
                        {
                            var boxAccount = boxAccounts.First();
                            var authorizeChecker = new Box.AuthChecker(User.Identity.RingCloneIdentity().RingCentralId, boxAccount.BoxAccountId);
                            authorizeChecker.Execute();
                            if (authorizeChecker.IsAuthenticated)
                            {
                                var boxTokenCrit = session.CreateCriteria<BoxTokenRec>();
                                boxTokenCrit.Add(Expression.Eq("BoxTokenId", boxAccount.BoxTokenId));
                                var boxTokens = boxTokenCrit.List<BoxTokenRec>();
                                if (boxTokens.Any())
                                {
                                    var boxToken = boxTokens.First();
                                    model.BoxEmail = boxToken.Email;
                                    return RedirectToAction("ChooseBoxFolder", model);
                                }
                            }
                        }
                    }
                }
            }

            JavaScriptSerializer jss = new JavaScriptSerializer();
            var serializedState = jss.Serialize(createBoxStateModel());
            var encryptedState = Helpers.EncryptedString.DatabaseEncrypt(serializedState);
            var url = AppConfig.Box.AuthUri;
            var requestParams = new NameValueCollection();
            requestParams.Add("response_type", "code");
            requestParams.Add("client_id", AppConfig.Box.ClientId);
            requestParams.Add("redirect_uri", AppConfig.Box.RedirectUri);
            requestParams.Add("state", encryptedState);
            var array = (from key in requestParams.AllKeys
                         from value in requestParams.GetValues(key)
                         select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
                .ToArray();
            var queryString = string.Join("&", array);
            model.BoxUrl = url + "?" + queryString;
            Session.Add("state", encryptedState);
            return View(model);

        }
        public ActionResult UseGoogle()
        {
            var model = (AdHocTransferModel)Session["AdHocTransferModel"];
            if (model == null || model.LogEntries == null)
                return RedirectToAction("Index");
            model.Destination = AdHocTransferModel.DestinationType.Google;

            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    var accountCrit = session.CreateCriteria<AccountRec>();
                    accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
                    var accounts = accountCrit.List<AccountRec>();
                    if (accounts.Any())
                    {
                        var account = accounts.First();
                        var googleAccountRec = session.CreateCriteria<GoogleAccountRec>();
                        googleAccountRec.Add(Expression.Eq("AccountId", account.AccountId));
                        var googleAccounts = googleAccountRec.List<GoogleAccountRec>();
                        if (googleAccounts.Any())
                        {
                            var googleAccount = googleAccounts.First();
                            var authorizeChecker = new GoogleActions.AuthChecker(User.Identity.RingCloneIdentity().RingCentralId, googleAccount.GoogleAccountId);
                            authorizeChecker.Execute();
                            if (authorizeChecker.IsAuthenticated)
                            {
                                var googleTokenCrit = session.CreateCriteria<GoogleTokenRec>();
                                googleTokenCrit.Add(Expression.Eq("GoogleTokenId", googleAccount.GoogleTokenId));
                                var googleTokens = googleTokenCrit.List<GoogleTokenRec>();
                                if (googleTokens.Any())
                                {
                                    var googleToken = googleTokens.First();
                                    model.GoogleEmail = googleToken.Email;
                                    return RedirectToAction("ChooseGoogleFolder", model);
                                }
                            }
                        }
                    }
                }
            }

			JavaScriptSerializer jss = new JavaScriptSerializer();
			var serializedState = jss.Serialize(createGoogleStateModel());
			var encryptedState = Helpers.EncryptedString.DatabaseEncrypt(serializedState);
			var url = AppConfig.Google.AuthUri;
			var requestParams = new NameValueCollection();
			requestParams.Add("response_type", "code");
			requestParams.Add("client_id", AppConfig.Google.ClientId);
			requestParams.Add("redirect_uri", AppConfig.Google.RedirectUri);
			requestParams.Add("state", encryptedState);
			requestParams.Add("access_type", "offline");
			requestParams.Add("prompt", "consent");
			requestParams.Add("scope", "https://www.googleapis.com/auth/drive https://www.googleapis.com/auth/userinfo.email");
			var array = (from key in requestParams.AllKeys
						 from value in requestParams.GetValues(key)
						 select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
				.ToArray();
			var queryString = string.Join("&", array);
			model.GoogleUrl = url + "?" + queryString;
			Session.Add("state", encryptedState);
			return View(model);

        }
		public ActionResult ChooseBoxFolder()
		{
			var model = (AdHocTransferModel)Session["AdHocTransferModel"];
			if (model == null || model.LogEntries == null)
				return RedirectToAction("Index");
			model.Destination = AdHocTransferModel.DestinationType.Box;

			using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenSession())
				{
					var accountCrit = session.CreateCriteria<AccountRec>();
					accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
					var accounts = accountCrit.List<AccountRec>();
					if (accounts.Any())
					{
						var account = accounts.First();
						var boxAccountRec = session.CreateCriteria<BoxAccountRec>();
						boxAccountRec.Add(Expression.Eq("AccountId", account.AccountId));
						var boxAccounts = boxAccountRec.List<BoxAccountRec>();
						if (boxAccounts.Any())
						{
							var boxAccount = boxAccounts.First();
							model.BoxAccountId = boxAccount.BoxAccountId;
							var boxTokenCrit = session.CreateCriteria<BoxTokenRec>();
							boxTokenCrit.Add(Expression.Eq("BoxTokenId", boxAccount.BoxTokenId));
							var boxTokens = boxTokenCrit.List<BoxTokenRec>();
							if (boxTokens.Any())
							{
								var boxToken = boxTokens.First();
								model.BoxTokenId = boxToken.BoxTokenId;
								return View(model);
							}
						}
					}
				}
			}
			return View();
		}
		public ActionResult ChooseGoogleFolder()
		{
			var model = (AdHocTransferModel)Session["AdHocTransferModel"];
			if (model == null || model.LogEntries == null)
				return RedirectToAction("Index");
			model.Destination = AdHocTransferModel.DestinationType.Google;

			using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
			{
				using (var session = sessionFactory.OpenSession())
				{
					var accountCrit = session.CreateCriteria<AccountRec>();
					accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
					var accounts = accountCrit.List<AccountRec>();
					if (accounts.Any())
					{
						var account = accounts.First();
						var googleAccountRec = session.CreateCriteria<GoogleAccountRec>();
						googleAccountRec.Add(Expression.Eq("AccountId", account.AccountId));
						var googleAccounts = googleAccountRec.List<GoogleAccountRec>();
						if (googleAccounts.Any())
						{
							var googleAccount = googleAccounts.First();
							model.GoogleAccountId = googleAccount.GoogleAccountId;
							var googleTokenCrit = session.CreateCriteria<GoogleTokenRec>();
							googleTokenCrit.Add(Expression.Eq("GoogleTokenId", googleAccount.GoogleTokenId));
							var googleTokens = googleTokenCrit.List<GoogleTokenRec>();
							if (googleTokens.Any())
							{
								var googleToken = googleTokens.First();
								model.GoogleTokenId = googleToken.GoogleTokenId;
								return View(model);
							}
						}
					}
				}
			}
			return View();
		}
        public ActionResult ChooseBoxFolderClick(string FolderId, string FolderName, string FolderPath)
        {
            var model = (AdHocTransferModel)Session["AdHocTransferModel"];
            if (model == null || model.LogEntries == null)
                return RedirectToAction("Index");
            model.DestinationFolderId = FolderId;
            model.DestinationFolderName = FolderName;
            model.DestinationFolderPath = FolderPath;
            return RedirectToAction("Confirm", model);
        }
        public ActionResult ChooseGoogleFolderClick(string FolderId, string FolderName, string FolderPath)
        {
            var model = (AdHocTransferModel)Session["AdHocTransferModel"];
            if (model == null || model.LogEntries == null)
                return RedirectToAction("Index");
            model.DestinationFolderId = FolderId;
            model.DestinationFolderName = FolderName;
            model.DestinationFolderPath = FolderPath;
            return RedirectToAction("Confirm", model);
        }
        public ActionResult UseAmazon()
        {
            var model = (AdHocTransferModel)Session["AdHocTransferModel"];
            if (model == null || model.LogEntries == null)
                return RedirectToAction("Index");
            model.Destination = AdHocTransferModel.DestinationType.Amazon;

            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    var accountCrit = session.CreateCriteria<AccountRec>();
                    accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
                    var accounts = accountCrit.List<AccountRec>();
                    if (accounts.Any())
                    {
                        var account = accounts.First();
                        var amazonAccountRec = session.CreateCriteria<AmazonAccountRec>();
                        amazonAccountRec.Add(Expression.Eq("AccountId", account.AccountId));
                        var amazonAccounts = amazonAccountRec.List<AmazonAccountRec>();
                        if (amazonAccounts.Any())
                        {
                            var amazonAccount = amazonAccounts.First();
                            var authorizeChecker = new AmazonActions.AuthChecker(User.Identity.RingCloneIdentity().RingCentralId, amazonAccount.AmazonAccountId);
                            authorizeChecker.Execute();
                            if (authorizeChecker.IsAuthenticated)
                            {
                                var amazonTokenCrit = session.CreateCriteria<AmazonUserRec>();
                                amazonTokenCrit.Add(Expression.Eq("AmazonUserId", amazonAccount.AmazonUserId));
                                var amazonTokens = amazonTokenCrit.List<AmazonUserRec>();
                                if (amazonTokens.Any())
                                {
                                    var amazonToken = amazonTokens.First();
                                    model.AmazonDisplayName = amazonToken.DisplayName;
                                    model.AmazonAccountId = amazonAccount.AmazonAccountId;
                                    return RedirectToAction("ChooseAmazonFolder", model);
                                }
                            }
                        }
                    }
                }
            }
            return View(model);
        }
        [HttpPost]
        public ActionResult UseAmazon(string AccessKeyId, string SecretAccessKey)
        {
            var model = (AdHocTransferModel)Session["AdHocTransferModel"];
            if (model == null || model.LogEntries == null)
                return RedirectToAction("Index");
            model.AmazonAccessKeyId = AccessKeyId;
            model.AmazonSecretAccessKey = SecretAccessKey;
            return RedirectToAction("ValidateAmazon");
        }
        public ActionResult ValidateAmazon()
        {
            var model = (AdHocTransferModel)Session["AdHocTransferModel"];
            if (model == null || model.LogEntries == null)
                return RedirectToAction("Index");
            return View(model);
        }
        [HttpPost]
        public ActionResult ValidateAmazon(bool validated, string displayName)
        {
            var model = (AdHocTransferModel)Session["AdHocTransferModel"];
            if (model == null || model.LogEntries == null)
                return RedirectToAction("Index");
            if (validated)
            {
                model.AmazonDisplayName = displayName;
                saveAmazonUser(model);
                return RedirectToAction("ChooseAmazonFolder", model);
            }
            else
            {
                return RedirectToAction("UseAmazon");
            }
        }
        public ActionResult ChooseAmazonFolder()
        {
            var model = (AdHocTransferModel)Session["AdHocTransferModel"];
            if (model == null || model.LogEntries == null)
                return RedirectToAction("Index");
            return View(model);
        }
        public ActionResult ChooseAmazonFolderClick(string OwnerName, string BucketName, string Key, string FolderName)
        {
            var model = (AdHocTransferModel)Session["AdHocTransferModel"];
            if (model == null || model.LogEntries == null)
                return RedirectToAction("Index");
            model.DestinationBucketName = BucketName;
            model.DestinationPrefix = Key;
            model.DestinationFolderName = FolderName;
            return RedirectToAction("Confirm");
        }
        public ActionResult Confirm()
        {
            var model = (AdHocTransferModel)Session["AdHocTransferModel"];
            if (model == null || model.LogEntries == null)
                return RedirectToAction("Index");

            //LOAD ALL RAW DATA INTO THE MODEL
            model.LogEntriesWithData = new List<AdHocTransferModel.LogEntryData>();
            var jss = new JavaScriptSerializer();
            var memoryCache = MemoryCache.Default;


            //ANALYZE ALL RECORDINGS, VOICEMAILS AND ATTACHMENTS
            if (model.Type == "voice")
            {
                var totalLogEntries = 0;
                var totalVoicemails = 0;
                var totalRecordings = 0;
                var voicemailList = new List<string>();
                var recordingList = new List<string>();
                foreach (var id in model.LogEntries)
                {
                    var rec = (RingCentral.CallLog.CallLogData.Record)memoryCache.Get(id);
                    if (rec == null)
                    {
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        model.LogEntriesWithData.Add(new AdHocTransferModel.LogEntryData()
                        {
                            Id = id,
                            RawData = jss.Serialize(rec),
                            SaveAsFilename = generateCoreFileName(rec)
                        });
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
                        totalLogEntries++;
                    }
                }
                model.ArchiveDescription = totalLogEntries + " call log " + (totalLogEntries > 1 ? "entries" : "entry");
                if (totalRecordings > 0 && totalVoicemails == 0)
                    model.ArchiveDescription += " with " + totalRecordings + " recorded call" + (totalRecordings != 1 ? "s" : "");
                else if (totalRecordings == 0 && totalVoicemails > 0)
                    model.ArchiveDescription += " with " + totalVoicemails + " voicemail" + (totalVoicemails != 1 ? "s" : "");
                else if (totalRecordings > 0 && totalVoicemails > 0)
                    model.ArchiveDescription += " with " + totalRecordings + " recorded call" + (totalRecordings != 1 ? "s" : "") + " and " + totalVoicemails + " voicemail" + (totalVoicemails > 1 ? "s" : "");
                model.ArchiveDescription += ".";

            }
            else if (model.Type == "fax" || model.Type == "sms") {
                var totalLogEntries = 0;
                var totalAttachments = 0;
                var attachmentList = new List<string>();
                foreach (var id in model.LogEntries)
                {
                    var rec = (RingCentral.MessageStore.MessageStoreData.Record)memoryCache.Get(id);
                    if (rec == null)
                    {
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        model.LogEntriesWithData.Add(new AdHocTransferModel.LogEntryData()
                        {
                            Id = id,
                            RawData = jss.Serialize(rec),
                            SaveAsFilename = generateCoreFileName(rec)
                        });
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
                        totalLogEntries++;
                    }
                }
                model.ArchiveDescription = totalLogEntries + " " + model.Type + " log " + (totalLogEntries > 1 ? "entries" : "entry");
                model.ArchiveDescription += " with " + totalAttachments + " attachment" + (totalAttachments != 1 ? "s" : "");
                model.ArchiveDescription += ".";
            }

            return View(model);
        }
        public ActionResult Execute()
        {
            var model = (AdHocTransferModel)Session["AdHocTransferModel"];
            if (model == null || model.LogEntries == null)
                return RedirectToAction("Index");
            
            if (model.TransferBatchId < 1)
			{
				using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
				{
					using (var session = sessionFactory.OpenSession())
					{
						var accountCrit = session.CreateCriteria<AccountRec>();
						accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
						var accounts = accountCrit.List<AccountRec>();
						if (accounts.Any())
						{
							var account = accounts.First();
							var max = session.CreateCriteria<TransferBatchRec>()
								.Add(Expression.Eq("AccountId", account.AccountId))
								.SetProjection(Projections.Max("LogNumber"))
								.UniqueResult<int>();
							var nextLogNumber = max + 1;
							using (var transaction = session.BeginTransaction())
							{
								var transferBatch = new TransferBatchRec()
								{
									AccountId = account.AccountId,
									CreateDate = DateTime.Now.ToUniversalTime(),
                                    QueuedInd = true,
									TransferRuleId = 0,
									LogNumber = nextLogNumber
								};
								session.Save(transferBatch);
								foreach (var rec in model.LogEntriesWithData)
								{

                                    var ticket = new TicketRec()
                                    {
                                        CreateDate = DateTime.Now.ToUniversalTime(),
                                        Destination = serializeDestination(model.Destination),
                                        DestinationBoxAccountId = model.BoxAccountId,
                                        DestinationGoogleAccountId = model.GoogleAccountId,
                                        DestinationFolderId = model.DestinationFolderId,
                                        DestinationFolderLabel = model.Destination == AdHocTransferModel.DestinationType.Amazon ? model.AmazonDisplayName + "/" + model.DestinationBucketName + "/" + model.DestinationPrefix : model.DestinationFolderPath + "\\" + model.DestinationFolderName,
                                        DestinationAmazonAccountId = model.AmazonAccountId,
                                        DestinationBucketName = model.DestinationBucketName,
                                        DestinationPrefix = model.DestinationPrefix,
                                        CallId = (model.Type == "voice" ? rec.Id : null),
                                        MessageId = (model.Type == "fax" || model.Type == "sms" ? rec.Id : null),
                                        InitiatedBy = "User",
                                        Type = model.Type,
                                        LogInd = true,
                                        ContentInd = true,
                                        SaveAsFilename = rec.SaveAsFilename,
										TransferBatchId = transferBatch.TransferBatchId
									};
									session.Save(ticket);
                                    var ticketRawData = new TicketRawDataRec()
                                    {
                                        TicketId = ticket.TicketId,
                                        TransferBatchId = transferBatch.TransferBatchId,
                                        RawData = rec.RawData,
                                        DeletedInd = false
                                    };
                                    session.Save(ticketRawData);
								}
								transaction.Commit();
								model.TransferBatchId = transferBatch.TransferBatchId;
							}
						}
					}
				}
			}
            var transferBatchId = model.TransferBatchId;
            if (Session["AdHocTransferModel"] != null)
            {
                Session.Remove("AdHocTransferModel");
            }
            return RedirectToAction("Status", "TransferBatch", new { id = transferBatchId });
        }

        public ActionResult BoxAuthenticated(AdHocTransferModel model)
        {
            if (model.BoxTokenId > 0)
            {
                using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
                {
                    using (var session = sessionFactory.OpenSession())
                    {
                        var accountCrit = session.CreateCriteria<AccountRec>();
                        accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
                        var accounts = accountCrit.List<AccountRec>();
                        if (accounts.Any())
                        {
                            var account = accounts.First();
                            var boxAccountCrit = session.CreateCriteria<BoxAccountRec>();
                            boxAccountCrit.Add(Expression.Eq("AccountId", account.AccountId));
                            var boxAccounts = boxAccountCrit.List<BoxAccountRec>();
                            if (boxAccounts.Any())
                            {
                                using (var transaction = session.BeginTransaction())
                                {
                                    var boxAccount = boxAccounts.First();
                                    boxAccount.BoxTokenId = model.BoxTokenId;
                                    session.SaveOrUpdate(boxAccount);
                                    transaction.Commit();
                                    return RedirectToAction("ChooseBoxFolder");
                                }
                            }
                            else
                            {
                                using (var transaction = session.BeginTransaction())
                                {
                                    var boxAccount = new BoxAccountRec();
                                    boxAccount.AccountId = account.AccountId;
                                    boxAccount.BoxTokenId = model.BoxTokenId;
                                    session.SaveOrUpdate(boxAccount);
                                    transaction.Commit();
                                    return RedirectToAction("ChooseBoxFolder");
                                }
                            }
                        }
                    }
                }
            }
            throw new WebException("Unable to authenticate");
        }

        public ActionResult GoogleAuthenticated(AdHocTransferModel model)
        {
            if (model.GoogleTokenId > 0)
            {
                using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
                {
                    using (var session = sessionFactory.OpenSession())
                    {
                        var accountCrit = session.CreateCriteria<AccountRec>();
                        accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
                        var accounts = accountCrit.List<AccountRec>();
                        if (accounts.Any())
                        {
                            var account = accounts.First();
                            var googleAccountCrit = session.CreateCriteria<GoogleAccountRec>();
                            googleAccountCrit.Add(Expression.Eq("AccountId", account.AccountId));
                            var googleAccounts = googleAccountCrit.List<GoogleAccountRec>();
                            if (googleAccounts.Any())
                            {
                                using (var transaction = session.BeginTransaction())
                                {
                                    var googleAccount = googleAccounts.First();
                                    googleAccount.GoogleTokenId = model.GoogleTokenId;
                                    session.SaveOrUpdate(googleAccount);
                                    transaction.Commit();
                                    return RedirectToAction("ChooseGoogleFolder");
                                }
                            }
                            else
                            {
                                using (var transaction = session.BeginTransaction())
                                {
                                    var googleAccount = new GoogleAccountRec();
                                    googleAccount.AccountId = account.AccountId;
                                    googleAccount.GoogleTokenId = model.GoogleTokenId;
                                    session.SaveOrUpdate(googleAccount);
                                    transaction.Commit();
                                    return RedirectToAction("ChooseGoogleFolder");
                                }
                            }
                        }
                    }
                }
            }
            throw new WebException("Unable to authenticate");
        }

        private BoxStateModel createBoxStateModel()
        {
            return new BoxStateModel()
            {
                AccountType = "box",
                ControllerName = "AdHocTransfer",
                User = User.Identity.RingCloneIdentity().RingCentralId
            };
        }
        private GoogleStateModel createGoogleStateModel()
        {
            return new GoogleStateModel()
            {
                AccountType = "google",
                ControllerName = "AdHocTransfer",
                User = User.Identity.RingCloneIdentity().RingCentralId
            };
        }

        private bool validateAmazonKeys(AdHocTransferModel model)
        {
            var response = AmazonActions.Helpers.Validate(model.AmazonAccessKeyId, model.AmazonSecretAccessKey);
            if (response.Validated)
            {
                model.AmazonDisplayName = response.DisplayName;
                return true;
            }
            return false;
        }
        private void saveAmazonUser(AdHocTransferModel model)
        {
            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    var accountCrit = session.CreateCriteria<AccountRec>();
                    accountCrit.Add(Expression.Eq("RingCentralId", User.Identity.RingCloneIdentity().RingCentralId));
                    var accounts = accountCrit.List<AccountRec>();
                    if (accounts.Any())
                    {
                        using (var transaction = session.BeginTransaction())
                        {
                            AmazonAccountRec amazonAccount;
                            var account = accounts.First();
                            var amazonCrit = session.CreateCriteria<AmazonAccountRec>();
                            amazonCrit.Add(Expression.Eq("AccountId", account.AccountId));
                            var amazonAccounts = amazonCrit.List<AmazonAccountRec>();
                            if (amazonAccounts.Any())
                            {
                                amazonAccount = amazonAccounts.First();
                            }
                            else
                            {
                                amazonAccount = new AmazonAccountRec();
                                amazonAccount.AccountId = account.AccountId;
                                amazonAccount.ActiveInd = true;
                                amazonAccount.AmazonAccountName = "";
                                session.Save(amazonAccount);
                            }
                            if (amazonAccount.AmazonUserId == 0)
                            {
                                var amazonUser = new AmazonUserRec();
                                amazonUser.AccessKeyId = model.AmazonAccessKeyId;
                                amazonUser.SecretAccessKey = model.AmazonSecretAccessKey;
                                amazonUser.DisplayName = model.AmazonDisplayName;
                                session.Save(amazonUser);
                                amazonAccount.AmazonUserId = amazonUser.AmazonUserId;
                                session.Save(amazonAccount);
                            }
                            else
                            {
                                var amazonUserCrit = session.CreateCriteria<AmazonUserRec>();
                                amazonUserCrit.Add(Expression.Eq("AmazonUserId", amazonAccount.AmazonUserId));
                                var amazonUsers = amazonCrit.List<AmazonUserRec>();
                                if (amazonUsers.Any())
                                {
                                    var amazonUser = amazonUsers.First();
                                    amazonUser.AccessKeyId = model.AmazonAccessKeyId;
                                    amazonUser.SecretAccessKey = model.AmazonSecretAccessKey;
                                    amazonUser.DisplayName = model.AmazonDisplayName;
                                    session.Save(amazonUser);
                                }
                                else
                                {
                                    var amazonUser = new AmazonUserRec();
                                    amazonUser.AccessKeyId = model.AmazonAccessKeyId;
                                    amazonUser.SecretAccessKey = model.AmazonSecretAccessKey;
                                    amazonUser.DisplayName = model.AmazonDisplayName;
                                    session.Save(amazonUser);
                                    amazonAccount.AmazonUserId = amazonUser.AmazonUserId;
                                    session.Save(amazonAccount);
                                }

                            }
                            transaction.Commit();
                            model.AmazonAccountId = amazonAccount.AmazonAccountId;
                        }
                    }
                }
            }
        }
        private string serializeDestination(AdHocTransferModel.DestinationType type)
        {
            if (type == AdHocTransferModel.DestinationType.Google)
                return "google";
            if (type == AdHocTransferModel.DestinationType.Box)
                return "box";
            if (type == AdHocTransferModel.DestinationType.Amazon)
                return "amazon";
            if (type == AdHocTransferModel.DestinationType.Ftp)
                return "ftp";
            return "error";
        }
        private static string generateCoreFileName(RingCentral.CallLog.CallLogData.Record rec)
        {
            var fileName = "";

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
            if (string.IsNullOrWhiteSpace(from))
                from = "Unknown";
            if (string.IsNullOrWhiteSpace(to))
                to = "Unknown";
            from = from.Replace(" ", "");
            to = to.Replace(" ", "");

            DateTime time;
            fileName += (DateTime.TryParse(rec.startTime, out time) ? time : DateTime.Now.ToUniversalTime()).ToString("yyyyMMdd_HHmm");
            fileName += ("_" + from + "_" + to + "_" + rec.direction + "_" + rec.result).Replace(" ", "-");

            return fileName;
        }
        private static string generateCoreFileName(RingCentral.MessageStore.MessageStoreData.Record rec)
        {
            var fileName = "";

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
                    to = "MultipleRecipients";
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
            if (string.IsNullOrWhiteSpace(from))
                from = "Unknown";
            if (string.IsNullOrWhiteSpace(to))
                to = "Unknown";
            from = from.Replace(" ", "");
            to = to.Replace(" ", "");

            DateTime time;
            fileName += (DateTime.TryParse(rec.creationTime, out time) ? time : DateTime.Now.ToUniversalTime()).ToString("yyyyMMdd_HHmm");
            fileName += ("_" + from + "_" + to + "_" + rec.direction + "_" + rec.messageStatus).Replace(" ", "-");

            return fileName;
        }



        #region Database Models

        private class BoxStateModel
        {
            public string User { get; set; }
            public string ControllerName { get; set; }
            public string AccountType { get; set; }
        }
        private class GoogleStateModel
        {
            public string User { get; set; }
            public string ControllerName { get; set; }
            public string AccountType { get; set; }
        }


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
        private class BoxAccountRec
        {
            public virtual int BoxAccountId { get; set; }
            public virtual int AccountId { get; set; }
            public virtual int BoxTokenId { get; set; }
        }
        private class BoxAccountRecMap : ClassMap<BoxAccountRec>
        {
            public BoxAccountRecMap()
            {
                Table("T_BOXACCOUNT");
                Id(x => x.BoxAccountId).Column("BoxAccountId");
                Map(x => x.AccountId);
                Map(x => x.BoxTokenId);
            }
        }
        private class BoxTokenRec
        {
            public virtual int BoxTokenId { get; set; }
            public virtual string AccessToken { get; set; }
            public virtual string ExpiresIn { get; set; }
            public virtual string TokenType { get; set; }
            public virtual string RefreshToken { get; set; }
            public virtual DateTime LastRefreshedOn { get; set; }
            public virtual bool DeletedInd { get; set; }
            public virtual string Email { get; set; }
        }
        private class BoxTokenRecMap : ClassMap<BoxTokenRec>
        {
            public BoxTokenRecMap()
            {
                Table("T_BOXTOKEN");
                Id(x => x.BoxTokenId);
                Map(x => x.AccessToken);
                Map(x => x.ExpiresIn);
                Map(x => x.TokenType);
                Map(x => x.RefreshToken);
                Map(x => x.DeletedInd);
                Map(x => x.LastRefreshedOn);
                Map(x => x.Email);
            }
        }
        private class GoogleAccountRec
        {
            public virtual int GoogleAccountId { get; set; }
            public virtual int AccountId { get; set; }
            public virtual int GoogleTokenId { get; set; }
        }
        private class GoogleAccountRecMap : ClassMap<GoogleAccountRec>
        {
            public GoogleAccountRecMap()
            {
                Table("T_GOOGLEACCOUNT");
                Id(x => x.GoogleAccountId).Column("GoogleAccountId");
                Map(x => x.AccountId);
                Map(x => x.GoogleTokenId);
            }
        }
        private class GoogleTokenRec
        {
            public virtual int GoogleTokenId { get; set; }
            public virtual string AccessToken { get; set; }
            public virtual string ExpiresIn { get; set; }
            public virtual string TokenType { get; set; }
            public virtual string RefreshToken { get; set; }
            public virtual DateTime LastRefreshedOn { get; set; }
            public virtual bool DeletedInd { get; set; }
            public virtual string Email { get; set; }
        }
        private class GoogleTokenRecMap : ClassMap<GoogleTokenRec>
        {
            public GoogleTokenRecMap()
            {
                Table("T_GOOGLETOKEN");
                Id(x => x.GoogleTokenId);
                Map(x => x.AccessToken);
                Map(x => x.ExpiresIn);
                Map(x => x.TokenType);
                Map(x => x.RefreshToken);
                Map(x => x.DeletedInd);
                Map(x => x.LastRefreshedOn);
                Map(x => x.Email);
            }
        }
        private class TransferBatchRec
        {
            public virtual int TransferBatchId { get; set; }
            public virtual int TransferRuleId { get; set; }
            public virtual int AccountId { get; set; }
            public virtual bool QueuedInd { get; set; }
            public virtual DateTime CreateDate { get; set; }
			public virtual int LogNumber { get; set; }
		}
        private class TransferBatchRecMap : ClassMap<TransferBatchRec>
        {
            public TransferBatchRecMap()
            {
                Table("T_TRANSFERBATCH");
                Id(x => x.TransferBatchId).Column("TransferBatchId");
                Map(x => x.TransferRuleId);
                Map(x => x.AccountId);
                Map(x => x.QueuedInd);
				Map(x => x.CreateDate);
				Map(x => x.LogNumber);
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
            public virtual int DestinationAmazonAccountId { get; set; }
            public virtual string DestinationBucketName { get; set; }
            public virtual string DestinationPrefix { get; set; }
            public virtual string CallId { get; set; }
            public virtual string MessageId { get; set; }
            public virtual string Type { get; set; }
            public virtual bool LogInd { get; set; }
            public virtual bool ContentInd { get; set; }
            public virtual string SaveAsFilename { get; set; }
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
                Map(x => x.DestinationAmazonAccountId);
                Map(x => x.DestinationBucketName);
                Map(x => x.DestinationPrefix);
				Map(x => x.CallId);
                Map(x => x.MessageId);
                Map(x => x.Type);
                Map(x => x.LogInd);
                Map(x => x.ContentInd);
                Map(x => x.SaveAsFilename);
                Map(x => x.ProcessingInd);
            }
		}
        private class TicketRawDataRec
        {
            public virtual int TicketRawDataId { get; set; }
            public virtual int TicketId { get; set; }
            public virtual int TransferBatchId { get; set; }
            public virtual string RawData { get; set; }
            public virtual bool DeletedInd { get; set; }
        }
        private class TicketRawDataRecMap : ClassMap<TicketRawDataRec>
        {
            public TicketRawDataRecMap()
            {
                Table("T_TICKETRAWDATA");
                Id(x => x.TicketRawDataId).Column("TicketRawDataId");
                Map(x => x.TicketId);
                Map(x => x.TransferBatchId);
                Map(x => x.RawData).Length(Int32.MaxValue);
                Map(x => x.DeletedInd);
            }
        }

        public class AmazonAccountRec
            {
                public virtual int AmazonAccountId { get; set; }
                public virtual int AmazonUserId { get; set; }
                public virtual int AccountId { get; set; }
                public virtual string AmazonAccountName { get; set; }
                public virtual bool DeletedInd { get; set; }
                public virtual bool ActiveInd { get; set; }
            }
            private class AmazonAccountRecMap : ClassMap<AmazonAccountRec>
            {
                public AmazonAccountRecMap()
                {
                    Table("T_AMAZONACCOUNT");
                    Id(x => x.AmazonAccountId);
                    Map(x => x.AmazonUserId);
                    Map(x => x.AmazonAccountName);
                    Map(x => x.AccountId);
                    Map(x => x.DeletedInd);
                    Map(x => x.ActiveInd);
                }
            }
            public class AmazonUserRec
            {
                public virtual int AmazonUserId { get; set; }
                public virtual string AccessKeyId { get; set; }
                public virtual string SecretAccessKey { get; set; }
                public virtual string DisplayName { get; set; }
                public virtual bool DeletedInd { get; set; }
            }
            private class AmazonUserRecMap : ClassMap<AmazonUserRec>
            {
                public AmazonUserRecMap()
                {
                    Table("T_AMAZONUSER");
                    Id(x => x.AmazonUserId);
                    Map(x => x.AccessKeyId);
                    Map(x => x.SecretAccessKey);
                    Map(x => x.DisplayName);
                    Map(x => x.DeletedInd);
                }
            }
        #endregion

    }
}
