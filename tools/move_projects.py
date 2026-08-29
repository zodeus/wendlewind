"""Physically split source files into the new projects."""
from __future__ import annotations

import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Wendlewind" / "Source"


def move(src: Path, dest: Path) -> None:
    dest.parent.mkdir(parents=True, exist_ok=True)
    if dest.exists():
        if dest.is_dir():
            shutil.rmtree(dest)
        else:
            dest.unlink()
    shutil.move(str(src), str(dest))
    print(f"{src.relative_to(ROOT)} -> {dest.relative_to(ROOT)}")


def main() -> None:
    common = ROOT / "Wendlewind.Common" / "Source"
    sim = ROOT / "Wendlewind.Simulation" / "Source"
    renderer = ROOT / "Wendlewind.Renderer" / "Source"

    move(SRC / "Log.cs", common / "Log.cs")
    move(SRC / "Maths", common / "Maths")
    move(SRC / "Debug", common / "Debug")
    move(SRC / "Definitions", common / "Definitions")
    move(SRC / "Sim" / "Persistence", common / "Persistence")

    # Utils minus Screen (Screen stays with Client)
    utils_dest = common / "Utils"
    utils_dest.mkdir(parents=True, exist_ok=True)
    for item in (SRC / "Utils").iterdir():
        if item.name == "Screen.cs":
            continue
        move(item, utils_dest / item.name)

    weather = SRC / "Scenes" / "MainGameScene" / "Gui" / "Widgets" / "PawnRenderer" / "Weather" / "WeatherType.cs"
    move(weather, common / "Definitions" / "WeatherType.cs")

    # Simulation: Sim minus presentation leftovers
    sim.mkdir(parents=True, exist_ok=True)
    for item in (SRC / "Sim").iterdir():
        if item.name in {"MainMenuScene.cs", "BaseContent.cs", "Persistence"}:
            continue
        move(item, sim / "Sim" / item.name)

    # Renderer
    move(SRC / "Graphics", renderer / "Graphics")
    move(SRC / "Assets", renderer / "Assets")

    # Client: park leftover Sim presentation next to Scenes
    scenes = SRC / "Scenes"
    if (SRC / "Sim" / "MainMenuScene.cs").exists():
        move(SRC / "Sim" / "MainMenuScene.cs", scenes / "MainMenuScene.cs")
    if (SRC / "Sim" / "BaseContent.cs").exists():
        move(SRC / "Sim" / "BaseContent.cs", SRC / "BaseContent.cs")

    print("Move complete")


if __name__ == "__main__":
    main()
