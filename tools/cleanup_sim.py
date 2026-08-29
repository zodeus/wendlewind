"""Second-pass cleanup after decouple_sim.py."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SIM = ROOT / "Wendlewind" / "Source" / "Sim"


def write(path: Path, text: str) -> None:
    path.write_text(text.replace("\r\n", "\n"), encoding="utf-8")


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def truncate_after_apply_to_part(text: str) -> str:
    """Keep ApplyToPart and drop leftover UI helpers."""
    m = re.search(r"public override bool ApplyToPart\(Item item, BodyPart part\)", text)
    if not m:
        return text
    brace = text.find("{", m.end())
    depth = 0
    i = brace
    while i < len(text):
        if text[i] == "{":
            depth += 1
        elif text[i] == "}":
            depth -= 1
            if depth == 0:
                # drop unused color constants if they sit above ApplyToPart
                head = text[: m.start()]
                head = re.sub(
                    r"\n    private static readonly Color \w+ = new\([^)]+\);[^\n]*",
                    "",
                    head,
                )
                return head + text[m.start() : i + 1] + "\n}\n"
        i += 1
    return text


def main() -> None:
    # Modifier extra paren
    for path in (SIM / "Entities" / "Pawns" / "Modifiers").glob("*.cs"):
        text = read(path)
        original = text
        text = text.replace("return new InfoPanelData { Lines = lines });", "return new InfoPanelData { Lines = lines };")
        text = re.sub(r"(public override InfoPanelData GetInfoData\(\) => new InfoPanelData\n    \{[\s\S]*?\n    )\}\);", r"\1};", text)
        text = re.sub(r"(return new InfoPanelData\n        \{[\s\S]*?\n        )\}\);", r"\1};", text)
        if text != original:
            write(path, text)

    # Truncate medicinal leftover UI
    for path in (SIM / "Entities" / "Items" / "Medicinals").glob("*Handler.cs"):
        text = read(path)
        if "Widget" in text or "CreateInfoRow" in text or "CreatePartBox" in text:
            write(path, truncate_after_apply_to_part(text))

    # Trinket leftover widget fields
    for name, patterns in {
        "PerforationTrapHandler.cs": [
            r"    private Label _cooldownLabel = null!;\n",
            r"    private Label _statusLabel = null!;\n",
            r"    private HorizontalProgressBar _fuseProgressBar = null!;\n",
        ],
        "DeathRattleHandler.cs": [
            r"    private Label _cooldownLabel = null!;\n",
            r"    private Label _chargesLabel = null!;\n",
            r"    private HorizontalProgressBar _hitProgressBar = null!;\n",
        ],
        "BoneCrackerHandler.cs": [r"    private Label _cooldownLabel = null!;\n"],
        "BloodyBellHandler.cs": [
            r"    private Label _cooldownLabel = null!;\n",
            r"    private Label _ringsLabel = null!;\n",
        ],
        "GoldenGooseHandler.cs": [
            r"    private static readonly Color HungerFullColor = Color.GreenYellow;   // Golden\n",
            r"    private static readonly Color HungerEmptyColor = Color.SandyBrown;   // Brown\n    \n",
            r"    private HorizontalProgressBar _hungerBar = null!;\n\n",
        ],
    }.items():
        path = SIM / "Entities" / "Items" / "Trinkets" / name
        text = read(path)
        for pat in patterns:
            text = re.sub(pat, "", text)
        write(path, text)

    # ZoneDef
    write(
        SIM / "Zones" / "ZoneDef.cs",
        """namespace Wendlewind.Sim.Zones;

public class ZoneDef : Def {
    public int Stage;
    public Color ZoneColor = new(150, 150, 150);

    public List<BiomeResourceRecord> Resources = new();
    public List<EncounterProperties> Encounters = new();
    public List<WeatherDef> Weathers = new();
}
""",
    )

    # BodyDef
    write(
        SIM / "Entities" / "Pawns" / "BodyDef.cs",
        """using Wendlewind.Sim.Entities.Pawns.Bodies.Handlers;

namespace Wendlewind.Sim.Entities.Pawns;

public class BodyDef : Def {
    public BloodDef? BloodType;
    public float MaxBlood = 0;
    public float MaxEnergy = 100;
    public float BoneDensity = 1;
    public Type GeneratorClass = typeof(IBodyGenerator);
    public Type HandlerClass = typeof(DefaultBodyHandler);
    public Type? LayoutClass;

    private IBodyGenerator? _generator;

    public DefaultBodyHandler Handler => (DefaultBodyHandler) Activator.CreateInstance(HandlerClass)!;
    public IBodyGenerator Generator => _generator ??= (IBodyGenerator) Activator.CreateInstance(GeneratorClass)!;
}
""",
    )

    # Pawn.Icon
    pawn = read(SIM / "Entities" / "Pawns" / "Pawn.cs")
    pawn = pawn.replace("    public override Texture2D Icon => PawnDef.Icon;\n", "")
    write(SIM / "Entities" / "Pawns" / "Pawn.cs", pawn)

    # BodyPart.Image
    bp = read(SIM / "Entities" / "Pawns" / "BodyPart.cs")
    bp = bp.replace("    public Texture2D? Image => Icon;\n\n\n", "\n")
    write(SIM / "Entities" / "Pawns" / "BodyPart.cs", bp)

    # CombatHandler leftover Font
    ch = read(SIM / "Combat" / "CombatHandler.cs")
    ch = ch.replace("                Font = BaseContent.Fonts.Default.Medium,\n", "")
    write(SIM / "Combat" / "CombatHandler.cs", ch)

    print("Cleanup complete")


if __name__ == "__main__":
    main()
