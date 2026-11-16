# Gun Gale Online: desert terrain, ruins, cover, vehicles, lobby hall and props.
exec(open(r"C:/Users/yachi/SwordArt/Tools/Blender/sao_lib.py").read())


def done(root, parts, rel):
    join(parts, root.name + "_Mesh", root)
    print(export(root, rel))


def terrain_height(x, y):
    # Blender coords before the 180-degree turn: Unity (ux, uz) = (-x, -y).
    ux, uz = -x, -y
    h = 0.6 * math.sin(ux * 0.045) * math.cos(uz * 0.05) + 0.4 * math.sin((ux + uz) * 0.09)
    hills = [(-55, -50, 9, 22), (40, 55, 6, 18), (-75, 30, 5, 16), (70, -60, 7, 20)]
    for hx, hz, hh, hr in hills:
        d = math.hypot(ux - hx, uz - hz)
        if d < hr:
            h += hh * (0.5 + 0.5 * math.cos(math.pi * d / hr))
    # flatten the town centre and the outpost
    for fx, fz, fr in ((0, 0, 28), (0, 62, 16), (65, -8, 18)):
        d = math.hypot(ux - fx, uz - fz)
        if d < fr:
            h *= (d / fr) ** 2
    # boundary berm
    edge = max(abs(ux), abs(uz))
    if edge > 112:
        h += (edge - 112) * 1.4
    return h


def terrain():
    clear(); r = empty("GGO_Terrain")
    g = plane_grid("Ground", 260, 260, 130, 130, terrain_height, "M_Sand")
    set_parent(g, r)
    print(export(r, "Environment/GGO_Terrain"))


def ruin_a():
    clear(); r = empty("Ruin_A")
    W, D, H, t = 10.0, 8.0, 3.2, 0.35
    m = "M_ConcreteRuin"
    p = [box("Floor", (W, D, 0.2), (0, 0, 0.1), "M_ConcreteDark"),
         # back wall with window holes
         box("BackL", (3.5, t, H), (-3.25, D / 2, H / 2), m), box("BackR", (3.5, t, H), (3.25, D / 2, H / 2), m),
         box("BackMidLow", (3.0, t, 1.0), (0, D / 2, 0.5), m), box("BackMidHigh", (3.0, t, 0.8), (0, D / 2, H - 0.4), m),
         # left wall (partly collapsed)
         box("Left1", (t, 5.0, H), (-W / 2, 1.5, H / 2), m), box("Left2", (t, 3.0, 1.2), (-W / 2, -2.5, 0.6), m),
         # right wall with door
         box("Right1", (t, 3.0, H), (W / 2, 2.5, H / 2), m), box("Right2", (t, 3.6, H), (W / 2, -2.2, H / 2), m),
         box("RightTop", (t, 1.4, 1.0), (W / 2, 0.3, H - 0.5), m),
         # front wall: two segments, big breach
         box("Front1", (3.0, t, H), (-3.5, -D / 2, H / 2), m), box("Front2", (2.0, t, 1.4), (3.0, -D / 2, 0.7), m),
         # upper floor slab (broken) and upper walls
         box("Slab", (6.0, D, 0.3), (-2.0, 0, H + 0.15), "M_ConcreteDark", rot=(0, 3, 0)),
         box("UpperBack", (5.5, t, 2.2), (-2.2, D / 2, H + 1.4), m),
         box("UpperLeft", (t, 4.0, 1.6), (-W / 2, 2.0, H + 1.1), m)]
    rnd = random.Random(4)
    for i in range(10):
        p.append(ico("Rubble%d" % i, rnd.uniform(0.25, 0.6), (rnd.uniform(-6, 6), rnd.uniform(-5.5, 5.5), 0.1), m, subdiv=1, jitter=0.3, seed=i,
                     scale=(1, 1, 0.6)))
    for i in range(6):
        p.append(cyl("Rebar%d" % i, 0.02, 1.2, (-1 + i * 0.6, D / 2, H + 2.4), "M_RustMetal", rot=(rnd.uniform(-20, 20), rnd.uniform(-20, 20), 0), segs=5))
    done(r, p, "Environment/Ruin_A")


