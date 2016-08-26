using System.Collections.Generic;

namespace PluralsightDownloader.Models
{
    public class CourseModule
    {
        public int ModuleIndex { get; set; }
        public string ModuleRef { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Duration { get; set; }
        public string FragmentIdentifier { get; set; }
        public List<Clip> Clips { get; set; }
    }
}
