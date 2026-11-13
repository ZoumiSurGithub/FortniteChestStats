using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Threading;

namespace FortniteChestCounter;

public class StatItem
{
    public string Key { get; set; }
    public string Label { get; set; }
    public string Value { get; set; }
    /// <summary>Valeur numérique brute (permet de ré-afficher la stat dans une autre langue).</summary>
    public long? Raw { get; set; }
    /// <summary>Donnée annexe (ex. nombre total de joueurs pour le classement).</summary>
    public long? Extra { get; set; }
    /// <summary>Précision affichée sous le libellé (ex. « sur 100 joueurs »).</summary>
    public string Sub { get; set; }
    /// <summary>Couleur d'accent.</summary>
    public string Accent { get; set; }
    /// <summary>Même couleur, très transparente (fond des pastilles d'icône).</summary>
    public string Tint { get; set; }

    [JsonIgnore]
    public string LabelUpper => (Label ?? "").ToUpper(Loc.Culture);
}

/// <summary>Un trésor de la chasse au trésor reçu depuis un coffre doré.</summary>
public class TreasureEntry
{
    /// <summary>Identifiant dans le catalogue (ex. « w2-outfit »), null si le trésor n'y figure pas.</summary>
    public string CatalogId { get; set; }
    public string PrizeId { get; set; }
    public string Name { get; set; }
    public string Kind { get; set; }
    public string ImageUrl { get; set; }
    public int? WeekNumber { get; set; }
    public float? Seconds { get; set; }
}

public class Session
{
    public string File { get; set; }
    public DateTime Date { get; set; }
    public string Mode { get; set; }
    /// <summary>Nom brut de la playlist (le mode est ré-écrit dans la langue courante).</summary>
    public string Playlist { get; set; }
    public List<StatItem> Items { get; set; } = new();
    public string Details { get; set; } = "";
    /// <summary>Trésors reçus pendant la partie (null pour les anciennes sauvegardes).</summary>
    public List<TreasureEntry> Treasures { get; set; }

    [JsonIgnore]
    public string DateText => Date.ToString("ddd dd/MM  HH:mm", Loc.Culture);

    /// <summary>Réécrit libellés, valeurs et précisions dans la langue courante (les nombres bruts sont conservés).</summary>
    public void Relocalize()
    {
        if (Playlist != null) Mode = Loc.Mode(Playlist);
        foreach (var it in Items)
        {
            // Anciennes sauvegardes sans valeur brute : on la retrouve depuis le texte affiché.
            if (it.Raw == null && it.Value != null)
            {
                if (it.Key == "Duration" && it.Value.Contains(':'))
                {
                    var p = it.Value.Split(':');
                    if (p.Length == 2 && int.TryParse(p[0], out var mm) && int.TryParse(p[1], out var ss)) it.Raw = mm * 60 + ss;
                }
                else
                {
                    var digits = new string(it.Value.Where(char.IsDigit).ToArray());
                    if (digits.Length > 0 && long.TryParse(digits, out var v)) it.Raw = v;
                }
            }
            if (it.Extra == null && it.Key == "Position" && it.Sub != null)
            {
                var digits = new string(it.Sub.Where(char.IsDigit).ToArray());
                if (digits.Length > 0 && long.TryParse(digits, out var v)) it.Extra = v;
            }
            var label = Loc.StatLabel(it.Key);
            if (label != null) it.Label = label;
            if (it.Raw != null) it.Value = Loc.StatValue(it.Key, it.Raw.Value);
            if (Loc.StatSub(it.Key, it.Extra) is string sub) it.Sub = sub;
        }
    }

    /// <summary>Valeur d'une stat par sa clé ("0" si absente).</summary>
    public string Get(string key) => Items.FirstOrDefault(i => i.Key == key)?.Value ?? "0";

    /// <summary>Valeur numérique d'une stat (0 si absente ou non numérique).</summary>
    public int GetInt(string key)
        => int.TryParse(new string(Get(key).Where(char.IsDigit).ToArray()), out var v) ? v : 0;

    /// <summary>Résumé court pour l'historique : le classement et le total fouillé.</summary>
    [JsonIgnore]
    public string Summary
    {
        get
        {
            var parts = new List<string>();
            var pos = Items.FirstOrDefault(i => i.Key == "Position")?.Value;
            if (pos != null) parts.Add(pos);
            parts.Add(Get("TotalSearched") + " " + Loc.T("fouillés", "searched", "buscados"));
            return string.Join("  ·  ", parts);
        }
    }
}

