using Newtonsoft.Json;
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
    public class BrandApiTests : IDisposable
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
        public void ComplexOrderLifecycleTest()
        {
            var getProductsRequest = new RestRequest("/product");

            var getProductResponse = client.Execute(getProductsRequest, Method.Get);
            Assert.That(getProductResponse.IsSuccessful, Is.True,
                "Fetching products failed");

            var products = JArray.Parse(getProductResponse.Content);
            Assert.That(products.Count, Is.GreaterThan(0),
                "No products available for the test");

            string productId = products.First()["_id"].ToString();
            Assert.That(productId, Is.Not.Null.Or.Empty,
                "Product ID is null or empty");

            var addToCartRequest = new RestRequest("/user/cart");
            addToCartRequest.AddHeader("Authorization", $"Bearer {userToken}");
            addToCartRequest.AddJsonBody(new
            {
                cart = new[]
                {
                    new { _id = productId, count = 2, color = "Red"}
                }
            });

            var addToCartResponse = client.Execute(addToCartRequest, Method.Post);
            Assert.That(addToCartResponse.IsSuccessful, Is.True, 
                "Adding first product to cart failed");

            var applyCouponRequest = new RestRequest("/user/cart/applycoupon");
            applyCouponRequest.AddHeader("Authorization", $"Bearer {userToken}");
            applyCouponRequest.AddJsonBody(new
            {
                coupon = "BLACKFRIDAY"
            });

            var applyCouponResponse = client.Execute(applyCouponRequest, Method.Post);
            Assert.That(applyCouponResponse.IsSuccessful, Is.True,
                "Applying coupon failed");

            var placeOrderRequest = new RestRequest("/user/cart/cash-order");
            placeOrderRequest.AddHeader("Authorization", $"Bearer {userToken}");
            placeOrderRequest.AddJsonBody(JsonConvert.SerializeObject(new
            {
                COD = true
            }));

            var placeOrderResponse = client.Execute(placeOrderRequest, Method.Post);
            Assert.That(placeOrderResponse.IsSuccessful, Is.True,
                "Placing order failed");

            var getOrderRequest = new RestRequest("/user/get-orders");
            getOrderRequest.AddHeader("Authorization", $"Bearer {userToken}");

            var getOrderResponse = client.Execute(getOrderRequest, Method.Get);
            Assert.That(getOrderResponse.IsSuccessful, Is.True,
                "Failed to retrieve order details");

            var order = JObject.Parse(getOrderResponse.Content);
            string orderId = order["_id"].ToString();
            Assert.That(orderId, Is.Not.Null.Or.Empty,
                "Order ID is null or empty");

            var cancelOrderRequest = new RestRequest("/user/order/update-order/{id}");
            cancelOrderRequest.AddHeader("Authorization", $"Bearer {adminToken}");
            cancelOrderRequest.AddUrlSegment("id", orderId);
            cancelOrderRequest.AddJsonBody(new
            {
                Status = "Cancelled"
            });

            var cancelOrderResponse = client.Execute(cancelOrderRequest, Method.Put);
            Assert.That(cancelOrderResponse.IsSuccessful, Is.True,
                "Order cancellation failed");
        }

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
