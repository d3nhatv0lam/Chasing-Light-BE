namespace ChasingLight.Api.Infrastructures.Identity;

public class SupabaseOptions
{
    public const string SectionName = "Supabase";

    public string Url { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
}
