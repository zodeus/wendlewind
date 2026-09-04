namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

public static class BodyPartColor {
    private static readonly Color DestroyedColor = Color.Red;
    private static readonly Color DisabledColor = new(50, 50, 50);
    private static readonly Color SealSocketColor = new(154, 120, 50);
    private static readonly Color ColdBodyColor = new(0, 0, 200);
    private static readonly Color NormalBodyColor = new(0, 200, 0);
    private static readonly Color HotBodyColor = new(200, 0, 0);
    private static readonly Color FullStomach = new(0, 225, 0);
    private static readonly Color EmptyStomach = new(225, 0, 0);

    public static Color Get(BodyPart bodyPart)
    {
        var tint = PawnPartTint.Get(bodyPart);
        var crisis = bodyPart.Body?.OrganCrisis;
        if (crisis == null || !OrganCrisis.IsInCrisis(bodyPart))
        {
            return tint;
        }

        if (crisis.IsActive(bodyPart.Type))
        {
            var pulse = 0.4f + 0.6f * (0.5f + 0.5f * MathF.Sin(Core.TotalTime * 8f));
            return Color.Lerp(tint, new Color(210, 190, 40), pulse);
        }

        if (crisis.IsPending(bodyPart))
        {
            var pulse = 0.2f + 0.25f * (0.5f + 0.5f * MathF.Sin(Core.TotalTime * 4f));
            return Color.Lerp(tint, new Color(180, 140, 40), pulse);
        }

        return tint;
    }

    public static Color Get(PawnBody body) => PawnPartTint.Get(body);

    public static Color GetBloodColor(float value) => PawnPartTint.GetBloodColor(value);

    public static Color Get(BodyPartSocket socket) {
        if (socket.IsSealed == false) {
            return DestroyedColor;
        }

        if (socket.ParentPart?.HasMobility == false) {
            return DisabledColor;
        }

        return SealSocketColor;
    }

    public static Color GetBodyTemperatureColor(float temperature) {
        float temperatureFloat;
        if (temperature < 31) {
            temperatureFloat = temperature / 20f;
            return Color.Lerp(ColdBodyColor, NormalBodyColor, temperatureFloat);
        }

        if (temperature is >= 31 and <= 33) {
            return NormalBodyColor;
        }

        temperatureFloat = (temperature - 34) / 20f;
        return Color.Lerp(NormalBodyColor, HotBodyColor, temperatureFloat);
    }

    public static Color GetStomachColor(float level) {
        return Color.Lerp(EmptyStomach, FullStomach, level);
    }
}