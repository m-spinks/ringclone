using FluentNHibernate.Mapping;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Criterion;
using RingClone.Portal.Helpers;
using RingClone.Portal.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Web.Security;

namespace RingClone.Portal.Controllers
{
    public class RingCentralAuthenticatedController : Controller
    {
		public ActionResult Index([FromUri]RingCentralAuthenticatedModel model)
        {
            logTheResponse(model);
            if (model == null)
            {
                throw new HttpException(401, "Auth Failed");
            }
            //FOR SOME REASON RINGCENTRAL UNENCODES THE "+" SYMBOL. THIS FIXES THAT.
            model.state = model.state.Replace(" ", "+");
            //confirm anti-forgery state token
            if (Session["state"] == null)
            {
                return RedirectToAction("LoginExpired","Account");
            }
            else if (HttpUtility.HtmlEncode(Session["state"].ToString()) != model.state)
            {
                throw new HttpException(401, "Auth Failed. A token error occurred. model.state = " + model.state + ". session state = " + Session["state"]);
            }
            if (!string.IsNullOrEmpty(model.error))
            {
                throw new HttpException(401, model.error_description);
            }
            var stateDecrypted = Helpers.EncryptedString.DatabaseDecrypt(model.state);
            JavaScriptSerializer jss = new JavaScriptSerializer();
            var stateModel = jss.Deserialize<RingCentralAuthStateModel>(stateDecrypted);
            getRingCentralToken(model);
            if (!string.IsNullOrEmpty(model.access_token))
            {
                getRingCentralAccountInfo(model);
                if (!string.IsNullOrEmpty(model.RingCentralId))
                {
                    saveTokenAndAccount(model);
                    saveContactInfo(model);
                    var userIdentity = new UserIdentityModel()
                    {
                        RingCentralId = model.RingCentralId,
                        DisplayName = model.DisplayName,
                        Company = ""
                    };
                    if (model.Contact != null)
                        userIdentity.Company = model.Contact.Company;
                    if (model.RingCentralId == "191625028")
                    {
                        userIdentity.DisplayName = "Test User";
                        userIdentity.Company = "North Technologies";
                    }
                    FormsAuthentication.SetAuthCookie(jss.Serialize(userIdentity), true);
                    if (model.FirstTimeLogin && (Session["refplan"] == null || string.IsNullOrEmpty(Session["refplan"].ToString()) || Session["refplan"].ToString().ToLower().Contains("bronze")))
                    {
                        return RedirectToAction("FirstTimeLogin", "Account");
                    }
                    else if (!string.IsNullOrWhiteSpace(stateModel.RedirectUrl))
                    {
                        return Redirect(stateModel.RedirectUrl);
                    }
                    else
                    {
                        return RedirectToAction("", "Log");
                    }
                }
            }
            throw new HttpException(401, "Auth Failed. An unknown error occurred.");
        }

