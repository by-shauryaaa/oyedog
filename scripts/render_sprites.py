import os
import struct
import zlib
import math

OUTPUT_DIR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "Assets", "Sprites")
os.makedirs(OUTPUT_DIR, exist_ok=True)

GRID_SIZE = 32
SCALE = 8 # 32 * 8 = 256x256 output

# Palette (R, G, B, A)
TRANSPARENT = (0, 0, 0, 0)
OUTLINE = (45, 30, 20, 255) # Dark brown outline
FUR_MAIN = (220, 160, 95, 255) # Warm golden/caramel dog fur
FUR_LIGHT = (245, 210, 165, 255) # Muzzle, belly, inner ears
FUR_SHADOW = (180, 120, 65, 255) # Underbody shading
EAR_COLOR = (170, 105, 55, 255) # Flop ears
EYE_COLOR = (30, 20, 15, 255)
EYE_SHINE = (255, 255, 255, 255)
NOSE_COLOR = (30, 20, 20, 255)
TONGUE = (240, 110, 130, 255)
COLLAR = (210, 50, 45, 255)
COLLAR_TAG = (255, 215, 0, 255)

# Barca colors
BARCA_BLUE = (0, 77, 152, 255)
BARCA_RED = (165, 0, 68, 255)
BARCA_GOLD = (237, 187, 0, 255)

# F1 colors
F1_RED = (230, 0, 0, 255)
F1_TIRE = (40, 40, 45, 255)
F1_RIM = (190, 190, 195, 255)

# Food / Water colors
BOWL_COLOR = (70, 160, 220, 255)
KIBBLE_COLOR = (140, 80, 40, 255)
WATER_COLOR = (90, 200, 250, 240)
WATER_DARK = (40, 140, 220, 255)

# Sleep colors
SLEEP_Z = (140, 170, 255, 255)

# Party / Birthday colors
HAT_BASE = (255, 75, 110, 255) # Bright pink/red party hat
HAT_STRIPE = (255, 220, 50, 255) # Yellow stripe
HAT_POMPOM = (50, 210, 255, 255) # Cyan pompom
CONFETTI_COLORS = [
    (255, 70, 85, 255),
    (255, 215, 0, 255),
    (50, 200, 255, 255),
    (150, 240, 60, 255),
    (210, 90, 255, 255)
]

class PixelCanvas:
    def __init__(self, width=GRID_SIZE, height=GRID_SIZE):
        self.width = width
        self.height = height
        self.pixels = [[TRANSPARENT for _ in range(width)] for _ in range(height)]

    def putpixel(self, xy, color):
        x, y = xy
        if 0 <= x < self.width and 0 <= y < self.height:
            self.pixels[y][x] = color

    def getpixel(self, xy):
        x, y = xy
        if 0 <= x < self.width and 0 <= y < self.height:
            return self.pixels[y][x]
        return TRANSPARENT

    def save_png(self, filepath, scale=SCALE):
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