def ruin_b():
    clear(); r = empty("Ruin_B")
    m = "M_ConcreteRuin"
    p = [box("Wall", (8.0, 0.4, 6.5), (0, 0, 3.25), m),
         box("Hole1", (2.0, 0.5, 1.6), (-1.5, 0, 4.0), "M_ConcreteDark"),
         box("Pillar1", (0.6, 0.6, 7.2), (-4.2, 0, 3.6), m), box("Pillar2", (0.6, 0.6, 5.0), (4.2, 0, 2.5), m),
         box("Beam", (6.0, 0.5, 0.5), (1.0, -0.4, 5.6), "M_ConcreteDark", rot=(0, -8, 0)),
         box("SideWall", (0.4, 5.0, 2.4), (-4.2, -2.7, 1.2), m),
         box("Low", (3.0, 0.4, 1.2), (2.0, -4.5, 0.6), m)]
    rnd = random.Random(8)
    for i in range(12):
        p.append(ico("Rubble%d" % i, rnd.uniform(0.3, 0.8), (rnd.uniform(-5, 5), rnd.uniform(-5, 1), 0.15), m, subdiv=1, jitter=0.3, seed=20 + i, scale=(1, 1, 0.5)))
    done(r, p, "Environment/Ruin_B")


def ruin_c():
    clear(); r = empty("Ruin_C")
    m = "M_ConcreteRuin"
    p = [box("WallA", (7.0, 0.4, 2.6), (0, 0, 1.3), m), box("WallB", (0.4, 6.0, 3.4), (3.3, 3.0, 1.7), m),
         box("WallC", (2.5, 0.4, 1.4), (-2.0, 6.0, 0.7), m),
         box("Stairs1", (1.5, 1.0, 0.4), (1.8, 4.0, 0.2), "M_ConcreteDark"),
         box("Stairs2", (1.5, 1.0, 0.8), (1.8, 5.0, 0.4), "M_ConcreteDark"),
         box("Stairs3", (1.5, 1.0, 1.2), (1.8, 6.0, 0.6), "M_ConcreteDark"),
         box("Ledge", (3.0, 3.0, 0.3), (1.8, 8.0, 1.35), "M_ConcreteDark"),
         box("LedgeWall", (3.0, 0.3, 1.0), (1.8, 9.4, 2.0), m)]
    done(r, p, "Environment/Ruin_C")


def container(name, mat):
    clear(); r = empty(name)
    L, W, H = 6.1, 2.44, 2.6
    p = [box("Body", (W, L, H), (0, 0, H / 2), mat)]
    for i in range(14):
        y = -L / 2 + 0.25 + i * (L - 0.5) / 13
        for sx in (-1, 1):
            p.append(box("Rib", (0.06, 0.12, H - 0.2), (sx * (W / 2 + 0.03), y, H / 2), mat))
    for sx in (-1, 1):
        for sy in (-1, 1):
            p.append(box("Corner", (0.18, 0.18, H), (sx * (W / 2 - 0.05), sy * (L / 2 - 0.05), H / 2), "M_RustMetal"))
    p.append(box("DoorBars", (2.0, 0.08, 2.2), (0, -L / 2 - 0.04, H / 2), "M_RustMetal"))
    done(r, p, "Environment/" + name)


def crates():
    clear(); r = empty("Crate")
    p = [box("Body", (1.2, 1.2, 1.2), (0, 0, 0.6), "M_MilitaryGreen")]
    for z in (0.05, 1.15):
        p.append(box("BandH", (1.24, 1.24, 0.1), (0, 0, z), "M_Gear"))
    for sx in (-1, 1):
        p.append(box("BandV", (0.1, 1.24, 1.2), (sx * 0.55, 0, 0.6), "M_Gear"))
    p.append(box("Stencil", (0.6, 0.02, 0.25), (0, -0.61, 0.7), "M_HazardYellow"))
    done(r, p, "Environment/Crate")
    clear(); r = empty("CrateStack")
    p = [box("A", (1.2, 1.2, 1.2), (-0.65, 0, 0.6), "M_MilitaryGreen"), box("B", (1.2, 1.2, 1.2), (0.65, 0.1, 0.6), "M_MilitaryGreen"),
         box("C", (1.2, 1.2, 1.2), (0, 0.05, 1.8), "M_MilitaryGreen", rot=(0, 0, 12)),
         box("Ammo", (0.8, 0.4, 0.35), (1.6, -0.5, 0.175), "M_GunMetal"), box("AmmoGlow", (0.5, 0.02, 0.06), (1.6, -0.71, 0.25), "M_GlowOrange")]
    done(r, p, "Environment/CrateStack")


def barrier():
    clear(); r = empty("Barrier")
    p = [box("Base", (3.0, 0.8, 0.35), (0, 0, 0.175), "M_Concrete"),
         box("Mid", (3.0, 0.55, 0.45), (0, 0, 0.55), "M_Concrete", taper=(1.0, 0.55)),
         box("Top", (3.0, 0.3, 0.3), (0, 0, 0.92), "M_Concrete"),
         box("Stripe", (3.02, 0.82, 0.08), (0, 0, 0.3), "M_HazardYellow")]
    done(r, p, "Environment/Barrier")


