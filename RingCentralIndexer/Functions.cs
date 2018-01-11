using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using NHibernate;
using FluentNHibernate.Cfg;
using FluentNHibernate.Mapping;
using System;
using FluentNHibernate.Cfg.Db;
using NHibernate.Criterion;
using NHibernate.Transform;
using Newtonsoft.Json;

namespace RingCentralIndexer
{
    public class Functions
    {
        public static void ProcessQueueMessage([QueueTrigger("ringcentralindexerqueue")] string message, TextWriter log)
        {
            var model = JsonConvert.DeserializeObject<MessageModel>(message);
            if (model != null)
            {
                if (model.TicketId > 0)
                {
                    log.WriteLine("Indexing Ticket " + model.TicketId);
                }
                if (!string.IsNullOrWhiteSpace(model.CallId))
                {
                    log.WriteLine("Indexing CallId " + model.CallId);
                }
                if (!string.IsNullOrWhiteSpace(model.MessageId))
                {
                    log.WriteLine("Indexing MessageId " + model.MessageId);
                }
            }
        }
        public class MessageModel
        {
            public int TicketId { get; set; }
            public string CallId { get; set; }
            public string MessageId { get; set; }
            public IEnumerable<string> TicketIds { get; set; }
            public IEnumerable<string> CallIds { get; set; }
            public IEnumerable<string> MessageIds { get; set; }
        }

    }
}
