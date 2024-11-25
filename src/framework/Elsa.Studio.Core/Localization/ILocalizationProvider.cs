namespace Elsa.Studio.Localization
{
    public interface ILocalizationProvider
    {
        string? GetTranslation(string key);
    }
}
