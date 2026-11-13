using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace FortniteChestCounter;

/// <summary>
/// Stats publiques d'un compte via l'API fortnite-api.com (clé gratuite, aucun mot de passe Epic).
/// Ne donne ni coffres ni trésors : seulement les stats de compte (victoires, éliminations...).
/// </summary>
public static class AccountStatsService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(25) };

    public class Result
    {
        public string AccountName { get; set; }
        public int? BattlePassLevel { get; set; }
        public Dictionary<string, List<StatItem>> Modes { get; } = new();
    }

    public static async Task<Result> LoadAsync(string name, string apiKey)
    {
        string url = "https://fortnite-api.com/v2/stats/br/v2?accountType=epic&timeWindow=lifetime&name="
                     + Uri.EscapeDataString(name.Trim());
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.TryAddWithoutValidation("Authorization", apiKey.Trim());

        using var resp = await Http.SendAsync(req);
        string body = await resp.Content.ReadAsStringAsync();

        if (resp.StatusCode == HttpStatusCode.Unauthorized || resp.StatusCode == HttpStatusCode.Forbidden)
            throw new Exception(Loc.T("Clé API refusée. Vérifiez qu'elle est bien copiée en entier.", "API key rejected. Check that it was copied in full.", "Clave API rechazada. Comprueba que está copiada entera."));
        if (resp.StatusCode == HttpStatusCode.NotFound)
            throw new Exception(Loc.T("Compte introuvable, ou stats en privé. Dans Fortnite, rendez vos stats de jeu publiques, puis réessayez.", "Account not found, or stats are private. In Fortnite, make your game stats public, then try again.", "Cuenta no encontrada o estadísticas privadas. En Fortnite, haz públicas tus estadísticas de juego y vuelve a intentarlo."));
        if (resp.StatusCode == (HttpStatusCode)429)
            throw new Exception(Loc.T("Trop de requêtes. Patientez une minute.", "Too many requests. Wait a minute.", "Demasiadas solicitudes. Espera un minuto."));
        if (!resp.IsSuccessStatusCode)
            throw new Exception($"{Loc.T("Erreur", "Error", "Error")} {(int)resp.StatusCode} : {Trim(body)}");

        using var doc = JsonDocument.Parse(body);
        var data = doc.RootElement.GetProperty("data");
        var res = new Result();

        if (data.TryGetProperty("account", out var acc) && acc.TryGetProperty("name", out var nm))
            res.AccountName = nm.GetString();
        if (data.TryGetProperty("battlePass", out var bp) && bp.ValueKind == JsonValueKind.Object
            && bp.TryGetProperty("level", out var lv) && lv.ValueKind == JsonValueKind.Number)
            res.BattlePassLevel = lv.GetInt32();

        if (!data.TryGetProperty("stats", out var stats) || stats.ValueKind != JsonValueKind.Object
            || !stats.TryGetProperty("all", out var all) || all.ValueKind != JsonValueKind.Object)
            throw new Exception(Loc.T("Aucune stat disponible pour ce compte (stats privées ?).", "No stats available for this account (private stats?).", "No hay estadísticas para esta cuenta (¿estadísticas privadas?)."));

        foreach (var key in new[] { "overall", "solo", "duo", "squad" })
            if (all.TryGetProperty(key, out var m) && m.ValueKind == JsonValueKind.Object)
                res.Modes[key] = Build(m);

        if (res.Modes.Count == 0) throw new Exception(Loc.T("Aucune stat disponible pour ce compte.", "No stats available for this account.", "No hay estadísticas para esta cuenta."));
        return res;
    }

    private static List<StatItem> Build(JsonElement m)
    {
        var fr = Loc.Culture;
        double N(string k) => m.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;
        bool Has(string k) => m.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number;

        StatItem T(string icon, string label, string value, string sub, string accent) => new()
        {
            Key = icon, Label = label, Value = value, Sub = sub, Accent = accent, Tint = "#33" + accent.TrimStart('#')
        };

        double minutes = N("minutesPlayed");
        string time = minutes >= 60 ? $"{(minutes / 60).ToString("N0", fr)} h" : $"{minutes:N0} min";

        var l = new List<StatItem>
        {
            T("Position", Loc.T("Victoires", "Wins", "Victorias"), N("wins").ToString("N0", fr), Loc.T($"taux de victoire {N("winRate").ToString("0.#", fr)} %", $"win rate {N("winRate").ToString("0.#", fr)} %", $"tasa de victorias {N("winRate").ToString("0.#", fr)} %"), "#F2B84B"),
            T("TotalSearched", Loc.T("Parties jouées", "Matches played", "Partidas jugadas"), N("matches").ToString("N0", fr), null, "#5AA9F0"),
            T("Eliminations", Loc.T("Éliminations", "Eliminations", "Eliminaciones"), N("kills").ToString("N0", fr), Loc.T($"{N("killsPerMatch").ToString("0.##", fr)} par partie", $"{N("killsPerMatch").ToString("0.##", fr)} per match", $"{N("killsPerMatch").ToString("0.##", fr)} por partida"), "#FF7A90"),
            T("Eliminations", Loc.T("Ratio K/D", "K/D ratio", "Ratio K/D"), N("kd").ToString("0.##", fr), Loc.T($"{N("deaths").ToString("N0", fr)} défaites", $"{N("deaths").ToString("N0", fr)} deaths", $"{N("deaths").ToString("N0", fr)} derrotas"), "#9B7BFF"),
            T("Duration", Loc.T("Temps de jeu", "Play time", "Tiempo de juego"), time, null, "#6CCB8B"),
        };
        if (Has("playersOutlived"))
            l.Add(T("TotalSearched", Loc.T("Joueurs dépassés", "Players outlived", "Jugadores superados"), N("playersOutlived").ToString("N0", fr), null, "#5AA9F0"));
        if (Has("top10") && N("top10") > 0) l.Add(T("Position", "Top 10", N("top10").ToString("N0", fr), null, "#F2B84B"));
        if (Has("top25") && N("top25") > 0) l.Add(T("Position", "Top 25", N("top25").ToString("N0", fr), null, "#F2B84B"));
        if (Has("top5") && N("top5") > 0) l.Add(T("Position", "Top 5", N("top5").ToString("N0", fr), null, "#F2B84B"));
        if (Has("top12") && N("top12") > 0) l.Add(T("Position", "Top 12", N("top12").ToString("N0", fr), null, "#F2B84B"));
        if (Has("top3") && N("top3") > 0) l.Add(T("Position", "Top 3", N("top3").ToString("N0", fr), null, "#F2B84B"));
        if (Has("top6") && N("top6") > 0) l.Add(T("Position", "Top 6", N("top6").ToString("N0", fr), null, "#F2B84B"));
        return l;
    }

    private static string Trim(string s) => s.Length > 200 ? s[..200] : s;
}
