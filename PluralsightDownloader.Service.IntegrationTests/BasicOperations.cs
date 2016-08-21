using System.IO;
using NUnit.Framework;

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

        private static void DeleteIfExists(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }
        }
    }
}
