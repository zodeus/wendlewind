"""Copy each root food sprite and paint visible steam / simmer overlays."""
from __future__ import annotations

from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

ORGANIC = Path(__file__).resolve().parent
STEAM = (255, 250, 240)


def paint_wisp(
    overlay: Image.Image,
    cx: float,
    cy: float,
    rx: float,
    ry: float,
    peak: int,
    color: tuple[int, int, int] = STEAM,
) -> None:
    pix = overlay.load()
    w, h = overlay.size
    x0, y0 = int(cx - rx - 1), int(cy - ry - 1)
    x1, y1 = int(cx + rx + 1), int(cy + ry + 1)
    for y in range(max(0, y0), min(h, y1 + 1)):
        for x in range(max(0, x0), min(w, x1 + 1)):
            nx = (x - cx) / max(rx, 0.5)
            ny = (y - cy) / max(ry, 0.5)
            d = nx * nx + ny * ny
            if d >= 1:
                continue
            # Keep a brighter core so blur later does not erase the wisp.
            falloff = (1 - d) ** 1.15
            core = 1.0 if d < 0.18 else 0.0
            alpha = int(peak * (falloff * 0.72 + core * 0.28))
            if alpha <= 0:
                continue
            r, g, b, a = pix[x, y]
            na = min(255, a + alpha)
            t = alpha / 255
            pix[x, y] = (
                min(255, int(r * (1 - t) + color[0] * t)),
                min(255, int(g * (1 - t) + color[1] * t)),
                min(255, int(b * (1 - t) + color[2] * t)),
                na,
            )


def steam_column(
    overlay: Image.Image,
    x: float,
    y: float,
    height: float,
    width: float,
    peak: int,
    sway: float = 0,
) -> None:
    steps = max(4, int(height / 8))
    for i in range(steps):
        t = i / (steps - 1)
        paint_wisp(
            overlay,
            x + sway * t * t,
            y - height * t,
            width * (0.85 + 0.85 * t),
            height / steps * 2.1,
            int(peak * (1 - t * 0.35)),
        )


def finish_overlay(overlay: Image.Image) -> Image.Image:
    soft = overlay.filter(ImageFilter.GaussianBlur(radius=0.7))
    # Blur thins alpha; push it back up and keep the color near-white.
    boosted = ImageEnhance.Brightness(soft).enhance(1.15)
    pix = boosted.load()
    w, h = boosted.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = pix[x, y]
            if a == 0:
                continue
            na = min(255, int(a * 1.85 + 18))
            pix[x, y] = (min(255, r + 20), min(255, g + 18), min(255, b + 12), na)
    return boosted


def compose(base: Image.Image, overlay: Image.Image) -> Image.Image:
    return Image.alpha_composite(base.convert("RGBA"), finish_overlay(overlay))


def simmer(base: Image.Image, bubbles: list[tuple[float, float, float, int]]) -> Image.Image:
    out = base.copy().convert("RGBA")
    draw = ImageDraw.Draw(out)
    for cx, cy, r, lift in bubbles:
        # Small glossy pops on the broth, not outlined rings.
        hx, hy = cx - r * 0.25, cy - r * 0.35
        draw.ellipse((hx - r * 0.55, hy - r * 0.35, hx + r * 0.55, hy + r * 0.35), fill=(255, 236, 200, lift))
        draw.ellipse((cx - r * 0.7, cy - r * 0.7, cx + r * 0.7, cy + r * 0.7), fill=(255, 210, 150, int(lift * 0.45)))
    return out


def save(img: Image.Image, name: str) -> None:
    dest = ORGANIC / name
    img.convert("RGBA").save(dest, "PNG")
    print(f"wrote {dest.name} {img.size}")


def frames_for(name: str, builder) -> None:
    root = Image.open(ORGANIC / f"{name}.png").convert("RGBA")
    for i, img in enumerate(builder(root), start=1):
        save(img, f"{name}_{i}.png")


