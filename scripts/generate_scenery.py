import os
import struct
import zlib
import math
import random

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCENERY_DIR = os.path.join(BASE_DIR, "Assets", "Scenery")
SPRITES_DIR = os.path.join(BASE_DIR, "Assets", "Sprites")
os.makedirs(SCENERY_DIR, exist_ok=True)
os.makedirs(SPRITES_DIR, exist_ok=True)

class PixelCanvas:
    def __init__(self, width, height):
        self.width = width
        self.height = height
        self.pixels = [[(0, 0, 0, 0) for _ in range(width)] for _ in range(height)]

    def putpixel(self, xy, color):
        x, y = xy
        if 0 <= x < self.width and 0 <= y < self.height:
            self.pixels[y][x] = color

    def getpixel(self, xy):
        x, y = xy
        if 0 <= x < self.width and 0 <= y < self.height:
            return self.pixels[y][x]
        return (0, 0, 0, 0)

    def fill_rect(self, x0, y0, w, h, color):
        for y in range(y0, min(y0 + h, self.height)):
            for x in range(x0, min(x0 + w, self.width)):
                self.putpixel((x, y), color)

    def draw_line(self, x0, y0, x1, y1, color):
        dx = abs(x1 - x0)
        dy = abs(y1 - y0)
        sx = 1 if x0 < x1 else -1
        sy = 1 if y0 < y1 else -1
        err = dx - dy
        while True:
            self.putpixel((x0, y0), color)
            if x0 == x1 and y0 == y1:
                break
            e2 = 2 * err
            if e2 > -dy:
                err -= dy
                x0 += sx
            if e2 < dx:
                err += dx
                y0 += sy

    def save_png(self, filepath, scale=4):
        out_w = self.width * scale
        out_h = self.height * scale
        
        raw_data = bytearray()
        for y in range(out_h):
            raw_data.append(0)
            src_y = y // scale
            for x in range(out_w):
                src_x = x // scale
                r, g, b, a = self.pixels[src_y][src_x]
                raw_data.extend([r, g, b, a])
        
        png = bytearray(b"\x89PNG\r\n\x1a\n")
        ihdr_data = struct.pack(">IIBBBBB", out_w, out_h, 8, 6, 0, 0, 0)
        png.extend(struct.pack(">I", len(ihdr_data)))
        png.extend(b"IHDR")
        png.extend(ihdr_data)
        png.extend(struct.pack(">I", zlib.crc32(b"IHDR" + ihdr_data) & 0xffffffff))
        
        compressed = zlib.compress(bytes(raw_data), level=9)
        png.extend(struct.pack(">I", len(compressed)))
        png.extend(b"IDAT")
        png.extend(compressed)
        png.extend(struct.pack(">I", zlib.crc32(b"IDAT" + compressed) & 0xffffffff))
        
        png.extend(struct.pack(">I", 0))
        png.extend(b"IEND")
        png.extend(struct.pack(">I", zlib.crc32(b"IEND") & 0xffffffff))
        
        with open(filepath, "wb") as f:
            f.write(png)
        print(f"Generated: {filepath}")

def lerp_color(c1, c2, t):
    t = max(0.0, min(1.0, t))
    return (
        int(c1[0] + (c2[0] - c1[0]) * t),
        int(c1[1] + (c2[1] - c1[1]) * t),
        int(c1[2] + (c2[2] - c1[2]) * t),
        255
    )

