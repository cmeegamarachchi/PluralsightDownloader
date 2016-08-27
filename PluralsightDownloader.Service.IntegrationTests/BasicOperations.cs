using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using PluralsightDownloader.Service.Behaviors;

namespace PluralsightDownloader.Service.IntegrationTests
{
    [TestFixture]
    public class BasicOperations
    {
        [Test]
        public void GetAuthCookie_returns_a_cookei_for_correct_username_and_password()
        {
            // given a correct username and a password
            var userName = "not-my-user-name";
            var password = "not-my-password";

            // when GetAuthCookie is executed
            var service = new PluralsightDownloaderService();
            var result = service.GetAuthCookie(userName, password);

            // a cookei is returned
            Assert.IsNotEmpty(result);
        }

        [Test]
        public void GetCourseDetails_returns_cource_details_for_cource()
        {
            // given a valid course url
            var courseUrl = "https://app.pluralsight.com/library/courses/java-ee-7-fundamentals";

            // when GetCourseDetails is executed
            var service = new PluralsightDownloaderService();
            var result = service.GetCourseDetails(courseUrl);

            // correct course details are returned
            Assert.IsNotNull(result);
        }

        [Test]
        public void GetClipUrl_returns_clip_url()
        {
            // given a clip params
            var clipParams = @"author=antonio-goncalves&name=java-ee-7-fundamentals-m2&mode=live&clip=0&course=java-ee-7-fundamentals";
            // and given a valid cookie
            var service = new PluralsightDownloaderService();
            var cookie = service.GetAuthCookie("not-my-username", "not-my-password");

            // when GetClipUrl is called
            var result = service.GetClipLocation(clipParams, cookie);

            // correct url representing the location of the clip is returned
            Assert.IsNotEmpty(result);
        }

        [Test]
        public void DownloadVideo_downloads_video()
        {
            // give a valid clip url
            var service = new PluralsightDownloaderService();
            var cookie = service.GetAuthCookie("not-my-username", "not-my-password");
            var clipParams = @"author=antonio-goncalves&name=java-ee-7-fundamentals-m2&mode=live&clip=0&course=java-ee-7-fundamentals";
            var clipUrl = service.GetClipLocation(clipParams, cookie);

            // and given a target file
            var filePath = @"C:/Users/devadmin/downloads/pluralsight.mp4";
            DeleteIfExists(filePath);

            // when DownloadVideo is executed
            service.DownloadVideo(clipUrl, filePath);

            // then video is downloaded to the correct path
        }

        [Test]
        public void Downloads_complete_course()
        {
            // given a valid username and a password
            var userName = "not-my-user-name";
            var password = "not-my-password";

            // given a valid course
            var course = "https://app.pluralsight.com/library/courses/java-ee-7-fundamentals";

            // given a base path
            var path = @"C:/Users/devadmin/downloads/plsdl";
            DeleteDirectoryIfExists(path);

            // when DownloadCourse is called
            var service = new PluralsightDownloaderService();
            service.DownloadDelay = new DelayBasedOnCourseTime().DelayTime;
            service.DownloadProgressEvent += (sender, args) => { Debug.WriteLine(args.Message); };
            service.DownloadCourse(course, userName, password, path);

            // complete course is downlaoded to base path
            var directoryInfo = new DirectoryInfo(path);
            Assert.IsTrue(directoryInfo.GetFiles("*.mp4", SearchOption.AllDirectories).Any());
        }

        private static void DeleteIfExists(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
        }

        private static void DeleteDirectoryIfExists(string filePath)
        {
            var directoryInfo = new DirectoryInfo(filePath);
            if (directoryInfo.Exists)
            {
                directoryInfo.Delete(true);
            }
        }
    }
}
