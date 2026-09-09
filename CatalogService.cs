using System.Reflection;
using System.Text.Json;

namespace NoitaTrainer;

internal static class CatalogService
{
    public const int IconCellSize = 32;
    public const int IconAtlasColumns = 24;
    public const int MaterialPreviewCellSize = 64;
    public const int MaterialPreviewAtlasColumns = 16;

    public static CatalogRoot Load()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .First(name => name.EndsWith("Assets.catalog.json", StringComparison.OrdinalIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("内置目录资源不存在。");
        var catalog = JsonSerializer.Deserialize<CatalogRoot>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("无法读取内置目录。");

        for (var index = 0; index < catalog.Materials.Count; index++)
            catalog.Materials[index].PreviewIndex = index;
        for (var index = 0; index < catalog.Items.Count; index++)
            catalog.Items[index].IconIndex = index;
        return catalog;
    }

    public static Bitmap LoadItemIconAtlas()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .First(name => name.EndsWith("Assets.item-icon-atlas.png", StringComparison.OrdinalIgnoreCase));
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("内置物品图标图集不存在。");
        using var source = new Bitmap(stream);
        return new Bitmap(source);
    }

    public static Bitmap LoadIconAtlas()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .First(name => name.EndsWith("Assets.icon-atlas.png", StringComparison.OrdinalIgnoreCase));
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("内置图标图集不存在。");
        using var source = new Bitmap(stream);
        return new Bitmap(source);
    }

    public static Bitmap LoadMaterialPreviewAtlas()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .First(name => name.EndsWith("Assets.material-preview-atlas.png", StringComparison.OrdinalIgnoreCase));
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("内置材质预览图集不存在。");
        using var source = new Bitmap(stream);
        return new Bitmap(source);
    }
}
