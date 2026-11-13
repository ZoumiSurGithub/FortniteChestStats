using System.IO;
using System.Net.Http;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkiaSharp;

namespace FortniteChestCounter;

/// <summary>
/// Images des récompenses de la chasse au trésor. Elles ne sont pas fournies avec l'application : elles sont
/// téléchargées une seule fois (comme le fait ChestCounter) depuis le dépôt GitHub de LED-N, ou depuis l'adresse
/// du catalogue à défaut, puis converties en PNG et gardées dans %APPDATA%\FortniteStats\images.
/// </summary>
public static class TreasureImageCache
{
    private static readonly string Folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FortniteStats", "images");

    private static readonly HttpClient Http = CreateClient();
    private static readonly Dictionary<string, Task<ImageSource>> InFlight = new();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("FortniteStats/1.0");
        return c;
    }

    /// <summary>Image prête à afficher, ou null si elle est introuvable (la carte montre alors une icône).</summary>
    public static Task<ImageSource> GetAsync(string url)
    {
        lock (InFlight)
        {
            if (!InFlight.TryGetValue(url, out var task))
                InFlight[url] = task = LoadAsync(url);
            return task;
        }
    }

    /// <summary>Nom du fichier local : nom de l'image, plus sa version si l'adresse en a une (« ?v=3 »).</summary>
    public static string LocalName(string url)
    {
        var uri = new Uri(url);
        var name = Path.GetFileNameWithoutExtension(uri.AbsolutePath);
        var ext = Path.GetExtension(uri.AbsolutePath);
        var query = new string(uri.Query.TrimStart('?').Where(char.IsAsciiLetterOrDigit).ToArray());
        return query.Length > 0 ? $"{name}-{query}{ext}" : name + ext;
    }

    private static async Task<ImageSource> LoadAsync(string url)
    {
        byte[] bytes = null;
        try
        {
            string local = LocalName(url);
            string png = Path.Combine(Folder, Path.ChangeExtension(local, ".png"));

            if (!System.IO.File.Exists(png))
            {
                Directory.CreateDirectory(Folder);
                bytes = await DownloadAsync(local, url);
                if (bytes == null) return null;
                await System.IO.File.WriteAllBytesAsync(png, ToPng(bytes));
            }
            return LoadFile(png);
        }
        catch
        {
            // Dernier recours : décodeur WebP de Windows, si l'extension est installée.
            try { return bytes == null ? null : DecodeWithWpf(bytes); } catch { return null; }
        }
    }

    private static async Task<byte[]> DownloadAsync(string localName, string catalogUrl)
    {
        string repo = global::ChestCounter.AppInfo.Repo;
        var urls = new[]
        {
            $"https://raw.githubusercontent.com/{repo}/main/assets/treasures/{localName}",
            catalogUrl
        };
        foreach (var u in urls)
        {
            try
            {
                var data = await Http.GetByteArrayAsync(u);
                if (data.Length > 0 && data.Length < 4 * 1024 * 1024) return data;
            }
            catch { }
        }
        return null;
    }

    private static byte[] ToPng(byte[] webp)
    {
        using var bmp = SKBitmap.Decode(webp) ?? throw new InvalidDataException("image illisible");
        using var img = SKImage.FromBitmap(bmp);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static ImageSource LoadFile(string path)
    {
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        bmp.UriSource = new Uri(path);
        bmp.EndInit();
        bmp.Freeze();
        return bmp;
    }

    private static ImageSource DecodeWithWpf(byte[] bytes)
    {
        using var ms = new MemoryStream(bytes);
        var frame = BitmapDecoder.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad).Frames[0];
        frame.Freeze();
        return frame;
    }
}