def draw_doghouse(canvas, hx, hy, time_of_day="day"):
    # Doghouse dimensions: ~36 wide x 34 high
    # Palette
    WOOD_DARK = (90, 50, 30, 255)
    WOOD_MID = (145, 85, 50, 255)
    WOOD_LIGHT = (185, 115, 70, 255)
    ROOF_DARK = (140, 40, 35, 255)
    ROOF_LIGHT = (195, 65, 55, 255)
    DOORWAY = (35, 20, 15, 255)
    SIGN_WOOD = (220, 185, 135, 255)
    
    if time_of_day == "night":
        WOOD_DARK = (45, 25, 20, 255)
        WOOD_MID = (75, 45, 35, 255)
        WOOD_LIGHT = (100, 60, 45, 255)
        ROOF_DARK = (80, 25, 25, 255)
        ROOF_LIGHT = (115, 40, 35, 255)
        DOORWAY = (18, 10, 10, 255)
        SIGN_WOOD = (130, 110, 85, 255)
    elif time_of_day == "evening":
        WOOD_DARK = (75, 40, 30, 255)
        WOOD_MID = (130, 70, 50, 255)
        WOOD_LIGHT = (170, 95, 65, 255)
        ROOF_DARK = (125, 35, 35, 255)
        ROOF_LIGHT = (175, 55, 50, 255)

    # Base house wall (hx..hx+32, hy+12..hy+32)
    canvas.fill_rect(hx + 2, hy + 12, 28, 20, WOOD_MID)
    # Horizontal wood planks lines
    for py in range(hy + 16, hy + 32, 4):
        canvas.fill_rect(hx + 2, py, 28, 1, WOOD_DARK)
    # Highlight on left edge
    canvas.fill_rect(hx + 2, hy + 12, 1, 20, WOOD_LIGHT)
    # Shadow on right edge
    canvas.fill_rect(hx + 29, hy + 12, 1, 20, WOOD_DARK)

    # Gable triangle wall
    for row in range(12):
        span = row * 2 + 6
        left = hx + 16 - span // 2
        canvas.fill_rect(left, hy + row, span, 1, WOOD_MID)

    # Slanted Roof
    roof_w = 36
    for i in range(14):
        # Peak at i=0
        rx_l = hx + 16 - i - 3
        rx_r = hx + 16 + i + 2
        ry = hy + i
        canvas.putpixel((rx_l, ry), ROOF_DARK)
        canvas.putpixel((rx_l + 1, ry), ROOF_LIGHT)
        canvas.putpixel((rx_r - 1, ry), ROOF_LIGHT)
        canvas.putpixel((rx_r, ry), ROOF_DARK)
        if i < 2:
            canvas.fill_rect(rx_l, ry, (rx_r - rx_l + 1), 1, ROOF_LIGHT)

    # Roof eaves overhang
    canvas.fill_rect(hx - 2, hy + 13, 36, 2, ROOF_DARK)
    canvas.fill_rect(hx - 2, hy + 12, 36, 1, ROOF_LIGHT)

    # Arched Doorway
    door_x = hx + 10
    door_y = hy + 18
    # Arch top
    canvas.fill_rect(door_x + 2, door_y, 8, 1, DOORWAY)
    canvas.fill_rect(door_x + 1, door_y + 1, 10, 1, DOORWAY)
    canvas.fill_rect(door_x, door_y + 2, 12, 12, DOORWAY)

    # Bone/Paw Nameplate above door
    canvas.fill_rect(hx + 11, hy + 14, 10, 3, SIGN_WOOD)
    canvas.putpixel((hx + 12, hy + 15), WOOD_DARK)
    canvas.putpixel((hx + 15, hy + 15), WOOD_DARK)
    canvas.putpixel((hx + 18, hy + 15), WOOD_DARK)

    # Porch Lantern on side of house (hx + 31)
    lantern_x = hx + 31
    lantern_y = hy + 15
    POLE = (50, 40, 40, 255)
    LANTERN_FRAME = (40, 30, 30, 255)
    canvas.fill_rect(lantern_x - 2, lantern_y + 1, 3, 1, POLE)
    canvas.fill_rect(lantern_x, lantern_y, 4, 6, LANTERN_FRAME)
    
    if time_of_day in ("evening", "night"):
        # Lit warm glowing lantern glass
        LANTERN_GLOW = (255, 220, 90, 255)
        LANTERN_CORE = (255, 255, 200, 255)
        canvas.fill_rect(lantern_x + 1, lantern_y + 1, 2, 4, LANTERN_GLOW)
        canvas.putpixel((lantern_x + 1, lantern_y + 2), LANTERN_CORE)
        # Glow halo on wall
        canvas.putpixel((lantern_x - 1, lantern_y + 2), (255, 200, 80, 180))
        canvas.putpixel((lantern_x - 1, lantern_y + 3), (255, 200, 80, 180))
        canvas.putpixel((lantern_x + 4, lantern_y + 2), (255, 200, 80, 150))
    else:
        # Off glass
        canvas.fill_rect(lantern_x + 1, lantern_y + 1, 2, 4, (180, 200, 220, 255))

    # Water & Food Bowls next to house
    BOWL_RIM = (70, 130, 180, 255)
    BOWL_FILL = (90, 190, 240, 255)
    bx = hx - 8
    by = hy + 27
    canvas.fill_rect(bx, by + 1, 6, 3, BOWL_RIM)
    canvas.fill_rect(bx + 1, by, 4, 2, BOWL_FILL)

