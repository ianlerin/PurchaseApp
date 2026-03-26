using Microsoft.JSInterop;
using System.Net.Http.Json;
using System.Text.Json;

namespace PurchaseBlazorApp2.Client.Service
{
    public class MyHttpService
    {
        private readonly HttpClient _http;
        private readonly IJSRuntime _js;

        public MyHttpService(HttpClient http, IJSRuntime js)
        {
            _http = http;
            _js = js;
        }

        private async Task<string> GetCompanyIDAsync()
        {
            var info = await GeneralLibrary.GetCurrentCompanyInfo(_js);
            return info.ID.ToString();
        }
        public async Task<T?> GetFromJsonAsync<T>(string url, JsonSerializerOptions? options = null)
        {
            var companyID = await GetCompanyIDAsync();

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("CompanyID", companyID);

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<T>(options);
        }
        public async Task<T?> GetFromJsonAsync<T>(Uri url, JsonSerializerOptions? options = null)
        {
            var companyID = await GetCompanyIDAsync();

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("CompanyID", companyID);

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<T>(options);
        }

        public async Task<byte[]> GetByteArrayAsync(string url)
        {
            var companyID = await GetCompanyIDAsync();

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("CompanyID", companyID);

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsByteArrayAsync();
        }

        public async Task<HttpResponseMessage> PostAsJsonAsync<T>(Uri url, T data)
        {
            var companyID = await GetCompanyIDAsync();

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(data) // Serializes the object to JSON
            };

            request.Headers.Add("CompanyID", companyID);

            return await _http.SendAsync(request);
        }
        public async Task<HttpResponseMessage> PostAsJsonAsync<T>(string url, T data)
        {
            var companyID = await GetCompanyIDAsync();

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(data) // Serializes the object to JSON
            };

            request.Headers.Add("CompanyID", companyID);

            return await _http.SendAsync(request);
        }
        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            var companyID = await GetCompanyIDAsync();

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("CompanyID", companyID);

            return await _http.SendAsync(request);
        }
    }
}
