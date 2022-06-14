using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SubtitlesLoader
{
    internal static class ApiServices
    {
        public class LoginResponse
        {
            public User user { get; set; }
            public string token { get; set; }
            public int status { get; set; }
        }

        public class User
        {
            public int allowed_downloads { get; set; }
            public string level { get; set; }
            public int user_id { get; set; }
            public bool ext_installed { get; set; }
            public bool vip { get; set; }
        }
        private static string filename;

        private static LoginResponse? userData;
        private static readonly string LoginUrl = "https://api.opensubtitles.com/api/v1/login";
        private static readonly string SubtitlesUrl = @"https://api.opensubtitles.com/api/v1/subtitles";
        private static readonly string DownloadUrl = @"https://api.opensubtitles.com/api/v1/download";

        public static async Task<string> GetSubtitles(string filepath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (userData == null)
                        Login(Properties.User.Default.Username, Properties.User.Default.Password);

                    string jsonContent = Search(filepath);

                    string fileId = GetFirstResultFileId(jsonContent);

                    string downloadLink = GetDownloadLink(fileId.ToString());

                    Download(downloadLink, filepath);

                    return "Download completed";
                } catch (Exception ex)
                {
                    return "Error: " + ex.Message;
                }
            });
        }

        private static string GetFirstResultFileId(string jsonResult)
        {
            try
            {
                Dictionary<string, JsonElement>? deserialized = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonResult);

                if(deserialized["total_count"].GetInt32() == 0)
                {
                    throw new Exception("No subtitles found");
                }

                List<JsonElement>? subtitlesList = JsonSerializer.Deserialize<List<JsonElement>>(deserialized["data"]);

                Dictionary<string, JsonElement>? subtitleInfo = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(subtitlesList[0].ToString());

                Dictionary<string, JsonElement>? attributes = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(subtitleInfo["attributes"]);

                List<JsonElement>? files = JsonSerializer.Deserialize<List<JsonElement>>(attributes["files"]);

                Dictionary<string, JsonElement>? file = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(files[0].ToString());

                filename = file["file_name"].ToString();

                filename = filename.Length > 0 ? filename : "subtitles.srt";

                return file["file_id"].ToString();
            } catch (NullReferenceException)
            {
                throw new Exception("Error parsing search result");
            }
        }

        private static void Download(string fileLink, string filepath)
        {
            using var client = new WebClient();
            client.DownloadFile(fileLink, Path.Combine(Path.GetDirectoryName(filepath), filename));
        }

        private static string GetDownloadLink(string file_id)
        {
            var client = new HttpClient();

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(DownloadUrl),
                Headers =
                {
                    { "Api-Key", Properties.User.Default.ApiKey },
                    { "Authorization", userData.token },
                    { "Accept", "*/*" }

                },
                Content = new StringContent("{\"file_id\":" + file_id + "}")
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/json")
                    }
                }
            };

            HttpResponseMessage response = client.Send(request);
            response.EnsureSuccessStatusCode();
            string resString = response.Content.ReadAsStringAsync().Result;

            Dictionary<string, JsonElement> deserialized = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(resString);

            return deserialized["link"].ToString();
        }

        public static string Search(string filepath)
        {
            string movieHash = ToHexadecimal(ComputeMovieHash(filepath));

            string filename = Path.GetFileNameWithoutExtension(filepath);

            using var request = new HttpRequestMessage(HttpMethod.Get, SubtitlesUrl + "?languages=" + Properties.User.Default.Language + "&moviehash=" + movieHash + "&query=" + filename);
            request.Headers.Add("Api-Key", Properties.User.Default.ApiKey);

            HttpClient client = new();
            HttpResponseMessage response = client.Send(request);


            return response.Content.ReadAsStringAsync().Result;
        }


        private static void Login(string username, string passwordProtected)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, LoginUrl);
            request.Headers.Add("Api-Key", "r0Bt86bXAfW5cs9tkUBuQta6TAQGNAap");
            request.Content = new MultipartFormDataContent();
            var content = new MultipartFormDataContent
            {
                { new StringContent(username), "username" },
                { new StringContent(PasswordProtection.Unprotect(passwordProtected)), "password" }
            };
            request.Content = content;


            HttpClient client = new();

            HttpResponseMessage response = client.Send(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
                throw new Exception("Bad username or password");

            response.EnsureSuccessStatusCode();

            string responseJson = response.Content.ReadAsStringAsync().Result;

            userData = JsonSerializer.Deserialize<LoginResponse>(responseJson);

            if(userData == null)
            {
                throw new Exception("Could not connect to authorization service");
            }
        }

        private static byte[] ComputeMovieHash(string filename)
        {
            byte[] result;
            using (Stream input = File.OpenRead(filename))
            {
                result = ComputeMovieHash(input);
            }
            return result;
        }

        private static byte[] ComputeMovieHash(Stream input)
        {
            long lhash, streamsize;
            streamsize = input.Length;
            lhash = streamsize;

            long i = 0;
            byte[] buffer = new byte[sizeof(long)];
            while (i < 65536 / sizeof(long) && (input.Read(buffer, 0, sizeof(long)) > 0))
            {
                i++;
                lhash += BitConverter.ToInt64(buffer, 0);
            }

            input.Position = Math.Max(0, streamsize - 65536);
            i = 0;
            while (i < 65536 / sizeof(long) && (input.Read(buffer, 0, sizeof(long)) > 0))
            {
                i++;
                lhash += BitConverter.ToInt64(buffer, 0);
            }
            input.Close();
            byte[] result = BitConverter.GetBytes(lhash);
            Array.Reverse(result);
            return result;
        }

        private static string ToHexadecimal(byte[] bytes)
        {
            StringBuilder hexBuilder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                hexBuilder.Append(bytes[i].ToString("x2"));
            }
            return hexBuilder.ToString();
        }
    }
}
