namespace MovieSearchCore.DTOs
{
    public class PaginatedList<T>
    {
        public PaginatedList(IEnumerable<T> source, PaginationFilter filter)
        {
            var items = source.ToList();

            if (!string.IsNullOrEmpty(filter.SortBy))
            {
                var propertyInfo = typeof(T).GetProperty(filter.SortBy);
                if (propertyInfo != null)
                {
                    if (filter.SortDirection.Equals("asc", StringComparison.CurrentCultureIgnoreCase))
                    {
                        items = items.OrderBy(e => propertyInfo.GetValue(e, null)).ToList();
                    }
                    else if (filter.SortDirection.Equals("desc", StringComparison.CurrentCultureIgnoreCase))
                    {
                        items = items.OrderByDescending(e => propertyInfo.GetValue(e, null)).ToList();
                    }
                }
            }

            TotalCount = items.Count;
            Items = items.Skip((filter.PageIndex - 1) * filter.PageSize).Take(filter.PageSize);
            PageIndex = filter.PageIndex;
            PageSize = filter.PageSize;
            TotalPages = (int)Math.Ceiling(TotalCount / (double)filter.PageSize);
        }

        public PaginatedList(IEnumerable<T> items, PaginationFilter filter, int totalCount)
        {
            Items = items;
            PageIndex = filter.PageIndex;
            PageSize = filter.PageSize;
            TotalCount = totalCount;
            TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize);
        }

        public IEnumerable<T> Items { get; private set; }
        public int PageIndex { get; private set; }
        public int PageSize { get; private set; }
        public int TotalCount { get; private set; }
        public int TotalPages { get; private set; }
        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;
    }
}
