using System.Globalization;
using System.Windows.Markup;

namespace FortniteChestCounter;

/// <summary>Langue de l'interface (français, anglais, espagnol) : s'appuie sur la classe L du moteur ChestCounter.</summary>
public static class Loc
{
    public static readonly string[] Codes = { "fr", "en", "es" };

    public static CultureInfo Culture => global::ChestCounter.L.Culture;
    public static string Code => global::ChestCounter.L.Code;

    /// <summary>Texte dans la langue courante ; l'espagnol retombe sur l'anglais s'il est absent.</summary>
    public static string T(string fr, string en, string es = null) => global::ChestCounter.L.T(fr, en, es ?? en);

    public static string NativeName(string code) => global::ChestCounter.L.NativeName(code);

    /// <summary>Applique la langue enregistrée, ou celle de Windows (français, anglais ou espagnol) à défaut.</summary>
    public static void Init(string saved)
    {
        string code = saved;
        if (code == null || Array.IndexOf(Codes, code) < 0)
        {
            string sys = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            code = Array.IndexOf(Codes, sys) >= 0 ? sys : "en";
        }
        global::ChestCounter.L.Set(code);
    }

    // ---- Libellés des statistiques d'une partie ----
    public static string StatLabel(string key) => key switch
    {
        "TotalSearched" => T("Total fouillé", "Total searched", "Total buscado"),
        "Chests" => T("Coffres", "Chests", "Cofres"),
        "AmmoBoxes" => T("Munitions", "Ammo", "Munición"),
        "OtherContainers" => T("Autres", "Others", "Otros"),
        "Position" => T("Classement", "Placement", "Clasificación"),
        "Eliminations" => T("Éliminations", "Eliminations", "Eliminaciones"),
        "Duration" => T("Durée", "Duration", "Duración"),
        "TeamChests" => T("Coffres de l'équipe", "Team chests", "Cofres del equipo"),
        "Treasures" => T("Trésors reçus", "Treasures received", "Tesoros recibidos"),
        "VehicleMeters" => T("En véhicule", "In vehicle", "En vehículo"),
        _ => null
    };

    public static string StatSub(string key, long? extra) => key switch
    {
        "Position" => extra != null
            ? T($"place finale, sur {extra.Value.ToString("N0", Culture)} joueurs", $"final place, out of {extra.Value.ToString("N0", Culture)} players", $"puesto final, de {extra.Value.ToString("N0", Culture)} jugadores")
            : T("place finale", "final place", "puesto final"),
        "Eliminations" => T("joueurs éliminés", "players eliminated", "jugadores eliminados"),
        "Duration" => T("minutes : secondes", "minutes : seconds", "minutos : segundos"),
        "TeamChests" => T("ouverts par vos coéquipiers", "opened by your teammates", "abiertos por tus compañeros"),
        "Treasures" => T("chasse au trésor", "treasure hunt", "búsqueda del tesoro"),
        "VehicleMeters" => T("parcourus en véhicule", "driven in a vehicle", "recorridos en vehículo"),
        _ => null
    };

    public static string StatValue(string key, long raw) => key switch
    {
        "Position" => "#" + raw,
        "Duration" => $"{raw / 60}:{raw % 60:00}",
        "VehicleMeters" => raw.ToString("N0", Culture) + " m",
        _ => raw.ToString("N0", Culture)
    };

    /// <summary>Mode de jeu lisible à partir du nom de la playlist.</summary>
    public static string Mode(string playlist)
    {
        if (string.IsNullOrWhiteSpace(playlist)) return null;
        string p = playlist.ToLowerInvariant();
        string size = p.Contains("solo") ? "Solo" : p.Contains("duo") ? "Duo" : p.Contains("trio") ? "Trio"
                    : p.Contains("squad") ? T("Escouade", "Squad", "Escuadrón") : null;
        if (size == null) return playlist;
        bool noBuild = p.Contains("nobuild") || p.Contains("zero");
        return noBuild ? size + " · " + T("Sans construction", "No Build", "Sin construcción") : size;
    }

    public static string AccountMode(string key) => key switch
    {
        "overall" => T("Global", "Overall", "Global"),
        "solo" => "Solo",
        "duo" => "Duo",
        _ => T("Escouade", "Squad", "Escuadrón")
    };
}

/// <summary>Texte traduit dans le XAML : <c>{local:L Fr='…', En='…', Es='…'}</c>.</summary>
[MarkupExtensionReturnType(typeof(string))]
public class LExtension : MarkupExtension
{
    public string Fr { get; set; }
    public string En { get; set; }
    public string Es { get; set; }
    public override object ProvideValue(IServiceProvider serviceProvider) => Loc.T(Fr ?? "", En ?? Fr ?? "", Es);
}
