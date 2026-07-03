using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Numerics;
using Dalamud.Game.DutyState;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using SamplePlugin.Windows;
using Dalamud.Bindings.ImGui;


namespace SamplePlugin;

public sealed class Plugin : IDalamudPlugin
{
    
    // dependency injection
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IDutyState DutyState { get; private set; } = null!;
    [PluginService] public static IChatGui ChatGui { get; private set; } = null!;
    [PluginService] public static INotificationManager NotificationManager { get; private set; } = null!;
    
    private const string CommandName = "/pmycommand";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("SamplePlugin");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    private bool showPopup;
    private bool leaveDuty {get; set; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // You might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this, goatImagePath);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });
        
        // Tell the UI system that we want our windows to be drawn through the window system
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.Draw += DrawPopup;

        // This adds a button to the plugin ins
        // xtaller entry of this plugin which allows
        // toggling the display status of the configuration ui
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;

        // Adds another button doing the same but for the main ui of the plugin
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        // Add a simple message to the log with level set to information
        // Use /xllog to open the log window in-game
        // Example Output: 00:57:54.959 | INF | [SamplePlugin] ===A cool log message from Sample Plugin===
        Log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");
        
        DutyState.DutyCompleted += DutyStateOnDutyCompleted;
    }

    private void DutyStateOnDutyCompleted(IDutyStateEventArgs args)
    {
        if (!PlayerState.IsLoaded) return; 
        
        Log.Info($"======Duty completed======");
        Log.Info($"{args}");
        
        ChatGui.Print($"A duty has been completed!");
        leaveDuty = false;
        showPopup = true;
    }
    
    

    public void Dispose()
    {
        // Unregister all actions to not leak anything during disposal of plugin
        DutyState.DutyCompleted -= DutyStateOnDutyCompleted;
        
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        PluginInterface.UiBuilder.Draw += DrawPopup;
        
        
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        // In response to the slash command, toggle the display status of our main ui
        MainWindow.Toggle();
    }
    
    private void DrawPopup()
    {
        if (!showPopup) return;
        ImGui.SetNextWindowSize(new Vector2(300, 120), ImGuiCond.Always);
        ImGui.SetNextWindowPos(
            ImGui.GetMainViewport().GetCenter(),
            ImGuiCond.Always,
            new Vector2(0.5f, 0.5f) // pivot: center
        );

        if (ImGui.Begin("Alert##myplugin", ref showPopup,
                        ImGuiWindowFlags.NoCollapse))
        {
            ImGui.TextWrapped("Would you like to leave?");
            ImGui.Spacing();

            if (ImGui.Button("Yes", new Vector2(-1, 0)))
            {
                Log.Information("User has chosen to leave the duty.");
                showPopup = false;
                leaveDuty = true;
            }
            if (ImGui.Button("No", new Vector2(-1, 0)))
            {
                showPopup = false;
                leaveDuty = false;
                Log.Information("User has chosen NOT to leave the duty.");
            }
        }
        ImGui.End();
    }

    private void LeaveDuty()
    {
        
    }
    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}