/// <summary>Compteurs d'une partie, conservés pour le cumul (jamais supprimés, même quand l'historique est limité).</summary>
public class LedgerEntry
{
    public string File { get; set; }
    public DateTime Date { get; set; }
    public int Chests { get; set; }
    public int Ammo { get; set; }
    public int Other { get; set; }
    public List<TreasureEntry> Treasures { get; set; }
}

public class Totals
{
    public int Games { get; set; }
    public int Chests { get; set; }
    public int Ammo { get; set; }
    public int Other { get; set; }
    public int All => Chests + Ammo + Other;
}

/// <summary>
/// Surveille le dossier des rediffusions et analyse chaque partie terminée avec le moteur de ChestCounter (LED-N, MIT).
/// </summary>
public class ReplayService
{
    public static readonly string DemosFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FortniteGame", "Saved", "Demos");

    private static readonly string DataDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FortniteStats");
    private static string StorePath => Path.Combine(DataDir, "sessions.json");
    private static string LogPath => Path.Combine(DataDir, "log.txt");
    private static string LedgerPath => Path.Combine(DataDir, "ledger.json");

    private static readonly TimeSpan QuietPeriod = TimeSpan.FromSeconds(6);
    private static readonly TimeSpan AbandonedAge = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan ParseTimeout = TimeSpan.FromMinutes(3);

    public List<Session> Sessions { get; private set; } = new();
    public List<LedgerEntry> Ledger { get; private set; } = new();

    /// <summary>Cumul de toutes les parties analysées (par fichier de rediffusion, sans doublon).</summary>
    public Totals GetTotals() => new Totals
    {
        Games = Ledger.Count,
        Chests = Ledger.Sum(e => e.Chests),
        Ammo = Ledger.Sum(e => e.Ammo),
        Other = Ledger.Sum(e => e.Other)
    };

    private void UpsertLedger(Session s)
    {
        Ledger.RemoveAll(e => e.File == s.File);
        Ledger.Add(new LedgerEntry
        {
            File = s.File,
            Date = s.Date,
            Chests = s.GetInt("Chests"),
            Ammo = s.GetInt("AmmoBoxes"),
            Other = s.GetInt("OtherContainers"),
            Treasures = s.Treasures
        });
    }
    public bool IsRunning { get; private set; }

    public event Action<Session> SessionAdded;
    public event Action<string> StatusChanged;

    private FileSystemWatcher _watcher;
    private DispatcherTimer _debounce, _retry;
    private bool _busy;
    private readonly HashSet<string> _failed = new();
    // Rediffusions illisibles, retenues d'un lancement à l'autre (nom -> taille) : on ne les retente pas.
    private Dictionary<string, long> _failedDisk = new();
    private static string FailedPath => Path.Combine(DataDir, "failed.json");

    private void MarkFailed(string name, string path)
    {
        _failed.Add(name);
        try
        {
            _failedDisk[name] = new FileInfo(path).Length;
            Directory.CreateDirectory(DataDir);
            System.IO.File.WriteAllText(FailedPath, JsonSerializer.Serialize(_failedDisk));
        }
        catch { }
    }

    private void ForgetFailed(string name)
    {
        _failed.Remove(name);
        if (_failedDisk.Remove(name))
            try { System.IO.File.WriteAllText(FailedPath, JsonSerializer.Serialize(_failedDisk)); } catch { }
    }

    /// <summary>Partie déjà traitée (historique, cumul avec trésors, ou illisible) : inutile de la relire.</summary>
    private bool IsKnown(string path)
    {
        string name = Path.GetFileName(path);
        if (Sessions.Any(x => x.File == name)) return true;
        if (Ledger.Any(e => e.File == name && e.Treasures != null)) return true;
        if (_failed.Contains(name)) return true;
        try { return _failedDisk.TryGetValue(name, out var len) && new FileInfo(path).Length == len; }
        catch { return false; }
    }

    public ReplayService() { LoadStore(); }

    private void Status(string s) => StatusChanged?.Invoke(s);

