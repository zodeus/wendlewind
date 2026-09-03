using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;

namespace Wendlemire.Scenes.MainGameScene;

public static class TestSimSettings
{
    public static string AttackerBuildId { get; set; } = "IroncladWarden";
    public static string DefenderBuildId { get; set; } = "WitchDoctorSage";
    public static int Seed { get; set; } = 384710648;
    public static int CatalogSeed { get; set; } = 384710648;

    /// <summary>
    /// When the player pawn is hand-configured on the Test Sim screen, its loadout/potion triggers are captured here
    /// so they survive the world re-initialization that happens when the encounter starts. Cleared whenever the
    /// attacker build template is changed.
    /// </summary>
    public static BuildSnapshot? AttackerOverride { get; set; }
}
