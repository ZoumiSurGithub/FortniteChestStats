using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FortniteChestCounter;

/// <summary>
/// Icônes de l'application. Chaque icône est dessinée ici en vectoriel (dessins originaux, style doux).
/// Pour utiliser vos propres images : déposez un fichier PNG nommé comme la clé (ex. Chests.png) dans le
/// dossier « icones » à côté de FortniteStats.exe : il remplace l'icône intégrée.
/// Clés : TotalSearched, Chests, AmmoBoxes, OtherContainers, Position, Eliminations, Duration, TeamChests, Treasures, VehicleMeters.
/// </summary>
public static class Icons
{
    private static readonly Dictionary<string, ImageSource> Cache = new();

    public static readonly string UserFolder = Path.Combine(
        Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory, "icones");

    public static ImageSource Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        if (Cache.TryGetValue(key, out var cached)) return cached;
        ImageSource img = LoadUser(key) ?? BuiltIn(key);
        if (img != null) Cache[key] = img;
        return img;
    }

    private static ImageSource LoadUser(string key)
    {
        try
        {
            foreach (var ext in new[] { ".png", ".jpg", ".jpeg", ".ico" })
            {
                var path = Path.Combine(UserFolder, key + ext);
                if (!System.IO.File.Exists(path)) continue;
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(path);
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
        }
        catch { }
        return null;
    }

    private static DrawingImage Draw(params (string data, string color)[] parts)
    {
        var group = new DrawingGroup();
        // Cadre transparent 24x24 : toutes les icônes ont la même taille.
        group.Children.Add(new GeometryDrawing(Brushes.Transparent, null, Geometry.Parse("M0,0 H24 V24 H0 Z")));
        var conv = new BrushConverter();
        foreach (var (data, color) in parts)
            group.Children.Add(new GeometryDrawing((Brush)conv.ConvertFromString(color), null, Geometry.Parse(data)));
        var img = new DrawingImage(group);
        img.Freeze();
        return img;
    }

    private static ImageSource BuiltIn(string key) => key switch
    {
        // Étoile
        "TotalSearched" => Draw(
            ("M12,2.5 L14.6,9.2 L21.8,9.8 L16.3,14.5 L18,21.5 L12,17.7 L6,21.5 L7.7,14.5 L2.2,9.8 L9.4,9.2 Z", "#F2B84B")),

        // Coffre
        "Chests" => Draw(
            ("M3,11 V8.5 A5.5,5.5 0 0 1 8.5,3 H15.5 A5.5,5.5 0 0 1 21,8.5 V11 Z", "#F6C453"),
            ("M3,12.5 H21 V19 A2,2 0 0 1 19,21 H5 A2,2 0 0 1 3,19 Z", "#E8A33A"),
            ("M3,11 H21 V12.5 H3 Z", "#B9741F"),
            ("M10,10 H14 V15.2 A2,2 0 0 1 10,15.2 Z", "#FFF1C2")),

        // Caisse de munitions
        "AmmoBoxes" => Draw(
            ("M4,8 H20 V19 A1.5,1.5 0 0 1 18.5,20.5 H5.5 A1.5,1.5 0 0 1 4,19 Z", "#7DB9F5"),
            ("M3,6.5 A1.5,1.5 0 0 1 4.5,5 H19.5 A1.5,1.5 0 0 1 21,6.5 V8.5 H3 Z", "#5A9BE0"),
            ("M9,5 V3.8 A1,1 0 0 1 10,2.8 H14 A1,1 0 0 1 15,3.8 V5 H13.5 V4.3 H10.5 V5 Z", "#3E78B8"),
            ("M7.2,12 H9.2 V17.5 H7.2 Z", "#EAF4FF"),
            ("M11,12 H13 V17.5 H11 Z", "#EAF4FF"),
            ("M14.8,12 H16.8 V17.5 H14.8 Z", "#EAF4FF")),

        // Cube (autres conteneurs)
        "OtherContainers" => Draw(
            ("M12,2.5 L20.5,7 V17 L12,21.5 L3.5,17 V7 Z", "#9B7BFF"),
            ("M12,2.5 L20.5,7 L12,11.5 L3.5,7 Z", "#C9B6FF"),
            ("M12,11.5 L20.5,7 V17 L12,21.5 Z", "#7F5FE6")),

        // Trophée
        "Position" => Draw(
            ("M7,3.5 H17 V9.5 A5,5 0 0 1 7,9.5 Z", "#9B7BFF"),
            ("M7,5.5 H4 V8 A3.5,3.5 0 0 0 7.4,11.4 L7.1,9.9 A2,2 0 0 1 5.5,8 V7 H7 Z", "#C9B6FF"),
            ("M17,5.5 H20 V8 A3.5,3.5 0 0 1 16.6,11.4 L16.9,9.9 A2,2 0 0 0 18.5,8 V7 H17 Z", "#C9B6FF"),
            ("M11,14.5 H13 V17.5 H11 Z", "#9B7BFF"),
            ("M8,18 H16 A1,1 0 0 1 17,19 V20.5 H7 V19 A1,1 0 0 1 8,18 Z", "#7F5FE6")),

        // Viseur
        "Eliminations" => Draw(
            ("F0 M12,4 A8,8 0 1 0 12,20 A8,8 0 1 0 12,4 Z M12,6.2 A5.8,5.8 0 1 1 12,17.8 A5.8,5.8 0 1 1 12,6.2 Z", "#FF7A90"),
            ("M11,1.5 H13 V6 H11 Z", "#FF7A90"),
            ("M11,18 H13 V22.5 H11 Z", "#FF7A90"),
            ("M1.5,11 H6 V13 H1.5 Z", "#FF7A90"),
            ("M18,11 H22.5 V13 H18 Z", "#FF7A90"),
            ("M12,10.2 A1.8,1.8 0 1 1 12,13.8 A1.8,1.8 0 1 1 12,10.2 Z", "#FF7A90")),

        // Horloge
        "Duration" => Draw(
            ("F0 M12,2.5 A9.5,9.5 0 1 0 12,21.5 A9.5,9.5 0 1 0 12,2.5 Z M12,4.7 A7.3,7.3 0 1 1 12,19.3 A7.3,7.3 0 1 1 12,4.7 Z", "#5AA9F0"),
            ("M11,7 H13 V12.2 L16.2,14 L15.2,15.6 L11,13.2 Z", "#5AA9F0")),

        // Équipe
        "TeamChests" => Draw(
            ("M9,5 A3,3 0 1 1 9,11 A3,3 0 1 1 9,5 Z", "#6CCB8B"),
            ("M3,19.5 A6,5.5 0 0 1 15,19.5 V20.5 H3 Z", "#6CCB8B"),
            ("M17,7 A2.5,2.5 0 1 1 17,12 A2.5,2.5 0 1 1 17,7 Z", "#A5E3BA"),
            ("M16.5,14.2 A5,4.4 0 0 1 22,18.5 V19.5 H17.5 Z", "#A5E3BA")),

        // Gemme
        "Treasures" => Draw(
            ("M7,3.5 H17 L21.5,9 L12,21 L2.5,9 Z", "#FF9EC4"),
            ("M7,3.5 H17 L21.5,9 H2.5 Z", "#FFC4DC")),

        // Voiture
        "VehicleMeters" => Draw(
            ("M2.5,15.5 L5,9.8 A2,2 0 0 1 6.9,8.5 H17.1 A2,2 0 0 1 19,9.8 L21.5,15.5 V18.5 H2.5 Z", "#5AA9F0"),
            ("M7.2,10.2 H16.8 L18.2,13.5 H5.8 Z", "#E6F2FF"),
            ("M7.5,16.5 A2.2,2.2 0 1 1 7.5,20.9 A2.2,2.2 0 1 1 7.5,16.5 Z", "#3E78B8"),
            ("M16.5,16.5 A2.2,2.2 0 1 1 16.5,20.9 A2.2,2.2 0 1 1 16.5,16.5 Z", "#3E78B8")),

        _ => null
    };
}

/// <summary>Convertisseur XAML : clé de stat → icône.</summary>
public class IconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => Icons.Get(value as string ?? parameter as string);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
