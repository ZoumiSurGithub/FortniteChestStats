using System.IO;
using System.Text.Json;

namespace FortniteChestCounter;

/// <summary>Réglages de l'application, enregistrés dans %APPDATA%\FortniteStats\settings.json.</summary>
public class AppSettings
{
    /// <summary>null = pas encore choisi : on suit le thème de Windows.</summary>
    public bool? DarkMode { get; set; }

    /// <summary>Pseudo Epic Games et clé fortnite-api.com pour l'onglet « Mon compte ».</summary>
    /// <summary>Langue de l'interface : "fr", "en" ou "es" (null = langue de Windows).</summary>
    public string Language { get; set; }

    public string EpicName { get; set; }
    public string ApiKey { get; set; }

    /// <summary>Récompenses de la chasse au trésor cochées à la main (identifiants du catalogue).</summary>
    public List<string> ManualTreasures { get; set; } = new();

    private static string PathOf => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FortniteStats", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (System.IO.File.Exists(PathOf))
                return JsonSerializer.Deserialize<AppSettings>(System.IO.File.ReadAllText(PathOf)) ?? new();
        }
        catch { }
        return new();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PathOf)!);
            System.IO.File.WriteAllText(PathOf, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
