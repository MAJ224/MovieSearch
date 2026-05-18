namespace MovieSearch.Core.Exceptions
{
    public class MovieProviderException : Exception
    {
        public MovieProviderException(string message, Exception? innerException = null)
            : base(message, innerException)
        {
        }
    }
}
