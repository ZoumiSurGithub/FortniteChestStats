using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FortniteChestCounter;

public partial class OverlayWindow : Window
{
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr h, int i);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr h, int i, int v);

    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x20;      // les clics passent à travers
    private const int WS_EX_TOOLWINDOW = 0x80;       // pas dans Alt+Tab
    private const int WS_EX_NOACTIVATE = 0x08000000; // ne prend jamais le focus

    public OverlayWindow()
    {
        InitializeComponent();
        IcoTotal.Source = Icons.Get("TotalSearched");
        IcoChests.Source = Icons.Get("Chests");
        IcoAmmo.Source = Icons.Get("AmmoBoxes");
        IcoOther.Source = Icons.Get("OtherContainers");
        IcoPlace.Source = Icons.Get("Position");
        IcoElim.Source = Icons.Get("Eliminations");
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, ex | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
    }

    public void Update(Session s)
    {
        if (s == null) return;

        TotalText.Text = s.Get("TotalSearched");
        ChestsText.Text = s.Get("Chests");
        AmmoText.Text = s.Get("AmmoBoxes");
        OtherText.Text = s.Get("OtherContainers");

        // Classement / éliminations : masqués si la rediffusion ne les contient pas.
        var pos = s.Items.FirstOrDefault(i => i.Key == "Position")?.Value;
        var elim = s.Items.FirstOrDefault(i => i.Key == "Eliminations")?.Value;
        PlaceText.Text = pos ?? "";
        PlaceRow.Visibility = pos != null ? Visibility.Visible : Visibility.Collapsed;
        ElimText.Text = elim ?? "";
        ElimRow.Visibility = elim != null ? Visibility.Visible : Visibility.Collapsed;

        TitleText.Text = Loc.T("Dernière partie", "Last game", "Última partida") + "  ·  " + s.Date.ToString("HH:mm");
        EmptyText.Visibility = Visibility.Collapsed;
        DataPanel.Visibility = Visibility.Visible;
    }

}
