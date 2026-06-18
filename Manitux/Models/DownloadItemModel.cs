using CommunityToolkit.Mvvm.ComponentModel;
using Manitux.Core.Models;

namespace Manitux.Models;

public enum DownloadStatus
{
    Queued,
    Downloading,
    Paused,
    Completed,
    Failed
}

public class DownloadItemModel : ObservableObject
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _fileName = string.Empty;
    private string _url = string.Empty;
    private string _filePath = string.Empty;
    private string? _referer;
    private List<HeaderModel>? _headers;
    private List<SubtitleModel>? _subtitles;
    private DownloadStatus _status = DownloadStatus.Queued;
    private long _bytesDownloaded;
    private long? _totalBytes;
    private int _hlsSegmentIndex;
    private string? _errorMessage;
    private DateTimeOffset _createdAt = DateTimeOffset.UtcNow;
    private DateTimeOffset? _completedAt;

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string FileName
    {
        get => _fileName;
        set => SetProperty(ref _fileName, value);
    }

    public string Url
    {
        get => _url;
        set => SetProperty(ref _url, value);
    }

    public string FilePath
    {
        get => _filePath;
        set => SetProperty(ref _filePath, value);
    }

    public string? Referer
    {
        get => _referer;
        set => SetProperty(ref _referer, value);
    }

    public List<HeaderModel>? Headers
    {
        get => _headers;
        set => SetProperty(ref _headers, value);
    }

    public List<SubtitleModel>? Subtitles
    {
        get => _subtitles;
        set => SetProperty(ref _subtitles, value);
    }

    public DownloadStatus Status
    {
        get => _status;
        set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(IsActive));
                OnPropertyChanged(nameof(CanStart));
                OnPropertyChanged(nameof(CanPause));
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    public long BytesDownloaded
    {
        get => _bytesDownloaded;
        set
        {
            if (SetProperty(ref _bytesDownloaded, value))
            {
                OnPropertyChanged(nameof(ProgressPercentage));
                OnPropertyChanged(nameof(ProgressText));
            }
        }
    }

    public long? TotalBytes
    {
        get => _totalBytes;
        set
        {
            if (SetProperty(ref _totalBytes, value))
            {
                OnPropertyChanged(nameof(ProgressPercentage));
                OnPropertyChanged(nameof(ProgressText));
            }
        }
    }

    public int HlsSegmentIndex
    {
        get => _hlsSegmentIndex;
        set => SetProperty(ref _hlsSegmentIndex, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public DateTimeOffset CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    public DateTimeOffset? CompletedAt
    {
        get => _completedAt;
        set => SetProperty(ref _completedAt, value);
    }

    public bool IsActive => Status == DownloadStatus.Downloading;
    public bool CanStart => Status is DownloadStatus.Queued or DownloadStatus.Paused or DownloadStatus.Failed;
    public bool CanPause => Status == DownloadStatus.Downloading;

    public double ProgressPercentage => TotalBytes is > 0
        ? Math.Clamp(BytesDownloaded * 100d / TotalBytes.Value, 0d, 100d)
        : 0d;

    public string ProgressText => TotalBytes is > 0
        ? $"{FormatBytes(BytesDownloaded)} / {FormatBytes(TotalBytes.Value)} ({ProgressPercentage:0.#}%)"
        : FormatBytes(BytesDownloaded);

    public string StatusText => Status switch
    {
        DownloadStatus.Queued => "Queued",
        DownloadStatus.Downloading => "Downloading",
        DownloadStatus.Paused => "Paused",
        DownloadStatus.Completed => "Completed",
        DownloadStatus.Failed => "Failed",
        _ => Status.ToString()
    };

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)bytes;
        var unit = 0;

        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.##} {units[unit]}";
    }
}
