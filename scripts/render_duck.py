import os
import struct
import zlib
import math

OUTPUT_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "Sprites")
os.makedirs(OUTPUT_DIR, exist_ok=True)

WIDTH = 256
HEIGHT = 256

# Palette
TRANSPARENT = (0, 0, 0, 0)
OUTLINE = (50, 35, 20, 255)         # Dark warm chocolate outline
DUCK_BODY = (255, 222, 15, 255)      # Classic bright yellow
DUCK_HIGHLIGHT = (255, 245, 95, 255) # Sunny light yellow
DUCK_SHEEN = (255, 254, 205, 255)    # Specular shine
DUCK_SHADOW = (215, 165, 0, 255)     # Amber shadow
DUCK_SHADOW_DARK = (185, 130, 0, 255)# Deep crease shadow

BEAK_MAIN = (255, 130, 0, 255)       # Vibrant rubber duck orange
BEAK_LIGHT = (255, 168, 35, 255)     # Beak top highlight
BEAK_SHADOW = (205, 85, 0, 255)      # Beak underside
BEAK_LINE = (160, 60, 0, 255)        # Mouth crease

FEET_COLOR = (255, 135, 0, 255)
FEET_SHADOW = (205, 90, 0, 255)

EYE_BLACK = (25, 20, 15, 255)
EYE_WHITE = (255, 255, 255, 255)
CHEEK_BLUSH = (255, 180, 50, 160)

# Barca colors
BARCA_BLUE = (0, 77, 152, 255)
BARCA_RED = (165, 0, 68, 255)
BARCA_GOLD = (237, 187, 0, 255)

# F1 colors
F1_RED = (230, 0, 0, 255)
F1_DARK = (40, 40, 45, 255)
F1_SILVER = (195, 195, 205, 255)

# Props
BOWL_BLUE = (70, 165, 230, 255)
WATER_COLOR = (90, 210, 255, 230)
WATER_SPLASH = (180, 240, 255, 255)
SEED_COLOR = (230, 180, 90, 255)
SLEEP_Z = (130, 175, 255, 255)

# Party
HAT_BASE = (255, 75, 115, 255)
HAT_STRIPE = (255, 225, 50, 255)
HAT_POMPOM = (50, 215, 255, 255)
CONFETTI_COLORS = [
    (255, 70, 90, 255),
    (255, 215, 0, 255),
    (50, 205, 255, 255),
    (150, 240, 60, 255),
    (215, 90, 255, 255)
]


class Canvas256:
    def __init__(self):
        self.width = WIDTH
        self.height = HEIGHT
        self.pixels = [[TRANSPARENT for _ in range(WIDTH)] for _ in range(HEIGHT)]

    def putpixel(self, x, y, color):
        ix = int(round(x))
        iy = int(round(y))
        if 0 <= ix < self.width and 0 <= iy < self.height:
            if color[3] == 255 or self.pixels[iy][ix][3] == 0:
                self.pixels[iy][ix] = color
            else:
                # Alpha blending
                sr, sg, sb, sa = color
                dr, dg, db, da = self.pixels[iy][ix]
                a_norm = sa / 255.0
                out_r = int(sr * a_norm + dr * (1.0 - a_norm))
                out_g = int(sg * a_norm + dg * (1.0 - a_norm))
                out_b = int(sb * a_norm + db * (1.0 - a_norm))
                self.pixels[iy][ix] = (out_r, out_g, out_b, 255)

    def fill_circle(self, cx, cy, r, color):
        r_sq = r * r
        min_x = max(0, int(cx - r - 1))
        max_x = min(self.width, int(cx + r + 2))
        min_y = max(0, int(cy - r - 1))
        max_y = min(self.height, int(cy + r + 2))
        for y in range(min_y, max_y):
            for x in range(min_x, max_x):
                dx = x - cx
                dy = y - cy
                if dx * dx + dy * dy <= r_sq:
                    self.putpixel(x, y, color)

    def fill_ellipse(self, cx, cy, rx, ry, color):
        min_x = max(0, int(cx - rx - 1))
        max_x = min(self.width, int(cx + rx + 2))
        min_y = max(0, int(cy - ry - 1))
        max_y = min(self.height, int(cy + ry + 2))
        for y in range(min_y, max_y):
            for x in range(min_x, max_x):
                dx = (x - cx) / rx
                dy = (y - cy) / ry
                if dx * dx + dy * dy <= 1.0:
                    self.putpixel(x, y, color)

    def fill_rect(self, x0, y0, w, h, color):
        for y in range(int(y0), int(y0 + h)):
            for x in range(int(x0), int(x0 + w)):
                self.putpixel(x, y, color)

    def save_png(self, filepath):
        raw_data = bytearray()
        for y in range(self.height):
            raw_data.append(0)  # filter type 0
            for x in range(self.width):
                r, g, b, a = self.pixels[y][x]
                raw_data.extend([r, g, b, a])

        png = bytearray(b"\x89PNG\r\n\x1a\n")
        ihdr_data = struct.pack(">IIBBBBB", self.width, self.height, 8, 6, 0, 0, 0)
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


