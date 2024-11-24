using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ApiTests
{
    [TestFixture]
    public class ColorApiTests : IDisposable
    {
        private RestClient client;
        private string token;

        [SetUp]
        public void SetUp()
        {
            client = new RestClient(GlobalConstants.BaseUrl);
            token = GlobalConstants.AuthenticateUser("admin@gmail.com", "admin@gmail.com");
            Assert.That(token, Is.Not.Null.Or.Empty,
                "Authentication token should not be null or empty");
        }

        [TearDown]
        public async Task TearDown()
        {
            await Task.Delay(1500);
        }

        [Test, Order(1)]
        public void Test_GetAllColors()
        {
            var request = new RestRequest("/color");

            var response = client.Execute(request, Method.Get);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Expected status code is OK (200)");
                Assert.That(response.Content, Is.Not.Empty,
                    "Response content should not be empty");

                var colors = JArray.Parse(response.Content);

                Assert.That(colors.Type, Is.EqualTo(JTokenType.Array),
                    "Expected response content to be a JSON array");
                Assert.That(colors.Count, Is.GreaterThan(0),
                    "Expected at least one color in the response");

                var colorTitles = colors.Select(c => c["title"].ToString()).ToList();
                Assert.That(colorTitles, Does.Contain("Black"),
                    "Expected color black");
                Assert.That(colorTitles, Does.Contain("White"),
                    "Expected color white");
                Assert.That(colorTitles, Does.Contain("Red"),
                    "Expected color red");

                foreach (var color in colors)
                {
                    Assert.That(color["_id"].ToString(), Is.Not.Null.And.Not.Empty,
                        "Color ID should not be null or empty");
                    Assert.That(color["title"].ToString(), Is.Not.Null.And.Not.Empty,
                        "Color title should not be null or empty");
                }

                Assert.That(colors.Count, Is.EqualTo(10),
                    "Expected exactly 10 colors in the response");
            });
        }

        [Test, Order(2)]
        public void Test_AddColor()
        {
            var request = new RestRequest("/color");
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddJsonBody(new { title = "New Test Color" });

            var response = client.Execute(request, Method.Post);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Expected status code is OK (200)");
                Assert.That(response.Content, Is.Not.Empty,
                    "Response content should not be empty");

                var content = JObject.Parse(response.Content);

                Assert.That(content["_id"].ToString(), Is.Not.Null.And.Not.Empty,
                    "Color ID should not be null or empty");
                Assert.That(content["title"].ToString(), Is.EqualTo("New Test Color"),
                    "Color title should not be null or empty");

                Assert.That(content["createdAt"].ToString(), Is.Not.Null.Or.Empty,
                    "Color should have createdAt field");
                Assert.That(content["updatedAt"].ToString(), Is.Not.Null.Or.Empty,
                    "Color should have updatedAt field");

                Assert.That(DateTime.TryParse(content["createdAt"].ToString(), out _), Is.True,
                    "createdAt should be a valid date-time format");
                Assert.That(DateTime.TryParse(content["updatedAt"].ToString(), out _), Is.True,
                    "updatedAt should be a valid date-time format");

                Assert.That(content["createdAt"].ToString(), Is.EqualTo(content["updatedAt"].ToString()),
                    "createdAt and updatedAt should be the same on creation");
            });
        }

        [Test, Order(3)]
        public void Test_UpdateColor()
        {
            var getRequest = new RestRequest("/color");

            var getResponse = client.Execute(getRequest, Method.Get);

            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), 
                "Failed to retrieve colors");
            Assert.That(getResponse.Content, Is.Not.Empty, 
                "Get colors response content is empty");

            var colors = JArray.Parse(getResponse.Content);
            var colorToUpdate = colors.FirstOrDefault(c => c["title"].ToString() == "New Test Color");

            Assert.That(colorToUpdate, Is.Not.Null,
                "Color with title 'New Test Color' not found");

            var colorId = colorToUpdate["_id"].ToString();

            var updateRequest = new RestRequest($"/color/{colorId}");
            updateRequest.AddHeader("Authorization", $"Bearer {token}");
            updateRequest.AddJsonBody(new { title = "Updated Test Color" });

            var updateResponse = client.Execute(updateRequest, Method.Put);

            Assert.Multiple(() =>
            {
                Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Expected status code is OK (200)");
                Assert.That(updateResponse.Content, Is.Not.Empty,
                    "Response content should not be empty");

                var content = JObject.Parse(updateResponse.Content);

                Assert.That(content["_id"].ToString(), Is.EqualTo(colorId),
                    "Color ID should match the updated color's ID");
                Assert.That(content["title"].ToString(), Is.EqualTo("Updated Test Color"),
                    "Color title should be updated correctly");
                Assert.That(content["createdAt"].ToString(), Is.Not.Null.Or.Empty,
                    "Color should have createdAt field");
                Assert.That(content["updatedAt"].ToString(), Is.Not.Null.Or.Empty,
                    "Color should have updatedAt field");
                Assert.That(DateTime.TryParse(content["createdAt"].ToString(), out _), Is.True,
                    "createdAt should be a valid date-time format");
                Assert.That(DateTime.TryParse(content["updatedAt"].ToString(), out _), Is.True,
                    "updatedAt should be a valid date-time format");
                Assert.That(content["createdAt"].ToString(), Is.Not.EqualTo(content["updatedAt"].ToString()),
                    "updatedAt should be different from createdAt after an update");
            });
        }

        [Test, Order(4)]
        public void Test_DeleteColor()
        {
            var getRequest = new RestRequest("/color");
            
            var getResponse = client.Execute(getRequest, Method.Get);

            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(getResponse.Content, Is.Not.Empty);

            var colors = JArray.Parse(getResponse.Content);
            var colorToDelete = colors.FirstOrDefault(c => c["title"].ToString() == "Updated Test Color");

            Assert.That(colorToDelete, Is.Not.Null);

            var colorId = colorToDelete["_id"].ToString();

            var deleteRequest = new RestRequest($"/color/{colorId}");
            deleteRequest.AddHeader("Authorization", $"Bearer {token}");

            var deleteResponse = client.Execute(deleteRequest, Method.Delete);

            Assert.Multiple(() =>
            {
                Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

                var verifyGetRequest = new RestRequest($"/color/{colorId}");
                var verifyGetResponse = client.Execute(verifyGetRequest, Method.Get);

                Assert.That(verifyGetResponse.Content, Is.Empty.Or.EqualTo("null"));

                var refreshedGetResponse = client.Execute(getRequest);
                var refreshedColors = JArray.Parse(refreshedGetResponse.Content);
                var colorExist = refreshedColors.Any(c => c["title"].ToString() == "Updated Test Color");

                Assert.That(colorExist, Is.False);
            });
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