def sandbags():
    clear(); r = empty("SandbagWall")
    p = []
    for row in range(3):
        n = 6 - (row % 2)
        for i in range(n):
            x = -1.5 + i * 0.6 + (0.3 if row % 2 else 0)
            a = (x / 3.0) * 25
            y = (x * x) * 0.08
            p.append(box("Bag%d%d" % (row, i), (0.6, 0.38, 0.22), (x, y, 0.11 + row * 0.22), "M_Sandbag", rot=(0, 0, a)))
    done(r, p, "Environment/SandbagWall")


def rocks():
    for i, (s, sc) in enumerate(((2.0, (1.4, 1.0, 0.8)), (3.2, (1.0, 1.3, 1.1)), (1.3, (1.2, 1.2, 0.7)))):
        clear(); r = empty("Rock_%s" % "ABC"[i])
        p = [ico("Rock", s, (0, 0, s * sc[2] * 0.6), "M_Rock", subdiv=2, jitter=0.18, seed=40 + i, scale=sc),
             ico("Rock2", s * 0.55, (s * 0.9, s * 0.3, s * 0.3), "M_Rock", subdiv=1, jitter=0.25, seed=50 + i)]
        done(r, p, "Environment/Rock_%s" % "ABC"[i])


def wrecked_tank():
    clear(); r = empty("WreckedTank")
    m = "M_RustMetal"
    p = [box("Hull", (3.4, 6.5, 1.4), (0, 0, 1.0), m, taper=(0.85, 0.9)),
         box("TrackL", (0.7, 6.8, 1.1), (1.9, 0, 0.55), "M_Gear"), box("TrackR", (0.7, 6.0, 1.0), (-1.9, 0.3, 0.5), "M_Gear", rot=(0, 6, 0)),
         cyl("Turret", 1.4, 0.9, (0.2, 0.6, 2.15), m, segs=12, r2=1.1),
         cyl("Barrel", 0.13, 4.2, (0.6, -2.4, 2.0), m, rot=(78, 0, 12), segs=8),
         box("Hatch", (0.8, 0.8, 0.15), (0.5, 1.0, 2.7), m, rot=(30, 0, 0))]
    done(r, p, "Environment/WreckedTank")


def platform():
    clear(); r = empty("Platform_Tower")
    H = 4.0
    p = [box("Deck", (5.0, 5.0, 0.25), (0, 0, H), "M_RustMetal")]
    for sx in (-1, 1):
        for sy in (-1, 1):
            p.append(box("Leg", (0.3, 0.3, H), (sx * 2.3, sy * 2.3, H / 2), "M_MetalDark"))
    for sx in (-1, 1):
        p.append(box("RailX", (0.08, 5.0, 1.0), (sx * 2.46, 0, H + 0.6), "M_MetalDark"))
    p.append(box("RailBack", (5.0, 0.08, 1.0), (0, 2.46, H + 0.6), "M_MetalDark"))
    p.append(box("Cover", (4.0, 0.15, 1.1), (0, -2.42, H + 0.68), "M_MilitaryGreen"))
    L = H / math.tan(math.radians(26))
    p.append(box("Ramp", (1.6, math.hypot(L, H), 0.15), (1.6, -2.5 - L / 2, H / 2), "M_RustMetal", rot=(-26, 0, 0)))
    p.append(box("Roof", (5.4, 5.4, 0.15), (0, 0, H + 2.6), "M_MilitaryGreen"))
    for sx in (-1, 1):
        for sy in (-1, 1):
            p.append(box("RoofPost", (0.12, 0.12, 2.5), (sx * 2.3, sy * 2.3, H + 1.3), "M_MetalDark"))
    p.append(box("Light", (0.6, 0.3, 0.15), (0, -2.6, H + 2.45), "M_GlowOrange"))
    done(r, p, "Environment/Platform_Tower")


def antenna():
    clear(); r = empty("AntennaMast")
    p = []
    H = 18.0
    for sx in (-1, 1):
        for sy in (-1, 1):
            p.append(box("Leg", (0.15, 0.15, H), (sx * 0.8, sy * 0.8, H / 2), "M_RustMetal", taper=(0.3, 0.3)))
    for i in range(8):
        z = 1 + i * 2.2
        s = 1.7 * (1 - z / H * 0.7)
        p.append(box("Ring%d" % i, (s, s, 0.1), (0, 0, z), "M_RustMetal"))
    p.append(cyl("Dish", 1.2, 0.3, (0.8, 0, H * 0.75), "M_Metal", rot=(0, 70, 0), segs=16, r2=0.3))
    p.append(sphere("Beacon", 0.2, (0, 0, H + 0.3), "M_GlowOrange"))
    done(r, p, "Environment/AntennaMast")


