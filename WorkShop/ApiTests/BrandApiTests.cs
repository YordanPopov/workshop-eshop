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
    public class BrandApiTests : IDisposable
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

        [Test, Order(4)]
        public void Test_GetAllBrands()
        {
            var request = new RestRequest("/brand");

            var response = client.Execute(request, Method.Get);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Expected status code Ok (200)");
                Assert.That(response.Content, Is.Not.Empty, "" +
                    "Response content should not be empty");

                var brands = JArray.Parse(response.Content);
                Assert.That(brands.Type, Is.EqualTo(JTokenType.Array),
                    "Expected response content to be a JSON array");
                Assert.That(brands.Count, Is.GreaterThan(0),
                    "Expected at least one brand in the response");

                var firstBrand = brands.FirstOrDefault();
                Assert.That(firstBrand, Is.Not.Null,
                    "Expected at least one brand in the response");

                var brandNames = brands.Select(b => b["title"].ToString()).ToList();
                Assert.That(brandNames, Does.Contain("TechCorp"), 
                    "Expected brand title 'TechCorp'");
                Assert.That(brandNames, Does.Contain("GameMaster"),
                    "Expected brand title 'GameMaster'");

                foreach (var brand in brands)
                {
                    Assert.That(brand["_id"].ToString(), Is.Not.Null.Or.Empty,
                        "Brand ID should not be null or empty");
                    Assert.That(brand["title"].ToString(), Is.Not.Null.Or.Empty,
                        "Brand title should not be null or empty");
                }

                Assert.That(brands.Count, Is.GreaterThan(5),
                    "Expected more than 5 brands in the response");
            });
        }

        [Test, Order(1)]
        public void Test_AddBrand()
        {
            var request = new RestRequest("/brand");
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddJsonBody(new { title = "New Test Brand" });

            var response = client.Execute(request, Method.Post);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Expected status code is OK (200)");
                Assert.That(response.Content, Is.Not.Empty, 
                    "Response content should not be empty");

                var content = JObject.Parse(response.Content);

                Assert.That(content["_id"].ToString(), Is.Not.Null.Or.Empty,
                    "Brand ID should not be null or empty");
                Assert.That(content["title"].ToString(), Is.EqualTo("New Test Brand"),
                    "Brand title should match the input");
            });
        }

        [Test, Order(2)]
        public void Test_UpdateBrand()
        {
            var getRequest = new RestRequest("/brand");

            var getResponse = client.Execute(getRequest, Method.Get);

            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "Failed to retrieve brands");
            Assert.That(getResponse.Content, Is.Not.Empty, 
                "Get brands response content is empty");

            var brands = JArray.Parse(getResponse.Content);
            var brandToUpdate = brands.FirstOrDefault(b => b["title"].ToString() == "New Test Brand");
            Assert.That(brandToUpdate, Is.Not.Null,
                "Brand with title 'New Test Brand' not found");

            var brandId = brandToUpdate["_id"].ToString();

            var updateRequest = new RestRequest($"/brand/{brandId}");
            updateRequest.AddHeader("Authorization", $"Bearer {token}");
            updateRequest.AddJsonBody(new { title = "Updated Brand Title" });

            var updateResponse = client.Execute(updateRequest, Method.Put);

            Assert.Multiple(() =>
            {
                Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Expected status code OK (200)");
                Assert.That(updateResponse.Content, Is.Not.Empty,
                    "Response content should not be empty");

                var content = JObject.Parse(updateResponse.Content);

                Assert.That(content["_id"].ToString(), Is.EqualTo(brandId),
                    "Brand ID should match the updated brad's ID");
                Assert.That(content["title"].ToString(), Is.EqualTo("Updated Brand Title"),
                    "Brand title should be updated correctly");

                //Assert.That(content, Contains.Key("createdAt"),
                //    "Brand should have a createdAt field");
                //Assert.That(content, Contains.Key("updatedAt"),
                //    "Brand should have a updatedAt field");
                Assert.That(content["createdAt"].ToString(), Is.Not.Null);
                Assert.That(content["updatedAt"].ToString(), Is.Not.Null);

                Assert.That(content["createdAt"].ToString(), Is.Not.EqualTo(content["updatedAt"].ToString()),
                    "createdAt should be different from updatedAt after update");   
            });
        }

        [Test, Order(3)]
        public void Test_DeleteBrand()
        {
            var getRequest = new RestRequest("/brand");

            var getResponse = client.Execute(getRequest, Method.Get);

            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "Failed to retrieve brands");
            Assert.That(getResponse.Content, Is.Not.Empty,
                "Get brands response content is empty");

            var brands = JArray.Parse(getResponse.Content);
            var brandToDelete = brands.FirstOrDefault(b => b["title"].ToString() == "Updated Brand Title");
            Assert.That(brandToDelete, Is.Not.Null,
                "Brand with title 'Updated Brand Title' not found");

            var brandId = brandToDelete["_id"].ToString();

            var deleteRequest = new RestRequest($"/brand/{brandId}");
            deleteRequest.AddHeader("Authorization", $"Bearer {token}");

            var deleteResponse = client.Execute(deleteRequest, Method.Delete);

            Assert.Multiple(() =>
            {
                Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Expected status code NoContent (204) after deletion");

                var verifyGetRequest = new RestRequest($"/brand/{brandId}");

                var verifyGetResponse = client.Execute(verifyGetRequest, Method.Get);

                Assert.That(verifyGetResponse.Content, Is.Empty.Or.EqualTo("null"),
                    "Verify get response content should be empty or null");
            });
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
