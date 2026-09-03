namespace Wendlemire.NetCode;

public enum BuildStage
{
    Signature,
    Early,
    Mid,
    Late,
    End
}

public static class BuildStages
{
    public static readonly BuildStage[] Generated =
    [
        BuildStage.Early,
        BuildStage.Mid,
        BuildStage.Late,
        BuildStage.End
    ];

    public static int TargetRound(this BuildStage stage) => stage switch
    {
        BuildStage.Early => 2,
        BuildStage.Mid => 5,
        BuildStage.Late => 8,
        BuildStage.End => 12,
        _ => 1
    };

    public static string Label(this BuildStage stage) => stage switch
    {
        BuildStage.Signature => "Signature",
        BuildStage.Early => "Early",
        BuildStage.Mid => "Mid",
        BuildStage.Late => "Late",
        BuildStage.End => "End",
        _ => stage.ToString()
    };
}
