using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace TicketDistributor
{
    public class Functions
    {
        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
        public static void ProcessQueueMessage([QueueTrigger("queue")] string message, TextWriter log)
        {
            log.WriteLine(message);
        }
        public class IndexRecord
        {
            public int IndexId { get; set; }
            public string OwnerId { get; set; }
            public int TicketId { get; set; }
            public int TicketLogId { get; set; }
            public string Type { get; set; }
            public string CallId { get; set; }
            public string MessageId { get; set; }
            public DateTime CallTime { get; set; }
            public int IndexRawDataId { get; set; }
            public int IndexMessageId { get; set; }
            public string DefaultFileName { get; set; }
            public bool DeletedInd { get; set; }
        }
        public static void ReIndex()
        {
            var ticketsString = "";
            var recordsPerBatch = 5000;
            IEnumerable<IndexRecord> indexes = null;
            using (System.Data.IDbConnection db = new System.Data.SqlClient.SqlConnection(ConnectionStringHelper.ConnectionString))
            {
                indexes = db.Query<IndexRecord>("SELECT * FROM T_INDEX WHERE OwnerId='386509065' AND CompleteInd=0 ORDER BY IndexId DESC");
            }

            if (indexes != null)
            {
                var counter = 0;
                foreach (var index in indexes)
                {
                    if (ticketsString.Length > 0)
                        ticketsString += "," + index.TicketId;
                    else
                        ticketsString += index.TicketId;
                    if (counter > recordsPerBatch || counter == indexes.Count() - 1)
                    {
                        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConnectionStringHelper.StorageConnectionString);
                        CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                        CloudQueue queue = queueClient.GetQueueReference("ringcentralindexerqueue");
                        queue.CreateIfNotExists();
                        CloudQueueMessage message = new CloudQueueMessage("{TicketIds:[" + ticketsString + "]}");
                        queue.AddMessage(message);
                        ticketsString = "";
                        counter = 0;
                    }
                    else
                    {
                        counter++;
                    }
                }
            }

        }
        public static void Execute()
        {
#if DEBUG
            ReIndex();
#endif
            IEnumerable<TransferBatch> batchesToRun = null;
            IEnumerable<TransferBatch> batchesToRedo = null;
            using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
            {
                batchesToRun = db.Query<TransferBatch>("SELECT * FROM T_TRANSFERBATCH WHERE QueuedInd=1 AND RedoInd=0 AND ProcessingInd=0 AND DeletedInd=0");
            }
            if (batchesToRun != null && batchesToRun.Any())
            {
                foreach (var batch in batchesToRun)
                {
                    using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                    {
                        db.Execute("UPDATE T_TRANSFERBATCH SET ProcessingInd=1, QueuedInd=0 WHERE TransferBatchId=@transferBatchId", new { transferBatchId = batch.TransferBatchId });
                    }
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConnectionStringHelper.StorageConnectionString);
                    CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                    CloudQueue queue = queueClient.GetQueueReference("ticketprocessorqueue");
                    queue.CreateIfNotExists();
                    CloudQueueMessage message = new CloudQueueMessage(batch.TransferBatchId.ToString());
                    queue.AddMessage(message);
                }
            }
            using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
            {
                batchesToRedo = db.Query<TransferBatch>("SELECT * FROM T_TRANSFERBATCH WHERE QueuedInd=1 AND RedoInd=1 AND ProcessingInd=0 AND DeletedInd=0");
            }
            if (batchesToRedo != null && batchesToRedo.Any())
            {
                foreach (var redo in batchesToRedo)
                {
                    using (IDbConnection db = new SqlConnection(ConnectionStringHelper.ConnectionString))
                    {
                        db.Execute("UPDATE T_TRANSFERBATCH SET ProcessingInd=1, QueuedInd=0 WHERE TransferBatchId=@transferBatchId", new { transferBatchId = redo.TransferBatchId });
                    }
                    CloudStorageAccount storageAccount = CloudStorageAccount.Parse(ConnectionStringHelper.StorageConnectionString);
                    CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
                    CloudQueue queue = queueClient.GetQueueReference("ticketprocessorqueue");
                    queue.CreateIfNotExists();
                    CloudQueueMessage message = new CloudQueueMessage(redo.TransferBatchId.ToString());
                    queue.AddMessage(message);
                }
            }
        }
        public class TransferBatch
        {
            public int TransferBatchId;
            public int TransferRuleId;
            public int AccountId;
            public bool QueuedInd;
            public bool ProcessingInd;
            public bool DeletedInd;
            public bool RedoInd;
            public DateTime CreateDate;
        }
    }
}
