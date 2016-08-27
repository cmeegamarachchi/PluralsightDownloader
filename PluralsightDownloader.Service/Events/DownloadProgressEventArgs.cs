using System;
using PluralsightDownloader.Service.Models;

namespace PluralsightDownloader.Service.Events
{
    public class DownloadProgressEventArgs : EventArgs
    {
        public string Message { get; set; }
        public CourseModuleClip CourseModuleClip { get; set; }
    }
}
