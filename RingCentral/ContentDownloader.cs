using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Criterion;
using RingCentral.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace RingCentral
{
    public class ContentDownloader : RingCentralAction
    {
		public string ContentUri;
        public byte[] data;
        public string headers;

		public ContentDownloader(string username, string contentUri)
			: base(username)
		{
			ContentUri = contentUri;
		}

        public override void DoAction()
        {
            throttle();
            var url = ContentUri;
			var request = (HttpWebRequest)WebRequest.Create(url);
			request.Method = "GET";
			request.Headers.Add("Authorization", "Bearer " + AccessToken);
			request.Accept = "application/json";
            try
            {
                WebResponse response = request.GetResponse();
                var dataStream = new MemoryStream();
                using (Stream responseStream = request.GetResponse().GetResponseStream())
                {
                    byte[] buffer = new byte[0x1000];
                    int bytes;
                    while ((bytes = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        dataStream.Write(buffer, 0, bytes);
                    }
                }
                data = new byte[dataStream.Length];
                dataStream.Seek(0, SeekOrigin.Begin);
                dataStream.Read(data, 0, (int)dataStream.Length);
                //Stream dataStream = response.GetResponseStream();
                //StreamReader reader = new StreamReader(dataStream);
                //data = new byte[dataStream.Length];
                //dataStream.Read(data, 0, (int)dataStream.Length);
                //reader.Close();
                dataStream.Close();
                response.Close();
            }
            catch (WebException ex)
            {
                headers = "Url: " + Environment.NewLine + Environment.NewLine + request.RequestUri + Environment.NewLine + Environment.NewLine;
                headers += "Request:" + Environment.NewLine + Environment.NewLine;
                foreach (var key in request.Headers.AllKeys)
                    headers += key + " = " + request.Headers[key] + Environment.NewLine;
                headers += Environment.NewLine + Environment.NewLine;
                headers += "Response:" + Environment.NewLine + Environment.NewLine;
                foreach (var key in ex.Response.Headers.AllKeys)
                    headers += key + " = " + ex.Response.Headers[key] + Environment.NewLine;
                throw ex;
            }
        }
        private void throttle()
        {
            var maxPerPeriod = 10;
            var keyPrefix = RingCentralId;
            var intervalPeriod = 60000;
            var sleepInterval = 5000;
            var recentTransactions = MemoryCache.Default.Count(x => x.Key.StartsWith(keyPrefix));
            while (recentTransactions >= maxPerPeriod)
            {
                System.Threading.Thread.Sleep(sleepInterval);
                recentTransactions = MemoryCache.Default.Count(x => x.Key.StartsWith(keyPrefix));
            }
            var key = keyPrefix + "_" + DateTime.Now.ToUniversalTime().ToString("yyyyMMddHHmm");
            var existing = MemoryCache.Default.Where(x => x.Key.StartsWith(key));
            if (existing != null && existing.Any())
            {
                var counter = 2;
                var last = existing.OrderBy(x => x.Key).Last();
                var pieces = last.Key.Split('_');
                if (pieces.Count() > 2)
                {
                    var lastCount = 0;
                    if (int.TryParse(pieces[2], out lastCount))
                    {
                        counter = lastCount + 1;
                    }
                }
                key = key + "_" + counter;
            }
            var policy = new CacheItemPolicy
            {
                AbsoluteExpiration = DateTimeOffset.UtcNow.AddMilliseconds(intervalPeriod)
            };
            MemoryCache.Default.Set(key, 1, policy);
        }
    }
}
