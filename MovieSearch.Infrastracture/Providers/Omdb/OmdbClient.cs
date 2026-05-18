using MovieSearch.Infrastracture.Providers.Omdb.Requests;
using MovieSearch.Infrastracture.Providers.Omdb.Responses;
using System.Net.Http.Json;

namespace MovieSearch.Infrastracture.Providers.Omdb
{
    public class OmdbClient(HttpClient httpClient, string apiKey)
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly string _apiKey = apiKey;

        internal async Task<OmdbSearchResponse> SearchAsync(OmdbSearchRequest request, CancellationToken ct = default)
        {
            var url = request.ToQueryString(_apiKey);
            return await _httpClient.GetFromJsonAsync<OmdbSearchResponse>(url, ct)
                   ?? new OmdbSearchResponse { Response = "False", Error = "Empty response" };
        }

        internal async Task<OmdbMovieResponse> GetByIdAsync(OmdbGetByIdRequest request, CancellationToken ct = default)
        {
            var url = request.ToQueryString(_apiKey);
            return await _httpClient.GetFromJsonAsync<OmdbMovieResponse>(url, ct)
                   ?? new OmdbMovieResponse { Response = "False", Error = "Empty response" };
        }
    }
}
