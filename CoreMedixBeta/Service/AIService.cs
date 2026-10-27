//using System;
//using System.Net.Http;
//using System.Text;
//using System.Threading.Tasks;
//using Newtonsoft.Json;

//namespace CoreMedixBeta.Repositories
//{
//    public static class AIService
//    {
//        private static readonly HttpClient httpClient = new HttpClient();
//        private const string API_URL = "https://api.openai.com/v1/chat/completions";
//        private const string API_KEY = "YOUR_OPENAI_API_KEY"; // Replace with your actual API key

//        public static async Task<string> SendPromptAsync(string prompt)
//        {
//            await Task.Delay(100);
//            return "Processing your request...";
//        }

//    }
//}