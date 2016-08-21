namespace PluralsightDownloader.Models
{
    public class Clip
    {
        public int ClipIndex { get; set; }
        public string Title { get; set; }
        public string Duration { get; set; }
        public string PlayerParameters { get; set; }
        public bool UserMayViewClip { get; set; }
        public string Name { get; set; }
    }
}
