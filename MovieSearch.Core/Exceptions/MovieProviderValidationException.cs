namespace MovieSearch.Core.Exceptions
{
    public class MovieProviderValidationException : Exception
    {
        public MovieProviderValidationException(string message)
            : base(message)
        {
        }
    }
}
