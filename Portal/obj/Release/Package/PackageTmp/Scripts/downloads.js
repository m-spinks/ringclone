var totalNotDownloaded = 0;
var totalNotSeen = 0;
function pollDownloads(refreshCache) {
    $.ajax({
        type: "GET",
        url: "/api/download/poll",
        data: { refreshCache: refreshCache === true },
        success: function (obj) {
            var somethingChanged = false;
            if (obj) {
                if (obj.TotalNotDownloaded != totalNotDownloaded || obj.totalNotSeen != totalNotSeen) {
                    if (obj.TotalNotDownloaded > 0)
                        $('#downloads button').html('Downloads (' + obj.TotalNotDownloaded + ')');
                    else
                        $('#downloads button').html('Downloads');
                    totalNotDownloaded = obj.TotalNotDownloaded;
                    totalNotSeen = obj.TotalNotSeen;
                    somethingChanged = true;
                }
                if (obj.Downloads && obj.Downloads.length > 0) {
                    $('#downloads').css('visibility', 'visible');
                }
                for (var i in obj.Downloads) {
                    var download = obj.Downloads[i];
                    if (!document.getElementById(download.DownloadId)) {
                        $('#downloads ul').prepend(generateDownloadContent(download));
                    } else {
                        var downloadEl = document.getElementById(download.DownloadId);
                        $(downloadEl).replaceWith(generateDownloadContent(download));
                    }
                }
                //TRIM HERE
            }
            $('#downloads [data-toggle="tooltip"]').tooltip({ html: true });
            if (somethingChanged)
                setTimeout(pollDownloads, 1500);
            else 
                setTimeout(pollDownloads, 7000);
        }
    });
}
function generateDownloadContent(download) {
    var icon = '<img src="/images/zip.svg" />';
    var progress = '';
    var cls = '';
    if (download.DownloadedInd)
        cls += ' downloaded';
    if (download.CompleteInd) {
        cls += ' complete';
        if (download.ErrorInd) {
            cls += ' error';
        }
        return '<li id="' + download.DownloadId + '" class="' + cls + '"><a href="/api/download/get?id=' + download.DownloadId + '" target="_New" class="filename" data-toggle="tooltip" data-placement="left" title="' + download.Tooltip + '"><span class="icon">' + icon + '</span>' + download.Filename + '</a></li>';
    } else {
        icon = '<img src="/images/zip-disabled.svg" />';
        if (download.Percent > 1) {
            progress = '<div class="download-progress"><div class="progress"><div class="progress-bar" role="progressbar" aria-valuenow="' + download.Percent + '" aria-valuemin="0" aria-valuemax="100" style="width:' + download.Percent + '%">' + download.Percent + '%</div></div><img src="/images/spinner-small.gif" /></div>';
        } else {
            progress = '<div class="download-progress">Building your download...<img src="/images/spinner-small.gif" /></div>';
        }
        return '<li id="' + download.DownloadId + '" class="' + cls + '"><span class="filename" data-toggle="tooltip" data-placement="left" title="' + download.Tooltip + '"><span class="icon">' + icon + '</span>' + download.Filename + '</span>' + progress + '</li>';
    }
}
$(document).ready(function () {
    pollDownloads(true);
    $("#downloads ul").on("click", function (e) {
        e.stopPropagation();
        $(".tooltip").tooltip("hide");
    });
});