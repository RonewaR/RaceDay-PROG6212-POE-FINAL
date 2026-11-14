using Microsoft.AspNetCore.Mvc;

namespace RaceDay.MVC.Controllers
{
    public class ApiTestController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ApiTestController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("RaceDayAPI");

                var response = await client.GetAsync("api/DbTest");
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    return Content(
                        "SUCCESS! RaceDay.MVC connected to RaceDay.API.\n\n" +
                        "Status: " + (int)response.StatusCode + "\n\n" +
                        "API response:\n" + content
                    );
                }

                return Content(
                    "The API responded, but returned status " +
                    (int)response.StatusCode + ".\n\n" + content
                );
            }
            catch (Exception ex)
            {
                return Content(
                    "Connection failed.\n\n" + ex.Message
                );
            }
        }
    }
}