def draw_rubber_duck(canvas, variant="idle", frame=0):
    # Bob / breathing animation
    bob = 0
    if variant in ("idle", "sleep", "rest", "barca", "f1", "water", "food"):
        bob_offsets = [0, 3, -3, 0, 1]
        bob = bob_offsets[frame % 5]

    tail_angle = 0
    if variant == "idle":
        tail_offsets = [0, 4, -4, 2, 0]
        tail_angle = tail_offsets[frame % 5]

    blink = (variant == "idle" and frame == 3)
    is_sleeping = (variant == "sleep")
    is_rest = (variant == "rest")
    is_water = (variant == "water")
    is_food = (variant == "food")
    is_barca = (variant == "barca")
    is_f1 = (variant == "f1")

    # Head & beak dip for eating/drinking
    head_y_offset = 0
    head_x_offset = 0
    if (is_water or is_food) and frame in (1, 2):
        head_y_offset = 12
        head_x_offset = 6

    # Rest wing stretch
    wing_stretch = 0
    if is_rest:
        wing_offsets = [0, 6, 12, 6, 0]
        wing_stretch = wing_offsets[frame % 5]

    # Floor shadow
    for y in range(212, 226):
        for x in range(45, 205):
            dx = (x - 125) / 78.0
            dy = (y - 219) / 7.0
            if dx * dx + dy * dy <= 1.0:
                canvas.putpixel(x, y, (40, 30, 20, 50))

    # --- 1. BODY BASE & OUTLINE ---
    body_cx = 118
    body_cy = 168 + bob
    body_rx = 66
    body_ry = 46

    # Function to check if (x, y) is inside the tail nub
    def is_inside_tail(x, y, extra_radius=0):
        # Tail curves upward-left from rear body
        tip_x = 36 - tail_angle
        tip_y = 132 + bob
        # Quadratic curve from rear body (70, 165) up to tip (36, 132) and back down to (56, 175)
        # We can model the tail as a tilted rounded wedge
        dx = (x - (tip_x + 16))
        dy = (y - (tip_y + 16))
        # Tilted ellipse rotated ~40 degrees
        cos_a = 0.766
        sin_a = -0.642
        rx_rot = (dx * cos_a - dy * sin_a) / (24.0 + extra_radius)
        ry_rot = (dx * sin_a + dy * cos_a) / (14.0 + extra_radius)
        return (rx_rot * rx_rot + ry_rot * ry_rot) <= 1.0

    # Outline pass
    for y in range(body_cy - body_ry - 4, body_cy + body_ry + 6):
        for x in range(body_cx - body_rx - 30, body_cx + body_rx + 25):
            dx = (x - body_cx) / (body_rx + 3)
            dy = (y - body_cy) / (body_ry + 3)
            in_body_outline = (dx * dx + dy * dy <= 1.0)
            in_tail_outline = is_inside_tail(x, y, extra_radius=2.5)

            if in_body_outline or in_tail_outline:
                canvas.putpixel(x, y, OUTLINE)

    # Tail fill
    for y in range(116 + bob, 175 + bob):
        for x in range(20 - tail_angle, 75):
            if is_inside_tail(x, y, extra_radius=0):
                col = DUCK_HIGHLIGHT if y < 140 + bob else DUCK_BODY
                canvas.putpixel(x, y, col)

    # Body fill
    for y in range(body_cy - body_ry, body_cy + body_ry + 1):
        for x in range(body_cx - body_rx, body_cx + body_rx + 1):
            dx = (x - body_cx) / body_rx
            dy = (y - body_cy) / body_ry
            if dx * dx + dy * dy <= 1.0:
                # Radial-style shading
                if dy > 0.35:
                    col = DUCK_SHADOW_DARK if dy > 0.7 else DUCK_SHADOW
                elif dy < -0.2 and dx > -0.3:
                    col = DUCK_HIGHLIGHT
                else:
                    col = DUCK_BODY
                canvas.putpixel(x, y, col)

    # Body highlight sheen (soft light on upper-left chest)
    for y in range(body_cy - 36, body_cy - 12):
        for x in range(body_cx - 40, body_cx + 20):
            dx = (x - (body_cx - 10)) / 25.0
            dy = (y - (body_cy - 24)) / 10.0
            if dx * dx + dy * dy <= 1.0:
                canvas.putpixel(x, y, DUCK_SHEEN)

    # Barca jersey band
    if is_barca:
        for y in range(body_cy - 16, body_cy + 22):
            for x in range(body_cx - 30, body_cx + 36):
                dx = (x - body_cx) / (body_rx - 10)
                dy = (y - body_cy) / (body_ry - 10)
                if dx * dx + dy * dy <= 0.85:
                    stripe = (x % 16 < 8)
                    canvas.putpixel(x, y, BARCA_BLUE if stripe else BARCA_RED)
        # Gold collar line
        for x in range(body_cx - 18, body_cx + 30):
            canvas.putpixel(x, body_cy - 18, BARCA_GOLD)
            canvas.putpixel(x, body_cy - 17, BARCA_GOLD)

    # --- 2. WING ON SIDE ---
    wing_cx = 104 - (wing_stretch // 2)
    wing_cy = 166 + bob - (wing_stretch // 3)
    wing_rx = 34 + wing_stretch
    wing_ry = 22 + (wing_stretch // 2)

    # Wing outline
    for y in range(wing_cy - wing_ry - 2, wing_cy + wing_ry + 3):
        for x in range(wing_cx - wing_rx - 2, wing_cx + wing_rx + 3):
            dx = (x - wing_cx) / (wing_rx + 2)
            dy = (y - wing_cy) / (wing_ry + 2)
            if dx * dx + dy * dy <= 1.0:
                canvas.putpixel(x, y, OUTLINE)

    # Wing fill
    for y in range(wing_cy - wing_ry, wing_cy + wing_ry + 1):
        for x in range(wing_cx - wing_rx, wing_cx + wing_rx + 1):
            dx = (x - wing_cx) / wing_rx
            dy = (y - wing_cy) / wing_ry
            if dx * dx + dy * dy <= 1.0:
                col = DUCK_SHADOW if dy > 0.2 else (DUCK_HIGHLIGHT if dy < -0.3 else DUCK_BODY)
                canvas.putpixel(x, y, col)

    # Wing feather scalloped crease
    for x in range(wing_cx - 18, wing_cx + 14):
        arc_y = int(wing_cy + 4 + math.sin((x - wing_cx) * 0.25) * 3)
        canvas.putpixel(x, arc_y, DUCK_SHADOW_DARK)

    # --- 3. NECK & HEAD ---
    head_cx = 152 + head_x_offset
    head_cy = 96 + bob + head_y_offset
    head_r = 38

    # Neck blend
    neck_cx = 142 + (head_x_offset // 2)
    neck_cy = 126 + bob + (head_y_offset // 2)
    for y in range(neck_cy - 18, neck_cy + 18):
        for x in range(neck_cx - 26, neck_cx + 26):
            dx = (x - neck_cx) / 24.0
            dy = (y - neck_cy) / 16.0
            if dx * dx + dy * dy <= 1.0:
                canvas.putpixel(x, y, DUCK_BODY)

    # Head outline pass
    for y in range(head_cy - head_r - 3, head_cy + head_r + 4):
        for x in range(head_cx - head_r - 3, head_cx + head_r + 4):
            dx = (x - head_cx) / (head_r + 2.5)
            dy = (y - head_cy) / (head_r + 2.5)
            if dx * dx + dy * dy <= 1.0:
                canvas.putpixel(x, y, OUTLINE)

    # Head fill
    for y in range(head_cy - head_r, head_cy + head_r + 1):
        for x in range(head_cx - head_r, head_cx + head_r + 1):
            dx = (x - head_cx) / head_r
            dy = (y - head_cy) / head_r
            if dx * dx + dy * dy <= 1.0:
                if dy > 0.4:
                    col = DUCK_SHADOW
                elif dy < -0.2 and dx > -0.3:
                    col = DUCK_HIGHLIGHT
                else:
                    col = DUCK_BODY
                canvas.putpixel(x, y, col)

    # Head gloss highlight streak (iconic plastic shine!)
    for y in range(head_cy - 30, head_cy - 12):
        for x in range(head_cx - 24, head_cx + 6):
            dx = (x - (head_cx - 10)) / 14.0
            dy = (y - (head_cy - 21)) / 7.0
            if dx * dx + dy * dy <= 1.0:
                canvas.putpixel(x, y, DUCK_SHEEN)

    # Cheerful cheek blush
    for y in range(head_cy + 6, head_cy + 18):
        for x in range(head_cx - 6, head_cx + 10):
            dx = (x - (head_cx + 2)) / 7.0
            dy = (y - (head_cy + 12)) / 5.0
            if dx * dx + dy * dy <= 1.0:
                canvas.putpixel(x, y, CHEEK_BLUSH)

    # --- 4. BEAK (FACING RIGHT) ---
    beak_base_x = head_cx + 24
    beak_base_y = head_cy + 4
    beak_tip_x = head_cx + 74
    beak_tip_y = head_cy + 10

    # Beak outline pass
    for y in range(beak_base_y - 14, beak_base_y + 20):
        for x in range(beak_base_x - 4, beak_tip_x + 4):
            # Beak shape: tapered rounded wedge
            t = max(0.0, min(1.0, (x - beak_base_x) / 50.0))
            half_h = (1.0 - t * 0.45) * 14.0
            center_y = beak_base_y + t * 4.0
            if abs(y - center_y) <= half_h + 2.5:
                canvas.putpixel(x, y, OUTLINE)

    # Beak fill
    for y in range(beak_base_y - 12, beak_base_y + 18):
        for x in range(beak_base_x, beak_tip_x + 1):
            t = max(0.0, min(1.0, (x - beak_base_x) / 50.0))
            half_h = (1.0 - t * 0.45) * 13.0
            center_y = beak_base_y + t * 4.0
            if abs(y - center_y) <= half_h:
                if y < center_y - 2:
                    col = BEAK_LIGHT
                elif y > center_y + 3:
                    col = BEAK_SHADOW
                else:
                    col = BEAK_MAIN
                canvas.putpixel(x, y, col)

    # Beak mouth line & smile
    for x in range(beak_base_x + 4, beak_tip_x - 2):
        t = (x - beak_base_x) / 50.0
        line_y = int(round(beak_base_y + t * 4.0 + 1.0))
        canvas.putpixel(x, line_y, BEAK_LINE)
        canvas.putpixel(x, line_y + 1, BEAK_LINE)

    # Beak nostril
    canvas.putpixel(beak_base_x + 14, beak_base_y - 3, BEAK_LINE)
    canvas.putpixel(beak_base_x + 15, beak_base_y - 3, BEAK_LINE)
    canvas.putpixel(beak_base_x + 14, beak_base_y - 2, BEAK_LINE)

    # --- 5. EYE ---
    eye_cx = head_cx + 8
    eye_cy = head_cy - 7

    if is_sleeping:
        # Curved sleeping eye arc: ^
        for x in range(eye_cx - 10, eye_cx + 11):
            arc_y = int(eye_cy + math.cos((x - eye_cx) * 0.2) * 5 - 2)
            canvas.putpixel(x, arc_y, EYE_BLACK)
            canvas.putpixel(x, arc_y + 1, EYE_BLACK)
            canvas.putpixel(x, arc_y + 2, EYE_BLACK)
    elif blink:
        # Blinking eye line
        for x in range(eye_cx - 9, eye_cx + 10):
            canvas.putpixel(x, eye_cy, EYE_BLACK)
            canvas.putpixel(x, eye_cy + 1, EYE_BLACK)
            canvas.putpixel(x, eye_cy + 2, EYE_BLACK)
    else:
        # Big adorable round eye
        canvas.fill_circle(eye_cx, eye_cy, 11, OUTLINE)
        canvas.fill_circle(eye_cx, eye_cy, 9.5, EYE_BLACK)
        # Specular shine
        canvas.fill_circle(eye_cx - 3, eye_cy - 3, 4.0, EYE_WHITE)
        canvas.fill_circle(eye_cx + 4, eye_cy + 3, 1.8, EYE_WHITE)

    # --- 6. PROPS & VARIANT ACCENTS ---
    if is_water:
        # Mini water pond / bowl
        bowl_x = 195
        bowl_y = 190
        canvas.fill_ellipse(bowl_x, bowl_y, 38, 16, OUTLINE)
        canvas.fill_ellipse(bowl_x, bowl_y - 2, 36, 14, BOWL_BLUE)
        canvas.fill_ellipse(bowl_x, bowl_y - 4, 32, 10, WATER_COLOR)

        # Splashing droplets
        splash_offsets = [
            [(188, 175), (196, 170), (204, 176)],
            [(184, 168), (198, 162), (210, 170)],
            [(180, 162), (195, 155), (212, 164)],
            [(182, 170), (198, 166), (208, 172)],
            [(188, 176), (196, 172), (204, 178)]
        ][frame % 5]
        for sx, sy in splash_offsets:
            canvas.fill_circle(sx, sy, 3, WATER_SPLASH)

    if is_food:
        # Feeding plate with breadcrumbs/seeds
        plate_x = 195
        plate_y = 190
        canvas.fill_ellipse(plate_x, plate_y, 36, 15, OUTLINE)
        canvas.fill_ellipse(plate_x, plate_y - 2, 34, 13, (220, 80, 60, 255))
        canvas.fill_ellipse(plate_x, plate_y - 4, 28, 9, (240, 120, 100, 255))

        # Seeds
        seeds = [
            (plate_x - 12, plate_y - 5), (plate_x - 4, plate_y - 3),
            (plate_x + 5, plate_y - 6), (plate_x + 12, plate_y - 4),
            (plate_x - 2, plate_y - 7)
        ]
        for sx, sy in seeds:
            canvas.fill_circle(sx, sy, 2.5, SEED_COLOR)

        # Pecking crumbs when head is down
        if frame in (1, 2, 3):
            canvas.fill_circle(beak_tip_x - 4, beak_tip_y + 4, 2.5, SEED_COLOR)

    if is_sleeping:
        # Floating Z z Z
        z_x = 185 + (frame * 5)
        z_y = 80 - (frame * 12)
        # Small Z
        if 0 <= z_y < HEIGHT:
            canvas.fill_rect(z_x, z_y, 14, 3, SLEEP_Z)
            canvas.fill_rect(z_x, z_y + 11, 14, 3, SLEEP_Z)
            for i in range(12):
                canvas.putpixel(z_x + 12 - i, z_y + i, SLEEP_Z)
                canvas.putpixel(z_x + 11 - i, z_y + i, SLEEP_Z)
        # Smaller z
        z2_x = 170 + (frame * 3)
        z2_y = 100 - (frame * 8)
        if 0 <= z2_y < HEIGHT:
            canvas.fill_rect(z2_x, z2_y, 9, 2, SLEEP_Z)
            canvas.fill_rect(z2_x, z2_y + 7, 9, 2, SLEEP_Z)
            for i in range(8):
                canvas.putpixel(z2_x + 7 - i, z2_y + i, SLEEP_Z)

    if is_barca:
        # Mini football bobbing beside the duck
        ball_x = 205
        ball_y = 195 + (bob * 2)
        canvas.fill_circle(ball_x, ball_y, 16, OUTLINE)
        canvas.fill_circle(ball_x, ball_y, 14, (245, 245, 245, 255))
        # Black pentagon patches
        canvas.fill_circle(ball_x, ball_y, 5, (30, 30, 35, 255))
        canvas.fill_circle(ball_x - 9, ball_y - 6, 4, (30, 30, 35, 255))
        canvas.fill_circle(ball_x + 8, ball_y - 6, 4, (30, 30, 35, 255))
        canvas.fill_circle(ball_x - 7, ball_y + 7, 4, (30, 30, 35, 255))
        canvas.fill_circle(ball_x + 7, ball_y + 7, 4, (30, 30, 35, 255))

    if is_f1:
        # Mini F1 racing wheel & indicator
        wheel_x = 202
        wheel_y = 175
        canvas.fill_circle(wheel_x, wheel_y, 18, F1_DARK)
        canvas.fill_circle(wheel_x, wheel_y, 12, (20, 20, 25, 255))
        canvas.fill_rect(wheel_x - 12, wheel_y - 3, 24, 6, F1_RED)
        canvas.fill_circle(wheel_x, wheel_y, 5, F1_SILVER)
        # LED shift lights
        led_colors = [
            (0, 255, 0, 255), (255, 255, 0, 255), (255, 0, 0, 255),
            (0, 180, 255, 255), (255, 0, 255, 255)
        ]
        canvas.fill_circle(wheel_x, wheel_y - 12, 3, led_colors[frame % 5])

    return canvas


def draw_rubber_duck_walking(canvas, frame=0, birthday=False):
    """
    8-frame bilateral waddling walk cycle for the rubber duck facing right.
    Features:
    - Side-to-side body rock (waddle tilt)
    - Cute orange webbed feet taking alternating paddle steps
    - Tail wagging in counter-balance
    - Head bobbing up/down
    - Optional party hat & confetti for birthday walk
    """
    # 8-step cycle
    # Waddle tilt: body rocks left-right
    # bobbing: bobs down on contact frames
    waddle_angles = [0, 4, 8, 4, 0, -4, -8, -4]
    waddle_x = waddle_angles[frame % 8]

    bobs = [0, -3, 2, 4, 0, -3, 2, 4]
    bob = bobs[frame % 8]

    # Feet stride offsets (left foot, right foot)
    # Forward/backward stride & lift
    foot_offsets = [
        # (left_dx, left_lift, right_dx, right_lift)
        (0, 0, 0, 0),       # frame 0: neutral
        (-12, 0, 10, 8),    # frame 1: right foot stepping forward lifted
        (-18, 0, 16, 2),    # frame 2: right foot plant
        (-10, 6, 12, 0),    # frame 3: left foot pushing off
        (0, 0, 0, 0),       # frame 4: neutral
        (10, 8, -12, 0),    # frame 5: left foot stepping forward lifted
        (16, 2, -18, 0),    # frame 6: left foot plant
        (12, 0, -10, 6),    # frame 7: right foot pushing off
    ]
    l_dx, l_lift, r_dx, r_lift = foot_offsets[frame % 8]

    # Floor shadow
    shadow_w = 75
    for y in range(222, 234):
        for x in range(125 - shadow_w, 125 + shadow_w):
            dx = (x - 125) / float(shadow_w)
            dy = (y - 228) / 6.0
            if dx * dx + dy * dy <= 1.0:
                canvas.putpixel(x, y, (40, 30, 20, 45))

    # --- 1. WEBBED FEET ---
    def draw_webbed_foot(fx, fy):
        # Draw cute orange paddle foot with 3 toes
        canvas.fill_ellipse(fx, fy, 18, 7, OUTLINE)
        canvas.fill_ellipse(fx, fy - 1, 16, 6, FEET_COLOR)
        # Toe splits
        canvas.putpixel(fx + 10, fy - 3, FEET_SHADOW)
        canvas.putpixel(fx + 14, fy - 2, FEET_SHADOW)
        canvas.putpixel(fx + 10, fy + 2, FEET_SHADOW)

    # Far foot (Left)
    draw_webbed_foot(105 + l_dx, 218 + bob - l_lift)

    # --- 2. BODY & TAIL ---
    body_cx = 118 + waddle_x
    body_cy = 166 + bob
    body_rx = 64
    body_ry = 46

    # Tail swing
    tail_swing = -waddle_x * 1.2

    # Function to check if (x, y) is inside the tail nub
    def is_inside_tail(x, y, extra_radius=0):
        tip_x = 36 + tail_swing
        tip_y = 132 + bob
        dx = (x - (tip_x + 16))
        dy = (y - (tip_y + 16))
        cos_a = 0.766
        sin_a = -0.642
        rx_rot = (dx * cos_a - dy * sin_a) / (24.0 + extra_radius)
        ry_rot = (dx * sin_a + dy * cos_a) / (14.0 + extra_radius)
        return (rx_rot * rx_rot + ry_rot * ry_rot) <= 1.0

    # Body outline
    for y in range(body_cy - body_ry - 4, body_cy + body_ry + 6):
        for x in range(body_cx - body_rx - 30, body_cx + body_rx + 25):
            dx = (x - body_cx) / (body_rx + 3)
            dy = (y - body_cy) / (body_ry + 3)
            in_body_outline = (dx * dx + dy * dy <= 1.0)
            in_tail_outline = is_inside_tail(x, y, extra_radius=2.5)
            if in_body_outline or in_tail_outline:
                canvas.putpixel(x, y, OUTLINE)

    # Tail nub fill
    for y in range(116 + bob, 175 + bob):
        for x in range(int(20 + tail_swing), int(75 + tail_swing)):
            if is_inside_tail(x, y, extra_radius=0):
                col = DUCK_HIGHLIGHT if y < 140 + bob else DUCK_BODY
                canvas.putpixel(x, y, col)

    # Body fill
    for y in range(body_cy - body_ry, body_cy + body_ry + 1):
        for x in range(body_cx - body_rx, body_cx + body_rx + 1):
            dx = (x - body_cx) / body_rx
            dy = (y - body_cy) / body_ry
            if dx * dx + dy * dy <= 1.0:
                if dy > 0.35:
                    col = DUCK_SHADOW_DARK if dy > 0.7 else DUCK_SHADOW
                elif dy < -0.2 and dx > -0.3:
                    col = DUCK_HIGHLIGHT
                else:
                    col = DUCK_BODY
                canvas.putpixel(x, y, col)

    # Near foot (Right)
    draw_webbed_foot(145 + r_dx, 222 + bob - r_lift)

    # Wing on side (bobs with waddle)
    wing_cx = body_cx - 14
    wing_cy = body_cy - 2
    wing_rx = 34
    wing_ry = 22

    for y in range(wing_cy - wing_ry - 2, wing_cy + wing_ry + 3):
        for x in range(wing_cx - wing_rx - 2, wing_cx + wing_rx + 3):
            dx = (x - wing_cx) / (wing_rx + 2)
            dy = (y - wing_cy) / (wing_ry + 2)
            if dx * dx + dy * dy <= 1.0:
                canvas.putpixel(x, y, OUTLINE)

    for y in range(wing_cy - wing_ry, wing_cy + wing_ry + 1):
        for x in range(wing_cx - wing_rx, wing_cx + wing_rx + 1):
            dx = (x - wing_cx) / wing_rx
            dy = (y - wing_cy) / wing_ry
            if dx * dx + dy * dy <= 1.0:
                col = DUCK_SHADOW if dy > 0.2 else (DUCK_HIGHLIGHT if dy < -0.3 else DUCK_BODY)
                canvas.putpixel(x, y, col)

    # --- 3. HEAD & NECK ---
    head_cx = 154 + (waddle_x // 2)
    head_cy = 94 + bob
    head_r = 38

    # Neck blend
    neck_cx = 144 + (waddle_x // 2)
    neck_cy = 124 + bob
    for y in range(neck_cy - 18, neck_cy + 18):
        for x in range(neck_cx - 26, neck_cx + 26):
            dx = (x - neck_cx) / 24.0
            dy = (y - neck_cy) / 16.0
            if dx * dx + dy * dy <= 1.0:
                canvas.putpixel(x, y, DUCK_BODY)

    # Head outline
    for y in range(head_cy - head_r - 3, head_cy + head_r + 4):
        for x in range(head_cx - head_r - 3, head_cx + head_r + 4):
            dx = (x - head_cx) / (head_r + 2.5)
            dy = (y - head_cy) / (head_r + 2.5)
            if dx * dx + dy * dy <= 1.0:
                canvas.putpixel(x, y, OUTLINE)

    # Head fill
    for y in range(head_cy - head_r, head_cy + head_r + 1):
        for x in range(head_cx - head_r, head_cx + head_r + 1):
            dx = (x - head_cx) / head_r
            dy = (y - head_cy) / head_r
            if dx * dx + dy * dy <= 1.0:
                if dy > 0.4:
                    col = DUCK_SHADOW
                elif dy < -0.2 and dx > -0.3:
                    col = DUCK_HIGHLIGHT
                else:
                    col = DUCK_BODY
                canvas.putpixel(x, y, col)

    # Head gloss streak
    for y in range(head_cy - 30, head_cy - 12):
        for x in range(head_cx - 24, head_cx + 6):
            dx = (x - (head_cx - 10)) / 14.0
            dy = (y - (head_cy - 21)) / 7.0
            if dx * dx + dy * dy <= 1.0:
                canvas.putpixel(x, y, DUCK_SHEEN)

    # Cheek blush
    for y in range(head_cy + 6, head_cy + 18):
        for x in range(head_cx - 6, head_cx + 10):
            dx = (x - (head_cx + 2)) / 7.0
            dy = (y - (head_cy + 12)) / 5.0
            if dx * dx + dy * dy <= 1.0:
                canvas.putpixel(x, y, CHEEK_BLUSH)

    # --- 4. BEAK ---
    beak_base_x = head_cx + 24
    beak_base_y = head_cy + 4
    beak_tip_x = head_cx + 74
    beak_tip_y = head_cy + 10

    for y in range(beak_base_y - 14, beak_base_y + 20):
        for x in range(beak_base_x - 4, beak_tip_x + 4):
            t = max(0.0, min(1.0, (x - beak_base_x) / 50.0))
            half_h = (1.0 - t * 0.45) * 14.0
            center_y = beak_base_y + t * 4.0
            if abs(y - center_y) <= half_h + 2.5:
                canvas.putpixel(x, y, OUTLINE)

    for y in range(beak_base_y - 12, beak_base_y + 18):
        for x in range(beak_base_x, beak_tip_x + 1):
            t = max(0.0, min(1.0, (x - beak_base_x) / 50.0))
            half_h = (1.0 - t * 0.45) * 13.0
            center_y = beak_base_y + t * 4.0
            if abs(y - center_y) <= half_h:
                if y < center_y - 2:
                    col = BEAK_LIGHT
                elif y > center_y + 3:
                    col = BEAK_SHADOW
                else:
                    col = BEAK_MAIN
                canvas.putpixel(x, y, col)

    for x in range(beak_base_x + 4, beak_tip_x - 2):
        t = (x - beak_base_x) / 50.0
        line_y = int(round(beak_base_y + t * 4.0 + 1.0))
        canvas.putpixel(x, line_y, BEAK_LINE)
        canvas.putpixel(x, line_y + 1, BEAK_LINE)

    # --- 5. EYE ---
    eye_cx = head_cx + 8
    eye_cy = head_cy - 7
    canvas.fill_circle(eye_cx, eye_cy, 11, OUTLINE)
    canvas.fill_circle(eye_cx, eye_cy, 9.5, EYE_BLACK)
    canvas.fill_circle(eye_cx - 3, eye_cy - 3, 4.0, EYE_WHITE)
    canvas.fill_circle(eye_cx + 4, eye_cy + 3, 1.8, EYE_WHITE)

    # --- 6. BIRTHDAY PARTY HAT & CONFETTI ---
    if birthday:
        hat_tip_x = head_cx - 4
        hat_tip_y = head_cy - head_r - 40

        # Pompom
        canvas.fill_circle(hat_tip_x, hat_tip_y, 7, HAT_POMPOM)

        # Cone
        for hy in range(int(hat_tip_y + 5), int(head_cy - head_r + 4)):
            t = (hy - hat_tip_y) / 45.0
            half_w = t * 24.0
            for hx in range(int(hat_tip_x - half_w), int(hat_tip_x + half_w + 1)):
                col = HAT_BASE if ((hy // 6) % 2 == 0) else HAT_STRIPE
                canvas.putpixel(hx, hy, col)

        # Floating Confetti
        confetti_positions = [
            (30, 45 + ((frame * 24) % 120), CONFETTI_COLORS[frame % 5]),
            (75, 30 + ((frame * 16) % 100), CONFETTI_COLORS[(frame + 1) % 5]),
            (130, 20 + ((frame * 32) % 80), CONFETTI_COLORS[(frame + 2) % 5]),
            (210, 35 + ((frame * 18) % 110), CONFETTI_COLORS[(frame + 3) % 5]),
            (230, 80 + ((frame * 24) % 100), CONFETTI_COLORS[(frame + 4) % 5]),
            (18, 110 + ((frame * 16) % 80), CONFETTI_COLORS[(frame + 1) % 5]),
            (225, 140 + ((frame * 20) % 70), CONFETTI_COLORS[(frame + 3) % 5])
        ]
        for cx, cy, ccol in confetti_positions:
            canvas.fill_rect(cx, cy, 6, 10, ccol)

    return canvas


def generate_all_duck_sprites():
    variants = ["idle", "water", "food", "sleep", "rest", "barca", "f1"]
    total = 0

    print("Generating native 256x256 Rubber Duck sprites...")

    # 1. Standard 5-frame variants
    for v in variants:
        for f in range(5):
            canvas = Canvas256()
            draw_rubber_duck(canvas, variant=v, frame=f)
            filename = f"duck_{v}_{f}.png"
            path = os.path.join(OUTPUT_DIR, filename)
            canvas.save_png(path)
            total += 1

    # 2. 8-frame walking waddle
    for f in range(8):
        canvas = Canvas256()
        draw_rubber_duck_walking(canvas, frame=f, birthday=False)
        filename = f"duck_walking_{f}.png"
        path = os.path.join(OUTPUT_DIR, filename)
        canvas.save_png(path)
        total += 1

    # 3. 8-frame birthday walking waddle
    for f in range(8):
        canvas = Canvas256()
        draw_rubber_duck_walking(canvas, frame=f, birthday=True)
        filename = f"duck_birthday_walk_{f}.png"
        path = os.path.join(OUTPUT_DIR, filename)
        canvas.save_png(path)
        total += 1

    print(f"Successfully generated {total} Duck sprite frames in {OUTPUT_DIR}")


if __name__ == "__main__":
    generate_all_duck_sprites()
