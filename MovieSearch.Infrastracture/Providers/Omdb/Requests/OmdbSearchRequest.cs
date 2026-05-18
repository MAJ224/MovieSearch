namespace MovieSearch.Infrastracture.Providers.Omdb.Requests
{
    internal class OmdbSearchRequest
    {
        public string Query { get; set; } = "";
        public string? Type { get; set; }   // movie, series, episode
        public int? Year { get; set; }
        public int Page { get; set; } = 1;

        public string ToQueryString(string apiKey) =>
            $"?apikey={apiKey}&s={Uri.EscapeDataString(Query)}" +
            (Type is not null ? $"&type={Type}" : "") +
            (Year is not null ? $"&y={Year}" : "") +
            $"&page={Page}&r=json";
    }
}
