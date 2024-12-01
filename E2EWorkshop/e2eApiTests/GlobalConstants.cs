using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace e2eApiTests
{
    public static class GlobalConstants
    {
        public const string BaseUrl = "http://localhost:5000/api";

        public static string AuthenticateUser(string email, string password)
        {
            var resource = email == "admin@gmail.com" ? "user/admin-login" : "user/login";
            var authClient = new RestClient(BaseUrl);
            var request = new RestRequest(resource);
            request.AddJsonBody(new { email, password });

            var response = authClient.Execute(request, Method.Post);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Assert.Fail($"Authentication failed with status code: {response.StatusCode} content: {response.Content}");
            }

            var content = JObject.Parse(response.Content);
            return content["token"]?.ToString();
        }
    }
}
