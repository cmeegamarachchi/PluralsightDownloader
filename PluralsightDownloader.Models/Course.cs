using System.Collections.Generic;

namespace PluralsightDownloader.Models
{
    public class Course
    {
        public string Title { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<CourseModule> CourseModules { get; set; }
    }
}
