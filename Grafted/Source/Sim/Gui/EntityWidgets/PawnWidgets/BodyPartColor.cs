using Grafted.Sim.Entities.Pawns;
using Microsoft.Xna.Framework;

namespace Grafted.Sim.Gui.EntityWidgets.PawnWidgets;

public static class BodyPartColor {
    private static readonly Color DestroyedColor = Color.Red;
    private static readonly Color DisabledColor = new(50, 50, 50);
    private static readonly Color LowColor = new(170, 0, 0);
    private static readonly Color HighColor = new(65, 120, 64);
    private static readonly Color SealSocketColor = new(77, 120, 50);

    public static Color Get(BodyPart bodyPart) {
        if (bodyPart.IsDestroyed) {
            return DestroyedColor;
        }

        if (bodyPart.HasMobility == false || bodyPart.IsFunctional == false) {
            return DisabledColor;
        }

        return Color.Lerp(LowColor, HighColor, bodyPart.HealthPercent);
    }

    public static Color Get(BodyPartSocket socket) {
        if (socket.IsSealed == false) {
            return DestroyedColor;
        }

        if (socket.ParentPart?.HasMobility == false) {
            return DisabledColor;
        }

        return SealSocketColor;
    }
}