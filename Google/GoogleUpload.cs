using FluentNHibernate.Mapping;
using GoogleActions;
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
using System.Web;
using System.Web.Script.Serialization;

namespace GoogleActions
{
    public class GoogleUpload : GoogleAction
    {
		private byte[] fileData;
		private string fileName;
		private string folderId;
		private string navigateToOrCreateSubFolder;
		public GoogleResponse Response;
        public bool Replaced;
        public int FilesThatExistWithSameName;

		public GoogleUpload(string username, int googleAccountId)
            : base(username, googleAccountId)
        {
			this.fileName = DateTime.Now.ToUniversalTime().ToString("yyyy-MM-dd_hh-mm-ss-tt");
			this.folderId = "root";
        }
        public override void DoAction()
        {
			try
			{
				if (!string.IsNullOrWhiteSpace(navigateToOrCreateSubFolder))
				{
					navigateToOrCreateSubFolderExecute();
				}

                var existingFiles = fileExists(this.fileName);
                if (existingFiles != null && existingFiles.Any())
                {
                    var existingFile = existingFiles.First();
                    FilesThatExistWithSameName = existingFiles.Count();
                    string requestUrl = "https://www.googleapis.com/upload/drive/v3/files/" + existingFile;

                    HttpClient client = new HttpClient();
                    var method = new HttpMethod("PATCH");
                    var content = new MultipartFormDataContent();
                    var attributes = new
                    {
                        trashed = false
                    };
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

                    var attrSerializer = new JavaScriptSerializer();
                    var stringContent = new StringContent(attrSerializer.Serialize(attributes));
                    stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    content.Add(stringContent);

                    var stream = new System.IO.MemoryStream(fileData);
                    var streamContent = new StreamContent(stream);
                    streamContent.Headers.Add("Content-Type", "application/octet-stream");
                    content.Add(streamContent, "file", fileName);

                    var request = new HttpRequestMessage(method, requestUrl)
                    {
                        Content = content
                    };
                    Task<HttpResponseMessage> task = client.SendAsync(request);
                    var response = task.Result.Content.ReadAsStringAsync();
                    var serializer = new JavaScriptSerializer();
                    Response = serializer.Deserialize<GoogleResponse>(response.Result);
                    if (Response != null)
                    {
                        if (!string.IsNullOrEmpty(Response.id))
                        {
                            //SUCCESS
                            Replaced = true;
                        }
                        else
                        {
                            if (Response.error != null && (!string.IsNullOrEmpty(Response.error.message) || !string.IsNullOrEmpty(Response.error.code)))
                            {
                                string msg = "Unable to upload file - ";
                                if (!string.IsNullOrEmpty(Response.error.message))
                                    msg += Response.error.message;
                                if (!string.IsNullOrEmpty(Response.error.code))
                                    msg += " (" + Response.error.code + ")";
                                ResultException = new Exception(msg);
                            }
                            else
                            {
                                ResultException = new Exception("Unable to upload file");
                            }
                        }
                    }
                    else
                    {
                        ResultException = new Exception("Unable to upload file");
                    }
                }
                else
                {
                    var url = "https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart";

                    var client = new HttpClient();
                    var content = new MultipartFormDataContent();
                    var attributes = new
                    {
                        name = fileName,
                        parents = new[] { folderId }
                    };


                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

                    var serializer = new JavaScriptSerializer();
                    var stringContent = new StringContent(serializer.Serialize(attributes));
                    stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    content.Add(stringContent);

                    var stream = new System.IO.MemoryStream(fileData);
                    var streamContent = new StreamContent(stream);
                    streamContent.Headers.Add("Content-Type", "application/octet-stream");
                    content.Add(streamContent, "file", fileName);

                    Task<HttpResponseMessage> message = client.PostAsync(url, content);
                    var input = message.Result.Content.ReadAsStringAsync();
                    //Console.WriteLine(input.Result.ToString());
                    stream.Close();

                    Response = serializer.Deserialize<GoogleResponse>(input.Result);

                    if (Response != null)
                    {
                        if (!string.IsNullOrEmpty(Response.id))
                        {
                            //SUCCESS
                        }
                        else
                        {
                            if (Response.error != null && (!string.IsNullOrEmpty(Response.error.message) || !string.IsNullOrEmpty(Response.error.code)))
                            {
                                string msg = "Unable to upload file - ";
                                if (!string.IsNullOrEmpty(Response.error.message))
                                    msg += Response.error.message;
                                if (!string.IsNullOrEmpty(Response.error.code))
                                    msg += " (" + Response.error.code + ")";
                                ResultException = new Exception(msg);
                            }
                            else
                            {
                                ResultException = new Exception("Unable to upload file");
                            }
                        }
                    }
                    else
                    {
                        ResultException = new Exception("Unable to upload file");
                    }
                }
            }
			catch (WebException ex)
			{
				if (ex.HResult == 401 || ex.Message.Contains("401"))
					throw ex;
				else
					ResultException = ex;
			}
			catch (Exception ex)
			{
				ResultException = ex;
			}
		}

        public string FolderId
        {
            get
            {
                return folderId;
            }
        }
        public GoogleUpload Folder(string folderId)
		{
			this.folderId = folderId;
			return this;
		}
		public GoogleUpload FileName(string fileName)
		{
			this.fileName = fileName;
			return this;
		}
		public GoogleUpload FileData(byte[] fileData)
		{
			this.fileData = fileData;
			return this;
		}

