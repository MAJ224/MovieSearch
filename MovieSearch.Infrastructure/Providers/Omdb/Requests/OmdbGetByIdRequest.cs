namespace MovieSearch.Infrastructure.Providers.Omdb.Requests
{
    internal class OmdbGetByIdRequest
    {
        public string ImdbId { get; set; } = "";
        public string Plot { get; set; } = "short";  // short, full

        public string ToQueryString(string apiKey) =>
            $"?apikey={apiKey}&i={ImdbId}&plot={Plot}&r=json";
    }
}
