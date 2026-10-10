namespace AdvancedPlanting
{
    /// <summary>Game localization for game tokens such as "$item_carrotseeds" or "$KEY_Attack".</summary>
    internal static class GameText
    {
        public static string Localize(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('$') < 0) return text;
            Localization loc = Localization.instance;
            return loc != null ? loc.Localize(text) : text;
        }
    }
}