		public GoogleUpload NavigateToOrCreateSubFolder(string folderName)
		{
			this.navigateToOrCreateSubFolder = folderName;
			return this;
		}

        private IEnumerable<string> fileExists(string fileName)
        {
            var q = "mimeType != 'application/vnd.google-apps.folder' and '" + folderId + "' in parents and name = '" + fileName + "'";
            var googleEmailUrl = "https://www.googleapis.com/drive/v3/files";
            var requestParams = new NameValueCollection();
            requestParams.Add("key", RingClone.AppConfig.Google.ClientId);
            requestParams.Add("access_token", AccessToken);
            requestParams.Add("q", q);
            requestParams.Add("fields", "nextPageToken, files(id, name, trashed)");
            requestParams.Add("spaces", "drive");
            var array = (from key in requestParams.AllKeys
                         from value in requestParams.GetValues(key)
                         select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
                .ToArray();
            var queryString = string.Join("&", array);
            var url = googleEmailUrl + "?" + queryString;
            WebRequest request = WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "application/x-www-form-urlencoded";
            WebResponse response = request.GetResponse();
            Stream dataStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            string responseFromServer = reader.ReadToEnd();
            JavaScriptSerializer jss = new JavaScriptSerializer();
            var gResponse = jss.Deserialize<GoogleFolderResponse>(responseFromServer);
            reader.Close();
            dataStream.Close();
            response.Close();
            if (gResponse != null && gResponse.files != null && gResponse.files.Any(x => !x.trashed))
            {
                return gResponse.files.Select(x => x.id);
            }
            return null;
        }

        private void navigateToOrCreateSubFolderExecute()
		{
			var q = "mimeType='application/vnd.google-apps.folder' and '" + folderId + "' in parents";
			var googleEmailUrl = "https://www.googleapis.com/drive/v3/files";
			var requestParams = new NameValueCollection();
			requestParams.Add("key", RingClone.AppConfig.Google.ClientId);
			requestParams.Add("access_token", AccessToken);
			requestParams.Add("q", q);
			requestParams.Add("fields", "nextPageToken, files(id, name)");
			requestParams.Add("spaces", "drive");
			var array = (from key in requestParams.AllKeys
						 from value in requestParams.GetValues(key)
						 select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
				.ToArray();
			var queryString = string.Join("&", array);
			var url = googleEmailUrl + "?" + queryString;
			WebRequest request = WebRequest.Create(url);
			request.Method = "GET";

			request.ContentType = "application/x-www-form-urlencoded";
			WebResponse response = request.GetResponse();
			Stream dataStream = response.GetResponseStream();
			StreamReader reader = new StreamReader(dataStream);
			string responseFromServer = reader.ReadToEnd();
			JavaScriptSerializer jss = new JavaScriptSerializer();
			var folders = jss.Deserialize<GoogleFolderResponse>(responseFromServer);
			reader.Close();
			dataStream.Close();
			response.Close();

			if (folders == null)
			{
				throw new Exception("Unable to navigate to or create sub folder in Google");
			}
			if (folders.files == null || !folders.files.Any(x => x.name.ToLower() == navigateToOrCreateSubFolder.ToLower()))
			{
				//CREATE NEW SUB FOLDER
				folderId = createSubFolder(folderId, navigateToOrCreateSubFolder);
			}
			else
			{
				//USE EXISTING SUB FOLDER
				folderId = folders.files.First(x => x.name.ToLower() == navigateToOrCreateSubFolder.ToLower()).id;
			}
		}

		private string createSubFolder(string folderId, string newFolderName)
		{
			string newFolderId = "0";
			try
			{
				var url = "https://www.googleapis.com/upload/drive/v3/files";
				var client = new HttpClient();
				var content = new MultipartFormDataContent();
				var attributes = new
				{
					name = newFolderName,
					mimeType = "application/vnd.google-apps.folder",
					parents = new[] { folderId }
				};

				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);

				var serializer = new JavaScriptSerializer();
				var stringContent = new StringContent(serializer.Serialize(attributes));
				stringContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
				content.Add(stringContent);

				Task<HttpResponseMessage> message = client.PostAsync(url, content);
				var input = message.Result.Content.ReadAsStringAsync();
				Console.WriteLine(input.Result.ToString());

				Response = serializer.Deserialize<GoogleResponse>(input.Result);

				if (Response != null && !string.IsNullOrWhiteSpace(Response.id))
				{
					newFolderId = Response.id;
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

        //public static Task<HttpResponseMessage> PatchAsJsonAsync<T>(this HttpClient client, string requestUri, MultipartFormDataContent value)
        //{
        //    var content = new ObjectContent<T>(value, new JsonMediaTypeFormatter());
        //    var request = new HttpRequestMessage(new HttpMethod("PATCH"), requestUri) { Content = content };

        //    return client.SendAsync(request);
        //}
        public class GoogleFolderResponse
		{
			public string kind { get; set; }
			public List<File> files { get; set; }
			public class File
			{
				public string kind { get; set; }
				public string id { get; set; }
				public string name { get; set; }
                public string mimeType { get; set; }
                public bool trashed { get; set; }
            }
        }

		public class GoogleResponse
		{
			public virtual string kind { get; set; }
			public virtual string id { get; set; }
			public virtual string name { get; set; }
            public virtual string mimeType { get; set; }
            public virtual Error error { get; set; }
            public class Error
            {
                public virtual string code { get; set; }
                public virtual string message { get; set; }
            }
        }

    }
}