def generate_morning():
    w, h = 180, 135
    cv = PixelCanvas(w, h)
    
    # 1. Sky Gradient (Sunrise: soft magenta/rose -> golden amber -> dawn yellow)
    C_SKY_TOP = (235, 140, 155)
    C_SKY_MID = (245, 185, 130)
    C_SKY_BOT = (255, 230, 150)
    for y in range(h):
        t = y / (h * 0.65)
        if t < 0.5:
            color = lerp_color(C_SKY_TOP, C_SKY_MID, t * 2.0)
        else:
            color = lerp_color(C_SKY_MID, C_SKY_BOT, (t - 0.5) * 2.0)
        for x in range(w):
            cv.putpixel((x, y), color)

    # 2. Rising Sun
    sun_cx, sun_cy, sun_r = 45, 55, 14
    for y in range(sun_cy - sun_r, sun_cy + sun_r + 1):
        for x in range(sun_cx - sun_r, sun_cx + sun_r + 1):
            d = math.hypot(x - sun_cx, y - sun_cy)
            if d <= sun_r:
                cv.putpixel((x, y), (255, 250, 190, 255))
            elif d <= sun_r + 3:
                cv.putpixel((x, y), (255, 230, 140, 140))

    # 3. Distant Mountains (Purple-Gold Mist)
    MNT_COLOR1 = (195, 135, 160, 255)
    MNT_COLOR2 = (175, 115, 145, 255)
    for x in range(w):
        m_y = int(58 + math.sin(x * 0.04) * 8 + math.cos(x * 0.08) * 4)
        for y in range(m_y, h):
            cv.putpixel((x, y), MNT_COLOR1)
            
    for x in range(w):
        m_y2 = int(68 + math.sin((x + 40) * 0.05) * 6)
        for y in range(m_y2, h):
            cv.putpixel((x, y), MNT_COLOR2)

    # 4. Rolling Green Hills Midground
    HILL_COLOR = (120, 175, 95, 255)
    HILL_DARK = (95, 145, 75, 255)
    for x in range(w):
        h_y = int(78 + math.sin(x * 0.03) * 5)
        for y in range(h_y, h):
            cv.putpixel((x, y), HILL_COLOR)
        cv.putpixel((x, h_y), HILL_DARK)

    # 5. Foreground Lawn & Grass
    GRASS_MAIN = (105, 185, 80, 255)
    GRASS_LIGHT = (135, 205, 95, 255)
    GRASS_DARK = (75, 145, 60, 255)
    
    lawn_y = 90
    for y in range(lawn_y, h):
        for x in range(w):
            cv.putpixel((x, y), GRASS_MAIN)
    # Grass tufts and morning dew flowers
    random.seed(42)
    for _ in range(40):
        gx = random.randint(5, w - 10)
        gy = random.randint(lawn_y + 2, h - 5)
        cv.putpixel((gx, gy), GRASS_LIGHT)
        cv.putpixel((gx, gy - 1), GRASS_LIGHT)
        cv.putpixel((gx + 1, gy), GRASS_DARK)
    # Little flowers
    for _ in range(12):
        fx = random.randint(10, w - 20)
        fy = random.randint(lawn_y + 4, h - 8)
        cv.putpixel((fx, fy), (255, 245, 180, 255))
        cv.putpixel((fx, fy + 1), (255, 150, 160, 255))

    # 6. Cozy Doghouse (placed on right side: x=128, y=72)
    draw_doghouse(cv, 128, 72, time_of_day="morning")
    
    cv.save_png(os.path.join(SCENERY_DIR, "scenery_morning.png"), scale=4)

