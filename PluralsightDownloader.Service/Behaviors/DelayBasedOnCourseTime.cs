using System;

namespace PluralsightDownloader.Service.Behaviors
{
    public class DelayBasedOnCourseTime : IDownloadBehavior
    {
        private readonly Random _rand;

        public DelayBasedOnCourseTime()
        {
            _rand = new Random();
        }

        public int DelayTime(int courceTimeInMinutes)
        {
            var sault = _rand.Next(0, 100);
            return (courceTimeInMinutes * 60) + sault;
        }
    }
}
