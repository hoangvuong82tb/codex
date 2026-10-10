namespace ASMILE.BO.Infrastructure.SettingsEngine;

internal static class SettingsStoragePaths
{
    public static string DefaultDataRoot
    {
        get
        {
            var basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (string.IsNullOrWhiteSpace(basePath))
            {
                basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }

            if (string.IsNullOrWhiteSpace(basePath))
            {
                throw new InvalidOperationException("Windows did not provide an application data directory.");
            }

            return Path.Combine(basePath, "ASMILE-BO", "Data");
        }
    }
}
