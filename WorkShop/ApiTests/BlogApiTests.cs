using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ApiTests
{
    [TestFixture]
    public class BlogApiTests : IDisposable
    {
        private RestClient client;
        private string token;

        [SetUp]
        public void SetUp()
        {
            client = new RestClient(GlobalConstants.BaseUrl);
            token = GlobalConstants.AuthenticateUser("admin@gmail.com", "admin@gmail.com");

            Assert.That(token, Is.Not.Null.Or.Empty,
                "Authentication token should not be null or empty!");
        }

        [Test, Order(4)]
        public void Test_GetAllBlogs()
        {
            var request = new RestRequest("/blog");

            var response = client.Execute(request, Method.Get);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Expected status code OK (200).");
                Assert.That(response.Content, Is.Not.Empty,
                    "Response content should not be empty.");

                var blogs = JArray.Parse(response.Content);

                Assert.That(blogs.Type, Is.EqualTo(JTokenType.Array),
                    "Expected response content to be a JSON array.");
                Assert.That(blogs.Count, Is.GreaterThan(0),
                    "Expected at least one blog in the response.");

                foreach (var blog in blogs)
                {
                    Assert.That(blog["title"].ToString(), Is.Not.Null.Or.Empty,
                        "Blog title should not be null or empty.");
                    Assert.That(blog["description"].ToString(), Is.Not.Null.Or.Empty,
                        "Blog description should not be null or empty.");
                    Assert.That(blog["author"].ToString(), Is.Not.Null.Or.Empty,
                        "Blog author should not be null or empty.");
                    Assert.That(blog["category"].ToString(), Is.Not.Null.Or.Empty,
                        "Blog category should not be null or empty.");
                    Assert.That(blog["numViews"].Value<int>, Is.GreaterThanOrEqualTo(0),
                        "Blog view numbers should be positive number.");
                }
            });
        }

        [Test, Order(1)]
        public void Test_AddBlog()
        {
            var request = new RestRequest("/blog");
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddJsonBody(new 
            {
                title = "New Test Blog",
                description = "New Test Description",
                category = "Technology"
            });

            var response = client.Execute(request, Method.Post);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Expected status code Created (200)");
                Assert.That((int)response.StatusCode, Is.EqualTo(200),
                    "Expected status code Created (200)");
                Assert.That(response.Content, Is.Not.Empty,
                    "Response content should not be empty");

                var newCreatedBlog = JObject.Parse(response.Content);

                Assert.That(newCreatedBlog["title"].ToString(), Is.EqualTo("New Test Blog"),
                    "Blog title should match the input");
                Assert.That(newCreatedBlog["description"].ToString(), Is.EqualTo("New Test Description"),
                    "Blog description should match the input");
                Assert.That(newCreatedBlog["category"].ToString(), Is.EqualTo("Technology"),
                    "Blog category should match the input");
                Assert.That(newCreatedBlog["author"].ToString(), Is.Not.Null.Or.Empty,
                    "Blog author should not be null or empty");
            });
        }

        [Test, Order(2)]
        public void Test_UpdateBlog()
        {
            var getRequest = new RestRequest("/blog");

            var getResponse = client.Execute(getRequest, Method.Get);

            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "Failed to retrived blogs");
            Assert.That(getResponse.Content, Is.Not.Empty, 
                "Get blogs response content is empty");

            var blogs = JArray.Parse(getResponse.Content);
            var blogToUpdate = blogs.FirstOrDefault(b => b["title"].ToString() == "New Test Blog");

            Assert.That(blogToUpdate, Is.Not.Null,
                "Blog with title 'New Test Blog' not found");

            var blogId = blogToUpdate["_id"].ToString();

            var updateRequest = new RestRequest($"/blog/{blogId}");
            updateRequest.AddHeader("Authorization", $"Bearer {token}");
            updateRequest.AddJsonBody(new
            {
                title = "New Test Blog",
                description = "Updated Description",
                category = "Lifestyle"
            });

            var updateResponse = client.Execute(updateRequest, Method.Put);

            Assert.Multiple(() =>
            {
                Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Expected status code OK (200)");
                Assert.That(updateResponse.Content, Is.Not.Empty,
                    "Update response content should not be empty");

                var content = JObject.Parse(updateResponse.Content);

                Assert.That(content["title"].ToString(), Is.EqualTo("New Test Blog"),
                    "Blog title should match the input");
                Assert.That(content["description"].ToString(), Is.EqualTo("Updated Description"),
                    "Blog description should match the updated value");
                Assert.That(content["category"].ToString(), Is.EqualTo("Lifestyle"),
                    "Blog category should match the updated value");
                Assert.That(content["author"].ToString(), Is.Not.Null.Or.Empty,
                    "Blog author should not be null or empty");
            });
        }

        [Test, Order(3)]
        public void Test_DeleteBlog()
        {
            var getRequest = new RestRequest("/blog");

            var getResponse = client.Execute(getRequest, Method.Get);

            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "Failed to retrived blogs");
            Assert.That(getResponse.Content, Is.Not.Empty,
                "Get blogs response content is empty");

            var blogs = JArray.Parse(getResponse.Content);
            var blogToDelete = blogs.FirstOrDefault(b => b["title"].ToString() == "New Test Blog");

            Assert.That(blogToDelete, Is.Not.Null,
                "Blog with title 'New Test Blog' not found");

            var blogId = blogToDelete["_id"].ToString();

            var deleteRequest = new RestRequest($"/blog/{blogId}");
            deleteRequest.AddHeader("Authorization", $"Bearer {token}");

            var deleteResponse = client.Execute(deleteRequest, Method.Delete);

            Assert.Multiple(() =>
            {
                Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Expected status code OK");

                var verifyGetRequest = new RestRequest($"/blog/{blogId}");

                var verifyGetResponse = client.Execute(verifyGetRequest, Method.Get);

                Assert.That(verifyGetResponse.Content, Is.Null.Or.EqualTo("null"),
                    "Verify get response content should be empty");
            });
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }

}
