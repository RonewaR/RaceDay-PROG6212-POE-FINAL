using System.Net.Http;
using System.Net.Http.Json;

namespace RaceDay.MVC.Services
{
    public class RaceDayApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public RaceDayApiService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            return await client.GetFromJsonAsync<T>(endpoint);
        }

        public async Task<HttpResponseMessage> PostAsync<T>(
            string endpoint, T data)
        {
            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            return await client.PostAsJsonAsync(endpoint, data);
        }

        public async Task<HttpResponseMessage> PutAsync<T>(
            string endpoint, T data)
        {
            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            return await client.PutAsJsonAsync(endpoint, data);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string endpoint)
        {
            var client = _httpClientFactory.CreateClient("RaceDayAPI");

            return await client.DeleteAsync(endpoint);
        }
    }
}