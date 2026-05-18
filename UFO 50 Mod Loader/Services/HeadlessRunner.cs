using UFO_50_Mod_Loader.Models;
using System.Text.Json;

namespace UFO_50_Mod_Loader.Services;

public static class HeadlessRunner
{
    public record Options(
        List<string> ModPaths,
        bool Launch,
        string? GamePathOverride,
        bool IgnoreWarnings,
        bool JsonOutput);

    public static async Task<int> RunAsync(Options opt)
    {
        var r = new Reporter(opt.JsonOutput);
        r.Emit("start", new { mods = opt.ModPaths.Count, launch = opt.Launch });
        var sw = System.Diagnostics.Stopwatch.StartNew();

        SettingsService.Load();
        if (opt.GamePathOverride is not null)
            SettingsService.Settings.GamePath = opt.GamePathOverride;

        var gameService = new InstalledGameService(parentWindow: null);
        if (!gameService.IsValidGamePath(SettingsService.Settings.GamePath))
            return r.Fail(11, "invalid game path", SettingsService.Settings.GamePath);

        if (SettingsService.Settings.CopiedVanillaVersion is null)
            return r.Fail(12, "vanilla copy not seeded; run the GUI once");

        r.Emit("validate", new { gamePath = "ok", vanillaCopy = "ok" });

        var conflicts = ModConflictService.CheckConflicts(opt.ModPaths);
        r.Emit("conflicts", new {
            blocking = conflicts.HasBlockingConflicts ? conflicts.Conflicts.Count : 0,
            warnings = conflicts.HasPatchWarnings ? conflicts.Conflicts.Count : 0,
        });
        if (conflicts.HasBlockingConflicts)
            return r.Fail(20, "blocking conflicts", conflicts.GetMessage());
        if (conflicts.HasPatchWarnings && !opt.IgnoreWarnings)
            return r.Fail(20, "patch warnings (use --ignore-warnings)", conflicts.GetMessage());

        r.Emit("install", new { step = "begin" });
        GMLoader.GMLoaderResult? result = await InstallService
            .InstallModsAsync(parent: null, opt.ModPaths, gameService);
        if (result is null || !result.Success)
            return r.Fail(30, "install failed", result?.ErrorMessage ?? "see log.txt");

        r.Emit("installed", new { ok = true, ms = sw.ElapsedMilliseconds });

        if (opt.Launch) {
            try {
                await LaunchGameService.LaunchGameAsync();
                r.Emit("launched", new { });
            }
            catch (Exception ex) {
                return r.Fail(40, "launch failed", ex.Message);
            }
        }

        r.Emit("done", new { exitCode = 0, totalMs = sw.ElapsedMilliseconds });
        return 0;
    }
}

internal class Reporter
{
    private readonly bool _json;
    public Reporter(bool json) => _json = json;

    public void Emit(string evt, object payload)
    {
        if (_json) {
            var dict = new Dictionary<string, object> { ["event"] = evt };
            foreach (var prop in payload.GetType().GetProperties())
                dict[prop.Name] = prop.GetValue(payload)!;
            Console.WriteLine(JsonSerializer.Serialize(dict));
        }
        else {
            Console.WriteLine($"[{evt}] {payload}");
        }
    }

    public int Fail(int code, string msg, string? detail = null)
    {
        Console.Error.WriteLine($"[error code={code}] {msg}{(detail is null ? "" : ": " + detail)}");
        return code;
    }
}

public static class CliParser
{
    public static readonly string Usage =
        "Usage: \"UFO 50 Mod Loader.exe\" --headless [--version] --mod <path> [--mod <path> ...] [--launch] [--game-path <path>] [--ignore-warnings] [--json]";

    public static HeadlessRunner.Options? Parse(string[] args)
    {
        var mods = new List<string>();
        bool launch = false;
        string? gamePath = null;
        bool ignoreWarnings = false;
        bool json = false;

        for (int i = 0; i < args.Length; i++) {
            switch (args[i]) {
                case "--headless": break;
                case "--version":
                    var ver = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                    Console.WriteLine($"{ver}  headless ✓");
                    Environment.Exit(0);
                    break;
                case "--mod":
                    if (i + 1 >= args.Length) return null;
                    mods.Add(args[++i]);
                    break;
                case "--launch": case "-L":
                    launch = true;
                    break;
                case "--game-path":
                    if (i + 1 >= args.Length) return null;
                    gamePath = args[++i];
                    break;
                case "--ignore-warnings":
                    ignoreWarnings = true;
                    break;
                case "--json":
                    json = true;
                    break;
                default:
                    return null;
            }
        }

        if (mods.Count == 0) return null;
        return new HeadlessRunner.Options(mods, launch, gamePath, ignoreWarnings, json);
    }
}
