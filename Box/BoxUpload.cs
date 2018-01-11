using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate.Criterion;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Box
{
    public class BoxUpload : BoxAction
    {
        public string Email;
		private byte[] fileData;
		private string fileName;
		private string folderId;
        public bool Replaced;
        private string navigateToOrCreateSubFolder;
		public BoxResponse Response;

		public BoxUpload(string username, int boxAccountId)
            : base(username, boxAccountId)
        {
			fileName = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd_hh-mm-ss-tt");
			folderId = "0";
        }
        public override void DoAction()
        {
			try
			{
				if (!string.IsNullOrWhiteSpace(navigateToOrCreateSubFolder))
				{
					navigateToOrCreateSubFolderExecute();
				}

                Response = uploadFile();

				if (Response != null)
				{
					if (Response.total_count == 1 && Response.entries != null && Response.entries.Count == 1)
					{
						//SUCCESS
					}
					else
					{
                        if (Response.context_info.conflicts != null && !string.IsNullOrWhiteSpace(Response.context_info.conflicts.id))
                        {
                            Response = updateExistingFile(Response.context_info.conflicts.id);
                            if (Response != null)
                            {
                                if (Response.total_count == 1 && Response.entries != null && Response.entries.Count == 1)
                                {
                                    //SUCCESS
                                    Replaced = true;
                                }
                                else
                                {
                                    if (Response.context_info == null || Response.context_info.errors == null || Response.context_info.errors.Count == 0)
                                    {
                                        if (!string.IsNullOrWhiteSpace(Response.message))
                                            ResultException = new Exception("Unable to upload file - " + Response.message);
                                        else
                                            ResultException = new Exception("Unable to upload file");
                                    }
                                    else
                                    {
                                        var errs = "";
                                        var first = true;
                                        foreach (var err in Response.context_info.errors)
                                        {
                                            if (!first)
                                                errs += " | ";
                                            errs += "reason: " + err.reason + ", name: " + err.name + ", message: " + err.message;
                                            first = false;
                                        }
                                        ResultException = new Exception("Unable to upload file (" + errs + ")");
                                    }
                                }
                            }
                            else
                            {
                                ResultException = new Exception("Unable to upload file");
                            }
                        }
                        else if (Response.context_info == null || Response.context_info.errors == null || Response.context_info.errors.Count == 0)
						{
							if (!string.IsNullOrWhiteSpace(Response.message))
                                ResultException = new Exception("Unable to upload file - " + Response.message);
                            else
                                ResultException = new Exception("Unable to upload file");
                        }
                        else
						{
                            var errs = "";
                            var first = true;
                            foreach (var err in Response.context_info.errors)
                            {
                                if (!first)
                                    errs += " | ";
                                errs += "reason: " + err.reason + ", name: " + err.name + ", message: " + err.message;
                                first = false;
                            }
                            ResultException = new Exception("Unable to upload file (" + errs + ")");
                        }
					}
				}
				else
				{
					ResultException = new Exception("Unable to upload file");
				}
			}
			catch (WebException ex)
			{
				if (ex.HResult == 401 || ex.Message.Contains("401") || ex.Message.Contains("Unauthorized"))
					throw ex;
				else
					ResultException = ex;
			}
			catch (Exception ex)
			{
                if (ex.Message == "Unauthorized")
                    throw ex;
                else
    				ResultException = ex;
			}
		}

		public BoxUpload Folder(string folderId)
		{
			this.folderId = folderId;
			return this;
		}
		public BoxUpload FileName(string fileName)
		{
			this.fileName = fileName;
			return this;
		}
		public BoxUpload FileData(byte[] fileData)
		{
			this.fileData = fileData;
			return this;
		}
		public BoxUpload NavigateToOrCreateSubFolder(string folderName)
		{
			this.navigateToOrCreateSubFolder = folderName;
			return this;
		}

        private BoxResponse uploadFile()
        {
            var url = "https://upload.box.com/api/2.0/files/content";
            var client = new HttpClient();
            var content = new MultipartFormDataContent();
            var attributes = new
            {
                name = fileName,
                parent = new
                {
                    id = folderId
                }
            };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            var serializer = new JavaScriptSerializer();
            var stringContent = new StringContent(serializer.Serialize(attributes));
            stringContent.Headers.Add("Content-Disposition", "form-data; name=\"attributes\"");
            content.Add(stringContent, "json");
            var stream = new System.IO.MemoryStream(fileData);
            var streamContent = new StreamContent(stream);
            streamContent.Headers.Add("Content-Type", "application/octet-stream");
            streamContent.Headers.Add("Content-Disposition", "form-data; name=\"file\"; filename=\"" + fileName + "\"");
            content.Add(streamContent, "file", fileName);
            Task<HttpResponseMessage> message = client.PostAsync(url, content);
            if (message.Result.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new Exception("Unauthorized");
            }
            var input = message.Result.Content.ReadAsStringAsync();
            Console.WriteLine(input.Result.ToString());
            stream.Close();
            var response = serializer.Deserialize<BoxResponse>(input.Result);
            return response;
        }

        private BoxResponse updateExistingFile(string fileId)
        {
            var url = string.Format("https://upload.box.com/api/2.0/files/{0}/content", fileId);
            var client = new HttpClient();
            var content = new MultipartFormDataContent();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
            var serializer = new JavaScriptSerializer();
            var stream = new System.IO.MemoryStream(fileData);
            var streamContent = new StreamContent(stream);
            streamContent.Headers.Add("Content-Type", "application/octet-stream");
            //streamContent.Headers.Add("Content-Disposition", "form-data; name=\"file\"; filename=\"" + fileName + "\"");
            content.Add(streamContent, "file", fileName);
            Task<HttpResponseMessage> message = client.PostAsync(url, content);
            var input = message.Result.Content.ReadAsStringAsync();
            Console.WriteLine(input.Result.ToString());
            stream.Close();
            var response = serializer.Deserialize<BoxResponse>(input.Result);
            return response;
        }

        private void navigateToOrCreateSubFolderExecute()
		{
			var boxEmailUrl = "https://api.box.com/2.0/folders/" + folderId;
			var requestParams = new NameValueCollection();
			requestParams.Add("access_token", AccessToken);
			var array = (from key in requestParams.AllKeys
						 from value in requestParams.GetValues(key)
						 select string.Format("{0}={1}", key, value))
				.ToArray();
			var queryString = string.Join("&", array);
			var url = boxEmailUrl + "?" + queryString;
			WebRequest request = WebRequest.Create(boxEmailUrl);
			request.Method = "GET";
			request.ContentType = "application/x-www-form-urlencoded";
			request.Headers.Add("Authorization", "Bearer " + AccessToken);
			WebResponse response = request.GetResponse();
			Stream dataStream = response.GetResponseStream();
			StreamReader reader = new StreamReader(dataStream);
			string responseFromServer = reader.ReadToEnd();
			JavaScriptSerializer jss = new JavaScriptSerializer();
			BoxFolderData data = jss.Deserialize<BoxFolderData>(responseFromServer);
			reader.Close();
			dataStream.Close();
			response.Close();

			if (data == null)
			{
				throw new Exception("Unable to navigate to or create sub folder in Box.com");
			}
			if (data.item_collection == null || data.item_collection.entries == null || !data.item_collection.entries.Any(x => x.type == "folder" && x.name.ToLower() == navigateToOrCreateSubFolder.ToLower()))
			{
				//CREATE NEW SUB FOLDER
				folderId = createSubFolder(folderId, navigateToOrCreateSubFolder);
			}
			else
			{
				//USE EXISTING SUB FOLDER
				folderId = data.item_collection.entries.First(x => x.type == "folder" && x.name.ToLower() == navigateToOrCreateSubFolder.ToLower()).id;
			}
		}

		private string createSubFolder(string folderId, string newFolderName)
		{
			string newFolderId = "0";
			try
			{
				var url = "https://api.box.com/2.0/folders";
				var client = new HttpClient();
				var attributes = new
				{
					name = newFolderName,
					parent = new
					{
						id = folderId
					}
				};

				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

				var serializer = new JavaScriptSerializer();
				//var stringContent = new StringContent(serializer.Serialize(attributes));
				//stringContent.Headers.Add("Content-Disposition", "form-data; name=\"attributes\"");
				var content = new StringContent(serializer.Serialize(attributes));
				//content.Add(stringContent, "json");
				Task<HttpResponseMessage> message = client.PostAsync(url, content);
				var input = message.Result.Content.ReadAsStringAsync();
				Console.WriteLine(input.Result.ToString());

				var data = serializer.Deserialize<BoxFolderData>(input.Result);

				if (data != null && !string.IsNullOrWhiteSpace(data.id))
				{
					newFolderId = data.id;
				}
				else
				{
					ResultException = new Exception("Unable to create folder (" + newFolderName + ")");
				}
			}
			catch (WebException ex)
			{
				if (ex.HResult == 401)
					throw ex;
				else
					ResultException = ex;
			}
			catch (Exception ex)
			{
				ResultException = ex;
			}

			return newFolderId;

		}

		public class BoxResponse
		{
			public string type { get; set; }
			public string status { get; set; }
			public string code { get; set; }
			public string help_url { get; set; }
			public string message { get; set; }
			public string request_id { get; set; }
			public int total_count { get; set; }
			public List<Entry> entries { get; set; }
			public ContextInfo context_info { get; set; }
			public class Entry
			{
				public string id { get; set; }
				public string name { get; set; }
				public string description { get; set; }
			}
			public class ContextInfo
			{
				public List<Error> errors { get; set; }
                public Conflicts conflicts;
				public class Error
				{
					public string reason { get; set; }
					public string name { get; set; }
					public string message { get; set; }
				}
                public class Conflicts
                {
                    public string type;
                    public string id;
                    public FileVersion file_version;
                    public string sequence_id;
                    public string etag;
                    public string sha1;
                    public string name;
                }
                public class FileVersion
                {
                    public string type;
                    public string sha1;
                }

            }
		}

		public class BoxFolderData
		{
			public string id { get; set; }
			public string name { get; set; }
			public string description { get; set; }
			public PathCollection path_collection { get; set; }
			public ItemCollection item_collection { get; set; }
			public class PathCollection
			{
				public int total_Count { get; set; }
				public List<Entry> entries { get; set; }
			}
			public class ItemCollection
			{
				public int total_Count { get; set; }
				public List<Entry> entries { get; set; }
			}
			public class Entry
			{
				public string type { get; set; }
				public string id { get; set; }
				public string sequence_id { get; set; }
				public string name { get; set; }
			}
		}



	}
}
