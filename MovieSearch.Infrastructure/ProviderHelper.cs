using MovieSearch.Core.Interfaces;

namespace MovieSearch.Infrastructure
{
    public static class ProviderHelper
    {
        public static IReadOnlyList<string> GetAvailableProviders(IEnumerable<IMovieProvider> providers)
        {
            return providers
                .Select(provider => provider.GetType().Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(providerName => providerName)
                .ToList();
        }

        public static IMovieProvider? ResolveProvider(IEnumerable<IMovieProvider> providers, string? provider)
        {
            var availableProviders = providers
                .OrderBy(provider => provider.GetType().Name)
                .ToList();

            if (string.IsNullOrWhiteSpace(provider))
            {
                return availableProviders.FirstOrDefault();
            }

            return availableProviders.FirstOrDefault(availableProvider =>
                availableProvider.GetType().Name.Equals(provider.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
