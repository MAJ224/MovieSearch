namespace MovieSearch.Infrastructure.Providers.Omdb.Responses
{
    internal class OmdbSearchResponse
    {
        public List<OmdbSearchItem> Search { get; set; } = [];
        public string totalResults { get; set; } = "0";
        public string Response { get; set; } = "";
        public string? Error { get; set; }
    }

    internal class OmdbSearchItem
    {
        public string imdbID { get; set; } = "";
        public string Title { get; set; } = "";
        public string Year { get; set; } = "";
        public string Type { get; set; } = "";
        public string Poster { get; set; } = "";
    }

}
