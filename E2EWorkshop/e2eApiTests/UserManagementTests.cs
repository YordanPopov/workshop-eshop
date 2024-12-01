using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace e2eApiTests
{
    public class UserManagementTests : IDisposable
    {
        private RestClient client;
        private string adminToken;

        [SetUp]
        public void SetUp()
        {
            client = new RestClient(GlobalConstants.BaseUrl);
            adminToken = GlobalConstants.AuthenticateUser("admin@gmail.com", "admin@gmail.com");
            Assert.That(adminToken, Is.Not.Null.Or.Empty,
                "Authentication token is null or empty");
        }

        [Test]
        public void UserSignUpLoginUpdateAndDeleteTest()
        {
            var signUpRequest = new RestRequest("/user/register");
            signUpRequest.AddJsonBody(new
            {
                Firstname = "Petar",
                Lastname = "Petrov",
                Email = "Pesho@gmail.com",
                Mobile = "+35977999988",
                Password = "Pesho123"
            });

            var signUpResponse = client.Execute(signUpRequest, Method.Post);
            Assert.That(signUpResponse.IsSuccessful, Is.True,
                "Signup failed");
            Assert.That(signUpResponse.Content, Is.Not.Null.Or.Not.EqualTo("null"),
                "Signup response data is null");

            var loginRequest = new RestRequest("/user/login");
            loginRequest.AddJsonBody(new
            {
                Email = "Pesho@gmail.com",
                Password = "Pesho123"
            });

            var loginResponse = client.Execute(loginRequest, Method.Post);
            Assert.That(loginResponse.IsSuccessful, Is.True,
                "login failed");
            Assert.That(loginResponse.Content, Is.Not.Null.Or.Not.EqualTo("null"),
                "Login response data is null");

            var userToken = JObject.Parse(loginResponse.Content)["token"].ToString();
            Assert.That(userToken, Is.Not.Null.Or.Empty,
                "User token is null or empty");

            var userId = JObject.Parse(loginResponse.Content)["_id"].ToString();
            Assert.That(userId, Is.Not.Null.Or.Empty,
                "User id is null or empty");

            var updateUserRequest = new RestRequest("/user/edit-user");
            updateUserRequest.AddHeader("Authorization", $"Bearer {userToken}");
            updateUserRequest.AddJsonBody(new
            {
                Firstname = "Stamat",
                Lastname = "Stamatov",
                Mobile = "+00-000-0000-0",
                Email = "Stamatov@gmail.com"
            });

            var updateUserResponse = client.Execute(updateUserRequest, Method.Put);
            Assert.That(updateUserResponse.IsSuccessful, Is.True,
                "Update user failed");
            Assert.That(updateUserResponse.Content, Is.Not.Null.Or.Not.EqualTo("null"),
                "Update response data is null");

            var deleteUserRequest = new RestRequest("/user/{id}");
            deleteUserRequest.AddUrlSegment("id", userId);

            var deleteUserResponse = client.Execute(deleteUserRequest, Method.Delete);
            Assert.That(deleteUserResponse.IsSuccessful, Is.True,
                "User deletion failed");
        }

        [Test]
        public void ProductAndUserCartTest()
        {
            var createProductRequest = new RestRequest("/product", Method.Post);
            createProductRequest.AddHeader("Authorization", $"Bearer {adminToken}");
            createProductRequest.AddJsonBody(new
            {
                Title = "Test Product",
                Description = "This is a test description",
                Slug = "test-product",
                Price = 9.99,
                Category = "Electronics",
                Brand = "Apple",
                Quantity = 10,
            });

            var createProductResponse = client.Execute(createProductRequest);
            Assert.That(createProductResponse.IsSuccessful, Is.True,
                "Product creation failed");
            
            string productId = JObject.Parse(createProductResponse.Content)["_id"].ToString();
            Assert.That(productId, Is.Not.Null.Or.Empty,
                "Product ID is null or empty");

            var loginRequest = new RestRequest("/user/login", Method.Post);
            loginRequest.AddJsonBody(new
            {
                Email = "jordan@gmail.com",
                Password = "jordan@gmail.com"
            });

            var loginResponse = client.Execute(loginRequest);
            Assert.That(loginResponse.IsSuccessful, Is.True,
                "login failed");
            Assert.That(loginResponse.Content, Is.Not.Null.Or.Not.EqualTo("null"),
                "Login response data is null");

            string userToken = JObject.Parse(loginResponse.Content)["token"].ToString();
            Assert.That(userToken, Is.Not.Null.Or.Empty,
                "User token is null or empty");

            var addToCartRequest = new RestRequest("/user/cart", Method.Post);
            addToCartRequest.AddHeader("Authorization", $"Bearer {userToken}");
            addToCartRequest.AddJsonBody(new
            {
                Cart = new[] 
                {
                    new { _id = productId, count = 1, color = "Red" }
                }
            });

            var addCartResponse = client.Execute(addToCartRequest);
            Assert.That(addCartResponse.IsSuccessful, Is.True,
                "Adding product to cart failed");

            var addCouponRequest = new RestRequest($"/user/cart/applycoupon", Method.Post);
            addCouponRequest.AddHeader("Authorization", $"Bearer {userToken}");
            addCouponRequest.AddUrlSegment("id", productId);
            addCouponRequest.AddJsonBody(new
            {
                Coupon = "BLACKFRIDAY"
            });

            var addCouponResponse = client.Execute(addCouponRequest);
            Assert.That(addCouponResponse.IsSuccessful, Is.True,
                "Adding coupon to cart failed");

            var deleteProductRequest = new RestRequest("/product/{id}", Method.Delete);
            deleteProductRequest.AddUrlSegment("id", productId);
            deleteProductRequest.AddHeader("Authorization", $"Bearer {adminToken}");

            var deleteProductResponse = client.Execute(deleteProductRequest);
            Assert.That(deleteProductResponse.IsSuccessful, Is.True,
                "Product deletion failed");
        }
        public void Dispose()
        {
            client?.Dispose();
        }
    }
}
