using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;
using Newtonsoft.Json;
using PluralsightDownloader.Models;
using PluralsightDownloader.Service.Events;
using PluralsightDownloader.Service.Models;

namespace PluralsightDownloader.Service
{
    public class PluralsightDownloaderService
    {
        // credit : https://github.com/Ebram-Tharwat/CSharp-Pluralsight-Downloader

        private const string LoginUrl = "https://app.pluralsight.com/id/";
        private const string BaseUrl = "https://app.pluralsight.com";
        private const string CourseSummaryUrl = "http://app.pluralsight.com/data/course/{0}";
        private const string CourseDetailsUrl = "http://app.pluralsight.com/data/course/content/{0}";
        private const string ClipDetailsUrl = "http://app.pluralsight.com/training/Player/ViewClip";
        private const string UserAgentString = @"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36";

        public Func<int, int> DownloadDelay { get; set; }

        public EventHandler<DownloadProgressEventArgs> DownloadProgressEvent { get; set; }

        public PluralsightDownloaderService()
        {
            DownloadDelay = a => 5*1000;
        }

        public string GetAuthCookie(string userName, string password)
        {
            var encoding = new ASCIIEncoding();
            var loginRequestData = encoding.GetBytes($"Username={userName}&Password={password}");

            var loginRequest = (HttpWebRequest)WebRequest.Create(LoginUrl);

            loginRequest.UserAgent = UserAgentString;

            loginRequest.Headers.Add("Origin", BaseUrl);
            loginRequest.Headers.Add("Cache-Control", "max-age=0");

            loginRequest.AllowAutoRedirect = false;
            loginRequest.Method = "POST";
            loginRequest.ContentType = "application/x-www-form-urlencoded";
            loginRequest.ContentLength = loginRequestData.Length;

            Stream newStream = loginRequest.GetRequestStream();
            newStream.Write(loginRequestData, 0, loginRequestData.Length);
            newStream.Close();

            var loginResponse = loginRequest.GetResponse();
            var cookie = loginResponse.Headers["Set-Cookie"];

            return cookie;
        }

        public Course GetCourseDetails(string courseUrl)
        {
            var coursename = courseUrl.Split('/').Last();

            var course = GetFromPluralSight<Course>(string.Format(CourseSummaryUrl, coursename));
            course.CourseModules = GetFromPluralSight<List<CourseModule>>(string.Format(CourseDetailsUrl, coursename));

            var moduleIndex = 0;
            course.CourseModules.ForEach(module =>
            {
                module.ModuleIndex = moduleIndex++;
            });

            return course;
        }

        public string GetClipLocation(string playerParameters, string cookie)
        {
            var clipDetails = (HttpWebRequest)WebRequest.Create(new Uri(ClipDetailsUrl));
            clipDetails.Accept = "application/json";
            clipDetails.ContentType = "application/json";
            clipDetails.Method = "POST";
            clipDetails.UserAgent = UserAgentString;
            clipDetails.Headers.Add("Cookie", cookie);

            var playerParameterQuery = HttpUtility.ParseQueryString(playerParameters);
            var playerParameterQueryObject = new
            {
                a = playerParameterQuery["author"],
                m = playerParameterQuery["name"],
                course = playerParameterQuery["course"],
                cn = playerParameterQuery["clip"],
                mt = "mp4",
                q = "1024x768",
                cap = false,
                lc = "en"
            };

            var encoding = new ASCIIEncoding();
            var dataBytes = encoding.GetBytes(JsonConvert.SerializeObject(playerParameterQueryObject));

            var sendStream = clipDetails.GetRequestStream();
            sendStream.Write(dataBytes, 0, dataBytes.Length);
            sendStream.Close();

            var response = clipDetails.GetResponse();
            var receiveStream = response.GetResponseStream();
            var sr = new StreamReader(receiveStream);
            var content = sr.ReadToEnd();

            return content;
        }

        public void DownloadVideo(string clipUrl, string filePath)
        {
            var client = new WebClient();
            using (var stream = client.OpenRead(clipUrl))
            {
                byte[] buffer = new byte[8192];
                var downloaded = 0;
                using (var fileStream = File.OpenWrite(filePath))
                {
                    var bytesRead = stream.Read(buffer, 0, buffer.Length);
                    while (bytesRead > 0)
                    {
                        downloaded = downloaded + buffer.Length;

                        fileStream.Write(buffer, 0, bytesRead);
                        bytesRead = stream.Read(buffer, 0, buffer.Length);

                        DownloadProgressEventHandeller(String.Format("Downloaded {0:N0}", downloaded));
                    }
                }
            }

        }

        private T GetFromPluralSight<T>(string url) where T : class
        {
            T result;

            using (var webClient = new WebClient())
            {
                var response = webClient.DownloadString(url);
                result = JsonConvert.DeserializeObject<T>(response, new JsonSerializerSettings { StringEscapeHandling = StringEscapeHandling.EscapeNonAscii});
            }

            return result;
        }

        public void DownloadCourse(string courseUrl, string userName, string password, string path)
        {
            // if the thread is not running
            DownloadCourseTask(courseUrl, userName, password, path);
        }

        private void DownloadCourseTask(string courseUrl, string userName, string password, string path)
        {
            DownloadProgressEventHandeller("Downloading cource details");

            var course = GetCourseDetails(courseUrl);

            var clipList = new List<CourseModuleClip>();

            DownloadProgressEventHandeller("Building cource module list");
            
            course.CourseModules.ForEach(
                cm => { cm.Clips.ForEach(clip => { clipList.Add(new CourseModuleClip {CourseModule = cm, Clip = clip}); }); });

            DownloadProgressEventHandeller("Getting auth cookie");
            
            var cookie = GetAuthCookie(userName, password);

            if (string.IsNullOrEmpty(cookie))
            {
                DownloadProgressEventHandeller("Error: Failed to get cookie");
                return;
            }

            clipList.ForEach(cl =>
            {
                DownloadProgressEventHandeller(String.Format(@"Downloading {0}\{1}", cl.CourseModule.Title, cl.Clip.Title));

                var fileName = MakeFileName(path, cl.CourseModule.Title, cl.CourseModule.ModuleIndex,
                    cl.Clip.Title, cl.Clip.ClipIndex);
                CreateFolderIfNotFound(fileName);

                DownloadVideo(GetClipLocation(cl.Clip.PlayerParameters, cookie), fileName);

                Thread.Sleep(DownloadDelay(DateTime.Parse(cl.Clip.Duration).Minute));
            });
        }

        private string MakeFileName(string path, string moduleTitle, int moduleIndex, string title, int clipIndex)
        {
            var sanatisedModuleTitle = moduleTitle.Replace('?', '_').Replace(':', '_');
            var sanatisedTitle = title.Replace('?', '_').Replace(':', '_');
            var folderName = $"{path}/{moduleIndex:00}-{sanatisedModuleTitle}";
            var fileName =  $"{folderName}/{clipIndex:00}-{sanatisedTitle}.mp4";
            return fileName;
        }

        private void CreateFolderIfNotFound(string fileName)
        {
            var fileInfo = new FileInfo(fileName);
            fileInfo.Directory.Create();
        }

        private void DownloadProgressEventHandeller(string message, CourseModuleClip courseModuleClip = null)
        {
            var handel = DownloadProgressEvent;

            if (handel != null)
            {
                DownloadProgressEvent(this, new DownloadProgressEventArgs { Message = message, CourseModuleClip = courseModuleClip });
            }
        }
    }
}