using System.Reflection;
using MovieSearch.Core.Interfaces;

namespace MovieSearch.Infrastracture
{
    public static class ProviderHelper
    {
        public static IReadOnlyList<string> GetAvailableProviders()
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(GetLoadableTypes)
                .Where(type => typeof(IMovieProvider).IsAssignableFrom(type))
                .Where(type => type is { IsClass: true, IsAbstract: false })
                .Select(type => type.Name)
                .OrderBy(providerName => providerName)
                .ToList();
        }

        public static bool IsAvailableProvider(string? provider)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                return true;
            }

            return GetAvailableProviders().Any(availableProvider =>
                availableProvider.Equals(provider.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                return exception.Types.Where(type => type is not null)!;
            }
        }

    }
}
