using System;

namespace CefSharp.OutOfProcess.Interface.Callbacks
{

    public sealed class DownloadItem
    {
        public string SuggestedFileName { get; set; }
        public string OriginalUrl { get; set; }
        public string Url { get; set; }
        public int Id { get; set; }
        public string FullPath { get; set; }
        public DateTime? EndTime { get; set; }
        public DateTime? StartTime { get; set; }
        public long ReceivedBytes { get; set; }
        public long TotalBytes { get; set; }
        public int PercentComplete { get; set; }
        public long CurrentSpeed { get; set; }
        public bool IsCancelled { get; set; }
        public bool IsComplete { get; set; }
        public bool IsInProgress { get; set; }
        public bool IsValid { get; set; }
        public string ContentDisposition { get; set; }
        public string MimeType { get; set; }
    }
}