def generate_day():
    w, h = 180, 135
    cv = PixelCanvas(w, h)
    
    # 1. Sky Gradient (Bright vibrant blue -> azure horizon)
    C_SKY_TOP = (70, 155, 235)
    C_SKY_BOT = (165, 215, 255)
    for y in range(h):
        t = y / (h * 0.65)
        color = lerp_color(C_SKY_TOP, C_SKY_BOT, t)
        for x in range(w):
            cv.putpixel((x, y), color)

    # 2. High Golden Sun
    sun_cx, sun_cy, sun_r = 135, 25, 12
    for y in range(sun_cy - sun_r, sun_cy + sun_r + 1):
        for x in range(sun_cx - sun_r, sun_cx + sun_r + 1):
            d = math.hypot(x - sun_cx, y - sun_cy)
            if d <= sun_r:
                cv.putpixel((x, y), (255, 245, 130, 255))
            elif d <= sun_r + 3:
                cv.putpixel((x, y), (255, 220, 80, 120))

    # 3. Fluffy Painted Clouds in backdrop
    def draw_bg_cloud(cx, cy):
        cv.fill_rect(cx - 10, cy, 20, 4, (255, 255, 255, 220))
        cv.fill_rect(cx - 6, cy - 3, 12, 3, (255, 255, 255, 240))
        cv.fill_rect(cx - 2, cy - 5, 6, 2, (255, 255, 255, 255))
    draw_bg_cloud(40, 30)
    draw_bg_cloud(95, 45)

    # 4. Lush Rolling Hills
    HILL_COLOR1 = (100, 185, 90, 255)
    HILL_COLOR2 = (80, 160, 75, 255)
    for x in range(w):
        hy1 = int(62 + math.sin(x * 0.04) * 6 + math.cos(x * 0.07) * 3)
        for y in range(hy1, h):
            cv.putpixel((x, y), HILL_COLOR1)
    for x in range(w):
        hy2 = int(72 + math.sin((x + 25) * 0.05) * 5)
        for y in range(hy2, h):
            cv.putpixel((x, y), HILL_COLOR2)

    # 5. Foreground Lawn
    GRASS_MAIN = (90, 195, 75, 255)
    GRASS_LIGHT = (130, 220, 95, 255)
    GRASS_DARK = (65, 155, 55, 255)
    lawn_y = 88
    for y in range(lawn_y, h):
        for x in range(w):
            cv.putpixel((x, y), GRASS_MAIN)
    # Grass details
    random.seed(101)
    for _ in range(45):
        gx = random.randint(5, w - 10)
        gy = random.randint(lawn_y + 2, h - 5)
        cv.putpixel((gx, gy), GRASS_LIGHT)
        cv.putpixel((gx, gy - 1), GRASS_LIGHT)
        cv.putpixel((gx + 1, gy), GRASS_DARK)
    for _ in range(15):
        fx = random.randint(8, w - 15)
        fy = random.randint(lawn_y + 4, h - 6)
        cv.putpixel((fx, fy), (255, 255, 255, 255))
        cv.putpixel((fx, fy + 1), (255, 215, 0, 255))

    # 6. Doghouse
    draw_doghouse(cv, 128, 70, time_of_day="day")
    
    cv.save_png(os.path.join(SCENERY_DIR, "scenery_day.png"), scale=4)

def generate_evening():
    w, h = 180, 135
    cv = PixelCanvas(w, h)
    
    # 1. Sky Gradient (Twilight: deep violet -> magenta -> fiery orange -> warm peach)
    C_SKY_TOP = (75, 45, 105)
    C_SKY_MID = (185, 75, 100)
    C_SKY_BOT = (250, 150, 75)
    for y in range(h):
        t = y / (h * 0.68)
        if t < 0.5:
            color = lerp_color(C_SKY_TOP, C_SKY_MID, t * 2.0)
        else:
            color = lerp_color(C_SKY_MID, C_SKY_BOT, (t - 0.5) * 2.0)
        for x in range(w):
            cv.putpixel((x, y), color)

    # 2. Low Setting Sun on horizon
    sun_cx, sun_cy, sun_r = 50, 68, 15
    for y in range(sun_cy - sun_r, sun_cy + sun_r + 1):
        for x in range(sun_cx - sun_r, sun_cx + sun_r + 1):
            d = math.hypot(x - sun_cx, y - sun_cy)
            if d <= sun_r:
                cv.putpixel((x, y), (255, 210, 90, 255))
            elif d <= sun_r + 4:
                cv.putpixel((x, y), (255, 140, 60, 140))

    # 3. Sunset Silhouette Hills
    MNT_COLOR1 = (110, 50, 85, 255)
    MNT_COLOR2 = (80, 40, 70, 255)
    for x in range(w):
        hy1 = int(64 + math.sin(x * 0.035) * 7)
        for y in range(hy1, h):
            cv.putpixel((x, y), MNT_COLOR1)
    for x in range(w):
        hy2 = int(74 + math.sin((x + 30) * 0.045) * 6)
        for y in range(hy2, h):
            cv.putpixel((x, y), MNT_COLOR2)

    # 4. Foreground Lawn (Warm evening shadows)
    GRASS_MAIN = (80, 130, 70, 255)
    GRASS_LIGHT = (115, 160, 85, 255)
    GRASS_DARK = (55, 95, 50, 255)
    lawn_y = 88
    for y in range(lawn_y, h):
        for x in range(w):
            cv.putpixel((x, y), GRASS_MAIN)
    # Warm highlights from lantern
    for py in range(70, 110):
        for px in range(120, 175):
            d = math.hypot(px - 160, py - 88)
            if d < 30:
                old = cv.getpixel((px, py))
                cv.putpixel((px, py), (min(255, old[0] + 35), min(255, old[1] + 20), old[2], 255))

    # 5. Doghouse with lit lantern
    draw_doghouse(cv, 128, 70, time_of_day="evening")
    
    cv.save_png(os.path.join(SCENERY_DIR, "scenery_evening.png"), scale=4)

