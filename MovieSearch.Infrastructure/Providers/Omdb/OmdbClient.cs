using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MovieSearch.Core.Exceptions;
using MovieSearch.Infrastructure.Providers.Omdb.Requests;
using MovieSearch.Infrastructure.Providers.Omdb.Responses;

namespace MovieSearch.Infrastructure.Providers.Omdb
{
    public class OmdbClient(HttpClient httpClient, string apiKey)
    {
        private readonly HttpClient _httpClient = httpClient;
        private readonly string _apiKey = apiKey;

        internal async Task<OmdbSearchResponse> SearchAsync(OmdbSearchRequest request, CancellationToken ct = default)
        {
            var url = request.ToQueryString(_apiKey);

            return await GetFromJsonAsync<OmdbSearchResponse>(url, ct);
        }

        internal async Task<OmdbMovieResponse> GetByIdAsync(OmdbGetByIdRequest request, CancellationToken ct = default)
        {
            var url = request.ToQueryString(_apiKey);

            return await GetFromJsonAsync<OmdbMovieResponse>(url, ct);
        }

        private async Task<TResponse> GetFromJsonAsync<TResponse>(string url, CancellationToken ct)
        {
            try
            {
                using var response = await _httpClient.GetAsync(url, ct);

                if (!response.IsSuccessStatusCode)
                {
                    throw new MovieProviderException(GetStatusCodeMessage(response.StatusCode));
                }

                return await response.Content.ReadFromJsonAsync<TResponse>(ct)
                    ?? throw new MovieProviderException("Movie provider returned an empty response.");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (MovieProviderException)
            {
                throw;
            }
            catch (OperationCanceledException exception)
            {
                throw new MovieProviderException("Movie provider request timed out.", exception);
            }
            catch (HttpRequestException exception)
            {
                throw new MovieProviderException("Movie provider request failed.", exception);
            }
            catch (JsonException exception)
            {
                throw new MovieProviderException("Movie provider returned invalid data.", exception);
            }
            catch (NotSupportedException exception)
            {
                throw new MovieProviderException("Movie provider returned unsupported data.", exception);
            }
        }

        private static string GetStatusCodeMessage(HttpStatusCode statusCode) =>
            $"Movie provider returned HTTP {(int)statusCode}.";
    }
}