        private void getRingCentralToken(RingCentralAuthenticatedModel model)
		{
            var requestParams = new NameValueCollection();
            requestParams.Add("grant_type", "authorization_code");
            requestParams.Add("code", model.code);
            requestParams.Add("redirect_uri", RingCentral.Config.RedirectUri);
            var array = (from key in requestParams.AllKeys
                         from value in requestParams.GetValues(key)
                         select string.Format("{0}={1}", key, HttpUtility.UrlEncode(value)))
                .ToArray();
            var postString = string.Join("&", array);
            byte[] byteArray = Encoding.UTF8.GetBytes(postString);
            var url = RingCentral.Config.TokenUri;
            WebRequest request = WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
            request.Headers.Add("Authorization", "Basic " + RingCentral.Config.Base64KeySecret);
            request.ContentLength = postString.Length;
            StreamWriter requestWriter = new StreamWriter(request.GetRequestStream());
            requestWriter.Write(postString);
            requestWriter.Close();
            try
            {
                WebResponse response = request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                JavaScriptSerializer jss = new JavaScriptSerializer();
                var data = jss.Deserialize<RingCentralAuthenticatedModel>(responseFromServer);
                if (data != null)
                {
                    model.access_token = data.access_token;
                    model.expires_in = data.expires_in;
                    model.refresh_token = data.refresh_token;
                    model.refresh_token_expires_in = data.refresh_token_expires_in;
                    model.token_type = data.token_type;
                    model.scope = data.scope;
                    model.owner_id = data.owner_id;
                }
                reader.Close();
                dataStream.Close();
                response.Close();
            }
            catch (WebException ex)
            {
                var msg = ex.Message + Environment.NewLine + Environment.NewLine + Environment.NewLine;
                msg += "Url: " + Environment.NewLine + Environment.NewLine + request.RequestUri + Environment.NewLine + Environment.NewLine;
                msg += "Request:" + Environment.NewLine + Environment.NewLine;
                foreach (var key in request.Headers.AllKeys)
                    msg += key + " = " + request.Headers[key] + Environment.NewLine;
                msg += Environment.NewLine + Environment.NewLine;
                msg += "Params:" + Environment.NewLine + Environment.NewLine;
                foreach (var key in requestParams.AllKeys)
                    msg += key + " = " + requestParams[key] + Environment.NewLine;
                msg += Environment.NewLine + Environment.NewLine;
                msg += "Response:" + Environment.NewLine + Environment.NewLine;
                foreach (var key in ex.Response.Headers.AllKeys)
                    msg += key + " = " + ex.Response.Headers[key] + Environment.NewLine;
                throw new Exception(msg);
            }
        }
        private void getRingCentralAccountInfo(RingCentralAuthenticatedModel model)
        {
            if (model.Contact == null)
                model.Contact = new RingCentralAuthenticatedModel.RingCentralContact();
            var url = RingCentral.Config.ApiUrl + "/restapi/v1.0/account/~/extension/~";
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Headers.Add("Authorization", "Bearer " + model.access_token);
            request.Accept = "application/json";
            try
            {
                WebResponse response = request.GetResponse();
                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                JavaScriptSerializer jss = new JavaScriptSerializer();
                var data = jss.Deserialize<AccountInfoData>(responseFromServer);
                reader.Close();
                dataStream.Close();
                response.Close();
                if (data != null)
                {
                    model.RingCentralId = data.id;
                    if (data.account != null && data.account.id != null)
                        model.RingCentralId = data.account.id;
                    model.NameOnRingCentralAccount = data.name;
                    model.DisplayName = data.name;
                    if (data.contact != null)
                    {
                        model.Contact.Firstname = data.contact.firstName;
                        model.Contact.Lastname = data.contact.lastName;
                        model.Contact.Company = data.contact.company;
                        model.Contact.Email = data.contact.email;
                        model.Contact.Company = data.contact.company;
                        model.Contact.BusinessPhone = data.contact.businessPhone;
                        if (data.contact.businessAddress != null)
                        {
                            model.Contact.City = data.contact.businessAddress.city;
                            model.Contact.Country = data.contact.businessAddress.country;
                            model.Contact.State = data.contact.businessAddress.state;
                            model.Contact.Street = data.contact.businessAddress.street;
                            model.Contact.Zip = data.contact.businessAddress.zip;
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                var msg = ex.Message + Environment.NewLine + Environment.NewLine + Environment.NewLine;
                msg += "Url: " + Environment.NewLine + Environment.NewLine + request.RequestUri + Environment.NewLine + Environment.NewLine;
                msg += "Request:" + Environment.NewLine + Environment.NewLine;
                foreach (var key in request.Headers.AllKeys)
                    msg += key + " = " + request.Headers[key] + Environment.NewLine;
                msg += Environment.NewLine + Environment.NewLine;
                msg += "Response:" + Environment.NewLine + Environment.NewLine;
                foreach (var key in ex.Response.Headers.AllKeys)
                    msg += key + " = " + ex.Response.Headers[key] + Environment.NewLine;
                throw new Exception(msg);
            }
        }
        private void saveTokenAndAccount(RingCentralAuthenticatedModel model)
        {
            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    AccountRec accountRec;
                    RingCentralTokenRec tokenRec;
                    var accountCrit = session.CreateCriteria<AccountRec>();
                    accountCrit.Add(Expression.Eq("RingCentralId", model.RingCentralId));
                    var accounts = accountCrit.List<AccountRec>();
                    if (accounts.Any())
                    {
                        accountRec = accounts.First();
                        var ringCentralTokenCrit = session.CreateCriteria<RingCentralTokenRec>();
                        ringCentralTokenCrit.Add(Expression.Eq("RingCentralTokenId", accountRec.RingCentralTokenId));
                        var ringCentralTokens = ringCentralTokenCrit.List<RingCentralTokenRec>();
                        using (var transaction = session.BeginTransaction())
                        {
                            if (ringCentralTokens.Any())
                            {
                                tokenRec = ringCentralTokens.First();
                                accountRec.RingCentralTokenId = tokenRec.RingCentralTokenId;
                            }
                            else
                            {
                                tokenRec = new RingCentralTokenRec();
                            }
                            tokenRec.AccessToken = model.access_token;
                            tokenRec.ExpiresIn = model.expires_in;
                            tokenRec.LastRefreshedOn = DateTime.Now.ToUniversalTime();
                            tokenRec.RefreshToken = model.refresh_token;
                            tokenRec.RefreshTokenExpiresIn = model.refresh_token_expires_in;
                            tokenRec.TokenType = model.token_type;
                            tokenRec.EndpointId = model.owner_id;
                            tokenRec.OwnerId = model.owner_id;
                            tokenRec.Scope = model.scope;
                            session.Save(tokenRec);
                            accountRec.RingCentralId = model.RingCentralId;
                            accountRec.NameOnRingCentralAccount = model.NameOnRingCentralAccount;
                            accountRec.DisplayName = model.NameOnRingCentralAccount;
                            accountRec.LastLogin = DateTime.Now.ToUniversalTime();
                            session.Save(accountRec);
                            transaction.Commit();
                        }
                    }
                    else
                    {
                        model.FirstTimeLogin = true;
                        using (var transaction = session.BeginTransaction())
                        {
                            tokenRec = new RingCentralTokenRec()
                            {
                                AccessToken = model.access_token,
                                ExpiresIn = model.expires_in,
                                LastRefreshedOn = DateTime.Now.ToUniversalTime(),
                                RefreshToken = model.refresh_token,
                                RefreshTokenExpiresIn = model.refresh_token_expires_in,
                                TokenType = model.token_type,
                                EndpointId = model.owner_id,
                                OwnerId = model.owner_id,
                                Scope = model.scope
                            };
                            session.Save(tokenRec);
                            accountRec = new AccountRec()
                            {
                                RingCentralId = model.RingCentralId,
                                RingCentralOwnerId = model.owner_id,
                                NameOnRingCentralAccount = model.NameOnRingCentralAccount,
                                DisplayName = model.NameOnRingCentralAccount,
                                RingCentralTokenId = tokenRec.RingCentralTokenId,
                                LastLogin = DateTime.Now.ToUniversalTime()
                            };
                            session.Save(accountRec);
                            transaction.Commit();
                        }
                    }
                }
            }

        }
        private void saveContactInfo(RingCentralAuthenticatedModel model)
        {
            if (model.Contact != null)
            {
                using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
                {
                    using (var session = sessionFactory.OpenSession())
                    {
                        var accountCrit = session.CreateCriteria<AccountRec>();
                        accountCrit.Add(Expression.Eq("RingCentralId", model.RingCentralId));
                        var accounts = accountCrit.List<AccountRec>();
                        if (accounts.Any())
                        {
                            var accountRec = accounts.First();
                            var contactCrit = session.CreateCriteria<RingCentralContactRec>();
                            contactCrit.Add(Expression.Eq("AccountId", accountRec.AccountId));
                            contactCrit.Add(Expression.Eq("RingCentralId", accountRec.RingCentralId));
                            contactCrit.Add(Expression.Eq("DeletedInd", false));
                            var contacts = contactCrit.List<RingCentralContactRec>();
                            if (contacts.Any())
                            {
                                var contact = contacts.First();

                                if (contact.Firstname == null) contact.Firstname = "";
                                if (contact.Lastname == null) contact.Lastname = "";
                                if (contact.Company == null) contact.Company = "";
                                if (contact.Email == null) contact.Email = "";
                                if (contact.BusinessPhone == null) contact.BusinessPhone = "";
                                if (contact.Street == null) contact.Street = "";
                                if (contact.City == null) contact.City = "";
                                if (contact.State == null) contact.State = "";
                                if (contact.Zip == null) contact.Zip = "";
                                if (contact.Country == null) contact.Country = "";

                                if (!string.IsNullOrWhiteSpace(model.Contact.Firstname))
                                    contact.Firstname = model.Contact.Firstname;
                                if (!string.IsNullOrWhiteSpace(model.Contact.Lastname))
                                    contact.Lastname = model.Contact.Lastname;
                                if (!string.IsNullOrWhiteSpace(model.Contact.Company))
                                    contact.Company = model.Contact.Company;
                                if (!string.IsNullOrWhiteSpace(model.Contact.Email))
                                    contact.Email = model.Contact.Email;
                                if (!string.IsNullOrWhiteSpace(model.Contact.BusinessPhone))
                                    contact.BusinessPhone = model.Contact.BusinessPhone;
                                if (!string.IsNullOrWhiteSpace(model.Contact.Street))
                                    contact.Street = model.Contact.Street;
                                if (!string.IsNullOrWhiteSpace(model.Contact.City))
                                    contact.City = model.Contact.City;
                                if (!string.IsNullOrWhiteSpace(model.Contact.State))
                                    contact.State = model.Contact.State;
                                if (!string.IsNullOrWhiteSpace(model.Contact.Zip))
                                    contact.Zip = model.Contact.Zip;
                                if (!string.IsNullOrWhiteSpace(model.Contact.Country))
                                    contact.Country = model.Contact.Country;

                                using (var transaction = session.BeginTransaction())
                                {
                                    session.Save(contact);
                                    transaction.Commit();
                                }
                            }
                            else
                            {
                                var contact = new RingCentralContactRec();
                                contact.AccountId = accountRec.AccountId;
                                contact.Firstname = model.Contact.Firstname ?? "";
                                contact.Lastname = model.Contact.Lastname ?? "";
                                contact.Company = model.Contact.Company ?? "";
                                contact.Email = model.Contact.Email ?? "";
                                contact.BusinessPhone = model.Contact.BusinessPhone ?? "";
                                contact.Street = model.Contact.Street ?? "";
                                contact.City = model.Contact.City ?? "";
                                contact.State = model.Contact.State ?? "";
                                contact.Zip = model.Contact.Zip ?? "";
                                contact.Country = model.Contact.Country ?? "";
                                contact.RingCentralId = model.RingCentralId;
                                using (var transaction = session.BeginTransaction())
                                {
                                    session.Save(contact);
                                    transaction.Commit();
                                }
                            }
                        }
                    }
                }
            }
        }
        private void logTheResponse(RingCentralAuthenticatedModel model)
        {
            var msg = "Url: " + Environment.NewLine + Environment.NewLine + "ringcentralauthenticated" + Environment.NewLine + Environment.NewLine;
            msg += "Request:" + Environment.NewLine + Environment.NewLine;
            foreach (var key in Request.Headers.AllKeys)
                msg += key + " = " + Request.Headers[key] + Environment.NewLine;
            msg += Environment.NewLine + Environment.NewLine;
            msg += "Response:" + Environment.NewLine + Environment.NewLine;
            foreach (var key in Response.Headers.AllKeys)
                msg += key + " = " + Response.Headers[key] + Environment.NewLine;
            msg += Environment.NewLine + Environment.NewLine;
            msg += "Data:" + Environment.NewLine + Environment.NewLine;
            JavaScriptSerializer jss = new JavaScriptSerializer();
            msg += JsonConvert.SerializeObject(model, Formatting.Indented);//.Replace("\\r\\n", Environment.NewLine);
            using (ISessionFactory sessionFactory = NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    using (var transaction = session.BeginTransaction())
                    {
                        var logRec = new OauthLogRec();
                        logRec.CreateDate = DateTime.Now.ToUniversalTime();
                        logRec.LogText = msg;
                        session.Save(logRec);
                        transaction.Commit();
                    }
                }
            }
        }