def generate_night():
    w, h = 180, 135
    cv = PixelCanvas(w, h)
    
    # 1. Sky Gradient (Deep midnight navy -> dark indigo)
    C_SKY_TOP = (15, 15, 45)
    C_SKY_BOT = (30, 35, 75)
    for y in range(h):
        t = y / (h * 0.7)
        color = lerp_color(C_SKY_TOP, C_SKY_BOT, t)
        for x in range(w):
            cv.putpixel((x, y), color)

    # 2. Twinkling Stars in base background
    random.seed(777)
    for _ in range(60):
        sx = random.randint(2, w - 3)
        sy = random.randint(2, 65)
        bright = random.choice([(255, 255, 255, 255), (200, 220, 255, 220), (255, 240, 180, 200)])
        cv.putpixel((sx, sy), bright)
        if random.random() < 0.2:
            cv.putpixel((sx + 1, sy), (bright[0], bright[1], bright[2], 120))
            cv.putpixel((sx - 1, sy), (bright[0], bright[1], bright[2], 120))
            cv.putpixel((sx, sy + 1), (bright[0], bright[1], bright[2], 120))
            cv.putpixel((sx, sy - 1), (bright[0], bright[1], bright[2], 120))

    # 3. Glowing Crescent Moon
    moon_cx, moon_cy, moon_r = 35, 30, 11
    for y in range(moon_cy - moon_r, moon_cy + moon_r + 1):
        for x in range(moon_cx - moon_r, moon_cx + moon_r + 1):
            d1 = math.hypot(x - moon_cx, y - moon_cy)
            d2 = math.hypot(x - (moon_cx + 4), y - (moon_cy - 2))
            if d1 <= moon_r and d2 > moon_r - 2:
                cv.putpixel((x, y), (255, 250, 200, 255))
            elif d1 <= moon_r + 2 and d2 > moon_r:
                cv.putpixel((x, y), (255, 240, 180, 100))

    # 4. Silhouette Night Hills
    HILL_DARK1 = (20, 25, 50, 255)
    HILL_DARK2 = (15, 20, 40, 255)
    for x in range(w):
        hy1 = int(66 + math.sin(x * 0.035) * 6)
        for y in range(hy1, h):
            cv.putpixel((x, y), HILL_DARK1)
    for x in range(w):
        hy2 = int(76 + math.sin((x + 35) * 0.045) * 5)
        for y in range(hy2, h):
            cv.putpixel((x, y), HILL_DARK2)

    # 5. Night Foreground Lawn
    GRASS_NIGHT = (25, 45, 40, 255)
    GRASS_LIGHT = (40, 65, 55, 255)
    lawn_y = 88
    for y in range(lawn_y, h):
        for x in range(w):
            cv.putpixel((x, y), GRASS_NIGHT)
    # Lantern warm glow pool
    for py in range(72, 115):
        for px in range(125, 175):
            d = math.hypot(px - 160, py - 88)
            if d < 32:
                factor = 1.0 - (d / 32.0)
                old = cv.getpixel((px, py))
                cv.putpixel((px, py), (
                    min(255, int(old[0] + 160 * factor)),
                    min(255, int(old[1] + 120 * factor)),
                    min(255, int(old[2] + 40 * factor)),
                    255
                ))

    # 6. Cozy Doghouse with glowing lantern
    draw_doghouse(cv, 128, 70, time_of_day="night")
    
    cv.save_png(os.path.join(SCENERY_DIR, "scenery_night.png"), scale=4)