def fish(root: Image.Image):
    w, h = root.size
    o1 = Image.new("RGBA", root.size, (0, 0, 0, 0))
    steam_column(o1, w * 0.56, h * 0.22, h * 0.40, w * 0.10, 255, sway=-8)
    steam_column(o1, w * 0.68, h * 0.18, h * 0.32, w * 0.08, 255, sway=10)
    yield compose(root, o1)

    o2 = Image.new("RGBA", root.size, (0, 0, 0, 0))
    steam_column(o2, w * 0.60, h * 0.16, h * 0.46, w * 0.11, 255, sway=6)
    steam_column(o2, w * 0.72, h * 0.12, h * 0.38, w * 0.09, 255, sway=14)
    steam_column(o2, w * 0.48, h * 0.24, h * 0.28, w * 0.08, 250, sway=-10)
    yield compose(root, o2)

    o3 = Image.new("RGBA", root.size, (0, 0, 0, 0))
    steam_column(o3, w * 0.66, h * 0.10, h * 0.42, w * 0.095, 255, sway=12)
    steam_column(o3, w * 0.76, h * 0.08, h * 0.32, w * 0.08, 245, sway=16)
    yield compose(root, o3)


def meat(root: Image.Image):
    w, h = root.size
    o1 = Image.new("RGBA", root.size, (0, 0, 0, 0))
    steam_column(o1, w * 0.68, h * 0.28, h * 0.40, w * 0.10, 255, sway=-5)
    yield compose(root, o1)

    o2 = Image.new("RGBA", root.size, (0, 0, 0, 0))
    steam_column(o2, w * 0.72, h * 0.22, h * 0.48, w * 0.12, 255, sway=7)
    steam_column(o2, w * 0.58, h * 0.30, h * 0.30, w * 0.08, 240, sway=-9)
    yield compose(root, o2)

    o3 = Image.new("RGBA", root.size, (0, 0, 0, 0))
    steam_column(o3, w * 0.76, h * 0.16, h * 0.42, w * 0.10, 255, sway=12)
    yield compose(root, o3)


def stew(root: Image.Image):
    w, h = root.size
    o1 = Image.new("RGBA", root.size, (0, 0, 0, 0))
    steam_column(o1, w * 0.48, h * 0.30, h * 0.34, w * 0.09, 255, sway=-8)
    steam_column(o1, w * 0.58, h * 0.32, h * 0.28, w * 0.075, 245, sway=10)
    yield simmer(compose(root, o1), [(w * 0.47, h * 0.48, 5, 210), (w * 0.58, h * 0.52, 4, 190)])

    o2 = Image.new("RGBA", root.size, (0, 0, 0, 0))
    steam_column(o2, w * 0.50, h * 0.26, h * 0.40, w * 0.10, 255, sway=5)
    steam_column(o2, w * 0.40, h * 0.30, h * 0.30, w * 0.075, 250, sway=-12)
    steam_column(o2, w * 0.62, h * 0.28, h * 0.32, w * 0.08, 250, sway=14)
    yield simmer(compose(root, o2), [(w * 0.52, h * 0.46, 6, 230), (w * 0.40, h * 0.54, 4, 200)])

    o3 = Image.new("RGBA", root.size, (0, 0, 0, 0))
    steam_column(o3, w * 0.54, h * 0.24, h * 0.34, w * 0.08, 250, sway=10)
    steam_column(o3, w * 0.44, h * 0.28, h * 0.26, w * 0.07, 235, sway=-8)
    yield simmer(compose(root, o3), [(w * 0.46, h * 0.50, 5, 200)])


def corn(root: Image.Image):
    w, h = root.size
    o1 = Image.new("RGBA", root.size, (0, 0, 0, 0))
    steam_column(o1, w * 0.68, h * 0.26, h * 0.36, w * 0.08, 255, sway=-6)
    yield compose(root, o1)

    o2 = Image.new("RGBA", root.size, (0, 0, 0, 0))
    steam_column(o2, w * 0.72, h * 0.20, h * 0.44, w * 0.095, 255, sway=8)
    steam_column(o2, w * 0.60, h * 0.28, h * 0.28, w * 0.07, 240, sway=-10)
    yield compose(root, o2)

    o3 = Image.new("RGBA", root.size, (0, 0, 0, 0))
    steam_column(o3, w * 0.76, h * 0.16, h * 0.38, w * 0.08, 250, sway=13)
    yield compose(root, o3)


if __name__ == "__main__":
    frames_for("CookedFish", fish)
    frames_for("CookedMeat", meat)
    frames_for("FishStew", stew)
    frames_for("CookedCorn", corn)
