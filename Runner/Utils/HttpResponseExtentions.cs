using System.Net.Http;

namespace Runner.Utils
{
    public static class HttpResponseExtensions
    {
        public static HttpResponseMessage EnsureSuccessStatusCodeImproved(this HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"The HTTP request failed with status code: {response.StatusCode}. Request: {response.RequestMessage}");
            }

            return response;
        }
    }
}