        private class AccountInfoData
        {
            public string id { get; set; }
            public string name { get; set; }
            public string type { get; set; }
            public string status { get; set; }
            public Account account { get; set; }
            public Contact contact { get; set; }
            public class BusinessAddress
            {
                public string street { get; set; }
                public string city { get; set; }
                public string state { get; set; }
                public string zip { get; set; }
                public string country { get; set; }
            }
            public class Contact
            {
                public string firstName { get; set; }
                public string lastName { get; set; }
                public string company { get; set; }
                public string email { get; set; }
                public string businessPhone { get; set; }
                public BusinessAddress businessAddress { get; set; }

            }
            public class Account
            {
                public string uri { get; set; }
                public string id { get; set; }
            }
        }

#region Database Models
        public class AccountRec
        {
            public virtual int AccountId { get; set; }
            public virtual string RingCentralId { get; set; }
            public virtual int RingCentralTokenId { get; set; }
            public virtual string RingCentralOwnerId { get; set; }
            public virtual string NameOnRingCentralAccount { get; set; }
            public virtual string DisplayName { get; set; }
            public virtual DateTime? LastLogin { get; set; }
        }
        private class AccountRecMap : ClassMap<AccountRec>
        {
            public AccountRecMap()
            {
                Table("T_ACCOUNT");
                Id(x => x.AccountId).Column("AccountId");
                Map(x => x.RingCentralId);
                Map(x => x.RingCentralTokenId);
                Map(x => x.RingCentralOwnerId);
                Map(x => x.NameOnRingCentralAccount);
                Map(x => x.DisplayName);
                Map(x => x.LastLogin);
            }
        }

