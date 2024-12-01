using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace e2eApiTests
{
    [TestFixture]
    public class BlogManagmentTest : IDisposable
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
        public void BlogPostLifecycleTest()
        {
            // Create a Blog
            var createBlogRequest = new RestRequest("/blog");
            createBlogRequest.AddHeader("Authorization", $"Bearer {token}");
            createBlogRequest.AddJsonBody(new
            {
                Title = "New Blog Post",
                Description = "This is a new blog post content",
                Category = "Technology"
            });

            var createBlogResponse = client.Execute(createBlogRequest, Method.Post);
            Assert.That(createBlogResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "Status code is not OK (200)");
            Assert.That(createBlogResponse.Content, Is.Not.Null.Or.Empty,
                "Response content is null or empty");

            string blogId = JObject.Parse(createBlogResponse.Content)["_id"]?.ToString();
            Assert.That(blogId, Is.Not.Null.Or.Empty,
                "Blog ID is null or empty");

            // Update last created Blog
            var updateBlogRequest = new RestRequest("/blog/{id}");
            updateBlogRequest.AddHeader("Authorization", $"Bearer {token}");
            updateBlogRequest.AddUrlSegment("id", blogId);
            updateBlogRequest.AddJsonBody(new
            {
                Title = "Updated Blog Post",
                Description = "This is an updated blog post content",
            });

            var updateBlogResponse = client.Execute(updateBlogRequest, Method.Put);
            Assert.That(updateBlogResponse.IsSuccessful, Is.True, 
                "Updating blog post failed");

            var updatedBlogContent = JObject.Parse(updateBlogResponse.Content);
            Assert.That(updatedBlogContent["_id"].ToString(), Is.EqualTo(blogId),
                "Updated blog ID is not equal to blogId");
            Assert.That(updatedBlogContent["title"].ToString(), Is.EqualTo("Updated Blog Post"),
                "Update blog title not match the input");
            Assert.That(updatedBlogContent["description"].ToString(), Is.EqualTo("This is an updated blog post content"),
                "Update blog description not match the input");
            Assert.That(updatedBlogContent["category"].ToString(), Is.EqualTo("Technology"),
                "Update blog category not match original input");

            // Delete last created blog
            var deleteBlogRequest = new RestRequest("/blog/{id}");
            deleteBlogRequest.AddUrlSegment("id", blogId);
            deleteBlogRequest.AddHeader("Authorization", $"Bearer {token}");

            var deleteBlogResponse = client.Execute(deleteBlogRequest, Method.Delete);
            Assert.That(deleteBlogResponse.IsSuccessful, Is.True,
                "Deleting blog post failed");

            // Verify that last created blog does not exist
            var verifyBlogRequest = new RestRequest("/blog/{id}");
            verifyBlogRequest.AddUrlSegment("id", blogId);

            var verifyBlogResponse = client.Execute(verifyBlogRequest, Method.Get);
            Assert.That(verifyBlogResponse.Content, Is.Null.Or.EqualTo("null"),
                "Blog post still exist after deletion");
        }

        [Test]
        public void BlogLificycleNegativeTest()
        {
            string invalidToken = "InvalidToken";
            string invalidBlogId = "InvalidBlogId";

            var createBlogRequest = new RestRequest("/blog");
            createBlogRequest.AddHeader("Authorization", $"Bearer {invalidToken}");
            createBlogRequest.AddJsonBody(new
            {
                Title = "test",
                Description = "test",
                Category = "test"
            });

            var createBlogResponse = client.Execute(createBlogRequest, Method.Post);
            Assert.That(createBlogResponse.IsSuccessful, Is.False,
                "Creating blog is successful");
            Assert.That(createBlogResponse.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError),
                "Response status is not internal server error");

            var getBlogRequest = new RestRequest($"/blog/{invalidBlogId}");
            var getBlogResponse = client.Execute(getBlogRequest, Method.Get);
            Assert.That(getBlogResponse.IsSuccessful, Is.False,
                "Response is successful");
            Assert.That(getBlogResponse.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError),
                "Response status is not internal server error");

            var deleteBlogRequest = new RestRequest($"/blog/{invalidBlogId}");
            deleteBlogRequest.AddHeader("Authorization", $"Bearer {invalidToken}");

            var deleteBlogResponse = client.Execute(deleteBlogRequest, Method.Delete);
            Assert.That(getBlogResponse.IsSuccessful, Is.False,
                "Response is successful");
            Assert.That(getBlogResponse.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError),
                "Response status is not internal server error");
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
