namespace MovieSearchCore.DTOs
{
    public class PaginationFilter
    {
        private const int MaxPageSize = 100;
        private int _pageIndex = 1;
        private int _pageSize = 10;

        public int PageIndex
        {
            get => _pageIndex;
            set => _pageIndex = value < 1 ? 1 : value;
        }

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value switch
            {
                < 1 => 1,
                > MaxPageSize => MaxPageSize,
                _ => value
            };
        }

        public string SortBy { get; set; } = string.Empty;
        public string SortDirection { get; set; } = "asc";
    }
}