    // ---------------- Démarrage / arrêt ----------------
    public void Start()
    {
        if (IsRunning) return;
        IsRunning = true;
        Directory.CreateDirectory(DemosFolder);

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _debounce.Tick += (_, _) => { _debounce.Stop(); _ = ProcessAsync(1, false); };

        _retry = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
        _retry.Tick += (_, _) => { _retry.Stop(); _ = ProcessAsync(1, false); };

        _watcher = new FileSystemWatcher(DemosFolder, "*.replay")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true
        };
        FileSystemEventHandler onChange = (_, e) =>
            Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                ForgetFailed(e.Name);
                _debounce.Stop();
                _debounce.Start();
            }));
        _watcher.Created += onChange;
        _watcher.Changed += onChange;

        // Une seule vérification complète au démarrage ; ensuite on ne regarde que la dernière rediffusion.
        Status(Loc.T("Surveillance active — vérification des parties…", "Monitoring active — checking games…", "Vigilancia activa — comprobando partidas…"));
        _ = ProcessAsync(40, false);
    }

    public void Stop()
    {
        IsRunning = false;
        _watcher?.Dispose();
        _watcher = null;
        _debounce?.Stop();
        _retry?.Stop();
        Status(Loc.T("Arrêté.", "Stopped.", "Detenido."));
    }

    // ---------------- Analyse ----------------
    public async Task ProcessAsync(int maxCount, bool force)
    {
        if (_busy) return;
        _busy = true;
        bool hadError = false, pending = false;
        try
        {
            if (!Directory.Exists(DemosFolder))
            {
                Status(Loc.T("Dossier de rediffusions introuvable. Activez-les dans Fortnite : Paramètres > Jeu > Replays.", "Replay folder not found. Enable replays in Fortnite: Settings > Game > Replays.", "Carpeta de repeticiones no encontrada. Actívalas en Fortnite: Ajustes > Juego > Repeticiones."));
                return;
            }

            var files = Directory.GetFiles(DemosFolder, "*.replay")
                .OrderByDescending(System.IO.File.GetLastWriteTime)
                .Take(maxCount).Reverse().ToList();

            if (files.Count == 0)
            {
                Status(Loc.T("Aucune rediffusion trouvée. Activez-les dans Fortnite : Paramètres > Jeu > Replays.", "No replay found. Enable replays in Fortnite: Settings > Game > Replays.", "No se encontró ninguna repetición. Actívalas en Fortnite: Ajustes > Juego > Repeticiones."));
                return;
            }

            int todo = files.Count(f => !IsKnown(f));
            int done = 0;
            foreach (var path in files)
            {
                string name = Path.GetFileName(path);
                if (force && path == files.Last()) { Sessions.RemoveAll(s => s.File == name); ForgetFailed(name); }
                else if (IsKnown(path)) continue;

                // Partie encore en cours d'enregistrement : Fortnite n'écrit la clé de lecture qu'à la fin.
                var age = DateTime.UtcNow - System.IO.File.GetLastWriteTimeUtc(path);
                bool live = global::ChestCounter.ReplayHeaderProbe.IsLive(path) == true;
                bool keyMissing = global::ChestCounter.ReplayHeaderProbe.KeyMissing(path);
                if (live && keyMissing)
                {
                    if (age > AbandonedAge)
                    {
                        MarkFailed(name, path);
                        Log($"{name} : enregistrement interrompu (Fortnite fermé pendant la partie), illisible.");
                    }
                    else pending = true;
                    continue;
                }
                if (age < QuietPeriod) { pending = true; continue; }

                done++;
                Status(todo > 1
                    ? Loc.T($"Analyse des parties… {done}/{todo}", $"Analysing games… {done}/{todo}", $"Analizando partidas… {done}/{todo}")
                    : Loc.T($"Analyse de {name}…", $"Analysing {name}…", $"Analizando {name}…"));
                try
                {
                    var session = await AnalyzeAsync(path);
                    Sessions = Sessions.Append(session).OrderByDescending(s => s.Date).Take(30).ToList();
                    UpsertLedger(session);
                    SaveStore();
                    SessionAdded?.Invoke(session);
                }
                catch (Exception ex)
                {
                    hadError = true;
                    MarkFailed(name, path);
                    Log($"{name} : {ex}");
                    Status(Loc.T("Erreur de lecture : ", "Read error: ", "Error de lectura: ") + ex.Message + Loc.T(" (voir log.txt)", " (see log.txt)", " (ver log.txt)"));
                }
            }

            if (pending)
            {
                Status(Loc.T("Partie en cours… les stats s'afficheront à la fin.", "Game in progress… stats will appear at the end.", "Partida en curso… las estadísticas aparecerán al final."));
                _retry?.Start();
            }
            else if (!hadError)
            {
                Status(IsRunning
                    ? Loc.T("À jour — en attente de la prochaine partie.", "Up to date — waiting for the next game.", "Al día — esperando la próxima partida.")
                    : Loc.T("Analyse terminée.", "Analysis finished.", "Análisis terminado."));
            }
        }
        finally { _busy = false; }
    }

    private static async Task<Session> AnalyzeAsync(string path)
    {
        var work = Task.Run(() =>
        {
            byte[] bytes;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                bytes = new byte[fs.Length];
                fs.ReadExactly(bytes);
            }
            using var ms = new MemoryStream(bytes, writable: false);
            // Les rediffusions d'un PC sont celles de son joueur : on suit le propriétaire de la rediffusion.
            return new global::ChestCounter.ReplayAnalyzer().Analyze(ms, Path.GetFileName(path), "");
        });

        if (await Task.WhenAny(work, Task.Delay(ParseTimeout)) != work)
            throw new TimeoutException("analyse trop longue");

        var match = await work;
        return BuildSession(match, path);
    }

    // ---------------- Construction des stats affichées ----------------
    // Couleurs de rareté Fortnite
    private const string Legendary = "#F2B84B", Epic = "#9B7BFF", Rare = "#5AA9F0", Uncommon = "#6CCB8B", Mythic = "#FF7A90";

    private static Session BuildSession(global::ChestCounter.MatchRecord m, string path)
    {
        var fr = Loc.Culture;
        var items = new List<StatItem>();
        void Add(string key, string label, string value, string accent, string sub = null, long? raw = null, long? extra = null)
            => items.Add(new StatItem { Key = key, Label = label, Value = value, Accent = accent, Sub = sub, Raw = raw, Extra = extra, Tint = "#33" + accent.TrimStart('#') });
        string N(int v) => v.ToString("N0", fr);

        // Bloc principal (affiché en grand) : le total et sa répartition, sans répétition ailleurs.
        int total = m.ChestsOpened + m.AmmoBoxesOpened + m.OtherContainersOpened;
        Add("TotalSearched", "Total fouillé", N(total), Legendary, null, total);
        Add("Chests", "Coffres", N(m.ChestsOpened), Legendary, null, m.ChestsOpened);
        Add("AmmoBoxes", "Munitions", N(m.AmmoBoxesOpened), Rare, null, m.AmmoBoxesOpened);
        Add("OtherContainers", "Autres", N(m.OtherContainersOpened), Epic, null, m.OtherContainersOpened);

        // Tuiles : une information = une tuile.
        if (m.Placement != null)
            Add("Position", "Classement", "#" + m.Placement, Epic,
                null, m.Placement, m.TotalPlayers);
        if (m.Eliminations != null) Add("Eliminations", "Éliminations", N(m.Eliminations.Value), Mythic, null, m.Eliminations.Value);
        if (m.DurationSeconds is int d) Add("Duration", "Durée", $"{d / 60}:{d % 60:00}", Rare, null, d);
        if (m.TeamChestsOpened > 0) Add("TeamChests", "Coffres de l'équipe", N(m.TeamChestsOpened), Uncommon, null, m.TeamChestsOpened);
        if (m.TreasureFinds.Count > 0) Add("Treasures", "Trésors reçus", N(m.TreasureFinds.Count), Legendary, null, m.TreasureFinds.Count);
        if (m.VehicleMeters > 0) Add("VehicleMeters", "En véhicule", N(m.VehicleMeters) + " m", Rare, null, (long)m.VehicleMeters);

        var details = new StringBuilder();
        details.AppendLine(Loc.T("Analyse faite avec le moteur de ChestCounter (LED-N, licence MIT).", "Analysis done with the ChestCounter engine (LED-N, MIT licence).", "Análisis hecho con el motor de ChestCounter (LED-N, licencia MIT)."));
        details.AppendLine(Loc.T("Rediffusion", "Replay", "Repetición") + $" : {Path.GetFileName(path)}");
        if (!string.IsNullOrWhiteSpace(m.Playlist)) details.AppendLine($"Playlist : {m.Playlist}");
        try
        {
            var why = global::ChestCounter.GameModes.WhyNot(m);
            if (why != null) details.AppendLine(Loc.T($"Note : cette partie ne serait pas comptée par ChestCounter ({why}).", $"Note: this game would not be counted by ChestCounter ({why}).", $"Nota: ChestCounter no contaría esta partida ({why})."));
        }
        catch { }
        if (m.VehicleChestsMissing)
            details.AppendLine(Loc.T($"⚠ Environ {m.UnrecordedVehicleLoot} tas de butin sans coffre enregistré (véhicule) : des coffres peuvent manquer.", $"⚠ About {m.UnrecordedVehicleLoot} loot piles with no recorded chest (vehicle): some chests may be missing.", $"⚠ Unos {m.UnrecordedVehicleLoot} montones de botín sin cofre registrado (vehículo): pueden faltar cofres."));
        if (m.OtherInteractions.Count > 0)
            details.AppendLine(Loc.T("Autres interactions : ", "Other interactions: ", "Otras interacciones: ") + string.Join(", ", m.OtherInteractions.Select(k => $"{k.Key} ×{k.Value}")));
        details.AppendLine();
        details.AppendLine(Loc.T("=== Données lues (JSON) ===", "=== Data read (JSON) ===", "=== Datos leídos (JSON) ==="));
        details.AppendLine(JsonSerializer.Serialize(m, new JsonSerializerOptions { WriteIndented = true }));

        // Trésors de la chasse au trésor (uniquement ceux sortis d'un coffre).
        var treasures = new List<TreasureEntry>();
        foreach (var f in m.TreasureFinds.Where(t => t.SourceIsChest))
        {
            var prize = f.PrizeId != null ? global::ChestCounter.TreasureCatalog.Find(f.PrizeId) : null;
            int? week = prize == null ? null
                : global::ChestCounter.TreasureCatalog.Weeks.FirstOrDefault(w => w.Prizes.Any(p => p.Id == prize.Id))?.Number;
            treasures.Add(new TreasureEntry
            {
                CatalogId = prize?.Id,
                PrizeId = f.PrizeId,
                Name = prize?.Name ?? f.DisplayName,
                Kind = prize?.Kind,
                ImageUrl = prize?.Image,
                WeekNumber = week,
                Seconds = f.ObtainedAtSeconds
            });
        }

        var built = new Session
        {
            File = Path.GetFileName(path),
            Date = System.IO.File.GetLastWriteTime(path),
            Mode = Loc.Mode(m.Playlist),
            Playlist = m.Playlist,
            Items = items,
            Details = details.ToString(),
            Treasures = treasures
        };
        built.Relocalize();
        return built;
    }

    // ---------------- Historique & journal ----------------
    private void LoadStore()
    {
        try
        {
            if (System.IO.File.Exists(StorePath))
                Sessions = JsonSerializer.Deserialize<List<Session>>(System.IO.File.ReadAllText(StorePath)) ?? new();
        }
        catch { Sessions = new(); }
        foreach (var sess0 in Sessions) sess0.Relocalize();
        Sessions.RemoveAll(x => x.Treasures == null && System.IO.File.Exists(Path.Combine(DemosFolder, x.File)));

        try
        {
            if (System.IO.File.Exists(LedgerPath))
                Ledger = JsonSerializer.Deserialize<List<LedgerEntry>>(System.IO.File.ReadAllText(LedgerPath)) ?? new();
        }
        catch { Ledger = new(); }

        try
        {
            if (System.IO.File.Exists(FailedPath))
                _failedDisk = JsonSerializer.Deserialize<Dictionary<string, long>>(System.IO.File.ReadAllText(FailedPath)) ?? new();
        }
        catch { _failedDisk = new(); }

        // Reprise : les parties déjà dans l'historique entrent dans le cumul.
        foreach (var sess in Sessions)
            if (!Ledger.Any(e => e.File == sess.File)) UpsertLedger(sess);
    }

    private void SaveStore()
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            System.IO.File.WriteAllText(StorePath,
                JsonSerializer.Serialize(Sessions, new JsonSerializerOptions { WriteIndented = true }));
            System.IO.File.WriteAllText(LedgerPath,
                JsonSerializer.Serialize(Ledger, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }

    private static void Log(string text)
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            System.IO.File.AppendAllText(LogPath, $"[{DateTime.Now}] {text}\n\n");
        }
        catch { }
    }
}
