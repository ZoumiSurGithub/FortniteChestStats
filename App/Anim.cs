using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace FortniteChestCounter;

/// <summary>Petites animations réutilisables (apparition, survol, compteur, changement d'onglet).</summary>
public static class Anim
{
    private static readonly IEasingFunction EaseOut = new CubicEase { EasingMode = EasingMode.EaseOut };
    private static int _stagger;

    /// <summary>À appeler quand une nouvelle liste est affichée : les cartes arrivent l'une après l'autre.</summary>
    public static void ResetStagger() => _stagger = 0;

    private static DoubleAnimation D(double from, double to, int ms, int delayMs = 0) => new(from, to, TimeSpan.FromMilliseconds(ms))
    {
        EasingFunction = EaseOut,
        BeginTime = TimeSpan.FromMilliseconds(delayMs)
    };

    private static (ScaleTransform scale, TranslateTransform move) Transforms(FrameworkElement el)
    {
        if (el.RenderTransform is TransformGroup g && g.Children.Count == 2 && g.Children[0] is ScaleTransform s && g.Children[1] is TranslateTransform t)
            return (s, t);
        var scale = new ScaleTransform(1, 1);
        var move = new TranslateTransform();
        el.RenderTransformOrigin = new Point(0.5, 0.5);
        el.RenderTransform = new TransformGroup { Children = { scale, move } };
        return (scale, move);
    }

    /// <summary>Apparition : fondu + léger glissement vers le haut + zoom, avec un décalage progressif.</summary>
    public static void Enter(FrameworkElement el)
    {
        var (scale, move) = Transforms(el);
        int delay = Math.Min(_stagger++, 16) * 45;

        var fade = D(0, 1, 420, delay);
        fade.Completed += (_, _) => { el.BeginAnimation(UIElement.OpacityProperty, null); el.Opacity = 1; };
        el.Opacity = 0;
        el.BeginAnimation(UIElement.OpacityProperty, fade);
        move.BeginAnimation(TranslateTransform.YProperty, D(18, 0, 420, delay));
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, D(0.96, 1, 420, delay));
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, D(0.96, 1, 420, delay));
    }

    /// <summary>Survol : la carte grossit légèrement.</summary>
    public static void Hover(FrameworkElement el, bool on)
    {
        var (scale, _) = Transforms(el);
        double to = on ? 1.035 : 1.0;
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, D(scale.ScaleX, to, 160));
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, D(scale.ScaleY, to, 160));
    }

    /// <summary>Changement d'onglet : le contenu apparaît en glissant.</summary>
    public static void Tab(FrameworkElement el)
    {
        var (_, move) = Transforms(el);
        var fade = D(0, 1, 280);
        fade.Completed += (_, _) => { el.BeginAnimation(UIElement.OpacityProperty, null); el.Opacity = 1; };
        el.BeginAnimation(UIElement.OpacityProperty, fade);
        move.BeginAnimation(TranslateTransform.YProperty, D(14, 0, 300));
    }

    /// <summary>Fondu d'ouverture de la fenêtre.</summary>
    public static void FadeIn(UIElement el, int ms = 450)
    {
        var a = D(0, 1, ms);
        a.Completed += (_, _) => { el.BeginAnimation(UIElement.OpacityProperty, null); el.Opacity = 1; };
        el.BeginAnimation(UIElement.OpacityProperty, a);
    }

    /// <summary>La barre se déploie de la gauche vers la droite.</summary>
    public static void GrowBar(FrameworkElement bar)
    {
        bar.RenderTransformOrigin = new Point(0, 0.5);
        var sc = new ScaleTransform(0, 1);
        bar.RenderTransform = sc;
        sc.BeginAnimation(ScaleTransform.ScaleXProperty, D(0, 1, 800, 150));
    }

    private static readonly Dictionary<System.Windows.Controls.TextBlock, DispatcherTimer> Counters = new();

    /// <summary>Compteur qui monte jusqu'à la valeur (texte final affiché tel quel).</summary>
    public static void CountUp(System.Windows.Controls.TextBlock tb, string finalText, int ms = 800)
    {
        if (Counters.TryGetValue(tb, out var old)) old.Stop();
        string digits = new string((finalText ?? "").Where(char.IsDigit).ToArray());
        if (!long.TryParse(digits, out long target) || target <= 0 || target > 1_000_000_000 || (finalText ?? "").Any(c => char.IsLetter(c) || c == '#'))
        {
            tb.Text = finalText;
            return;
        }
        var fr = Loc.Culture;
        bool grouped = finalText.Any(c => c == ' ' || c == ' ' || c == ' ');
        var start = DateTime.UtcNow;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        timer.Tick += (_, _) =>
        {
            double t = Math.Min(1, (DateTime.UtcNow - start).TotalMilliseconds / ms);
            double eased = 1 - Math.Pow(1 - t, 3);
            if (t >= 1) { timer.Stop(); tb.Text = finalText; return; }
            long v = (long)Math.Round(target * eased);
            tb.Text = grouped ? v.ToString("N0", fr) : v.ToString();
        };
        Counters[tb] = timer;
        timer.Start();
    }
}
