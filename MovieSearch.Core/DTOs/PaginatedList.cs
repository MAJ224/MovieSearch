namespace MovieSearchCore.DTOs
{
    public class PaginatedList<T>
    {
        public PaginatedList(IEnumerable<T> source, PaginationFilter filter)
        {
            source = source.Skip((filter.PageIndex - 1) * filter.PageSize).Take(filter.PageSize);

            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                var propertyInfo = typeof(T).GetProperty(filter.SortBy);
                if (propertyInfo != null)
                {
                    if (filter.SortDirection.Equals("asc", StringComparison.CurrentCultureIgnoreCase))
                    {
                        source = source.OrderBy(e => propertyInfo.GetValue(e, null));
                    }
                    else if (filter.SortDirection.Equals("desc", StringComparison.CurrentCultureIgnoreCase))
                    {
                        source = source.OrderByDescending(e => propertyInfo.GetValue(e, null));
                    }
                }
            }

            Items = source;
            PageIndex = filter.PageIndex;
            TotalPages = (int)Math.Ceiling(source.Count() / (double)filter.PageSize);
        }

        public IEnumerable<T> Items { get; private set; }
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

    }
}
