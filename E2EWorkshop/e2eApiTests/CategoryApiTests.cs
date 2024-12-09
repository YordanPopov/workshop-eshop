using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace e2eApiTests
{
    [TestFixture]
    public class CategoryApiTests : IDisposable
    {
        private RestClient client;
        private string token;

        [SetUp]
        public void SetUp()
        {
            client = new RestClient(GlobalConstants.BaseUrl);
            token = GlobalConstants.AuthenticateUser("admin@gmail.com", "admin@gmail.com");
            Assert.That(token, Is.Not.Null.Or.Empty,
                "Authentication token is null or empty!");
        }

        [Test]
        public void CategoryLifecycleTest()
        {
            var createCategoryRequest = new RestRequest("/category");
            createCategoryRequest.AddHeader("Authorization", $"Bearer {token}");
            createCategoryRequest.AddJsonBody(new { Title = "New Test Category" });

            var createCategoryResponse = client.Execute(createCategoryRequest, Method.Post);
            Assert.That(createCategoryResponse.IsSuccessful, Is.True,
                "Creating new category failed!");
            Assert.That(createCategoryResponse.Content, Is.Not.Empty,
                "Category response is empty");

            string categoryId = JObject.Parse(createCategoryResponse.Content)["_id"].ToString();

            var getAllCategoryRequest = new RestRequest("/category");

            var getAllCategoryResponse = client.Execute(getAllCategoryRequest, Method.Get);
            Assert.That(getAllCategoryResponse.IsSuccessful, Is.True,
                "Getting categories failed!");
            Assert.That(getAllCategoryResponse.Content, Is.Not.Empty,
                "Get categories response is empty");

            var allCategories = JArray.Parse(getAllCategoryResponse.Content);
            Assert.That(allCategories.ToString(), Does.Contain(categoryId),
                "Category not exist in categories list");

            var updateCategoryRequest = new RestRequest("/category/{id}");
            updateCategoryRequest.AddHeader("Authorization", $"Bearer {token}");
            updateCategoryRequest.AddUrlSegment("id", categoryId);
            updateCategoryRequest.AddJsonBody(new { Title = "Updated Title" });

            var updateCategoryResponse = client.Execute(updateCategoryRequest, Method.Put);
            Assert.That(updateCategoryResponse.IsSuccessful, Is.True,
                "Updating category failed!");
            Assert.That(updateCategoryResponse.Content, Is.Not.Empty,
                "Update category response is empty");

            var verifyUpdateRequest = new RestRequest("/category/{id}");
            verifyUpdateRequest.AddUrlSegment("id", categoryId);

            var verifyUpdateResponse = client.Execute(verifyUpdateRequest); // Method.Get by Default
            Assert.That(verifyUpdateResponse.IsSuccessful, Is.True,
                "Category not found");

            var deleteCategoryRequest = new RestRequest("/category/{id}");
            deleteCategoryRequest.AddHeader("Authorization", $"Bearer {token}");
            deleteCategoryRequest.AddUrlSegment("id", categoryId);

            var deleteCategoryResponse = client.Execute(deleteCategoryRequest, Method.Delete);
            Assert.That(deleteCategoryResponse.IsSuccessful, Is.True,
                "Deleting category failed!");
        }
        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
