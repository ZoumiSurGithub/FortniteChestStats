using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace FortniteChestCounter;

public partial class MainWindow : Window
{
    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr h, int id, uint mod, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr h, int id);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_TOGGLE = 1, HOTKEY_QUIT = 2;
    private const uint MOD_CONTROL = 0x2;
    private const uint VK_F7 = 0x76, VK_F8 = 0x77;

    private readonly ReplayService _svc = new();
    private readonly AppSettings _settings = AppSettings.Load();
    private bool _dark;
    private OverlayWindow _overlay;

    public MainWindow()
    {
        InitializeComponent();
        ApplyTheme(_settings.DarkMode ?? WindowsPrefersDark());
        HeroIcon.Source = Icons.Get("TotalSearched");
        IcoChests.Source = Icons.Get("Chests");
        IcoAmmo.Source = Icons.Get("AmmoBoxes");
        IcoOther.Source = Icons.Get("OtherContainers");
        IcoCumul.Source = Icons.Get("Chests");

        _svc.StatusChanged += s => StatusText.Text = s;
        _svc.SessionAdded += s =>
        {
            RefreshHistory();
            // L'overlay ne montre que la dernière partie (la plus récente), jamais une plus ancienne.
            if (_svc.Sessions.Count > 0) _overlay?.Update(_svc.Sessions[0]);
        };

        RefreshHistory();
        LangButton.Content = "🌐  " + Loc.Code.ToUpperInvariant();
        EpicNameBox.Text = _settings.EpicName ?? "";
        ApiKeyBox.Password = _settings.ApiKey ?? "";
        int startTab = _restoreTab ?? 0;
        _restoreTab = null;
        ShowTab(startTab, false);
        Loaded += (_, _) => Anim.FadeIn((UIElement)Content);
    }

    // ---------------- Langue ----------------
    private void LangButton_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu
        {
            PlacementTarget = LangButton,
            Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom
        };
        foreach (var code in Loc.Codes)
        {
            var c = code;
            var item = new MenuItem { Header = Loc.NativeName(c), IsChecked = c == Loc.Code, FontSize = 14 };
            item.Click += (_, _) => SwitchLanguage(c);
            menu.Items.Add(item);
        }
        menu.IsOpen = true;
    }

    /// <summary>Change la langue : la fenêtre est recréée à la même place, la surveillance reprend si elle était active.</summary>
    private void SwitchLanguage(string code)
    {
        if (code == Loc.Code) return;
        _settings.Language = code;
        _settings.Save();
        Loc.Init(code);

        bool running = _svc.IsRunning;
        _restoreTab = _tab;
        var (l, t, w, h) = (Left, Top, Width, Height);
        bool max = WindowState == WindowState.Maximized;

        var app = Application.Current;
        var mode = app.ShutdownMode;
        app.ShutdownMode = ShutdownMode.OnExplicitShutdown;   // fermer l'ancienne fenêtre ne doit pas quitter l'appli
        Close();

        var win = new MainWindow
        {
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = l, Top = t, Width = w, Height = h
        };
        if (max) win.WindowState = WindowState.Maximized;
        app.MainWindow = win;
        win.Show();
        app.ShutdownMode = mode;
        if (running) win.StartAll();
    }

    // ---------------- Bouton Lancer / Arrêter ----------------
    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_svc.IsRunning) StartAll(); else StopAll();
    }

    private void StartAll()
    {
        _overlay = new OverlayWindow();
        _overlay.Show();
        if (_svc.Sessions.Count > 0) _overlay.Update(_svc.Sessions[0]);
        RefreshCumul();

        _svc.Start();
        StartButton.Content = Loc.T("■  Arrêter", "■  Stop", "■  Detener");
        StartButton.Background = (Brush)FindResource("StopBrush");
    }

    private void StopAll()
    {
        _svc.Stop();
        _overlay?.Close();
        _overlay = null;
        StartButton.Content = Loc.T("▶  Lancer", "▶  Start", "▶  Iniciar");
        StartButton.Background = (Brush)FindResource("StartBrush");
    }

    private async void AnalyzeButton_Click(object sender, RoutedEventArgs e)
    {
        AnalyzeButton.IsEnabled = false;
        try { await _svc.ProcessAsync(3, true); }
        finally { AnalyzeButton.IsEnabled = true; }
    }

    // ---------------- Affichage des stats ----------------
    private void RefreshHistory()
    {
        HistoryList.ItemsSource = null;
        HistoryList.ItemsSource = _svc.Sessions.ToList();
        if (_svc.Sessions.Count > 0) HistoryList.SelectedIndex = 0;
        else ShowSession(null);
        RefreshCumul();
        RefreshTreasure();
    }

    // ---------------- Onglets ----------------
    private int _tab;
    // Onglet à rouvrir après un changement de langue (lu par la nouvelle fenêtre dès sa création).
    private static int? _restoreTab;
    private void TabGames_Click(object sender, RoutedEventArgs e) => ShowTab(0);
    private void TabTreasure_Click(object sender, RoutedEventArgs e) => ShowTab(1);
    private void TabAccount_Click(object sender, RoutedEventArgs e) => ShowTab(2);

    // Animations des cartes (apparition échelonnée + zoom au survol)
    private void Card_Loaded(object sender, RoutedEventArgs e) { if (sender is FrameworkElement el) Anim.Enter(el); }
    private void Card_Enter(object sender, System.Windows.Input.MouseEventArgs e) { if (sender is FrameworkElement el) Anim.Hover(el, true); }
    private void Card_Leave(object sender, System.Windows.Input.MouseEventArgs e) { if (sender is FrameworkElement el) Anim.Hover(el, false); }

    private void ShowTab(int tab, bool animate = true)
    {
        bool changed = tab != _tab;
        _tab = tab;
        GamesScroll.Visibility = tab == 0 ? Visibility.Visible : Visibility.Collapsed;
        TreasureScroll.Visibility = tab == 1 ? Visibility.Visible : Visibility.Collapsed;
        AccountScroll.Visibility = tab == 2 ? Visibility.Visible : Visibility.Collapsed;
        StyleTab(TabGames, tab == 0);
        StyleTab(TabTreasure, tab == 1);
        StyleTab(TabAccount, tab == 2);
        if (animate && changed)
        {
            Anim.ResetStagger();
            Anim.Tab(tab == 0 ? GamesScroll : tab == 1 ? TreasureScroll : AccountScroll);
        }
    }

    private void StyleTab(Button b, bool on)
    {
        bool pill = ReferenceEquals(b.Style, FindResource("TabButton"));
        b.Background = on ? (Brush)FindResource("StartBrush") : pill ? Brushes.Transparent : (Brush)FindResource("SecBtnBg");
        b.Foreground = on ? Brushes.White : (Brush)FindResource("AccentText");
    }

    // ---------------- Mon compte ----------------
    private AccountStatsService.Result _account;
    private string _mode = "overall";

    private async void AccountLoad_Click(object sender, RoutedEventArgs e)
    {
        string name = EpicNameBox.Text.Trim(), key = ApiKeyBox.Password.Trim();
        if (name.Length == 0 || key.Length == 0)
        {
            AccountStatus.Text = Loc.T("Renseignez votre pseudo Epic et la clé API.", "Enter your Epic username and the API key.", "Introduce tu nombre de Epic y la clave API.");
            return;
        }
        _settings.EpicName = name;
        _settings.ApiKey = key;
        _settings.Save();

        AccountLoadButton.IsEnabled = false;
        AccountStatus.Text = Loc.T("Chargement…", "Loading…", "Cargando…");
        try
        {
            _account = await AccountStatsService.LoadAsync(name, key);
            AccountStatus.Text = Loc.T("Stats chargées.", "Stats loaded.", "Estadísticas cargadas.");
            AccountHeader.Text = (_account.AccountName ?? name)
                + (_account.BattlePassLevel is int lv ? Loc.T($"  ·  niveau {lv}", $"  ·  level {lv}", $"  ·  nivel {lv}") : "");
            AccountHeader.Visibility = Visibility.Visible;
            if (!_account.Modes.ContainsKey(_mode)) _mode = _account.Modes.Keys.First();
            BuildModeBar();
            ShowMode(_mode);
        }
        catch (Exception ex)
        {
            AccountStatus.Text = ex.Message;
        }
        finally { AccountLoadButton.IsEnabled = true; }
    }

    private void BuildModeBar()
    {
        ModeBar.Children.Clear();
        foreach (var m in _account.Modes.Keys)
        {
            var b = new Button
            {
                Content = Loc.AccountMode(m), Style = (Style)FindResource("SoftButton"), FontSize = 13,
                Padding = new Thickness(18, 7, 18, 7), Margin = new Thickness(0, 0, 10, 0), Tag = m
            };
            b.Click += (_, _) => ShowMode((string)b.Tag);
            ModeBar.Children.Add(b);
        }
        ModeBar.Visibility = Visibility.Visible;
    }

    private void ShowMode(string mode)
    {
        _mode = mode;
        Anim.ResetStagger();
        AccountStats.ItemsSource = _account.Modes[mode];
        foreach (Button b in ModeBar.Children) StyleTab(b, (string)b.Tag == mode);
    }

    // ---------------- Chasse au trésor ----------------
    private void RewardCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not RewardCard c) return;
        if (c.Count > 0) return; // déjà trouvé dans une rediffusion : rien à cocher
        c.Manual = !c.Manual;
        _settings.ManualTreasures ??= new();
        if (c.Manual) { if (!_settings.ManualTreasures.Contains(c.Id)) _settings.ManualTreasures.Add(c.Id); }
        else _settings.ManualTreasures.Remove(c.Id);
        _settings.Save();
    }

    private static StatItem Tile(string key, string label, string value, string sub, string accent) => new()
    {
        Key = key, Label = label, Value = value, Sub = sub, Accent = accent, Tint = "#33" + accent.TrimStart('#')
    };

    private void RefreshTreasure()
    {
        var fr = Loc.Culture;
        Anim.ResetStagger();

        // Semaine en cours
        var now = DateTime.UtcNow;
        var week = global::ChestCounter.TreasureCatalog.WeekOf(now);
        TreasureTitle.Text = week != null
            ? Loc.T($"Chasse au trésor · Semaine {week.Number}", $"Treasure hunt · Week {week.Number}", $"Búsqueda del tesoro · Semana {week.Number}")
            : Loc.T("Chasse au trésor", "Treasure hunt", "Búsqueda del tesoro");
        TreasureSub.Text = week != null
            ? $"{week.Title}  ·  " + Loc.T("jusqu'au ", "until ", "hasta el ") + week.To.ToLocalTime().ToString("d MMMM", fr)
            : Loc.T("Aucune semaine de chasse en cours d'après les données connues.", "No hunt week in progress according to the known data.", "Ninguna semana de búsqueda en curso según los datos conocidos.");

        // Dernière partie : fouille uniquement (ni éliminations ni classement)
        var last = _svc.Sessions.FirstOrDefault();
        TreasureNoGame.Visibility = last == null ? Visibility.Visible : Visibility.Collapsed;
        TreasureLast.ItemsSource = last == null ? null : new List<StatItem>
        {
            Tile("Chests", Loc.T("Coffres fouillés", "Chests searched", "Cofres abiertos"), last.GetInt("Chests").ToString("N0", fr), null, "#F2B84B"),
            Tile("AmmoBoxes", Loc.T("Boîtes de munitions", "Ammo boxes", "Cajas de munición"), last.GetInt("AmmoBoxes").ToString("N0", fr), null, "#5AA9F0"),
            Tile("OtherContainers", Loc.T("Autres contenants", "Other containers", "Otros contenedores"), last.GetInt("OtherContainers").ToString("N0", fr), null, "#6CCB8B"),
            Tile("Treasures", Loc.T("Trésors trouvés", "Treasures found", "Tesoros encontrados"), (last.Treasures?.Count ?? 0).ToString("N0", fr), Loc.T("dans cette partie", "in this game", "en esta partida"), "#9B7BFF"),
        };

        // Cumul (le registre n'est jamais purgé)
        var t = _svc.GetTotals();
        var all = _svc.Ledger.Where(l => l.Treasures != null).SelectMany(l => l.Treasures).ToList();
        TreasureAll.ItemsSource = new List<StatItem>
        {
            Tile("Chests", Loc.T("Coffres cumulés", "Total chests", "Cofres acumulados"), t.Chests.ToString("N0", fr), Loc.T($"sur {t.Games} partie{(t.Games > 1 ? "s" : "")}", $"over {t.Games} game{(t.Games > 1 ? "s" : "")}", $"en {t.Games} partida{(t.Games > 1 ? "s" : "")}"), "#F2B84B"),
            Tile("AmmoBoxes", Loc.T("Munitions cumulées", "Total ammo", "Munición acumulada"), t.Ammo.ToString("N0", fr), null, "#5AA9F0"),
            Tile("OtherContainers", Loc.T("Autres cumulés", "Total others", "Otros acumulados"), t.Other.ToString("N0", fr), null, "#6CCB8B"),
            Tile("Treasures", Loc.T("Trésors trouvés", "Treasures found", "Tesoros encontrados"), all.Count.ToString("N0", fr), Loc.T($"{all.Select(a => a.CatalogId ?? a.Name).Distinct().Count()} différents", $"{all.Select(a => a.CatalogId ?? a.Name).Distinct().Count()} different", $"{all.Select(a => a.CatalogId ?? a.Name).Distinct().Count()} distintos"), "#9B7BFF"),
        };

        // Collection : toutes les récompenses du catalogue, trouvées en couleur
        var counts = all.Where(a => a.CatalogId != null).GroupBy(a => a.CatalogId).ToDictionary(g => g.Key, g => g.Count());

        // Coffres fouillés (rediffusions suivies) dans une période ; sans période : depuis le début de la chasse.
        var hunt0 = global::ChestCounter.TreasureCatalog.Weeks.Count > 0
            ? global::ChestCounter.TreasureCatalog.Weeks.Min(w => w.From) : DateTime.MinValue;
        long ChestsBetween(DateTime from, DateTime to) =>
            _svc.Ledger.Where(l => { var d = l.Date.ToUniversalTime(); return d >= from && d < to; }).Sum(l => (long)l.Chests);

        static string Pct(double p)
        {
            double v = p * 100;
            string f = v >= 10 ? "0.#" : v >= 1 ? "0.##" : v >= 0.01 ? "0.###" : "0.######";
            return v.ToString(f, Loc.Culture) + " %";
        }

        RewardCard Card(global::ChestCounter.TreasurePrize p, global::ChestCounter.TreasureWeek w = null)
        {
            var odds = global::ChestCounter.TreasureCatalog.Odds.FirstOrDefault(o => o.Kind == p.Kind);
            string oddsText = null, chanceText = null;
            if (odds != null)
            {
                long chests = w != null ? ChestsBetween(w.From, w.To) : ChestsBetween(hunt0, DateTime.MaxValue);
                oddsText = Loc.T($"1 sur {odds.OneIn.ToString("N0", fr)}  ·  {Pct(1.0 / odds.OneIn)} par coffre", $"1 in {odds.OneIn.ToString("N0", fr)}  ·  {Pct(1.0 / odds.OneIn)} per chest", $"1 entre {odds.OneIn.ToString("N0", fr)}  ·  {Pct(1.0 / odds.OneIn)} por cofre");
                double chance = global::ChestCounter.TreasureCatalog.AtLeastOnce(odds.OneIn, chests);
                string cn = chests.ToString("N0", fr), pc = Pct(chance);
                chanceText = Loc.T($"Votre chance : {pc} ({cn} coffre{(chests > 1 ? "s" : "")})",
                                   $"Your chance: {pc} ({cn} chest{(chests > 1 ? "es" : "")})",
                                   $"Tu probabilidad: {pc} ({cn} cofre{(chests > 1 ? "s" : "")})");
            }

            var c = new RewardCard
            {
                Id = p.Id,
                OddsText = oddsText,
                ChanceText = chanceText,
                Name = p.Name,
                Manual = _settings.ManualTreasures.Contains(p.Id),
                Kind = global::ChestCounter.TreasureCatalog.KindName(p.Kind),
                Count = counts.GetValueOrDefault(p.Id)
            };
            c.LoadImage(p.Image);
            return c;
        }

        var groups = new List<WeekGroup>();
        foreach (var w in global::ChestCounter.TreasureCatalog.Weeks)
        {
            groups.Add(new WeekGroup
            {
                Title = Loc.T($"Semaine {w.Number} · {w.Title}", $"Week {w.Number} · {w.Title}", $"Semana {w.Number} · {w.Title}"),
                Sub = Loc.T($"du {w.From.ToLocalTime().ToString("d MMM", fr)} au {w.To.ToLocalTime().ToString("d MMM", fr)}",
                            $"from {w.From.ToLocalTime().ToString("d MMM", fr)} to {w.To.ToLocalTime().ToString("d MMM", fr)}",
                            $"del {w.From.ToLocalTime().ToString("d MMM", fr)} al {w.To.ToLocalTime().ToString("d MMM", fr)}"),
                Current = week != null && w.Number == week.Number,
                Cards = w.Prizes.Select(p => Card(p, w)).ToList()
            });
        }
        groups.Add(new WeekGroup
        {
            Title = Loc.T("Accès à la boutique pirate", "Pirate Shop Access", "Acceso a la tienda pirata"),
            Sub = Loc.T("récompense spéciale", "special reward", "recompensa especial"),
            Cards = new List<RewardCard> { Card(global::ChestCounter.TreasureCatalog.PirateShopAccess) }
        });
        WeekList.ItemsSource = groups;

        // Trouvailles hors catalogue (artefacts, trésors de type inconnu…), sans image
        var others = all.Where(a => a.CatalogId == null)
            .GroupBy(a => a.Name ?? Loc.T("Trésor inconnu", "Unknown treasure", "Tesoro desconocido"))
            .Select(g => new OtherFind { Name = g.Key, Kind = Loc.T("hors catalogue", "not in catalogue", "fuera del catálogo"), Count = g.Count() })
            .OrderByDescending(o => o.Count).ToList();
        OtherList.ItemsSource = others;
        OtherTitle.Visibility = others.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    // ---------------- Mode jour / nuit ----------------
    private static readonly Dictionary<string, string> LightTheme = new()
    {
        ["TextPrimary"] = "#2D2B55", ["TextSecondary"] = "#6E6C9C", ["TextMuted"] = "#8E8CB8", ["AccentText"] = "#6B57D6",
        ["CardBrush"] = "#E8FFFFFF", ["RowBrush"] = "#C8FFFFFF", ["RowHoverBrush"] = "#E6FFFFFF", ["RowSelBrush"] = "#F0E9FF",
        ["DetailsBg"] = "#F6F3FF", ["DetailsFg"] = "#3B3966", ["SecBtnBg"] = "#EDE8FF"
    };

    private static readonly Dictionary<string, string> DarkTheme = new()
    {
        ["TextPrimary"] = "#F4F2FF", ["TextSecondary"] = "#C4C1F0", ["TextMuted"] = "#9390C9", ["AccentText"] = "#BBA8FF",
        ["CardBrush"] = "#EE262358", ["RowBrush"] = "#B8262358", ["RowHoverBrush"] = "#D4322E70", ["RowSelBrush"] = "#E04036A0",
        ["DetailsBg"] = "#161433", ["DetailsFg"] = "#D8D6F5", ["SecBtnBg"] = "#33305F"
    };

    private static SolidColorBrush Solid(string hex) => (SolidColorBrush)new BrushConverter().ConvertFromString(hex);

    private void ApplyTheme(bool dark)
    {
        _dark = dark;
        foreach (var kv in dark ? DarkTheme : LightTheme) Resources[kv.Key] = Solid(kv.Value);

        var (c1, c2, c3) = dark ? ("#17142F", "#1D1A46", "#12213F") : ("#F1ECFF", "#E6EEFF", "#DDF2FF");
        Resources["BgBrush"] = new LinearGradientBrush(new GradientStopCollection
        {
            new GradientStop((Color)ColorConverter.ConvertFromString(c1), 0),
            new GradientStop((Color)ColorConverter.ConvertFromString(c2), 0.55),
            new GradientStop((Color)ColorConverter.ConvertFromString(c3), 1)
        })
        { StartPoint = new Point(0, 0), EndPoint = new Point(1, 1) };

        ThemeButton.Content = dark ? Loc.T("☀  Mode jour", "☀  Day mode", "☀  Modo día") : Loc.T("☾  Mode nuit", "☾  Night mode", "☾  Modo noche");
        if (TabGames != null) ShowTab(_tab, false);
        ApplyTitleBar();
    }

    private void ThemeButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyTheme(!_dark);
        _settings.DarkMode = _dark;
        _settings.Save();
    }

    /// <summary>Par défaut, on suit le thème d'application de Windows.</summary>
    private static bool WindowsPrefersDark()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int v && v == 0;
        }
        catch { return false; }
    }

    /// <summary>Barre de titre sombre en mode nuit (Windows 10 2004+ et Windows 11).</summary>
    private void ApplyTitleBar()
    {
        try
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;
            int v = _dark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, 20, ref v, sizeof(int));
        }
        catch { }
    }

    private void RefreshCumul()
    {
        var t = _svc.GetTotals();
        var fr = Loc.Culture;
        CumulPanel.Visibility = t.Games > 0 ? Visibility.Visible : Visibility.Collapsed;
        Anim.CountUp(CumulChests, t.Chests.ToString("N0", fr), 900);
        double avg = t.Games > 0 ? (double)t.Chests / t.Games : 0;
        string am = t.Ammo.ToString("N0", fr), ot = t.Other.ToString("N0", fr), av = avg.ToString("0.#", fr);
        CumulDetail.Text = Loc.T(
            $"sur {t.Games} partie{(t.Games > 1 ? "s" : "")} suivie{(t.Games > 1 ? "s" : "")}  ·  moyenne {av} par partie  ·  + {am} munitions, {ot} autres",
            $"over {t.Games} tracked game{(t.Games > 1 ? "s" : "")}  ·  average {av} per game  ·  + {am} ammo, {ot} others",
            $"en {t.Games} partida{(t.Games > 1 ? "s" : "")} seguida{(t.Games > 1 ? "s" : "")}  ·  media {av} por partida  ·  + {am} munición, {ot} otros");
    }

    private void HistoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        => ShowSession(HistoryList.SelectedItem as Session);

    // Ces stats sont affichées dans le grand bloc : elles ne sont pas répétées en tuiles.
    private static readonly string[] HeroKeys = { "TotalSearched", "Chests", "AmmoBoxes", "OtherContainers" };

    private void ShowSession(Session s)
    {
        if (s == null)
        {
            StatsList.ItemsSource = null;
            HeroPanel.Visibility = Visibility.Collapsed;
            EmptyText.Visibility = Visibility.Visible;
            SessionTitle.Text = Loc.T("Dernière partie", "Last game", "Última partida");
            DetailsBox.Text = "";
            return;
        }

        EmptyText.Visibility = Visibility.Collapsed;
        HeroPanel.Visibility = Visibility.Visible;
        Anim.CountUp(HeroTotal, s.Get("TotalSearched"), 900);
        Anim.CountUp(HeroChests, s.Get("Chests"));
        Anim.CountUp(HeroAmmo, s.Get("AmmoBoxes"));
        Anim.CountUp(HeroOther, s.Get("OtherContainers"));
        Anim.ResetStagger();
        StatsList.ItemsSource = s.Items.Where(i => !HeroKeys.Contains(i.Key)).ToList();

        bool latest = s == _svc.Sessions.FirstOrDefault();
        string title = (latest ? Loc.T("Dernière partie", "Last game", "Última partida") : Loc.T("Partie", "Game", "Partida")) + "  ·  " + s.DateText
                       + (string.IsNullOrEmpty(s.Mode) ? "" : "  ·  " + s.Mode);
        SessionTitle.Text = title;

        // Barre de répartition coffres / munitions / autres
        int c = s.GetInt("Chests"), a = s.GetInt("AmmoBoxes"), o = s.GetInt("OtherContainers");
        HeroBar.Visibility = c + a + o > 0 ? Visibility.Visible : Visibility.Collapsed;
        HeroBar.ColumnDefinitions[0].Width = new GridLength(c, GridUnitType.Star);
        HeroBar.ColumnDefinitions[1].Width = new GridLength(a, GridUnitType.Star);
        HeroBar.ColumnDefinitions[2].Width = new GridLength(o, GridUnitType.Star);
        if (HeroBar.Visibility == Visibility.Visible) Anim.GrowBar(HeroBar);
        DetailsBox.Text = s.Details;
    }

    // ---------------- Raccourcis globaux ----------------
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ApplyTitleBar();
        var hwnd = new WindowInteropHelper(this).Handle;
        RegisterHotKey(hwnd, HOTKEY_TOGGLE, MOD_CONTROL, VK_F7);
        RegisterHotKey(hwnd, HOTKEY_QUIT, MOD_CONTROL, VK_F8);
        HwndSource.FromHwnd(hwnd).AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            if (id == HOTKEY_TOGGLE && _overlay != null)
                _overlay.Visibility = _overlay.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
            else if (id == HOTKEY_QUIT)
                Application.Current.Shutdown();
            handled = true;
        }
        return IntPtr.Zero;
    }

    protected override void OnClosed(EventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        UnregisterHotKey(hwnd, HOTKEY_TOGGLE);
        UnregisterHotKey(hwnd, HOTKEY_QUIT);
        _svc.Stop();
        _overlay?.Close();
        base.OnClosed(e);
    }
}
