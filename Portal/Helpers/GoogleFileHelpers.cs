using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Helpers
{
    public class GoogleFileHelpers
    {

        public static ICollection<FileTransferStat> Transfer()
        {
            //TRANSFER FILES FROM ONE GOOGLE DRIVE TO ANOTHER
            var allFiles = new List<GoogleActions.GoogleFolder.GoogleFolderResponse.File>();
            var stats = new List<FileTransferStat>();

            //201708
            //var sourceRingCentralId = "191625028";
            //var sourceFolderId = "0B5pWMgg3fgamalcteGItSElrSUU";
            //var sourceGoogleAccountId = 61;
            //var targetRingCentralId = "400234016";
            //var targetFolderId = "0B4BWiFyv3VCgaVBvWjNJN1VId1k";
            //var targetGoogleAccountId = 23;

            //201707
            //var sourceRingCentralId = "191625028";
            //var sourceFolderId = "0B5pWMgg3fgambEp2Q21NcjBqeFE";
            //var sourceGoogleAccountId = 61;
            //var targetRingCentralId = "400234016";
            //var targetFolderId = "0B4BWiFyv3VCgNV94QUFIS3cwZ3c";
            //var targetGoogleAccountId = 23;

            //201706
            //var sourceRingCentralId = "191625028";
            //var sourceFolderId = "0B5pWMgg3fgamV1Qyc2NXZy1MWHM";
            //var sourceGoogleAccountId = 61;
            //var targetRingCentralId = "400234016";
            //var targetFolderId = "0B4BWiFyv3VCgbEZzNjdzOURjaVE";
            //var targetGoogleAccountId = 23;

            //201706-2
            var sourceRingCentralId = "191625028";
            var sourceFolderId = "0B5pWMgg3fgamdjVXMldjVHVGNDQ";
            var sourceGoogleAccountId = 61;
            var targetRingCentralId = "400234016";
            var targetFolderId = "0B4BWiFyv3VCgbEZzNjdzOURjaVE";
            var targetGoogleAccountId = 23;

            //MY TESTING
            //var targetRingCentralId = "191625028";
            //var targetFolderId = "0B5pWMgg3fgamMzZubVF0MDZtS3c";
            //var targetGoogleAccountId = 61;

            var gFolder = new GoogleActions.GoogleFolder(sourceRingCentralId, sourceGoogleAccountId, sourceFolderId);

            gFolder.Execute();
            if (gFolder.ResultException != null || gFolder.Files == null || gFolder.Files.files == null)
            {
                //ERROR
                throw new Exception("Unhandled error");
            }
            else
            {
                allFiles.AddRange(gFolder.Files.files);
                while (!string.IsNullOrEmpty(gFolder.Files.nextPageToken))
                {
                    gFolder.NextPageToken(gFolder.Files.nextPageToken);
                    gFolder.Execute();
                    if (gFolder.ResultException != null || gFolder.Files == null || gFolder.Files.files == null)
                    {
                        //ERROR
                        throw new Exception("Unhandled error");
                    }
                    else
                    {
                        allFiles.AddRange(gFolder.Files.files);
                    }
                }
                //var subset = new string[] { "20170803_1813_(616)643-3133_(937)949-4653_Inbound_Missed.log",
                //    "20170811_1817_(858)225-4580_(888)243-4151_Inbound_Missed.log"
                //     };
                //var subsetFiles = allFiles.Where(x => subset.Contains(x.name));
                foreach (var file in allFiles)
                {
                    var stat = new FileTransferStat();
                    stats.Add(stat);
                    stat.SourceRingCentralId = gFolder.RingCentralId;
                    stat.SourceGoogleAccountId = gFolder.GoogleAccountId;
                    stat.SourceFolderId = gFolder.FolderId;
                    stat.SourceFileId = file.id;
                    stat.Filename = file.name;
                    var gDownload = new GoogleActions.GoogleDownload(sourceRingCentralId, sourceGoogleAccountId).FileId(file.id);
                    gDownload.Execute();
                    if (gDownload.ResultException != null || gDownload.FileData == null || gDownload.FileData.Length < 1)
                    {
                        //ERROR
                        stat.Status = "error";
                        stat.Message = gDownload.ResultException != null ? gDownload.ResultException.Message : "unknown dowload error";
                    }
                    else
                    {
                        stat.Bytes = gDownload.FileData.Length;
                        var gUpload = new GoogleActions.GoogleUpload(targetRingCentralId, targetGoogleAccountId);
                        gUpload.Folder(targetFolderId);
                        gUpload.FileName(file.name);
                        gUpload.FileData(gDownload.FileData);
                        stat.TargetRingCentralId = gUpload.RingCentralId;
                        stat.TargetGoogleAccountId = gUpload.GoogleAccountId;
                        stat.TargetFolderId = gUpload.FolderId;
                        gUpload.Execute();
                        if (gUpload.ResultException != null || gUpload.Response == null || string.IsNullOrWhiteSpace(gUpload.Response.id))
                        {
                            //ERROR
                            stat.Status = "error";
                            stat.Message = gUpload.ResultException != null ? gUpload.ResultException.Message : "unknown upload error";
                        }
                        else
                        {
                            stat.TargetFileId = gUpload.Response.id;
                            stat.Replaced = gUpload.Replaced;
                            stat.FilesWithSameName = gUpload.FilesThatExistWithSameName;
                            stat.Status = "success";
                        }
                    }
                }
            }
            return stats;
            //var properties = typeof(FileTransferStat).GetFields();
            //var result = new StringBuilder();
            //var header = string.Join(",", properties.Select(x => x.Name).ToList());
            //result.AppendLine(header);
            //foreach (var row in stats)
            //{
            //    var values = properties.Select(p => p.GetValue(row));
            //    var line = string.Join(",", values);
            //    result.AppendLine(line);
            //}
            //using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"D:\temp\file-xfer-stats-201706-2.csv"))
            //{
            //    file.Write(result.ToString());
            //}
        }

        public static ICollection<FileTransferAndCleanStat> TransferAndClean()
        {
            //TRANSFER FILES FROM ONE GOOGLE FOLDER TO ANOTHER, USING ONLY A SUBSET
            //IF THE FILE EXISTS IN THE TARGET FOLDER, DELETE ALL COPIES AND THEN UPLOAD
            var allFiles = new List<GoogleActions.GoogleFolder.GoogleFolderResponse.File>();
            var stats = new List<FileTransferAndCleanStat>();
            var fileListFilter = new string[] {
                "20170601_1442_(513)454-6259_(888)444-4891_Inbound_Accepted.mp3",
                "20170601_1442_(513)454-6259_(888)444-4891_Inbound_Accepted.mp3",
                "20170601_1444_(513)454-6259_(888)444-4891_Inbound_Accepted.mp3",
                "20170601_1858_(423)371-0456_(888)877-6101_Inbound_Accepted.mp3",
                "20170602_2240_(888)243-4151_(865)441-7217_Outbound_RecordedCall.mp3",
                "20170604_0103_(270)283-2439_Inbound_VoiceMail.mp3",
                "20170605_1731_(888)243-4151_Inbound_VoiceMail.mp3",
                "20170606_2103_(888)243-4151_Inbound_VoiceMail.mp3",
                "20170607_2146_(888)243-4151_(937)829-4038_Outbound_RecordedCall.mp3",
                "20170608_1702_(678)613-3071_Inbound_VoiceMail.mp3",
                "20170613_1010_(937)949-4476_(937)818-3867_Outbound_RecordedCall.mp3",
                "20170613_1728_(937)949-4653_(937)270-8182_Outbound_RecordedCall.mp3",
                "20170613_2125_(606)493-6362_Inbound_VoiceMail.mp3",
                "20170614_1441_(937)877-8414_(888)877-6101_Inbound_Accepted.mp3",
                "20170614_1545_(256)605-5815_Inbound_VoiceMail.mp3",
                "20170614_1926_(888)243-4151_Inbound_VoiceMail.mp3",
                "20170614_1927_(888)243-4151_Inbound_VoiceMail.mp3",
                "20170615_1813_(937)949-4476_(678)613-3071_Outbound_RecordedCall.mp3",
                "20170616_2126_(937)329-9569_(866)813-1511_Outbound_RecordedCall.mp3",
                "20170619_0908_(740)819-9977_(937)999-3284_Inbound_Accepted.mp3",
                "20170619_1823_(256)608-2213_(888)877-6101_Inbound_Accepted.mp3",
                "20170620_0654_(888)243-4151_(812)212-8959_Outbound_RecordedCall.mp3",
                "20170620_1006_(937)949-4653_(937)725-8350_Outbound_RecordedCall.mp3",
                "20170621_1853_(706)313-2578_(888)877-6101_Inbound_Accepted.mp3",
                "20170621_2317_(706)669-3907_(888)877-6101_Inbound_Accepted.mp3",
                "20170622_1951_(888)243-4151_(270)696-3189_Outbound_RecordedCall.mp3",
                "20170623_1613_(888)243-4151_Inbound_VoiceMail.mp3",
                "20170624_1810_(888)243-4151_(931)250-6478_Outbound_RecordedCall.mp3",
                "20170626_2003_(888)243-4151_(423)741-0247_Outbound_RecordedCall.mp3",
                "20170626_2003_(888)243-4151_(423)741-0247_Outbound_RecordedCall.mp3",
                "20170713_1232_(888)243-4151_(423)248-4147_Outbound_Call-connected_Recording.mp3",
                "20170716_0554_(859)576-9092_(606)393-6677_Inbound_Voicemail_Message.mp3",
                "20170716_1540_(423)480-9859_(629)777-9199_Inbound_Accepted_Recording.mp3",
                "20170717_2201_(901)517-2124_(888)877-6101_Inbound_Missed.log",
                "20170717_2226_(740)808-3879_(740)661-4466_Inbound_Missed.log",
                "20170717_2313_(662)707-0354_(888)243-4151_Inbound_Missed.log",
                "20170719_1734_(888)243-4151_(731)333-6941_Outbound_Hang-Up.log",
                "20170719_2011_(888)243-4151_(574)312-0060_Outbound_Call-connected.log",
                "20170719_2011_(888)243-4151_(574)312-0060_Outbound_Call-connected_Recording.mp3",
                "20170720_0108_(423)334-9690_(888)877-6101_Inbound_Voicemail.log",
                "20170720_0108_(423)334-9690_(888)877-6101_Inbound_Voicemail_Message.mp3",
                "20170720_0159_(517)410-0083_(517)394-0100_Inbound_Missed.log",
                "20170720_1555_(205)340-4102_(888)877-6101_Inbound_Accepted.log",
                "20170720_1555_(205)340-4102_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170721_2117_(216)650-9260_(888)243-4151_Inbound_Missed.log",
                "20170722_0036_(567)315-9578_(616)888-6855_Inbound_Missed.log",
                "20170723_1746_(937)823-6950_(937)660-9033_Inbound_Missed.log",
                "20170724_1616_(662)606-0231_(888)243-4151_Inbound_Missed.log",
                "20170724_1913_(270)304-7424_(888)243-4151_Inbound_Missed.log",
                "20170725_1516_(706)937-6556_(888)243-4151_Inbound_Missed.log",
                "20170725_1644_(270)403-6747_(888)243-4151_Inbound_Missed.log",
                "20170725_2028_(662)469-2734_(888)243-4151_Inbound_Missed.log",
                "20170727_0135_(423)948-3934_(888)243-4151_Inbound_Missed.log",
                "20170728_1633_x5583_x5558_Inbound_Missed.log",
                "20170728_2122_(614)558-2673_(888)877-6101_Inbound_Missed.log",
                "20170729_1525_(364)888-8699_(606)657-4001_Inbound_Accepted.log",
                "20170729_1525_(364)888-8699_(606)657-4001_Inbound_Accepted_Recording.mp3",
                "20170730_0308_(470)241-9493_(888)877-6101_Inbound_Voicemail.log",
                "20170730_0308_(470)241-9493_(888)877-6101_Inbound_Voicemail_Message.mp3",
                "20170730_1302_(678)877-1553_(888)877-6101_Inbound_Missed.log",
                "20170730_1600_(812)571-0386_(317)348-3223_Inbound_Missed.log",
                "20170730_2045_(662)516-9850_(888)877-6101_Inbound_Missed.log",
                "20170731_1633_(606)791-6493_(606)657-4001_Inbound_Missed.log",
                "20170731_1803_(937)949-4654_(423)356-6390_Outbound_Wrong-Number.log",
                "20170731_1931_(662)519-4766_(888)877-6101_Inbound_Missed.log",
                "20170731_2104_(270)403-0997_(888)243-4151_Inbound_Missed.log",
                "20170731_2128_(662)582-7306_(888)877-6101_Inbound_Accepted.log",
                "20170731_2128_(662)582-7306_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170803_1813_(616)643-3133_(937)949-4653_Inbound_Missed.log",
                "20170811_1700_(423)505-8042_(888)243-4151_Inbound_Missed.log",
                "20170811_1753_(561)275-8571_(740)661-4466_Inbound_Missed.log",
                "20170812_1334_(865)426-6269_(888)877-6101_Inbound_Missed.log",
                "20170812_1340_(937)674-8331_(937)310-5441_Inbound_Missed.log",
                "20170812_1353_(386)868-7266_(888)877-6101_Inbound_Missed.log",
                "20170812_1402_(662)739-0251_(888)877-6101_Inbound_Missed.log",
                "20170812_1405_(606)614-8365_(606)657-4001_Inbound_Missed.log",
                "20170812_1413_(662)590-5967_(888)877-6101_Inbound_Missed.log",
                "20170812_1416_(662)613-1026_(888)877-6101_Inbound_Missed.log",
                "20170812_1416_(901)832-6987_(888)877-6101_Inbound_Missed.log",
                "20170812_1422_(765)557-4791_(317)348-3223_Inbound_Missed.log",
                "20170812_1424_(812)592-7464_(317)348-3223_Inbound_Missed.log",
                "20170812_1425_(859)322-1917_(888)877-6101_Inbound_Missed.log",
                "20170812_1428_(606)505-8787_(888)877-6101_Inbound_Missed.log",
                "20170812_1428_(740)215-9747_(740)661-4466_Inbound_Missed.log",
                "20170812_1430_(812)592-7464_(317)348-3223_Inbound_Missed.log",
                "20170812_1443_(662)739-0251_(888)877-6101_Inbound_Missed.log",
                "20170812_1451_(513)770-2181_(888)877-6101_Inbound_Missed.log",
                "20170812_1451_(662)613-1026_(888)877-6101_Inbound_Missed.log",
                "20170812_1454_(606)505-8787_(888)877-6101_Inbound_Missed.log",
                "20170812_1508_(317)385-8388_(888)877-6101_Inbound_Missed.log",
                "20170812_1512_(812)592-7464_(317)348-3223_Inbound_Missed.log",
                "20170812_1516_(662)613-1026_(888)877-6101_Inbound_Missed.log",
                "20170812_1518_(662)613-1026_(888)877-6101_Inbound_Missed.log",
                "20170812_1529_(662)510-2217_(888)877-6101_Inbound_Missed.log",
                "20170812_1530_(662)510-2217_(888)877-6101_Inbound_Missed.log",
                "20170812_1534_(317)385-8630_(317)348-3223_Inbound_Missed.log",
                "20170812_1554_(606)558-3518_(888)877-6101_Inbound_Missed.log",
                "20170812_1607_(606)558-3518_(937)829-3679_Outbound_Stopped.log",
                "20170812_1612_(812)827-1424_(317)348-3223_Inbound_Accepted.log",
                "20170812_1612_(812)827-1424_(317)348-3223_Inbound_Accepted_Recording.mp3",
                "20170812_1624_(601)942-2975_(888)877-6101_Inbound_Missed.log",
                "20170812_1628_(985)778-1158_(888)877-6101_Inbound_Accepted.log",
                "20170812_1628_(985)778-1158_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170812_1640_(662)449-4128_(888)877-6101_Inbound_Accepted.log",
                "20170812_1640_(662)449-4128_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170812_1654_(888)560-1550_(606)558-3518_Outbound_Call-connected.log",
                "20170812_1654_(888)560-1550_(606)558-3518_Outbound_Call-connected_Recording.mp3",
                "20170812_1657_(888)560-1550_(317)385-8630_Outbound_Call-connected.log",
                "20170812_1657_(888)560-1550_(317)385-8630_Outbound_Call-connected_Recording.mp3",
                "20170812_1658_(859)202-3072_(423)525-0819_Outbound_Call-connected.log",
                "20170812_1700_(888)560-1550_(662)510-2217_Outbound_Call-connected.log",
                "20170812_1700_(888)560-1550_(662)510-2217_Outbound_Call-connected_Recording.mp3",
                "20170812_1703_(888)560-1550_(662)613-1026_Outbound_Call-connected.log",
                "20170812_1703_(888)560-1550_(662)613-1026_Outbound_Call-connected_Recording.mp3",
                "20170812_1704_(812)592-7464_(317)348-3223_Inbound_Voicemail.log",
                "20170812_1710_(574)773-5670_(888)877-6101_Inbound_Missed.log",
                "20170812_1711_(574)773-5670_(888)877-6101_Inbound_Missed.log",
                "20170812_1711_(662)647-3114_(888)877-6101_Inbound_Missed.log",
                "20170812_1712_(662)739-0251_(888)877-6101_Inbound_Missed.log",
                "20170812_1715_(937)429-5830_(888)560-1550_Inbound_Voicemail.log",
                "20170812_1715_(937)429-5830_(888)560-1550_Inbound_Voicemail_Message.mp3",
                "20170812_1718_(937)429-5830_(888)560-1550_Inbound_Missed.log",
                "20170812_1720_(937)429-5830_(888)560-1550_Inbound_Voicemail.log",
                "20170812_1720_(937)429-5830_(888)560-1550_Inbound_Voicemail_Message.mp3",
                "20170812_1721_(513)601-6318_(740)661-4466_Inbound_Accepted.log",
                "20170812_1721_(513)601-6318_(740)661-4466_Inbound_Accepted_Recording.mp3",
                "20170812_1722_(888)560-1550_(601)942-2975_Outbound_Call-connected.log",
                "20170812_1722_(888)560-1550_(601)942-2975_Outbound_Call-connected_Recording.mp3",
                "20170812_1724_(888)560-1550_(574)773-5670_Outbound_Call-connected.log",
                "20170812_1724_(888)560-1550_(574)773-5670_Outbound_Call-connected_Recording.mp3",
                "20170812_1727_(937)364-4118_(937)310-5441_Inbound_Missed.log",
                "20170812_1735_(419)576-2507_(740)661-4466_Inbound_Accepted.log",
                "20170812_1735_(419)576-2507_(740)661-4466_Inbound_Accepted_Recording.mp3",
                "20170812_1741_(931)261-3467_(888)877-6101_Inbound_Accepted.log",
                "20170812_1741_(931)261-3467_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170812_1744_(888)560-1550_(662)647-3114_Outbound_Call-connected.log",
                "20170812_1744_(888)560-1550_(662)647-3114_Outbound_Call-connected_Recording.mp3",
                "20170812_1745_(423)357-5266_(888)877-6101_Inbound_Accepted.log",
                "20170812_1745_(423)357-5266_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170812_1749_(888)560-1550_(662)739-0251_Outbound_Call-connected.log",
                "20170812_1749_(888)560-1550_(662)739-0251_Outbound_Call-connected_Recording.mp3",
                "20170812_1757_(888)560-1550_(317)385-8388_Outbound_Call-connected.log",
                "20170812_1757_(888)560-1550_(317)385-8388_Outbound_Call-connected_Recording.mp3",
                "20170812_1759_(888)560-1550_(606)505-8787_Outbound_Call-connected.log",
                "20170812_1759_(888)560-1550_(606)505-8787_Outbound_Call-connected_Recording.mp3",
                "20170812_1800_(317)385-8388_(888)560-1550_Inbound_Missed.log",
                "20170812_1800_(888)560-1550_(513)770-2181_Outbound_Call-connected.log",
                "20170812_1800_(888)560-1550_(513)770-2181_Outbound_Call-connected_Recording.mp3",
                "20170812_1801_(888)560-1550_(662)739-0251_Outbound_Call-connected.log",
                "20170812_1801_(888)560-1550_(662)739-0251_Outbound_Call-connected_Recording.mp3",
                "20170812_1807_(865)585-2699_(888)877-6101_Inbound_Missed.log",
                "20170812_1842_(888)560-1550_(865)585-2699_Outbound_Call-connected.log",
                "20170812_1842_(888)560-1550_(865)585-2699_Outbound_Call-connected_Recording.mp3",
                "20170812_1845_(888)560-1550_(859)322-1917_Outbound_Call-connected.log",
                "20170812_1845_(888)560-1550_(859)322-1917_Outbound_Call-connected_Recording.mp3",
                "20170812_1859_(574)361-0689_(616)888-6855_Inbound_Missed.log",
                "20170812_1905_(740)418-1438_(888)877-6101_Inbound_Missed.log",
                "20170812_1907_(423)377-9844_(629)777-9199_Inbound_Missed.log",
                "20170812_1908_(870)557-4047_(740)661-4466_Inbound_Missed.log",
                "20170812_1909_(740)418-1438_(888)877-6101_Inbound_Accepted.log",
                "20170812_1909_(740)418-1438_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170812_1911_(937)384-4369_(937)660-9033_Inbound_Missed.log",
                "20170812_1914_(269)816-4737_(888)243-4151_Inbound_Missed.log",
                "20170812_1921_(423)377-9844_(629)777-9199_Inbound_Missed.log",
                "20170812_1927_(888)560-1550_(740)418-1438_Outbound_Call-connected.log",
                "20170812_1927_(888)560-1550_(740)418-1438_Outbound_Call-connected_Recording.mp3",
                "20170812_1933_(601)278-3125_(888)877-6101_Inbound_Missed.log",
                "20170812_1933_(601)278-3125_(888)877-6101_Inbound_Missed.log",
                "20170812_1933_(601)278-3125_(888)877-6101_Inbound_Missed.log",
                "20170812_1935_(601)278-3125_(888)877-6101_Inbound_Missed.log",
                "20170812_1936_(901)643-2254_(888)877-6101_Inbound_Missed.log",
                "20170812_1937_(334)431-0134_(888)877-6101_Inbound_Missed.log",
                "20170812_1942_(740)418-1438_(888)560-1550_Inbound_Missed.log",
                "20170812_1943_(888)560-1550_(740)418-1438_Outbound_Call-connected.log",
                "20170812_1943_(888)560-1550_(740)418-1438_Outbound_Call-connected_Recording.mp3",
                "20170812_1945_(888)560-1550_(740)418-1438_Outbound_Call-connected.log",
                "20170812_1945_(888)560-1550_(740)418-1438_Outbound_Call-connected_Recording.mp3",
                "20170812_1946_(423)377-9844_(629)777-9199_Inbound_Missed.log",
                "20170812_1956_(248)459-4118_(616)888-6855_Inbound_Accepted.log",
                "20170812_1956_(248)459-4118_(616)888-6855_Inbound_Accepted_Recording.mp3",
                "20170812_2001_(740)418-1438_(888)560-1550_Inbound_Missed.log",
                "20170812_2001_(888)560-1550_(574)361-0689_Outbound_Call-connected.log",
                "20170812_2001_(888)560-1550_(574)361-0689_Outbound_Call-connected_Recording.mp3",
                "20170812_2002_(740)418-1438_(888)560-1550_Inbound_Accepted.log",
                "20170812_2002_(740)418-1438_(888)560-1550_Inbound_Accepted_Recording.mp3",
                "20170812_2011_(423)377-9844_(629)777-9199_Inbound_Accepted.log",
                "20170812_2011_(423)377-9844_(629)777-9199_Inbound_Accepted_Recording.mp3",
                "20170812_2011_(888)560-1550_(870)557-4047_Outbound_Call-connected.log",
                "20170812_2011_(888)560-1550_(870)557-4047_Outbound_Call-connected_Recording.mp3",
                "20170812_2017_(601)906-3611_(888)877-6101_Inbound_Missed.log",
                "20170812_2027_(888)560-1550_(901)643-2254_Outbound_Call-connected.log",
                "20170812_2027_(888)560-1550_(901)643-2254_Outbound_Call-connected_Recording.mp3",
                "20170812_2033_(901)896-9318_(888)877-6101_Inbound_Accepted.log",
                "20170812_2033_(901)896-9318_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170812_2034_(901)896-9318_(888)877-6101_Inbound_Accepted.log",
                "20170812_2034_(901)896-9318_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170812_2039_(888)560-1550_(334)431-0134_Outbound_Call-connected.log",
                "20170812_2039_(888)560-1550_(334)431-0134_Outbound_Call-connected_Recording.mp3",
                "20170812_2051_(423)254-4607_(888)877-6101_Inbound_Accepted.log",
                "20170812_2051_(423)254-4607_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170812_2052_(423)254-4607_(888)877-6101_Inbound_Accepted.log",
                "20170812_2052_(423)254-4607_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170812_2059_(800)955-6600_(937)310-5441_Inbound_Missed.log",
                "20170812_2107_(765)228-6769_(888)877-6101_Inbound_Accepted.log",
                "20170812_2107_(765)228-6769_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170812_2113_(662)752-1463_(888)877-6101_Inbound_Accepted.log",
                "20170812_2113_(662)752-1463_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170812_2120_(205)764-2924_(888)877-6101_Inbound_Missed.log",
                "20170812_2126_(419)889-7254_(740)661-4466_Inbound_Accepted.log",
                "20170812_2126_(419)889-7254_(740)661-4466_Inbound_Accepted_Recording.mp3",
                "20170812_2128_(706)218-2266_(888)877-6101_Inbound_Missed.log",
                "20170812_2134_(888)560-1550_(205)764-2924_Outbound_Call-connected.log",
                "20170812_2134_(888)560-1550_(205)764-2924_Outbound_Call-connected_Recording.mp3",
                "20170812_2135_(937)508-1494_(616)888-6855_Inbound_Accepted.log",
                "20170812_2135_(937)508-1494_(616)888-6855_Inbound_Accepted_Recording.mp3",
                "20170812_2142_(888)560-1550_(706)218-2266_Outbound_Call-connected.log",
                "20170812_2142_(888)560-1550_(706)218-2266_Outbound_Call-connected_Recording.mp3",
                "20170812_2149_(870)557-4047_(888)560-1550_Inbound_Missed.log",
                "20170812_2210_(901)826-1047_(888)877-6101_Inbound_Accepted.log",
                "20170812_2210_(901)826-1047_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170812_2210_(901)826-1047_(888)877-6101_Inbound_Missed.log",
                "20170812_2310_(513)888-3824_(888)877-6101_Inbound_Accepted.log",
                "20170812_2310_(513)888-3824_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170812_2347_(606)595-7323_(888)877-6101_Inbound_Accepted.log",
                "20170812_2347_(606)595-7323_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170812_2350_(765)615-1551_(888)243-4151_Inbound_Accepted.log",
                "20170812_2350_(865)293-7354_(888)877-6101_Inbound_Accepted.log",
                "20170812_2350_(865)293-7354_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170812_2355_(662)519-3838_(888)877-6101_Inbound_Accepted.log",
                "20170812_2355_(662)519-3838_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170813_0101_(423)646-5290_(888)877-6101_Inbound_Voicemail.log",
                "20170813_0101_(423)646-5290_(888)877-6101_Inbound_Voicemail_Message.mp3",
                "20170813_0115_(706)879-1160_(888)877-6101_Inbound_Voicemail.log",
                "20170813_0115_(706)879-1160_(888)877-6101_Inbound_Voicemail_Message.mp3",
                "20170813_0412_(423)580-9275_(888)877-6101_Inbound_Missed.log",
                "20170813_1356_(937)603-3582_(937)310-5441_Inbound_Missed.log",
                "20170813_1405_(662)288-5438_(888)877-6101_Inbound_Missed.log",
                "20170813_1431_(870)945-1288_(888)877-6101_Inbound_Accepted.log",
                "20170813_1431_(870)945-1288_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170813_1454_(901)286-0208_(888)877-6101_Inbound_Accepted.log",
                "20170813_1454_(901)286-0208_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170813_1504_(706)879-1160_(888)877-6101_Inbound_Accepted.log",
                "20170813_1504_(706)879-1160_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170813_1546_(662)519-4962_(888)877-6101_Inbound_Missed.log",
                "20170813_1546_(937)270-5057_(937)310-5441_Inbound_Missed.log",
                "20170813_1604_(606)657-4001_(662)519-4962_Outbound_Call-connected.log",
                "20170813_1604_(606)657-4001_(662)519-4962_Outbound_Call-connected_Recording.mp3",
                "20170813_1625_(423)525-6791_(888)877-6101_Inbound_Accepted.log",
                "20170813_1625_(423)525-6791_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170813_1649_(662)213-0662_(888)877-6101_Inbound_Missed.log",
                "20170813_1650_(662)213-0662_(888)877-6101_Inbound_Accepted.log",
                "20170813_1650_(662)213-0662_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170813_1744_(662)832-3523_(888)877-6101_Inbound_Accepted.log",
                "20170813_1744_(662)832-3523_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170813_1806_(769)610-6241_(888)877-6101_Inbound_Accepted.log",
                "20170813_1806_(769)610-6241_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170813_1809_(865)566-3569_(888)877-6101_Inbound_Missed.log",
                "20170813_1820_(606)657-4001_(865)566-3569_Outbound_Call-connected.log",
                "20170813_1820_(606)657-4001_(865)566-3569_Outbound_Call-connected_Recording.mp3",
                "20170813_1836_(734)341-6617_(616)888-6855_Inbound_Accepted.log",
                "20170813_1836_(734)341-6617_(616)888-6855_Inbound_Accepted_Recording.mp3",
                "20170813_1848_(662)312-8168_(888)877-6101_Inbound_Accepted.log",
                "20170813_1848_(662)312-8168_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170813_1905_(423)432-0007_(888)877-6101_Inbound_Accepted.log",
                "20170813_1905_(423)432-0007_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170813_1935_(423)579-3033_(888)877-6101_Inbound_Accepted.log",
                "20170813_1935_(423)579-3033_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170813_1944_(205)330-9227_(888)877-6101_Inbound_Accepted.log",
                "20170813_1944_(205)330-9227_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170813_1950_(740)506-0357_(888)877-6101_Inbound_Accepted.log",
                "20170813_1950_(740)506-0357_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170813_1957_(937)710-2106_(740)661-4466_Inbound_Missed.log",
                "20170813_1958_(937)710-2106_(740)661-4466_Inbound_Missed.log",
                "20170813_2015_(865)235-3274_(888)877-6101_Inbound_Accepted.log",
                "20170813_2015_(865)235-3274_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170813_2025_(304)633-3750_(888)877-6101_Inbound_Accepted.log",
                "20170813_2025_(304)633-3750_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170813_2031_(606)657-4001_(937)710-2106_Outbound_Call-connected.log",
                "20170813_2031_(606)657-4001_(937)710-2106_Outbound_Call-connected_Recording.mp3",
                "20170813_2047_(765)437-7396_(317)348-3223_Inbound_Accepted.log",
                "20170813_2047_(765)437-7396_(317)348-3223_Inbound_Accepted_Recording.mp3",
                "20170813_2144_(706)889-7405_(888)877-6101_Inbound_Accepted.log",
                "20170813_2144_(706)889-7405_(888)877-6101_Inbound_Accepted_Recording.mp3",
                "20170813_2305_(888)578-3436_(937)310-5441_Inbound_Missed.log",
                "20170813_2312_(662)404-0835_(888)877-6101_Inbound_Missed.log",
                "20170813_2337_(888)578-3436_(937)310-5441_Inbound_Missed.log",
                "20170813_2340_(765)318-7574_(317)348-3223_Inbound_Missed.log",
                "20170814_1729_x5582_(888)243-4151_Inbound_Missed.log",
                "20170814_1744_(205)208-1300_(901)609-8828_Outbound_Call-connected.log",
                "20170814_1744_(205)208-1300_(901)609-8828_Outbound_Call-connected_Recording.mp3",
                "20170814_2137_(888)243-4151_(937)944-0308_Outbound_Call-connected.log",
                "20170815_1436_(205)208-1300_(830)730-7060_Outbound_Call-connected.log",
                "20170815_1436_(205)208-1300_(830)730-7060_Outbound_Call-connected_Recording.mp3",
                "20170815_1705_x5582_x5587_Outbound_Call-connected.log"
            };
            //201706
            var sourceRingCentralId = "191625028";
            var sourceFolderId = "0B5pWMgg3fgamalcteGItSElrSUU";
            var sourceGoogleAccountId = 61;
            var targetRingCentralId = "400234016";
            var targetFolderId = "0B4BWiFyv3VCgaVBvWjNJN1VId1k";
            var targetGoogleAccountId = 23;
            var gSource = new GoogleActions.GoogleFolder(sourceRingCentralId, sourceGoogleAccountId, sourceFolderId);
            gSource.Execute();
            if (gSource.ResultException != null || gSource.Files == null || gSource.Files.files == null)
            {
                //ERROR
                throw new Exception("Unhandled error");
            }
            else
            {
                allFiles.AddRange(gSource.Files.files);
                while (!string.IsNullOrEmpty(gSource.Files.nextPageToken))
                {
                    gSource.NextPageToken(gSource.Files.nextPageToken);
                    gSource.Execute();
                    if (gSource.ResultException != null || gSource.Files == null || gSource.Files.files == null)
                    {
                        //ERROR
                        throw new Exception("Unhandled error");
                    }
                    else
                    {
                        allFiles.AddRange(gSource.Files.files);
                    }
                }
                var subsetFiles = allFiles.Where(x => fileListFilter.Contains(x.name));
                foreach (var file in subsetFiles)
                {
                    var stat = new FileTransferAndCleanStat();
                    stats.Add(stat);
                    stat.SourceRingCentralId = gSource.RingCentralId;
                    stat.SourceGoogleAccountId = gSource.GoogleAccountId;
                    stat.SourceFolderId = gSource.FolderId;
                    stat.SourceFileId = file.id;
                    stat.Filename = file.name;
                    var gDest = new GoogleActions.GoogleFolder(targetRingCentralId, targetGoogleAccountId, targetFolderId).FileName(file.name);
                    gDest.Execute();
                    if (gDest.ResultException != null || gDest.Files == null || gDest.Files.files == null)
                    {
                        //ERROR
                        stat.Status = "error";
                        stat.Message = gDest.ResultException != null ? gDest.ResultException.Message : "unknown destination lookup error";
                    }
                    else
                    {
                        stat.FilesFoundInDest = gDest.Files.files.Count;
                        var gDownload = new GoogleActions.GoogleDownload(sourceRingCentralId, sourceGoogleAccountId).FileId(file.id);
                        gDownload.Execute();
                        if (gDownload.ResultException != null || gDownload.FileData == null || gDownload.FileData.Length < 1)
                        {
                            //ERROR
                            stat.Status = "error";
                            stat.Message = gDownload.ResultException != null ? gDownload.ResultException.Message : "unknown dowload error";
                        }
                        else
                        {
                            stat.Bytes = gDownload.FileData.Length;
                            var deleteCount = 0;
                            foreach (var fileToDeleteInDest in gDest.Files.files)
                            {
                                var gDelete = new GoogleActions.DeleteFile(targetRingCentralId, targetGoogleAccountId).FileId(fileToDeleteInDest.id);
                                gDelete.Execute();
                                if (gDelete.ResultException != null)
                                    stat.Message = gDelete.ResultException != null ? gDelete.ResultException.Message : "unknown delete error";
                                else
                                    deleteCount++;
                            }
                            if (deleteCount != gDest.Files.files.Count)
                            {
                                stat.Status = "error";
                            }
                            else
                            {
                                var gUpload = new GoogleActions.GoogleUpload(targetRingCentralId, targetGoogleAccountId);
                                gUpload.Folder(targetFolderId);
                                gUpload.FileName(file.name);
                                gUpload.FileData(gDownload.FileData);
                                stat.TargetRingCentralId = gUpload.RingCentralId;
                                stat.TargetGoogleAccountId = gUpload.GoogleAccountId;
                                stat.TargetFolderId = gUpload.FolderId;
                                gUpload.Execute();
                                if (gUpload.ResultException != null || gUpload.Response == null || string.IsNullOrWhiteSpace(gUpload.Response.id))
                                {
                                    //ERROR
                                    stat.Status = "error";
                                    stat.Message = gUpload.ResultException != null ? gUpload.ResultException.Message : "unknown upload error";
                                }
                                else
                                {
                                    stat.TargetFileId = gUpload.Response.id;
                                    stat.Replaced = gUpload.Replaced;
                                    stat.Status = "success";
                                    var gDestAfter = new GoogleActions.GoogleFolder(targetRingCentralId, targetGoogleAccountId, targetFolderId).FileName(file.name);
                                    gDestAfter.Execute();
                                    if (gDestAfter.ResultException != null || gDestAfter.Files == null || gDestAfter.Files.files == null)
                                    {
                                        //ERROR
                                        stat.Status = "error";
                                        stat.Message = gDestAfter.ResultException != null ? gDestAfter.ResultException.Message : "unknown destination lookup error (2)";
                                    }
                                    else
                                    {
                                        stat.FilesFoundInDestAfter = gDestAfter.Files.files.Count;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return stats;
            //var properties = typeof(FileTransferAndCleanStat).GetFields();
            //var result = new StringBuilder();
            //var header = string.Join(",", properties.Select(x => x.Name).ToList());
            //result.AppendLine(header);
            //foreach (var row in stats)
            //{
            //    var values = properties.Select(p => p.GetValue(row));
            //    var line = string.Join(",", values);
            //    result.AppendLine(line);
            //}
            //using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"D:\temp\file-xfer-clean-stats-201708.csv"))
            //{
            //    file.Write(result.ToString());
            //}
        }

        public static ICollection<FileListStat> ScanFolder()
        {
            //SCAN ALL FILES IN A GOOGLE FOLDER
            var allFiles = new List<GoogleActions.GoogleFolder.GoogleFolderResponse.File>();
            //201706
            var testRingCentralId = "400234016";
            var testFolderId = "0B4BWiFyv3VCgaVBvWjNJN1VId1k";
            var testGoogleAccountId = 23;
            var gTest = new GoogleActions.GoogleFolder(testRingCentralId, testGoogleAccountId, testFolderId);
            //gTest.FileName("20170801_0121_(901)831-4649_(888)877-6101_Inbound_Voicemail_Message.mp3");
            gTest.Execute();
            if (gTest.ResultException != null || gTest.Files == null || gTest.Files.files == null)
            {
                //ERROR
                throw new Exception("Unhandled error");
            }
            else
            {
                allFiles.AddRange(gTest.Files.files);
                while (!string.IsNullOrEmpty(gTest.Files.nextPageToken))
                {
                    gTest.NextPageToken(gTest.Files.nextPageToken);
                    gTest.Execute();
                    if (gTest.ResultException != null || gTest.Files == null || gTest.Files.files == null)
                    {
                        //ERROR
                        throw new Exception("Unhandled error");
                    }
                    else
                    {
                        allFiles.AddRange(gTest.Files.files);
                    }
                }
                var stats = new List<FileListStat>();
                foreach (var file in allFiles.OrderBy(x => x.name))
                {
                    stats.Add(new FileListStat()
                    {
                        FileId = file.id,
                        Filename = file.name,
                        FolderId = gTest.FolderId,
                        GoogleAccountId = gTest.GoogleAccountId,
                        MimeType = file.mimeType,
                        RingCentralId = gTest.RingCentralId,
                        Size = file.size,
                        Trashed = file.trashed
                    });
                }
                return stats;
                //var properties = typeof(FileListStat).GetFields();
                //var result = new StringBuilder();
                //var header = string.Join(",", properties.Select(x => x.Name).ToList());
                //result.AppendLine(header);
                //foreach (var row in stats)
                //{
                //    var values = properties.Select(p => p.GetValue(row));
                //    var line = string.Join(",", values);
                //    result.AppendLine(line);
                //}
                //using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"D:\temp\kaan-file-list-201708.csv"))
                //{
                //    file.Write(result.ToString());
                //}
            }

        }

        public class FileTransferStat
        {
            public string SourceRingCentralId;
            public string SourceFolderId;
            public int SourceGoogleAccountId;
            public string TargetRingCentralId;
            public string TargetFolderId;
            public int TargetGoogleAccountId;
            public string SourceFileId;
            public string Filename;
            public string TargetFileId;
            public string Status;
            public string Message;
            public int Bytes;
            public bool Replaced;
            public int FilesWithSameName;
        }

        public class FileTransferAndCleanStat
        {
            public string SourceRingCentralId;
            public string SourceFolderId;
            public int SourceGoogleAccountId;
            public string TargetRingCentralId;
            public string TargetFolderId;
            public int TargetGoogleAccountId;
            public string SourceFileId;
            public string Filename;
            public string TargetFileId;
            public string Status;
            public string Message;
            public int Bytes;
            public bool Replaced;
            public int FilesFoundInDest;
            public int FilesFoundInDestAfter;
        }


        public class FileListStat
        {
            public string RingCentralId;
            public string FolderId;
            public int GoogleAccountId;
            public string FileId;
            public string Filename;
            public string MimeType;
            public long Size;
            public string Trashed;
        }
    }
}