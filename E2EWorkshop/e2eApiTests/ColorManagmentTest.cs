using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace e2eApiTests
{
    [TestFixture]
    public class ColorManagmentTest : IDisposable
    {
        private RestClient client;
        private string token;

        [SetUp]
        public void SetUp()
        {
            client = new RestClient(GlobalConstants.BaseUrl);
            token = GlobalConstants.AuthenticateUser("admin@gmail.com", "admin@gmail.com");
            Assert.That(token, Is.Not.Null.Or.Empty,
                "Authentication token is null or empty");
        }

        [Test]
        public void ColorLifecycleTest()
        {
            // Create a color
            var createColorRequest = new RestRequest("/color");
            createColorRequest.AddHeader("Authorization", $"Bearer {token}");
            createColorRequest.AddJsonBody(new { title = "Test Color"});

            var createColorResponse = client.Execute(createColorRequest, Method.Post);
            Assert.That(createColorResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "Response status code is not OK (200)");
            Assert.That(createColorResponse.Content, Is.Not.Null.Or.Empty,
                "Response is null or empty");

            // Update a color
            string colorId = JObject.Parse(createColorResponse.Content)["_id"]?.ToString();
            Assert.That(colorId, Is.Not.Null.Or.Empty,
                "Color ID is null or empty");

            var updateColorRequest = new RestRequest("/color/{id}");
            updateColorRequest.AddHeader("Authorization", $"Bearer {token}");
            updateColorRequest.AddUrlSegment("id", colorId);
            updateColorRequest.AddJsonBody(new { title = "Updated Color" });

            var updateColorResponse = client.Execute(updateColorRequest, Method.Put);
            Assert.That(updateColorResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "Response status code is not OK (200)");
            Assert.That(updateColorResponse.Content, Is.Not.Null.Or.Empty,
                "Response is null or empty");

            var updatedColorContent = JObject.Parse(updateColorResponse.Content);
            Assert.That(updatedColorContent["title"].ToString(), Is.EqualTo("Updated Color"),
                "Color title not match the input");

            // Delete a color
            var deleteColorRequest = new RestRequest("/color/{id}");
            deleteColorRequest.AddHeader("Authorization", $"Bearer {token}");
            deleteColorRequest.AddUrlSegment("id", colorId);

            var deleteColorResponse = client.Execute(deleteColorRequest, Method.Delete);
            Assert.That(deleteColorResponse.IsSuccessful, Is.True,
                "Deleting product color failed");

            // Verify that product color does not exist
            var verifyColorRequest = new RestRequest("/color/{id}");
            verifyColorRequest.AddUrlSegment("id", colorId);

            var verifyColorResponse = client.Execute(verifyColorRequest, Method.Get);
            Assert.That(verifyColorResponse.Content, Is.Null.Or.EqualTo("null"),
                "Product color still exist");
        }

        [Test]
        public void ColorLifecycleNegativeTest()
        {
            string invalidToken = "InvalidToken";
            string invalidColorId = "InvalidColorId";

            var createColorRequest = new RestRequest("/color");
            createColorRequest.AddHeader("Authorization", $"Bearer {invalidToken}");
            createColorRequest.AddJsonBody(new { Title = "Test Color" });

            var createColorResponse = client.Execute(createColorRequest, Method.Post);
            Assert.That(createColorResponse.IsSuccessful, Is.False,
                "Adding color is successful");
            Assert.That(createColorResponse.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError),
                "Response status code is not internal server error");

            var getColorRequest = new RestRequest($"/color/{invalidColorId}");
            
            var getColorResponse = client.Execute(getColorRequest, Method.Get);
            Assert.That(getColorResponse.IsSuccessful, Is.False,
                "Retrieving color is successful");
            Assert.That(getColorResponse.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError),
                "Response status code is not internal server error");

            var deleteColorRequest = new RestRequest($"/color/{invalidColorId}");
            deleteColorRequest.AddHeader("Authorization", $"Bearer {invalidToken}");

            var deleteColorResponse = client.Execute(deleteColorRequest, Method.Delete);
            Assert.That(deleteColorResponse.IsSuccessful, Is.False,
                "Deleting color is successful");
            Assert.That(deleteColorResponse.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError),
                "Response status code is not internal server error");

        }
         
        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
