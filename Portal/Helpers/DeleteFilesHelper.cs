using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace RingClone.Portal.Helpers
{
    public class DeleteFilesHelper
    {
        public static void DeleteFilesInGoogleFolder(string ringCentralId, int googleAccountId, string googleFolderId)
        {
            var files = new List<GoogleActions.GoogleFolder.GoogleFolderResponse.File>();
            var g = new GoogleActions.GoogleFolder(ringCentralId, googleAccountId, googleFolderId);
            g.Execute();
            if (g.Files != null && g.Files.files != null)
            {
                foreach (var f in g.Files.files)
                {
                    files.Add(f);
                }
            }
            if (!string.IsNullOrWhiteSpace(g.Files.nextPageToken))
            {
                while (!string.IsNullOrWhiteSpace(g.Files.nextPageToken))
                {
                    g.NextPageToken(g.Files.nextPageToken);
                    g.Execute();
                    if (g.Files != null && g.Files.files != null)
                    {
                        foreach (var f in g.Files.files)
                        {
                            files.Add(f);
                        }
                    }
                }
            }
            foreach (var file in files)
            {
                var d = new GoogleActions.DeleteFile(ringCentralId, googleAccountId);
                d.FileId(file.id);
                d.Execute();
                var r = d.ResponseFromServer;
            }

        }
    }
}