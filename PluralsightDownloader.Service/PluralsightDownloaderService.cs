using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using PluralsightDownloader.Models;

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
                using (var fileStream = File.OpenWrite(filePath))
                {
                    var bytesRead = stream.Read(buffer, 0, buffer.Length);

                    while(bytesRead > 0)
                    {
                        fileStream.Write(buffer, 0, bytesRead);
                        bytesRead = stream.Read(buffer, 0, buffer.Length);
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
                result = JsonConvert.DeserializeObject<T>(response);
            }

            return result;
        }
    }
}