using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace e2eApiTests
{
    [TestFixture]
    public class ProductManegementTests : IDisposable
    {
        private RestClient client;
        private string adminToken;
        private string userToken;

        [SetUp]
        public void SetUp()
        {
            client = new RestClient(GlobalConstants.BaseUrl);
            adminToken = GlobalConstants.AuthenticateUser("admin@gmail.com", "admin@gmail.com");
            userToken = GlobalConstants.AuthenticateUser("jordan@gmail.com", "jordan@gmail.com");
            Assert.That(adminToken, Is.Not.Null.Or.Empty,
                "Authentication token is null or empty");
            Assert.That(userToken, Is.Not.Null.Or.Empty,
                "Authentication token is null or empty");
        }

        [Test]
        public void ProductLifecycleTest()
        {
            var createProductRequest = new RestRequest("/product");
            createProductRequest.AddHeader("Authorization", $"Bearer {adminToken}");
            createProductRequest.AddJsonBody(new 
            {
                Title = "Test Product",
                Description = "This is test description",
                Slug = "test-product",
                Price = 0.99,
                Category = "Electronics",
                Brand = "Apple",
                Quantity = 10
            });

            var createProductResponse = client.Execute(createProductRequest, Method.Post);
            Assert.That(createProductResponse.IsSuccessful, Is.True,
                "Product creation failed");

            string productId = JObject.Parse(createProductResponse.Content)["_id"].ToString();
            Assert.That(productId, Is.Not.Null.Or.Empty,
                "Product ID is null or empty");

            var getProductRequest = new RestRequest("/product/{id}");
            getProductRequest.AddUrlSegment("id", productId);

            var getProductResponse = client.Execute(getProductRequest, Method.Get);
            Assert.That(getProductResponse.IsSuccessful, Is.True,
                "Failed to retrieve product details");
            Assert.That(getProductResponse.Content, Is.Not.Null.Or.Empty,
                "Product details are null");

            var updateProductRequest = new RestRequest("/product/{id}");
            updateProductRequest.AddHeader("Authorization", $"Bearer {adminToken}");
            updateProductRequest.AddUrlSegment("id", productId);
            updateProductRequest.AddJsonBody(new
            {
                Name = "Updated Product",
                Description = "Updated Product Description",
                Price = 39.99
            });

            var updateProductResponse = client.Execute(updateProductRequest, Method.Put);
            Assert.That(updateProductResponse.IsSuccessful, Is.True,
                "Product update failed");

            var deleteProductRequest = new RestRequest("/product/{id}");
            deleteProductRequest.AddHeader("Authorization", $"Bearer {adminToken}");
            deleteProductRequest.AddUrlSegment("id", productId);

            var deleteProductResponse = client.Execute(deleteProductRequest, Method.Delete);
            Assert.That(deleteProductResponse.IsSuccessful, Is.True,
                "Product deletion failed");

            var verifyProductRequest = new RestRequest("/product/{id}");
            verifyProductRequest.AddUrlSegment("id", productId);

            var verifyProductResponse = client.Execute(verifyProductRequest, Method.Get);
            Assert.That(verifyProductResponse.Content, Is.Null.Or.EqualTo("null"),
                "Product still exist after deleteion");
        }

        [Test]
        public void ProductRatingLifecycleTest()
        {
            var getProductListRequest = new RestRequest("/product");

            var getProductListResponse = client.Execute(getProductListRequest, Method.Get);
            Assert.That(getProductListResponse.IsSuccessful, Is.True,
                "Failed to retrieve product list");

            var products = JArray.Parse(getProductListResponse.Content);
            Assert.That(products.Count, Is.GreaterThan(0),
                "No products found");

            var randomProduct = products[new Random().Next(products.Count)];
            string productId = randomProduct["_id"].ToString();
            Assert.That(productId, Is.Not.Null.Or.Empty,
                "Product ID is null or empty");

            var addReviewRequest = new RestRequest("/product/rating");
            addReviewRequest.AddHeader("Authorization", $"Bearer {userToken}");
            addReviewRequest.AddJsonBody(new
            {
                Star = 5,
                ProdId = productId,
                Comment = "Best product I've ever owned!"
            });

            var addReviewResponse = client.Execute(addReviewRequest, Method.Put);
            Assert.That(addReviewResponse.IsSuccessful, Is.True,
                "Adding raiting failed");

            var addToWishListRequest = new RestRequest("/product/wishlist");
            addToWishListRequest.AddHeader("Authorization", $"Bearer {userToken}");
            addToWishListRequest.AddJsonBody(new
            {
                ProdId = productId
            });

            var addToWishListResponse = client.Execute(addToWishListRequest, Method.Put);
            Assert.That(addToWishListResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "Adding product to wishlist failed");
        }

        [Test]
        public void ComplexProductInteractionTest()
        {
            var getProductListRequest = new RestRequest("/product");
            var getProductListResponse = client.Execute(getProductListRequest, Method.Get);
            Assert.That(getProductListResponse.IsSuccessful, Is.True,
                "Failed to retrieve product list");

            var products = JArray.Parse(getProductListResponse.Content);
            Assert.That(products.Count, Is.GreaterThan(0),
                "No products found");

            var randomProduct = products[new Random().Next(products.Count)];
            var productId = randomProduct["_id"].ToString();
            Assert.That(productId, Is.Not.Null.Or.Empty,
                "Product ID is null or empty");

            var addToWishListRequest = new RestRequest("/product/wishlist");
            addToWishListRequest.AddHeader("Authorization", $"Bearer {userToken}");
            addToWishListRequest.AddJsonBody(new
            {
                ProdId = productId
            });

            var addToWishListResponse = client.Execute(addToWishListRequest, Method.Put);
            Assert.That(addToWishListResponse.IsSuccessful, Is.True,
                "Adding product to wishlist failed");

            var uploadPhotoRequest = new RestRequest("/product/upload/{id}");
            uploadPhotoRequest.AddHeader("Authorization", $"Bearer {adminToken}");
            uploadPhotoRequest.AddUrlSegment("id", productId);
            uploadPhotoRequest.AddJsonBody(new
            {
                images = new[] {
                    "https://example.com/image1.jpg",
                    "https://example.com/image2.jpg"
                }
            });

            var uploadPhotoResponse = client.Execute(uploadPhotoRequest, Method.Put);
            Assert.That(uploadPhotoResponse.IsSuccessful, Is.True,
                "Uploading photo failed");

            var addRatingRequest = new RestRequest("/product/rating");
            addRatingRequest.AddHeader("Authorization", $"Bearer {userToken}");
            addRatingRequest.AddJsonBody(new
            {
                Star = 5,
                ProdId = productId,
                Comment = "Best product I've ever owned!"
            });

            var addReviewResponse = client.Execute(addRatingRequest, Method.Put);
            Assert.That(addReviewResponse.IsSuccessful, Is.True,
                "Adding raiting failed");

            var removeFromWishListRequest = new RestRequest("/product/wishlist");
            removeFromWishListRequest.AddHeader("Authorization", $"Bearer {userToken}");
            removeFromWishListRequest.AddJsonBody(new
            {
                ProdId = productId
            });

            var removeFromWishListResponse = client.Execute(removeFromWishListRequest, Method.Put);
            Assert.That(removeFromWishListResponse.IsSuccessful, Is.True,
                "Removing product from wishlist failed");
        }
        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
