using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace FortniteChestCounter;

/// <summary>Une récompense de la chasse au trésor : image, nom, type, et si elle a été trouvée.</summary>
public class RewardCard : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public string Id { get; set; }
    /// <summary>Chance par coffre, ex. « 1 sur 568  ·  0,176 % par coffre ».</summary>
    public string OddsText { get; set; }
    /// <summary>Chance cumulée de l'avoir eue au moins une fois avec les coffres du joueur.</summary>
    public string ChanceText { get; set; }
    public string Name { get; set; }
    public string Kind { get; set; }
    /// <summary>Nombre de fois vu dans les rediffusions.</summary>
    public int Count { get; set; }

    private bool _manual;
    /// <summary>Coché à la main par l'utilisateur.</summary>
    public bool Manual
    {
        get => _manual;
        set
        {
            _manual = value;
            foreach (var n in new[] { nameof(Obtained), nameof(CardOpacity), nameof(CheckVisibility), nameof(EmptyBoxVisibility), nameof(StateText) })
                Raise(n);
        }
    }

    public bool Obtained => Count > 0 || _manual;
    public double CardOpacity => Obtained ? 1.0 : 0.5;
    public Visibility CheckVisibility => Obtained ? Visibility.Visible : Visibility.Collapsed;
    public Visibility EmptyBoxVisibility => Obtained ? Visibility.Collapsed : Visibility.Visible;
    public string CountText => Count > 1 ? "×" + Count : "";
    public Visibility CountVisibility => Count > 1 ? Visibility.Visible : Visibility.Collapsed;
    public string StateText => Count > 0
        ? Loc.T("Trouvé (rediffusion)", "Found (replay)", "Encontrado (repetición)")
        : _manual ? Loc.T("Coché à la main", "Ticked by hand", "Marcado a mano")
        : Loc.T("Cliquez pour cocher", "Click to tick", "Haz clic para marcar");

    private ImageSource _image;
    public ImageSource Image
    {
        get => _image;
        private set { _image = value; Raise(nameof(Image)); Raise(nameof(PlaceholderVisibility)); }
    }
    public Visibility PlaceholderVisibility => _image == null ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Télécharge / charge l'image sans bloquer l'interface.</summary>
    public async void LoadImage(string url)
    {
        if (string.IsNullOrEmpty(url)) return;
        try { Image = await TreasureImageCache.GetAsync(url); } catch { }
    }

    private void Raise(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

/// <summary>Une semaine de la chasse au trésor et ses récompenses.</summary>
public class WeekGroup
{
    public string Title { get; set; }
    public string Sub { get; set; }
    public bool Current { get; set; }
    public List<RewardCard> Cards { get; set; } = new();
    public Visibility EmptyVisibility => Cards.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
}

/// <summary>Trouvaille sans image (artefact, trésor de type inconnu…).</summary>
public class OtherFind
{
    public string Name { get; set; }
    public string Kind { get; set; }
    public int Count { get; set; }
    public string CountText => "×" + Count;
}
