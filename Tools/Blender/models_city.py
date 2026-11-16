# Real-world city: buildings (front faces -Y in Blender) and street / interior props.
exec(open(r"C:/Users/yachi/SwordArt/Tools/Blender/sao_lib.py").read())
import bmesh as _bm


def prism(name, w, d, h, loc, mat, parent=None):
    """Triangular prism along X (gable end fill): base width d (Y), height h (Z), length w (X)."""
    bm = _new_bm()
    pts = [(-d / 2, 0), (d / 2, 0), (0, h)]
    vs = []
    for sx in (-w / 2, w / 2):
        vs.append([bm.verts.new((sx, y, z)) for y, z in pts])
    bm.faces.new(vs[0][::-1])
    bm.faces.new(vs[1])
    for i in range(3):
        j = (i + 1) % 3
        bm.faces.new((vs[0][i], vs[0][j], vs[1][j], vs[1][i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    uv = bm.loops.layers.uv.active
    for f in bm.faces:
        for l in f.loops:
            l[uv].uv = (l.vert.co.x + l.vert.co.y, l.vert.co.z)
    return _mesh_obj(name, bm, mat, loc, (0, 0, 0), parent)


def finish(root, parts, rel):
    body = join(parts, root.name + "_Mesh", root)
    print(export(root, rel))


def apartment_building():
    clear()
    root = empty("Building_Apartment")
    W, D, FH, F = 16.0, 12.0, 3.0, 5
    H = FH * F
    p = [box("Body", (W, D, H), (0, 0, H / 2), "M_Plaster"),
         box("Base", (W + 0.2, D + 0.2, 0.6), (0, 0, 0.3), "M_ConcreteDark"),
         box("Parapet", (W + 0.3, D + 0.3, 0.5), (0, 0, H + 0.25), "M_ConcreteDark")]
    for f in range(1, F):
        p.append(box("Band%d" % f, (W + 0.1, D + 0.1, 0.15), (0, 0, FH * f), "M_PlasterWarm"))
    # Front: upper floors with balconies, ground floor with entrance.
    p += window_grid("F", root, -D / 2, -W / 2 + 0.5, W / 2 - 0.5, FH, F - 1, FH, 4, 1.8, 1.9, sill=0.4)
    for f in range(1, F):
        z = FH * f
        p.append(box("BalconySlab%d" % f, (W - 1, 1.3, 0.15), (0, -D / 2 - 0.65, z + 0.05), "M_ConcreteDark"))
        p.append(box("BalconyRail%d" % f, (W - 1, 0.06, 0.95), (0, -D / 2 - 1.27, z + 0.6), "M_PlasterWarm"))
        for k in range(-1, 2):
            p.append(box("BalconyDiv%d_%d" % (f, k), (0.08, 1.3, 1.1), (k * 3.75 + 1.875 * (1 if k >= 0 else -1) * 0, -D / 2 - 0.65, z + 0.65), "M_Trim"))
    for sx in (-1, 1):
        p += window_grid("G%d" % sx, root, -D / 2, sx * 3.0, sx * 7.5, 0, 1, FH, 2, 1.6, 1.6, sill=0.8)
    # Entrance
    p.append(box("DoorRecess", (3.2, 0.25, 2.8), (0, -D / 2 + 0.05, 1.4), "M_Trim"))
    p.append(box("DoorL", (1.0, 0.08, 2.3), (-0.52, -D / 2 - 0.05, 1.15), "M_Glass"))
    p.append(box("DoorR", (1.0, 0.08, 2.3), (0.52, -D / 2 - 0.05, 1.15), "M_Glass"))
    p.append(box("DoorFrame", (2.3, 0.12, 0.15), (0, -D / 2 - 0.06, 2.35), "M_Metal"))
    p.append(box("Canopy", (4.2, 1.8, 0.18), (0, -D / 2 - 0.9, 2.95), "M_ConcreteDark"))
    p.append(box("CanopyLight", (1.2, 0.4, 0.03), (0, -D / 2 - 0.9, 2.85), "M_LampGlow"))
    p.append(box("Steps", (4.0, 1.4, 0.15), (0, -D / 2 - 0.7, 0.075), "M_Concrete"))
    p.append(box("NamePlate", (1.8, 0.06, 0.5), (2.6, -D / 2 - 0.04, 2.3), "M_Metal"))
    p.append(text("NameText", "SAKURA HEIGHTS", 0.2, (2.6, -D / 2 - 0.08, 2.3), "M_Trim"))
    # Sides & back windows
    for sx in (-1, 1):
        p += window_grid_side("S%d" % sx, sx * W / 2, -D / 2 + 1, D / 2 - 1, 0, F, FH, 3, 1.4, 1.5, facing=sx)
    p += window_grid("B", root, D / 2, -W / 2 + 0.5, W / 2 - 0.5, 0, F, FH, 5, 1.2, 1.4, facing=1)
    # Rooftop water tank and AC units
    p.append(cyl("Tank", 1.0, 1.6, (4, 2, H + 1.6), "M_PlasticWhite", segs=16))
    for lx in (3.4, 4.6):
        for ly in (1.4, 2.6):
            p.append(box("TankLeg", (0.1, 0.1, 0.8), (lx, ly, H + 0.4), "M_MetalDark"))
    for i in range(3):
        p.append(box("AC%d" % i, (0.9, 0.35, 0.65), (-6 + i * 1.2, -D / 2 - 1.0, FH + 0.5), "M_PlasticWhite"))
    finish(root, p, "Buildings/Building_Apartment")


def shop_building():
    clear()
    root = empty("Building_Shop")
    W, D, H = 14.0, 10.0, 4.5
    t = 0.3
    p = [box("Floor", (W, D, 0.1), (0, 0, 0.05), "M_Tile"),
         box("BackWall", (W, t, H), (0, D / 2 - t / 2, H / 2), "M_PlasterBlue"),
         box("SideL", (t, D, H), (-W / 2 + t / 2, 0, H / 2), "M_PlasterBlue"),
         box("SideR", (t, D, H), (W / 2 - t / 2, 0, H / 2), "M_PlasterBlue"),
         box("Roof", (W + 0.4, D + 0.4, 0.4), (0, 0, H + 0.2), "M_ConcreteDark"),
         box("Ceiling", (W - 0.6, D - 0.6, 0.05), (0, 0, H - 0.03), "M_WallInterior"),
         box("Header", (W, t, 1.2), (0, -D / 2 + t / 2, 3.9), "M_PlasterBlue")]
    door = 1.25
    for sx in (-1, 1):
        xa, xb = sx * door, sx * (W / 2 - t)
        cx, w = (xa + xb) / 2, abs(xb - xa)
        p.append(box("Kick%d" % sx, (w, t, 0.6), (cx, -D / 2 + t / 2, 0.3), "M_PlasterBlue"))
        p.append(box("Glass%d" % sx, (w, 0.06, 2.7), (cx, -D / 2 + t / 2, 1.95), "M_GlassClear"))
        p.append(box("Mullion%d" % sx, (0.08, t + 0.04, 3.3), (sx * door, -D / 2 + t / 2, 1.65), "M_Metal"))
        p.append(box("MullionMid%d" % sx, (0.06, t + 0.02, 2.7), (cx, -D / 2 + t / 2, 1.95), "M_Metal"))
    p.append(box("Awning", (W + 0.6, 1.8, 0.12), (0, -D / 2 - 0.85, 3.2), "M_Awning", rot=(-12, 0, 0)))
    p.append(box("SignBoard", (9.5, 0.15, 0.9), (0, -D / 2 - 0.08, 3.95), "M_Trim"))
    p.append(text("SignText", "ARMS & GEAR 24", 0.55, (0, -D / 2 - 0.17, 3.93), "M_NeonBlue"))
    for i in range(4):
        p.append(box("CeilLight%d" % i, (2.2, 0.5, 0.04), (-4.5 + i * 3, 0.5, H - 0.07), "M_LampGlow"))
    p.append(box("DoorMat", (2.2, 1.2, 0.02), (0, -D / 2 + 1.0, 0.11), "M_Trim"))
    finish(root, p, "Buildings/Building_Shop")


def office_building():
    clear()
    root = empty("Building_Office")
    W, D, G, FH, F = 18.0, 14.0, 4.5, 3.5, 5
    t = 0.3
    top = G + 0.4 + FH * F
    p = [box("Floor", (W, D, 0.1), (0, 0, 0.05), "M_Tile"),
         box("BackWall", (W, t, G), (0, D / 2 - t / 2, G / 2), "M_WallInterior"),
         box("SideL", (t, D, G), (-W / 2 + t / 2, 0, G / 2), "M_ConcreteDark"),
         box("SideR", (t, D, G), (W / 2 - t / 2, 0, G / 2), "M_ConcreteDark"),
         box("Slab", (W, D, 0.4), (0, 0, G + 0.2), "M_ConcreteDark"),
         box("Header", (W, t, 1.3), (0, -D / 2 + t / 2, G - 0.65), "M_ConcreteDark"),
         box("Upper", (W, D, FH * F), (0, 0, G + 0.4 + FH * F / 2), "M_Glass"),
         box("Roof", (W + 0.4, D + 0.4, 0.6), (0, 0, top + 0.3), "M_ConcreteDark")]
    door = 1.5
    for sx in (-1, 1):
        xa, xb = sx * door, sx * (W / 2 - t)
        cx, w = (xa + xb) / 2, abs(xb - xa)
        p.append(box("Glass%d" % sx, (w, 0.06, G - 1.3), (cx, -D / 2 + t / 2, (G - 1.3) / 2), "M_GlassClear"))
        p.append(box("Mullion%d" % sx, (0.1, t + 0.04, G - 1.3), (sx * door, -D / 2 + t / 2, (G - 1.3) / 2), "M_Metal"))
        for k in range(1, 3):
            p.append(box("MullionM%d%d" % (sx, k), (0.06, t + 0.02, G - 1.3), (xa + sx * w * k / 3, -D / 2 + t / 2, (G - 1.3) / 2), "M_Metal"))
    for f in range(F + 1):
        z = G + 0.4 + FH * f
        p.append(box("Band%d" % f, (W + 0.25, D + 0.25, 0.3), (0, 0, z), "M_Metal"))
    for i in range(10):
        x = -W / 2 + W * i / 9
        p.append(box("FinF%d" % i, (0.15, 0.35, FH * F), (x, -D / 2 - 0.1, G + 0.4 + FH * F / 2), "M_Metal"))
        p.append(box("FinB%d" % i, (0.15, 0.35, FH * F), (x, D / 2 + 0.1, G + 0.4 + FH * F / 2), "M_Metal"))
    for i in range(8):
        y = -D / 2 + D * i / 7
        for sx in (-1, 1):
            p.append(box("FinS%d%d" % (i, sx), (0.35, 0.15, FH * F), (sx * (W / 2 + 0.1), y, G + 0.4 + FH * F / 2), "M_Metal"))
    p.append(box("Canopy", (6.5, 2.4, 0.2), (0, -D / 2 - 1.2, 3.6), "M_Metal"))
    p.append(box("CanopySign", (5.5, 0.15, 0.6), (0, -D / 2 - 0.25, 4.2), "M_Trim"))
    p.append(text("SignText", "NISHIKAWA DATA", 0.4, (0, -D / 2 - 0.34, 4.2), "M_NeonBlue"))
    for i in range(3):
        for j in range(2):
            p.append(box("CeilLight%d%d" % (i, j), (2.4, 0.6, 0.04), (-5 + i * 5, -2 + j * 4, G - 0.03), "M_LampGlow"))
    p.append(box("HVAC1", (3, 2, 1.5), (-4, 2, top + 1.35), "M_MetalDark"))
    p.append(box("HVAC2", (2, 2, 1.2), (3, -1, top + 1.2), "M_MetalDark"))
    finish(root, p, "Buildings/Building_Office")


def house(name, wall_mat, roof_mat, seed):
    clear()
    rnd = random.Random(seed)
    root = empty(name)
    W, D, H = 8.0, 7.0, 5.6
    p = [box("Body", (W, D, H), (0, 0, H / 2), wall_mat),
         box("Base", (W + 0.1, D + 0.1, 0.4), (0, 0, 0.2), "M_ConcreteDark"),
         box("RoofF", (W + 0.8, 4.6, 0.18), (0, -1.9, H + 1.1), roof_mat, rot=(30, 0, 0)),
         box("RoofB", (W + 0.8, 4.6, 0.18), (0, 1.9, H + 1.1), roof_mat, rot=(-30, 0, 0)),
         prism("Gable", W, D, D / 2 * math.tan(math.radians(30)), (0, 0, H), wall_mat),
         box("Door", (1.0, 0.08, 2.1), (-2.2, -D / 2 - 0.04, 1.25), "M_Door"),
         box("DoorCanopy", (1.6, 0.8, 0.1), (-2.2, -D / 2 - 0.4, 2.6), roof_mat),
         box("PorchLight", (0.15, 0.1, 0.2), (-1.4, -D / 2 - 0.06, 2.2), "M_LampGlow"),
         box("FenceF", (W + 1, 0.15, 1.1), (1.8, -D / 2 - 1.6, 0.55), "M_ConcreteDark")]
    p += window_grid("F", root, -D / 2, -0.8, W / 2 - 0.4, 0.2, 1, 2.8, 2, 1.3, 1.2, sill=0.9)
    p += window_grid("U", root, -D / 2, -W / 2 + 0.4, W / 2 - 0.4, 2.8, 1, 2.8, 3, 1.2, 1.1, sill=0.8)
    p.append(box("AC", (0.8, 0.3, 0.55), (rnd.uniform(-2, 2), -D / 2 - 0.2, 3.1), "M_PlasticWhite"))
    for sx in (-1, 1):
        p += window_grid_side("S%d" % sx, sx * W / 2, -1.5, 1.5, 0.2, 2, 2.8, 1, 1.0, 1.0, facing=sx)
    finish(root, p, "Buildings/" + name)


def tower(name, mat, floors, seed):
    clear()
    root = empty(name)
    W, D, FH = 12.0, 12.0, 3.2
    H = FH * floors
    p = [box("Body", (W, D, H), (0, 0, H / 2), mat),
         box("Crown", (W + 0.4, D + 0.4, 0.8), (0, 0, H + 0.4), "M_ConcreteDark"),
         box("Shopfront", (W - 1, 0.1, 2.6), (0, -D / 2 - 0.05, 1.6), "M_Glass"),
         box("ShopAwning", (W - 0.5, 1.2, 0.1), (0, -D / 2 - 0.6, 3.0), "M_FabricRed" if seed % 2 else "M_Awning")]
    p += window_grid("F", root, -D / 2, -W / 2 + 0.5, W / 2 - 0.5, FH, floors - 1, FH, 4, 1.6, 1.6, sill=0.7)
    p += window_grid("B", root, D / 2, -W / 2 + 0.5, W / 2 - 0.5, FH, floors - 1, FH, 4, 1.6, 1.6, sill=0.7, facing=1)
    for sx in (-1, 1):
        p += window_grid_side("S%d" % sx, sx * W / 2, -D / 2 + 0.5, D / 2 - 0.5, FH, floors - 1, FH, 4, 1.6, 1.6, facing=sx)
    finish(root, p, "Buildings/" + name)


apartment_building()
shop_building()
office_building()
house("House_A", "M_PlasterWarm", "M_Roof", 1)
house("House_B", "M_Plaster", "M_FabricRed", 2)
tower("Building_Tower_A", "M_PlasterBlue", 8, 3)
tower("Building_Tower_B", "M_Concrete", 10, 4)
