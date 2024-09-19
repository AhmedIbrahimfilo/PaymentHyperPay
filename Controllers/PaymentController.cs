using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace PaymentHyperPay.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public PaymentController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }
        [HttpPost("request")]
        public async Task<IActionResult> RequestPayment(PaymentRequestModel model)
        {
            {
                if (model.OrderAmount <= 0)
                {
                    return BadRequest("Invalid amount.");
                }

                if (string.IsNullOrWhiteSpace(model.Currency))
                {
                    return BadRequest("Currency is required.");
                }

                var entityId = _configuration["PaymentSettings:EntityId"];
                var amount = model.OrderAmount.ToString("F2");
                var currency = model.Currency;
                var paymentType = "DB";  

                string data = $"entityId={entityId}&amount={amount}&currency={currency}&paymentType={paymentType}";

                var url = "https://eu-test.oppwa.com/v1/checkouts";
                var content = new StringContent(data, Encoding.ASCII, "application/x-www-form-urlencoded");

                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer OGE4Mjk0MTc0ZDA1OTViYjAxNGQwNWQ4MjllNzAxZDF8OVRuSlBjMm45aA==");

                var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, "Payment request failed.");
                }

                var responseData = await response.Content.ReadAsStringAsync();

                return Content(responseData, "application/json");
            }
        }

        [HttpGet("payment/{id}")]
        public IActionResult GetPaymentDetails(string id)
        {
            try
            {
                var responseData = RequestPaymentDetails(id);

                // Accessing properties using JsonElement
                if (responseData.TryGetValue("result", out JsonElement result) &&
                    result.TryGetProperty("description", out JsonElement description))
                {
                    return Ok(new { description = description.GetString() });
                }
                else
                {
                    return NotFound("Payment details not found.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        private Dictionary<string, JsonElement> RequestPaymentDetails(string id)
        {
            Dictionary<string, JsonElement> responseData;
            string data = "entityId=8a8294174d0595bb014d05d829cb01cd";
            string url = $"https://eu-test.oppwa.com/v1/checkouts/{id}/payment?{data}";
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.Method = "GET";
            request.Headers["Authorization"] = "Bearer OGE4Mjk0MTc0ZDA1OTViYjAxNGQwNWQ4MjllNzAxZDF8OVRuSlBjMm45aA==";

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream dataStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(dataStream))
                {
                    string jsonResponse = reader.ReadToEnd();
                    responseData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonResponse);
                }
            }
            return responseData;
        }

        //[HttpPost("callback")]
        //public async Task<IActionResult> PaymentCallback()
        //{
        //    // Read the JSON content from the request body
        //    var jsonContent = await new StreamReader(Request.Body).ReadToEndAsync();

        //    // Deserialize the content into a dynamic object (or create a class for it)
        //    dynamic data = JsonConvert.DeserializeObject(jsonContent);

        //    // Process the payment status
        //    // For example, you can check the status and update your order records accordingly
        //    var paymentStatus = data?.status;
        //    var paymentId = data?.id;  // Assuming 'id' is the payment identifier

        //    if (paymentStatus == "success")
        //    {
        //        // Update your order status in the database
        //        // var order = await _orderService.GetOrderByPaymentId(paymentId);
        //        // if (order != null)
        //        // {
        //        //     order.Status = "Paid";
        //        //     await _orderService.UpdateOrder(order);
        //        // }

        //        return Ok("Payment status received and processed.");
        //    }

        //    return BadRequest("Invalid payment status.");
        //}

    }
    public class PaymentRequestModel
    {
        public decimal OrderAmount { get; set; }
        public string Currency { get; set; }
    }
}