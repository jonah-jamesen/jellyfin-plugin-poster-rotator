using System;

public class Configuration : BasePluginConfiguration
{
    public string ServerUrl { get; set; } =
        Environment.GetEnvironmentVariable("POSTERROTATOR_SERVER_URL") ?? "http://localhost:8096";

    public string ApiKey { get; set; } =
        Environment.GetEnvironmentVariable("POSTERROTATOR_API_KEY") ?? "";

    public List<string> Libraries { get; set; } = new();
    public int PoolSize { get; set; } = 5;
    public bool SequentialRotation { get; set; } = true;
    public bool SaveNextToMedia { get; set; } = true;
    public bool LockImagesAfterFill { get; set; } = false;
    public bool DryRun { get; set; } = false;
    public List<string> ExtraPosterPatterns { get; set; } = new();
}
