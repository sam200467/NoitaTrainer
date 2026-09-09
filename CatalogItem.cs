namespace NoitaTrainer;

internal sealed class CatalogItem
{
    public string Id { get; set; } = "";
    public string Zh { get; set; } = "";
    public string En { get; set; } = "";
    public string Description { get; set; } = "";
    public string? Path { get; set; }
    public int IconIndex { get; set; } = -1;

    public string DisplayName
    {
        get
        {
            var name = !string.IsNullOrWhiteSpace(Zh) ? Zh : En;
            if (string.IsNullOrWhiteSpace(name))
                name = Id;
            return $"{name}  [{Id}]";
        }
    }

    public override string ToString() => DisplayName;
}

internal sealed class CatalogRoot
{
    public List<CatalogItem> Spells { get; set; } = [];
    public List<CatalogItem> Perks { get; set; } = [];
    public List<CatalogItem> Events { get; set; } = [];
    public List<CatalogItem> Wands { get; set; } = [];
    public List<MaterialItem> Materials { get; set; } = [];
    public List<CatalogItem> Items { get; set; } = [];
}

internal sealed class MaterialItem
{
    public string Id { get; set; } = "";
    public string Zh { get; set; } = "";
    public string En { get; set; } = "";
    public string Category { get; set; } = ""; // spark / liquid / powder / solid / gas / box2d
    public string Color { get; set; } = "";    // RRGGBB
    public int PreviewIndex { get; set; } = -1;

    public string DisplayName
    {
        get
        {
            var name = !string.IsNullOrWhiteSpace(Zh) ? Zh :
                (!string.IsNullOrWhiteSpace(En) ? En : Id);
            return $"{name}  [{Id}]";
        }
    }

    public System.Drawing.Color SwatchColor
    {
        get
        {
            var hex = Color;
            try
            {
                if (hex.Length >= 6)
                {
                    var r = Convert.ToInt32(hex.Substring(0, 2), 16);
                    var g = Convert.ToInt32(hex.Substring(2, 2), 16);
                    var b = Convert.ToInt32(hex.Substring(4, 2), 16);
                    return System.Drawing.Color.FromArgb(r, g, b);
                }
            }
            catch
            {
                // fall through to gray
            }
            return System.Drawing.Color.Gray;
        }
    }
}
