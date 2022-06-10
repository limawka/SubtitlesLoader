using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private static LoginResponse? userData;
        private static readonly string LoginUrl = "https://api.opensubtitles.com/api/v1/login";
        private static readonly string SubtitlesUrl = @"https://api.opensubtitles.com/api/v1/subtitles";
        private static readonly string DownloadUrl = @"https://api.opensubtitles.com/api/v1/download";

        public static string GetSubtitles(string filepath)
        {
            string jsonContent = Search(filepath);

            Dictionary<string, JsonElement>? deserialized = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonContent);

            if (deserialized == null)
                throw new Exception("Search result json not valid");

            if (!deserialized.TryGetValue("total_count", out JsonElement number))
                throw new Exception("Couldnt read count");

            if (number.GetInt32() == 0)
                throw new Exception("No subtitles found");

            if (!deserialized.TryGetValue("data", out JsonElement data))
                throw new Exception("");

            List<JsonElement>? subtitlesList = JsonSerializer.Deserialize<List<JsonElement>>(data);

            if (subtitlesList == null)
                throw new Exception("Could not parse subtitles list");

            return subtitlesList[0].ToString();
        }

        private static string GetDownloadLink(int file_id)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, DownloadUrl);
            request.Headers.Add("Api-Key", "r0Bt86bXAfW5cs9tkUBuQta6TAQGNAap");
            request.Headers.Add("Authorization", userData.token);



            return "";
        }

        public static string Search(string filepath)
        {
            if(userData == null)
                Login(Properties.User.Default.Username, Properties.User.Default.Password);

            if (userData == null)
                return "blad";

            string movieHash = ToHexadecimal(ComputeMovieHash(filepath));

            string filename = Path.GetFileNameWithoutExtension(filepath);

            using var request = new HttpRequestMessage(HttpMethod.Get, SubtitlesUrl + "?moviehash=" + movieHash + "&query=" + filename);
            request.Headers.Add("Api-Key", Properties.User.Default.ApiKey);
            
            HttpClient client = new();
            HttpResponseMessage response = client.Send(request);


            return response.Content.ReadAsStringAsync().Result;
        }


        private static void Login(string username, string password)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, LoginUrl);
            request.Headers.Add("Api-Key", "r0Bt86bXAfW5cs9tkUBuQta6TAQGNAap");
            request.Content = new MultipartFormDataContent();
            var content = new MultipartFormDataContent
            {
                { new StringContent(username), "username" },
                { new StringContent(password), "password" }
            };
            request.Content = content;


            HttpClient client = new();

            HttpResponseMessage response = client.Send(request);

            response.EnsureSuccessStatusCode();

            string responseJson = response.Content.ReadAsStringAsync().Result;

            userData = JsonSerializer.Deserialize<LoginResponse>(responseJson);
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
