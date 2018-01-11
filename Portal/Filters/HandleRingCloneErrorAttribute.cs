using FluentNHibernate.Mapping;
using NHibernate;
using System;
using System.Net;
using System.Web.Mvc;

public class HandleRingCloneErrorAttribute : IExceptionFilter
{
	public void OnException(ExceptionContext context)
	{
		Exception raisedException = context.Exception;
        var controller = "";
        var action = "";
        if (context.RouteData.Values["action"] != null)
            action = context.RouteData.Values["action"].ToString();
        if (context.RouteData.Values["controller"] != null)
            controller = context.RouteData.Values["controller"].ToString();
        if (raisedException != null && raisedException is WebException)
        {
            WebException wex = (WebException)raisedException;
            var msg = wex.Message;
            msg += Environment.NewLine + Environment.NewLine + Environment.NewLine;
            if (wex.Response != null)
            {
                var response = wex.Response as HttpWebResponse;
                if (response != null)
                {
                    msg += "Responding Uri: " + Environment.NewLine + Environment.NewLine + response.ResponseUri + Environment.NewLine + Environment.NewLine;
                    msg += "Method: " + Environment.NewLine + Environment.NewLine + response.Method + Environment.NewLine + Environment.NewLine;
                    msg += "Status Code: " + Environment.NewLine + Environment.NewLine + response.StatusCode + Environment.NewLine + Environment.NewLine;
                    msg += "Response:" + Environment.NewLine + Environment.NewLine;
                    foreach (var key in response.Headers.AllKeys)
                        msg += key + " = " + response.Headers[key] + Environment.NewLine;
                }
                else
                {
                    msg += "Response:" + Environment.NewLine + Environment.NewLine;
                    foreach (var key in wex.Response.Headers.AllKeys)
                        msg += key + " = " + wex.Response.Headers[key] + Environment.NewLine;
                }
            }
            using (ISessionFactory sessionFactory = RingClone.Portal.Helpers.NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    var errorRec = new ErrorRec();
                    errorRec.CreateDate = DateTime.Now.ToUniversalTime();
                    errorRec.Controller = controller;
                    errorRec.Action = action;
                    errorRec.Exception1 = msg;
                    errorRec.Exception2 = "";
                    errorRec.Exception3 = "";
                    errorRec.Exception4 = "";
                    using (var transaction = session.BeginTransaction())
                    {
                        session.Save(errorRec);
                        transaction.Commit();
                    }
                }
            }
        }
        else
        {
            using (ISessionFactory sessionFactory = RingClone.Portal.Helpers.NHibernateHelper.CreateSessionFactory())
            {
                using (var session = sessionFactory.OpenSession())
                {
                    var errorRec = new ErrorRec();
                    errorRec.CreateDate = DateTime.Now.ToUniversalTime();
                    errorRec.Controller = controller;
                    errorRec.Action = action;
                    errorRec.Exception1 = "";
                    errorRec.Exception2 = "";
                    errorRec.Exception3 = "";
                    errorRec.Exception4 = "";
                    if (raisedException != null)
                    {
                        errorRec.Exception1 = raisedException.Message;
                        if (raisedException.InnerException != null)
                        {
                            errorRec.Exception2 = raisedException.InnerException.Message;
                            if (raisedException.InnerException.InnerException != null)
                            {
                                errorRec.Exception3 = raisedException.InnerException.InnerException.Message;
                                if (raisedException.InnerException.InnerException.InnerException != null)
                                {
                                    errorRec.Exception4 = raisedException.InnerException.InnerException.InnerException.Message;
                                }
                            }
                        }
                    }
                    using (var transaction = session.BeginTransaction())
                    {
                        session.Save(errorRec);
                        transaction.Commit();
                    }
                }
            }
        }
    }

	#region Database Mapping
	private class ErrorRec
	{
		public virtual int ErrorId { get; set; }
		public virtual DateTime CreateDate { get; set; }
		public virtual string Exception1 { get; set; }
		public virtual string Exception2 { get; set; }
		public virtual string Exception3 { get; set; }
        public virtual string Exception4 { get; set; }
        public virtual string Controller { get; set; }
        public virtual string Action { get; set; }
    }

    private class ErrorRecMap : ClassMap<ErrorRec>
	{
		public ErrorRecMap()
		{
			Table("T_ERROR");
			Id(x => x.ErrorId);
			Map(x => x.CreateDate);
			Map(x => x.Exception1).Length(5000);
			Map(x => x.Exception2).Length(5000);
			Map(x => x.Exception3).Length(5000);
            Map(x => x.Exception4).Length(5000);
            Map(x => x.Controller);
            Map(x => x.Action);
        }
    }

	#endregion


}