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
    public class ProductApiTests : IDisposable
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
        public void Test_GetAllProducts()
        {
            var request = new RestRequest("/product");
            var response = client.Execute(request, Method.Get);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Status code should be 200 0K!");
                Assert.That((int)response.StatusCode, Is.EqualTo(200),
                    "Status code should be 200 Ok!");
                Assert.That(response.Content, Is.Not.Empty,
                    "Response content should not be empty!");

                var content = JArray.Parse(response.Content);

                var expectedProductTitles = new[]
                {
                    "Smartwatch Kids",
                    "Electric Bike",
                    "Electric Toothbrush",
                    "Smartwatch Pro",
                    "Gaming Laptop",
                    "4K Ultra HD TV"
                };

                foreach (var title in expectedProductTitles)
                {
                    Assert.That(content.ToString(), Does.Contain(title),
                        "Content array should contains expected product titles!");
                }

                var expectedProductsPrices = new Dictionary<string, decimal>
                {
                    {"Smartwatch Kids", 99 },
                    {"Electric Bike", 999 },
                    {"Electric Toothbrush", 79 },
                    {"Smartwatch Pro", 299 },
                    {"Gaming Laptop", 1499 },
                    {"4K Ultra HD TV", 899}
                };

                foreach (var product in content)
                {
                    var title = product["title"].ToString();
                    if (expectedProductsPrices.ContainsKey(title))
                    {
                        Assert.That(product["price"].Value<decimal>(), Is.EqualTo(expectedProductsPrices[title]));
                    }
                }
            });
        }

        [Test, Order(1)]
        public void Test_AddProduct()
        {
            var request = new RestRequest("/product");
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddJsonBody(new
            {
                title = "New Test Product",
                slug = "new-test-product",
                description = "New Test Description",
                price = 99.99,
                category = "test",
                brand = "test",
                quantity = 100
            });

            var response = client.Execute(request, Method.Post);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Status code should be 200 OK!");
                Assert.That(response.Content, Is.Not.Empty, 
                    "Response content should not be empty!");

                var content = JObject.Parse(response.Content);

                Assert.That(content["title"].ToString(), Is.EqualTo("New Test Product"));
                Assert.That(content["slug"].ToString(), Is.EqualTo("new-test-product"));
                Assert.That(content["description"].ToString(), Is.EqualTo("New Test Description"));
                Assert.That(content["price"].Value<decimal>, Is.EqualTo(99.99));
                Assert.That(content["category"].ToString(), Is.EqualTo("test"));
                Assert.That(content["brand"].ToString(), Is.EqualTo("test"));
                Assert.That(content["quantity"].Value<int>, Is.EqualTo(100));
            });
        }

        [Test, Order(2)]
        public void Test_UpdateProduct_InvalidProductId()
        {
            string invalidProductId = "invalidProductId12345";

            var request = new RestRequest($"/product/{invalidProductId}");
            request.AddHeader("Authorization", $"Bearer {token}");
            request.AddJsonBody(new
            {
                title = "Invalid Product Update",
                description = "This Should fail due to invalid product ID",
                price = 99.99,
                brand = "Invalid brand",
                quantity = 0
            });

            var response = client.Execute(request, Method.Put);

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError).Or.EqualTo(HttpStatusCode.BadRequest),
                    "Expected 404 Not Found or 500 Bad Request for invalid product ID.");
                Assert.That(response.Content, Does.Contain("This id is not valid or not Found").Or.EqualTo("Invalid ID"),
                    "Expected an error message indicating the product ID is invalid or not found.");
            });
        }

        [Test, Order(3)]
        public void Test_DeleteProduct()
        {
            var getRequest = new RestRequest("/product");

            var getResponse = client.Execute(getRequest, Method.Get);

            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "Expected response is OK (200)");
            Assert.That(getResponse.Content, Is.Not.Empty,
                "Expected response should not be empty");

            var products = JArray.Parse(getResponse.Content);
            var productToDelete = products.FirstOrDefault(p => p["title"].ToString() == "New Test Product");

            Assert.That(productToDelete, Is.Not.Null,
                "Product with title 'New Test Product' not found");

            var productId = productToDelete["_id"].ToString();

            var deleteRequest = new RestRequest($"/product/{productId}");
            deleteRequest.AddHeader("Authorization", $"Bearer {token}");

            var deleteResponse = client.Execute(deleteRequest, Method.Delete);

            Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "Expexted status code is OK (200)");

            var verifyGetRequest = new RestRequest($"product/{productId}");

            var verifyGetResponse = client.Execute(verifyGetRequest, Method.Get);

            Assert.That(verifyGetResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "Expected status code is OK (200)");
            Assert.That(verifyGetResponse.Content, Is.Null.Or.EqualTo("null"),
                "Expected response is not null");
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
