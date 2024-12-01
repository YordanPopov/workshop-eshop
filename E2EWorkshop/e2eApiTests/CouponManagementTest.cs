using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace e2eApiTests
{
    [TestFixture]
    public class CouponManagementTest : IDisposable
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
        public void CouponLifecycleTest()
        {
            var getProductRequest = new RestRequest("/product");

            var getProductResponse = client.Execute(getProductRequest, Method.Get);
            Assert.That(getProductResponse.IsSuccessful, Is.True,
                "Fetching products failed");

            var products = JArray.Parse(getProductResponse.Content);
            Assert.That(products.Count, Is.GreaterThan(2),
                "Not enough products available for the test");

            var rnd = new Random();
            var productIds = products.Select(p => p["_id"]?.ToString()).ToList();
            string firstProductId = productIds[rnd.Next(productIds.Count)]!;
            string secondProductId = productIds[rnd.Next(productIds.Count)]!;

            while (firstProductId == secondProductId)
            {
                secondProductId = productIds[rnd.Next(productIds.Count)]!;
            }

            var createCouponRequest = new RestRequest("/coupon");
            createCouponRequest.AddHeader("Authorization", $"Bearer {adminToken}");
            createCouponRequest.AddJsonBody(new
            {
                Name = "DISCOUNT20",
                Discount = 20,
                Expiry = "2024-12-31"
            });

            var createCouponResponse = client.Execute(createCouponRequest, Method.Post);
            Assert.That(createCouponResponse.IsSuccessful, Is.True,
                "Coupon creation failed");

            string couponId = JObject.Parse(createCouponResponse.Content)["_id"]?.ToString();
            Assert.That(couponId, Is.Not.Null.Or.Empty,
                "Coupon ID is null or empty");

            var createCartRequest = new RestRequest("/user/cart");
            createCartRequest.AddHeader("Authorization", $"Bearer {userToken}");
            createCartRequest.AddJsonBody(new
            {
                cart = new[]
                {
                    new { _id = firstProductId, count = 1, color = "red" },
                    new { _id = secondProductId, count = 2, color = "blue" }
                }
            });

            var createCartResponse = client.Execute(createCartRequest, Method.Post);
            Assert.That(createCartResponse.IsSuccessful, Is.True,
                "Cart creation failed");

            var applyCouponRequest = new RestRequest("/user/cart/applycoupon");
            applyCouponRequest.AddHeader("Authorization", $"Bearer {userToken}");
            applyCouponRequest.AddJsonBody(new
            {
                Coupon = "DISCOUNT20"
            });

            var applyCouponResponse = client.Execute(applyCouponRequest, Method.Post);
            Assert.That(applyCouponResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "Status code is not OK (200)");

            var deleteCouponRequest = new RestRequest($"/coupon/{couponId}");
            deleteCouponRequest.AddHeader("Authorization", $"Bearer {adminToken}");

            var deleteCouponResponse = client.Execute(deleteCouponRequest, Method.Delete);
            Assert.That(deleteCouponResponse.IsSuccessful, Is.True,
                "Coupon deletion failed");

            var verifyCouponRequest = new RestRequest($"/coupon/{couponId}");
            verifyCouponRequest.AddHeader("Authorization", $"Bearer {adminToken}");

            var verifyCouponResponse = client.Execute(verifyCouponRequest, Method.Get);
            Assert.That(verifyCouponResponse.Content, Is.Null.Or.EqualTo("null"),
                "Coupon still exist after deletion");
        }

        [Test]
        public void CouponApplicationToOrderTest()
        {
            var getProductRequest = new RestRequest("/product");

            var getProductResponse = client.Execute(getProductRequest, Method.Get);
            Assert.That(getProductResponse.IsSuccessful, Is.True,
                "Fetching products failed");

            var products = JArray.Parse(getProductResponse.Content);
            Assert.That(products.Count, Is.GreaterThan(0),
                "No products for the test");

            string productId = products.First()["_id"].ToString();
            Assert.That(productId, Is.Not.Null.Or.Empty,
                "Product ID is null or empty");

            var createCouponRequest = new RestRequest("/coupon");
            createCouponRequest.AddHeader("Authorization", $"Bearer {adminToken}");
            createCouponRequest.AddJsonBody(new
            {
                Name = "SAVE10",
                Discount = 10,
                Expiry = "2026-12-31"
            });

            var createCouponResponse = client.Execute(createCouponRequest, Method.Post);
            Assert.That(createCouponResponse.IsSuccessful, Is.True,
                "Coupon creation failed");

            var couponId = JObject.Parse(createCouponResponse.Content)["_id"].ToString();

            var addToCartRequest = new RestRequest("/user/cart");
            addToCartRequest.AddHeader("Authorization", $"Bearer {userToken}");
            addToCartRequest.AddJsonBody(new
            {
                cart = new[]
                {
                    new { _id = productId, count = 2, color = "Red" }
                }
            });

            var addToCartResponse = client.Execute(addToCartRequest, Method.Post);
            Assert.That(addToCartResponse.IsSuccessful, Is.True,
                "Addint product to cart failed");

            var applyCouponRequest = new RestRequest("/user/cart/applycoupon");
            applyCouponRequest.AddHeader("Authorization", $"Bearer {userToken}");
            applyCouponRequest.AddJsonBody(new
            {
                Coupon = "SAVE10"
            });

            var applyCouponResponse = client.Execute(applyCouponRequest, Method.Post);
            Assert.That(applyCouponResponse.IsSuccessful, Is.True,
                "Applying coupon to cart failed");

            var placeOrderRequest = new RestRequest("/user/cart/cash-order");
            placeOrderRequest.AddHeader("Authorization", $"Bearer {userToken}");
            placeOrderRequest.AddJsonBody(JsonConvert.SerializeObject(new
            {
                COD = true,
                couponApplied = true
            }));

            var placeOrderResponse = client.Execute(placeOrderRequest, Method.Post);
            Assert.That(placeOrderResponse.IsSuccessful, Is.True,
                "Place order failed");
             
            var deleteCouponRequest = new RestRequest($"/coupon/{couponId}");
            deleteCouponRequest.AddHeader("Authorization", $"Bearer {adminToken}");

            var deleteCouponResponse = client.Execute(deleteCouponRequest, Method.Delete);
            Assert.That(deleteCouponResponse.IsSuccessful, Is.True,
                "Coupon deletion failed");

            var verifyCouponRequest = new RestRequest($"/coupon/{couponId}");
            verifyCouponRequest.AddHeader("Authorization", $"Bearer {adminToken}");

            var verifyCouponResponse = client.Execute(verifyCouponRequest, Method.Get);
            Assert.That(verifyCouponResponse.Content, Is.Null.Or.EqualTo("null"),
                "Coupon still exist after deletion");
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