def generate_ambient_sprites():
    # 1. Birds (16x16)
    for frame in range(2):
        cv = PixelCanvas(16, 16)
        COLOR = (45, 30, 25, 255)
        if frame == 0:
            # Wings Up
            cv.putpixel((7, 8), COLOR)
            cv.putpixel((8, 8), COLOR)
            cv.putpixel((6, 7), COLOR)
            cv.putpixel((9, 7), COLOR)
            cv.putpixel((5, 6), COLOR)
            cv.putpixel((10, 6), COLOR)
            cv.putpixel((4, 5), COLOR)
            cv.putpixel((11, 5), COLOR)
        else:
            # Wings Down
            cv.putpixel((7, 6), COLOR)
            cv.putpixel((8, 6), COLOR)
            cv.putpixel((6, 7), COLOR)
            cv.putpixel((9, 7), COLOR)
            cv.putpixel((5, 8), COLOR)
            cv.putpixel((10, 8), COLOR)
            cv.putpixel((4, 9), COLOR)
            cv.putpixel((11, 9), COLOR)
        cv.save_png(os.path.join(SPRITES_DIR, f"bird_{frame}.png"), scale=3)

    # 2. Drifting Clouds (40x20)
    for c_idx in range(2):
        cv = PixelCanvas(48, 24)
        WHITE = (255, 255, 255, 240)
        SHADOW = (220, 235, 250, 220)
        offset = c_idx * 2
        # Cloud puffs
        cv.fill_rect(8, 10, 32, 8, WHITE)
        cv.fill_rect(12, 6 + offset, 16, 6, WHITE)
        cv.fill_rect(24, 4 + offset, 12, 8, WHITE)
        cv.fill_rect(8, 16, 32, 2, SHADOW)
        cv.save_png(os.path.join(SPRITES_DIR, f"cloud_{c_idx}.png"), scale=3)

    # 3. Fireflies (8x8)
    for f_idx in range(2):
        cv = PixelCanvas(8, 8)
        GLOW = (240, 255, 120, 255) if f_idx == 0 else (200, 240, 80, 180)
        CORE = (255, 255, 220, 255)
        cv.putpixel((3, 3), CORE)
        cv.putpixel((4, 3), CORE)
        cv.putpixel((3, 4), CORE)
        cv.putpixel((4, 4), CORE)
        cv.putpixel((3, 2), GLOW)
        cv.putpixel((4, 2), GLOW)
        cv.putpixel((2, 3), GLOW)
        cv.putpixel((5, 3), GLOW)
        cv.putpixel((2, 4), GLOW)
        cv.putpixel((5, 4), GLOW)
        cv.putpixel((3, 5), GLOW)
        cv.putpixel((4, 5), GLOW)
        cv.save_png(os.path.join(SPRITES_DIR, f"firefly_{f_idx}.png"), scale=4)

    # 4. Twinkling Stars (8x8)
    for s_idx in range(2):
        cv = PixelCanvas(8, 8)
        STAR = (255, 255, 240, 255)
        DIM = (200, 220, 255, 140)
        cv.putpixel((3, 3), STAR)
        cv.putpixel((4, 3), STAR)
        cv.putpixel((3, 4), STAR)
        cv.putpixel((4, 4), STAR)
        if s_idx == 0:
            cv.putpixel((3, 2), STAR)
            cv.putpixel((4, 2), STAR)
            cv.putpixel((3, 5), STAR)
            cv.putpixel((4, 5), STAR)
            cv.putpixel((2, 3), STAR)
            cv.putpixel((2, 4), STAR)
            cv.putpixel((5, 3), STAR)
            cv.putpixel((5, 4), STAR)
        else:
            cv.putpixel((3, 2), DIM)
            cv.putpixel((4, 2), DIM)
            cv.putpixel((2, 3), DIM)
            cv.putpixel((5, 4), DIM)
        cv.save_png(os.path.join(SPRITES_DIR, f"star_{s_idx}.png"), scale=4)

if __name__ == "__main__":
    print("Generating pixel art scenery backgrounds...")
    generate_morning()
    generate_day()
    generate_evening()
    generate_night()
    print("Generating ambient particle sprites...")
    generate_ambient_sprites()
    print("Done!")
