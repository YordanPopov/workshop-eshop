using RestSharp;
using System;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ApiTests
{
    public static class GlobalConstants
    {
        public const string BaseUrl = "http://localhost:5000/api";
        public static string AuthenticateUser(string email, string password)
        {
            var authClient = new RestClient(BaseUrl);
            var request = new RestRequest("/user/admin-login");
            request.AddJsonBody(new {email, password});

            var response = authClient.Execute(request, Method.Post);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                Assert.Fail($"Authentication failed with status code: {response.StatusCode}, content: {response.Content}");
            }

            var content = JObject.Parse(response.Content);
            return content["token"]!.ToString();
        }
    }
}
