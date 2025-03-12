namespace Elsa.Studio.Localization;

internal class DefaultLocalizationProvider : ILocalizationProvider
{
    public string GetTranslation(string key)
    {
        return key;
    }
}