        public class RingCentralTokenRec
        {
            public virtual int RingCentralTokenId { get; set; }
            public virtual string AccessToken { get; set; }
            public virtual string ExpiresIn { get; set; }
            public virtual string TokenType { get; set; }
            public virtual string RefreshToken { get; set; }
            public virtual string RefreshTokenExpiresIn { get; set; }
            public virtual string Scope { get; set; }
            public virtual string OwnerId { get; set; }
            public virtual string EndpointId { get; set; }
            public virtual DateTime LastRefreshedOn { get; set; }
            public virtual bool DeletedInd { get; set; }
        }
        private class RingCentralTokenRecMap : ClassMap<RingCentralTokenRec>
        {
            public RingCentralTokenRecMap()
            {
                Table("T_RINGCENTRALTOKEN");
                Id(x => x.RingCentralTokenId);
                Map(x => x.AccessToken);
                Map(x => x.ExpiresIn);
                Map(x => x.TokenType);
                Map(x => x.RefreshToken);
                Map(x => x.RefreshTokenExpiresIn);
                Map(x => x.DeletedInd);
                Map(x => x.LastRefreshedOn);
                Map(x => x.Scope);
                Map(x => x.OwnerId);
                Map(x => x.EndpointId);
            }
        }
        private class RingCentralContactRec
        {
            public virtual int RingCentralContactId { get; set; }
            public virtual int AccountId { get; set; }
            public virtual string RingCentralId { get; set; }
            public virtual string Firstname { get; set; }
            public virtual string Lastname { get; set; }
            public virtual string Company { get; set; }
            public virtual string Email { get; set; }
            public virtual string BusinessPhone { get; set; }
            public virtual string Street { get; set; }
            public virtual string City { get; set; }
            public virtual string State { get; set; }
            public virtual string Zip { get; set; }
            public virtual string Country { get; set; }
            public virtual bool DeletedInd { get; set; }
        }
        private class RingCentralContactRecMap : ClassMap<RingCentralContactRec>
        {
            public RingCentralContactRecMap()
            {
                Table("T_RINGCENTRALCONTACT");
                Id(x => x.RingCentralContactId);
                Map(x => x.AccountId);
                Map(x => x.RingCentralId);
                Map(x => x.Firstname);
                Map(x => x.Lastname);
                Map(x => x.Company);
                Map(x => x.Email);
                Map(x => x.BusinessPhone);
                Map(x => x.Street);
                Map(x => x.City);
                Map(x => x.State);
                Map(x => x.Zip);
                Map(x => x.Country);
                Map(x => x.DeletedInd);
            }
        }
        private class OauthLogRec
        {
            public virtual int OauthLogId { get; set; }
            public virtual DateTime CreateDate { get; set; }
            public virtual string LogText { get; set; }
        }

        private class OauthLogRecMap : ClassMap<OauthLogRec>
        {
            public OauthLogRecMap()
            {
                Table("T_OAUTHLOG");
                Id(x => x.OauthLogId);
                Map(x => x.CreateDate);
                Map(x => x.LogText).Length(7000);
            }
        }

        #endregion

    }
}