def draw_dog(grid, variant="idle", frame=0):
    breath = 1 if frame in (1, 2) else 0
    tail_angle = [-1, 0, 1, 2, 0][frame % 5]
    blink = (frame == 3)
    
    is_sleeping = (variant == "sleep")
    is_stretch = (variant == "rest")
    is_food = (variant == "food")
    is_water = (variant == "water")
    is_barca = (variant == "barca")
    is_f1 = (variant == "f1")

    # 1. TAIL
    tail_x_base = 6
    tail_y_base = 23
    if not is_stretch:
        tail_coords = [
            (tail_x_base - 1, tail_y_base - 1 + tail_angle),
            (tail_x_base - 2, tail_y_base - 3 + tail_angle),
            (tail_x_base - 3, tail_y_base - 5 + tail_angle),
            (tail_x_base - 2, tail_y_base - 6 + tail_angle)
        ]
        for tx, ty in tail_coords:
            grid.putpixel((tx, ty), FUR_MAIN)
        grid.putpixel((tail_x_base - 2, tail_y_base - 6 + tail_angle), FUR_LIGHT)
    else:
        tail_coords = [
            (5, 17 - frame), (4, 15 - frame), (3, 13 - frame), (4, 12 - frame)
        ]
        for tx, ty in tail_coords:
            grid.putpixel((tx, ty), FUR_MAIN)

    # 2. BODY & LEGS
    if is_stretch:
        for y in range(16, 26):
            for x in range(6, 16):
                dist = (x-11)**2 / 16 + (y-21)**2 / 20
                if dist <= 1.0:
                    grid.putpixel((x, y), FUR_MAIN)
        for y in range(21, 27):
            for x in range(14, 25):
                dist = (x-19)**2 / 25 + (y-24)**2 / 8
                if dist <= 1.0:
                    grid.putpixel((x, y), FUR_MAIN)
        paw_reach = 22 + min(frame, 3)
        for x in range(18, paw_reach + 1):
            grid.putpixel((x, 26), FUR_LIGHT)
            grid.putpixel((x, 27), OUTLINE)
        grid.putpixel((7, 26), FUR_LIGHT)
        grid.putpixel((8, 26), FUR_LIGHT)
        grid.putpixel((7, 27), OUTLINE)
        grid.putpixel((8, 27), OUTLINE)
    else:
        body_top = 15 - breath
        body_bottom = 26
        for y in range(body_top, body_bottom + 1):
            for x in range(9, 23):
                dx = (x - 16) / 5.5
                dy = (y - 20) / 5.5
                if dx*dx + dy*dy <= 1.0:
                    color = FUR_MAIN
                    if y >= 24:
                        color = FUR_SHADOW
                    if 14 <= x <= 18 and 17 <= y <= 23:
                        color = FUR_LIGHT
                    grid.putpixel((x, y), color)

        if is_barca:
            for y in range(body_top + 1, 23):
                for x in range(11, 21):
                    dx = (x - 16) / 5.0
                    dy = (y - 19) / 4.5
                    if dx*dx + dy*dy <= 0.85:
                        stripe = (x % 3 == 0 or x % 3 == 1)
                        grid.putpixel((x, y), BARCA_BLUE if stripe else BARCA_RED)
            for x in range(13, 19):
                if grid.getpixel((x, body_top + 1)) != TRANSPARENT:
                    grid.putpixel((x, body_top + 1), BARCA_GOLD)

        if not is_barca:
            for x in range(12, 20):
                grid.putpixel((x, body_top + 1), COLLAR)
            grid.putpixel((15, body_top + 2), COLLAR_TAG)
            grid.putpixel((16, body_top + 2), COLLAR_TAG)

        for x in range(8, 12):
            grid.putpixel((x, 25), FUR_SHADOW)
            grid.putpixel((x, 26), FUR_LIGHT)
            grid.putpixel((x, 27), OUTLINE)
        for x in range(20, 24):
            grid.putpixel((x, 25), FUR_SHADOW)
            grid.putpixel((x, 26), FUR_LIGHT)
            grid.putpixel((x, 27), OUTLINE)
        for x in range(13, 15):
            grid.putpixel((x, 25), FUR_MAIN)
            grid.putpixel((x, 26), FUR_LIGHT)
            grid.putpixel((x, 27), OUTLINE)
        for x in range(17, 19):
            grid.putpixel((x, 25), FUR_MAIN)
            grid.putpixel((x, 26), FUR_LIGHT)
            grid.putpixel((x, 27), OUTLINE)

    # 3. HEAD & EARS
    head_cx = 16
    head_cy = 10 - breath if not is_stretch else 15
    head_rad_x = 7
    head_rad_y = 6

    for y in range(head_cy - head_rad_y, head_cy + head_rad_y + 1):
        for x in range(head_cx - head_rad_x, head_cx + head_rad_x + 1):
            dx = (x - head_cx) / head_rad_x
            dy = (y - head_cy) / head_rad_y
            if dx*dx + dy*dy <= 1.0:
                grid.putpixel((x, y), FUR_MAIN)

    ear_bounce = 1 if frame in (2, 3) else 0
    for y in range(head_cy - 4, head_cy + 4 + ear_bounce):
        for x in range(head_cx - 8, head_cx - 4):
            grid.putpixel((x, y), EAR_COLOR)
    for y in range(head_cy - 4, head_cy + 4 + ear_bounce):
        for x in range(head_cx + 5, head_cx + 9):
            grid.putpixel((x, y), EAR_COLOR)

    muzzle_y = head_cy + 1
    for y in range(muzzle_y, muzzle_y + 4):
        for x in range(head_cx - 4, head_cx + 5):
            dx = (x - head_cx) / 3.8
            dy = (y - (muzzle_y + 1.5)) / 2.0
            if dx*dx + dy*dy <= 1.0:
                grid.putpixel((x, y), FUR_LIGHT)

    grid.putpixel((head_cx, muzzle_y + 1), NOSE_COLOR)
    grid.putpixel((head_cx - 1, muzzle_y + 1), NOSE_COLOR)
    grid.putpixel((head_cx, muzzle_y + 2), OUTLINE)

    if is_sleeping:
        eye_y = head_cy
        for x in (head_cx - 4, head_cx - 3, head_cx - 2):
            grid.putpixel((x, eye_y), EYE_COLOR)
        for x in (head_cx + 2, head_cx + 3, head_cx + 4):
            grid.putpixel((x, eye_y), EYE_COLOR)
    elif blink:
        eye_y = head_cy
        grid.putpixel((head_cx - 4, eye_y), EYE_COLOR)
        grid.putpixel((head_cx - 3, eye_y), EYE_COLOR)
        grid.putpixel((head_cx + 3, eye_y), EYE_COLOR)
        grid.putpixel((head_cx + 4, eye_y), EYE_COLOR)
    else:
        eye_y = head_cy - 1
        grid.putpixel((head_cx - 4, eye_y), EYE_COLOR)
        grid.putpixel((head_cx - 3, eye_y), EYE_COLOR)
        grid.putpixel((head_cx - 4, eye_y + 1), EYE_COLOR)
        grid.putpixel((head_cx - 3, eye_y + 1), EYE_COLOR)
        grid.putpixel((head_cx - 4, eye_y), EYE_SHINE)

        grid.putpixel((head_cx + 3, eye_y), EYE_COLOR)
        grid.putpixel((head_cx + 4, eye_y), EYE_COLOR)
        grid.putpixel((head_cx + 3, eye_y + 1), EYE_COLOR)
        grid.putpixel((head_cx + 4, eye_y + 1), EYE_COLOR)
        grid.putpixel((head_cx + 3, eye_y), EYE_SHINE)

    if frame in (1, 2) and not is_sleeping and not is_food:
        grid.putpixel((head_cx, muzzle_y + 3), TONGUE)
        grid.putpixel((head_cx, muzzle_y + 4), TONGUE)

    # 4. VARIANT-SPECIFIC PROPS
    if is_water:
        for y in range(22, 27):
            for x in range(22, 29):
                grid.putpixel((x, y), BOWL_COLOR)
        for x in range(23, 28):
            grid.putpixel((x, 22), WATER_COLOR)
        sip_y = 19 - (frame % 3)
        grid.putpixel((21, sip_y), WATER_COLOR)
        grid.putpixel((20, sip_y - 1), WATER_DARK)
        if frame in (2, 3):
            grid.putpixel((head_cx, muzzle_y + 3), WATER_COLOR)

    if is_food:
        for y in range(22, 27):
            for x in range(22, 30):
                grid.putpixel((x, y), (200, 70, 50, 255))
        for x in range(23, 29):
            grid.putpixel((x, 22), KIBBLE_COLOR)
        grid.putpixel((24, 21), KIBBLE_COLOR)
        grid.putpixel((26, 21), KIBBLE_COLOR)
        grid.putpixel((27, 21), (170, 100, 50, 255))
        if frame in (1, 2):
            grid.putpixel((21, 19), KIBBLE_COLOR)
        if frame in (3, 4):
            grid.putpixel((head_cx, muzzle_y + 3), TONGUE)

    if is_sleeping:
        z1_x, z1_y = 22 + (frame % 2), 12 - frame
        if 0 <= z1_y < 32:
            grid.putpixel((z1_x, z1_y), SLEEP_Z)
            grid.putpixel((z1_x+1, z1_y), SLEEP_Z)
            grid.putpixel((z1_x, z1_y+1), SLEEP_Z)
            grid.putpixel((z1_x-1, z1_y+2), SLEEP_Z)
            grid.putpixel((z1_x, z1_y+2), SLEEP_Z)
        z2_x, z2_y = 25, 6 - (frame // 2)
        if 0 <= z2_y < 32:
            for dx in range(3):
                grid.putpixel((z2_x + dx, z2_y), SLEEP_Z)
                grid.putpixel((z2_x + dx, z2_y + 3), SLEEP_Z)
            grid.putpixel((z2_x + 1, z2_y + 1), SLEEP_Z)
            grid.putpixel((z2_x, z2_y + 2), SLEEP_Z)

    if is_f1:
        wheel_y = 20
        for x in range(21, 28):
            grid.putpixel((x, wheel_y), F1_TIRE)
            grid.putpixel((x, wheel_y + 3), F1_TIRE)
        grid.putpixel((21, wheel_y + 1), F1_TIRE)
        grid.putpixel((21, wheel_y + 2), F1_TIRE)
        grid.putpixel((27, wheel_y + 1), F1_TIRE)
        grid.putpixel((27, wheel_y + 2), F1_TIRE)
        grid.putpixel((24, wheel_y + 1), F1_RED)
        led_color = [(0, 255, 0, 255), (255, 255, 0, 255), (255, 0, 0, 255), (0, 150, 255, 255), (255, 0, 255, 255)][frame]
        grid.putpixel((23 + (frame % 3), wheel_y - 1), led_color)
        grid.putpixel((24, wheel_y + 2), F1_RIM)

    if is_barca:
        ball_x, ball_y = 23, 24
        ball_colors = [(240, 240, 240, 255), (40, 40, 40, 255)]
        for y in range(ball_y - 2, ball_y + 3):
            for x in range(ball_x - 2, ball_x + 3):
                if (x-ball_x)**2 + (y-ball_y)**2 <= 4:
                    c = ball_colors[(x + y + frame) % 2]
                    grid.putpixel((x, y), c)

    return grid

def draw_dog_walking(grid, frame=0, birthday=False):
    """
    8-frame bilateral walking cycle facing right.
    Leg strides alternate smoothly, body bobs up/down, ears and tail swing.
    If birthday=True, draws party hat on head and animated confetti around.
    """
    # 8-step cycle
    # Body bob: bobs up on frames 1, 2 and 5, 6
    bob = 1 if frame in (1, 2, 5, 6) else 0
    # Stride offsets for front & back legs
    # Front-left, Front-right, Rear-left, Rear-right
    stride_offsets = [
        # (FL_x, FR_x, RL_x, RR_x, FL_lift, FR_lift)
        (0, 0, 0, 0, 0, 0),       # frame 0: neutral
        (2, -2, -2, 2, 1, 0),      # frame 1: right forward, left back (passing)
        (3, -3, -3, 3, 0, 0),      # frame 2: full stride extension
        (1, -1, -1, 1, 0, 1),      # frame 3: pushing off
        (0, 0, 0, 0, 0, 0),       # frame 4: neutral
        (-2, 2, 2, -2, 0, 1),      # frame 5: left forward, right back (passing)
        (-3, 3, 3, -3, 0, 0),      # frame 6: full stride extension
        (-1, 1, 1, -1, 1, 0)       # frame 7: pushing off
    ]
    fl_x, fr_x, rl_x, rr_x, fl_lift, fr_lift = stride_offsets[frame % 8]

    # 1. TAIL (happy wagging in sync with walk)
    tail_swing = [-2, -1, 0, 1, 2, 1, 0, -1][frame % 8]
    tail_coords = [
        (6, 21 - bob + tail_swing),
        (5, 19 - bob + tail_swing),
        (4, 17 - bob + tail_swing),
        (5, 16 - bob + tail_swing)
    ]
    for tx, ty in tail_coords:
        if 0 <= tx < 32 and 0 <= ty < 32:
            grid.putpixel((tx, ty), FUR_MAIN)
    grid.putpixel((5, 16 - bob + tail_swing), FUR_LIGHT)

    # 2. BODY (horizontal walking torso)
    body_y_top = 14 - bob
    body_y_bottom = 23 - bob
    for y in range(body_y_top, body_y_bottom + 1):
        for x in range(8, 23):
            dx = (x - 15.5) / 6.5
            dy = (y - (18.5 - bob)) / 4.0
            if dx*dx + dy*dy <= 1.0:
                col = FUR_MAIN
                if y >= body_y_bottom - 1:
                    col = FUR_SHADOW
                if 12 <= x <= 18 and y >= 17 - bob:
                    col = FUR_LIGHT
                grid.putpixel((x, y), col)

    # Collar
    for y in range(body_y_top + 1, body_y_top + 4):
        grid.putpixel((20, y), COLLAR)
    grid.putpixel((21, body_y_top + 2), COLLAR_TAG)

    # 3. LEGS & PAWS (4 animated limbs)
    # Rear Left (far side)
    rl_pos_x = 10 + rl_x
    for y in range(21 - bob, 26):
        grid.putpixel((rl_pos_x, y), FUR_SHADOW)
    grid.putpixel((rl_pos_x, 26), OUTLINE)

    # Front Left (far side)
    fl_pos_x = 19 + fl_x
    fl_y_end = 26 - fl_lift
    for y in range(21 - bob, fl_y_end):
        grid.putpixel((fl_pos_x, y), FUR_SHADOW)
    grid.putpixel((fl_pos_x, fl_y_end), OUTLINE)

    # Rear Right (near side)
    rr_pos_x = 12 + rr_x
    for y in range(21 - bob, 26):
        grid.putpixel((rr_pos_x, y), FUR_MAIN)
    grid.putpixel((rr_pos_x, 26), FUR_LIGHT)
    grid.putpixel((rr_pos_x, 27), OUTLINE)

    # Front Right (near side)
    fr_pos_x = 21 + fr_x
    fr_y_end = 26 - fr_lift
    for y in range(21 - bob, fr_y_end):
        grid.putpixel((fr_pos_x, y), FUR_MAIN)
    grid.putpixel((fr_pos_x, fr_y_end), FUR_LIGHT)
    grid.putpixel((fr_pos_x, fr_y_end + 1), OUTLINE)

    # 4. HEAD & EARS (Facing right towards direction of travel)
    head_cx = 22
    head_cy = 10 - bob
    head_rx = 6
    head_ry = 5

    for y in range(head_cy - head_ry, head_cy + head_ry + 1):
        for x in range(head_cx - head_rx, head_cx + head_rx + 1):
            dx = (x - head_cx) / head_rx
            dy = (y - head_cy) / head_ry
            if dx*dx + dy*dy <= 1.0:
                grid.putpixel((x, y), FUR_MAIN)

    # Floppy ear (swings backward as dog walks forward)
    ear_swing = [-1, 0, 1, 2, 1, 0, -1, -2][frame % 8]
    for y in range(head_cy - 3, head_cy + 4):
        for x in range(head_cx - 6 + ear_swing, head_cx - 2 + ear_swing):
            if 0 <= x < 32:
                grid.putpixel((x, y), EAR_COLOR)

    # Muzzle (facing right)
    for y in range(head_cy, head_cy + 4):
        for x in range(head_cx + 1, head_cx + 7):
            grid.putpixel((x, y), FUR_LIGHT)

    # Nose (at right tip)
    grid.putpixel((head_cx + 6, head_cy + 1), NOSE_COLOR)
    grid.putpixel((head_cx + 6, head_cy + 2), OUTLINE)

    # Eye (profile/3-quarter facing right)
    eye_x = head_cx + 2
    eye_y = head_cy - 1
    grid.putpixel((eye_x, eye_y), EYE_COLOR)
    grid.putpixel((eye_x + 1, eye_y), EYE_COLOR)
    grid.putpixel((eye_x, eye_y + 1), EYE_COLOR)
    grid.putpixel((eye_x + 1, eye_y + 1), EYE_COLOR)
    grid.putpixel((eye_x, eye_y), EYE_SHINE) # Sparkle!

    # Cute happy open mouth / panting tongue
    if frame in (1, 2, 3, 5, 6, 7):
        grid.putpixel((head_cx + 4, head_cy + 3), TONGUE)
        grid.putpixel((head_cx + 5, head_cy + 4), TONGUE)

    # 5. BIRTHDAY PARTY HAT & CONFETTI OVERLAY
    if birthday:
        # Party hat cone on head
        hat_tip_x = head_cx - 1
        hat_tip_y = head_cy - head_ry - 5
        
        # Pompom at tip
        if 0 <= hat_tip_y < 32:
            grid.putpixel((hat_tip_x, hat_tip_y), HAT_POMPOM)
            grid.putpixel((hat_tip_x + 1, hat_tip_y), HAT_POMPOM)
        
        # Hat cone
        for hy in range(hat_tip_y + 1, head_cy - head_ry + 1):
            if 0 <= hy < 32:
                row_w = (hy - hat_tip_y)
                for hx in range(hat_tip_x - row_w // 2, hat_tip_x + row_w // 2 + 1):
                    if 0 <= hx < 32:
                        col = HAT_BASE if (hy % 2 == 0) else HAT_STRIPE
                        grid.putpixel((hx, hy), col)

        # Floating Confetti particles around the dog
        confetti_positions = [
            (4, 5 + ((frame * 3) % 15), CONFETTI_COLORS[frame % 5]),
            (10, 3 + ((frame * 2) % 12), CONFETTI_COLORS[(frame + 1) % 5]),
            (17, 2 + ((frame * 4) % 10), CONFETTI_COLORS[(frame + 2) % 5]),
            (26, 4 + ((frame * 2) % 14), CONFETTI_COLORS[(frame + 3) % 5]),
            (29, 9 + ((frame * 3) % 12), CONFETTI_COLORS[(frame + 4) % 5]),
            (2, 14 + ((frame * 2) % 10), CONFETTI_COLORS[(frame + 1) % 5]),
            (28, 18 + ((frame * 3) % 8), CONFETTI_COLORS[(frame + 3) % 5])
        ]
        for cx, cy, ccol in confetti_positions:
            if 0 <= cx < 32 and 0 <= cy < 32:
                grid.putpixel((cx, cy), ccol)
                grid.putpixel((cx + 1, cy), ccol)

    return grid

def generate_all_sprites():
    variants = ["idle", "water", "food", "sleep", "rest", "barca", "f1"]
    total = 0
    
    # 1. 5-frame standard variants
    for v in variants:
        for f in range(5):
            canvas = PixelCanvas()
            draw_dog(canvas, variant=v, frame=f)
            filename = f"{v}_{f}.png"
            path = os.path.join(OUTPUT_DIR, filename)
            canvas.save_png(path, scale=SCALE)
            total += 1

    # 2. 8-frame walking variant
    for f in range(8):
        canvas = PixelCanvas()
        draw_dog_walking(canvas, frame=f, birthday=False)
        filename = f"walking_{f}.png"
        path = os.path.join(OUTPUT_DIR, filename)
        canvas.save_png(path, scale=SCALE)
        total += 1

    # 3. 8-frame birthday walk variant (with party hat & confetti)
    for f in range(8):
        canvas = PixelCanvas()
        draw_dog_walking(canvas, frame=f, birthday=True)
        filename = f"birthday_walk_{f}.png"
        path = os.path.join(OUTPUT_DIR, filename)
        canvas.save_png(path, scale=SCALE)
        total += 1

    print(f"Successfully generated {total} sprite frames in {OUTPUT_DIR}")

if __name__ == "__main__":
    generate_all_sprites()