def girders():
    clear(); r = empty("Girders")
    p = [box("G1", (0.4, 8.0, 0.6), (0, 0, 0.3), "M_RustMetal", rot=(0, 0, 10)),
         box("G2", (0.4, 7.0, 0.6), (1.0, 0.5, 0.9), "M_RustMetal", rot=(8, 0, -15)),
         box("G3", (0.4, 6.0, 0.6), (-0.8, -0.5, 0.3), "M_RustMetal", rot=(0, 0, 40)),
         cyl("Pipe", 0.45, 6.0, (2.5, 0, 0.45), "M_MetalDark", rot=(90, 0, 15), segs=12)]
    done(r, p, "Environment/Girders")


def lobby():
    clear(); r = empty("Lobby_Hall")
    W, D, H = 30.0, 22.0, 8.0
    p = [box("Floor", (W, D, 0.3), (0, 0, -0.15), "M_LobbyFloor"),
         box("Ceiling", (W, D, 0.3), (0, 0, H + 0.15), "M_LobbyPanel"),
         box("WallN", (W, 0.4, H), (0, D / 2 + 0.2, H / 2), "M_LobbyPanel"),
         box("WallS", (W, 0.4, H), (0, -D / 2 - 0.2, H / 2), "M_LobbyPanel"),
         box("WallE", (0.4, D, H), (W / 2 + 0.2, 0, H / 2), "M_LobbyPanel"),
         box("WallW", (0.4, D, H), (-W / 2 - 0.2, 0, H / 2), "M_LobbyPanel")]
    for i in range(6):
        x = -W / 2 + 2.5 + i * 5
        p.append(box("CeilStrip%d" % i, (0.3, D - 2, 0.05), (x, 0, H - 0.03), "M_GlowCyan"))
        p.append(box("PillarN%d" % i, (0.6, 0.6, H), (x, D / 2 - 0.3, H / 2), "M_LobbyFloor"))
        p.append(box("PillarS%d" % i, (0.6, 0.6, H), (x, -D / 2 + 0.3, H / 2), "M_LobbyFloor"))
    for y in (-D / 2 + 0.05, D / 2 - 0.05):
        p.append(box("FloorStrip", (W - 1, 0.1, 0.05), (0, y * 0.92, 0.01), "M_GlowOrange"))
    # Banner on the north wall (faces -Y = into the hall after placement)
    p.append(box("Screen", (14.0, 0.1, 3.5), (0, D / 2 - 0.05, 4.8), "M_PlasticDark"))
    p.append(text("Banner", "BULLET OF BULLETS", 1.15, (0, D / 2 - 0.15, 5.3), "M_GlowOrange", depth=0.05))
    p.append(text("Banner2", "GUN GALE ONLINE  -  BATTLE ROYALE", 0.42, (0, D / 2 - 0.15, 3.85), "M_GlowCyan", depth=0.03))
    # Shooting range lane on the east side: back wall stripes
    p.append(box("RangeLine", (0.15, 10, 0.03), (6, -2, 0.01), "M_HazardYellow"))
    done(r, p, "Environment/Lobby_Hall")


def terminal():
    clear(); r = empty("Terminal")
    p = [box("Base", (0.8, 0.6, 0.15), (0, 0, 0.075), "M_LobbyFloor"),
         box("Pillar", (0.3, 0.3, 1.0), (0, 0.05, 0.65), "M_LobbyPanel"),
         box("Screen", (0.9, 0.08, 0.6), (0, 0.0, 1.35), "M_PlasticDark", rot=(-20, 0, 0)),
         box("ScreenGlow", (0.8, 0.02, 0.5), (0, -0.05, 1.36), "M_GlowCyan", rot=(-20, 0, 0)),
         box("Ring", (0.9, 0.7, 0.03), (0, 0, 0.16), "M_GlowOrange")]
    done(r, p, "Props/Terminal")


def target_dummy():
    clear(); r = empty("TargetDummy")
    p = [box("Stand", (0.5, 0.5, 0.1), (0, 0, 0.05), "M_MetalDark"),
         box("Pole", (0.08, 0.08, 1.0), (0, 0, 0.55), "M_MetalDark"),
         box("Body", (0.6, 0.08, 0.8), (0, 0, 1.3), "M_HazardYellow"),
         box("Head", (0.3, 0.08, 0.3), (0, 0, 1.9), "M_HazardYellow"),
         box("Ring", (0.3, 0.09, 0.3), (0, 0, 1.3), "M_GlowOrange")]
    done(r, p, "Props/TargetDummy")


for fn in (terrain, ruin_a, ruin_b, ruin_c, crates, barrier, sandbags, rocks, wrecked_tank, platform, antenna, girders, lobby,
           terminal, target_dummy):
    fn()
container("Container_Red", "M_ContainerRed")
container("Container_Blue", "M_ContainerBlue")
