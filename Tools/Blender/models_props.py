# Street props, shop/office interior props and apartment interior + furniture.
exec(open(r"C:/Users/yachi/SwordArt/Tools/Blender/sao_lib.py").read())


def done(root, parts, rel, join_parts=True):
    if join_parts:
        join(parts, root.name + "_Mesh", root)
    print(export(root, rel))


def street_lamp():
    clear(); r = empty("StreetLamp")
    p = [cyl("Pole", 0.08, 5.0, (0, 0, 2.5), "M_MetalDark", r2=0.06),
         cyl("Base", 0.16, 0.4, (0, 0, 0.2), "M_MetalDark"),
         box("Arm", (0.08, 1.2, 0.08), (0, -0.55, 4.95), "M_MetalDark"),
         box("Head", (0.32, 0.55, 0.12), (0, -1.15, 4.9), "M_MetalDark"),
         box("Glow", (0.26, 0.45, 0.03), (0, -1.15, 4.83), "M_LampGlow")]
    done(r, p, "Props/StreetLamp")


def bench():
    clear(); r = empty("Bench")
    p = []
    for i in range(3):
        p.append(box("Seat%d" % i, (1.8, 0.12, 0.05), (0, -0.15 + i * 0.14, 0.45), "M_Wood"))
    for i in range(2):
        p.append(box("Back%d" % i, (1.8, 0.04, 0.12), (0, 0.17, 0.62 + i * 0.16), "M_Wood"))
    for sx in (-1, 1):
        p.append(box("Leg%d" % sx, (0.06, 0.5, 0.45), (sx * 0.8, 0, 0.225), "M_MetalDark"))
        p.append(box("BackLeg%d" % sx, (0.06, 0.05, 0.45), (sx * 0.8, 0.19, 0.65), "M_MetalDark"))
    done(r, p, "Props/Bench")


def trash_bins():
    clear(); r = empty("TrashBins")
    p = []
    for i, m in enumerate(("M_Awning", "M_Leaves", "M_SignRed")):
        x = (i - 1) * 0.5
        p.append(box("Bin%d" % i, (0.45, 0.45, 0.9), (x, 0, 0.45), "M_PlasticWhite"))
        p.append(box("Lid%d" % i, (0.47, 0.47, 0.08), (x, 0, 0.94), m))
        p.append(box("Label%d" % i, (0.3, 0.02, 0.15), (x, -0.235, 0.7), m))
    done(r, p, "Props/TrashBins")


def vending_machine():
    clear(); r = empty("VendingMachine")
    p = [box("Body", (1.0, 0.8, 1.85), (0, 0, 0.925), "M_PlasticWhite"),
         box("Window", (0.84, 0.03, 0.95), (0, -0.4, 1.3), "M_GlassClear"),
         box("Back", (0.84, 0.02, 0.95), (0, -0.25, 1.3), "M_WallInterior"),
         box("TopGlow", (0.9, 0.03, 0.12), (0, -0.405, 1.86), "M_NeonBlue"),
         box("Slot", (0.84, 0.05, 0.2), (0, -0.41, 0.25), "M_PlasticDark"),
         box("Panel", (0.12, 0.03, 0.35), (0.38, -0.41, 0.6), "M_PlasticDark")]
    cols = ("M_SignRed", "M_Awning", "M_Leaves", "M_HazardYellow", "M_PlasticWhite", "M_FabricRed")
    for row in range(3):
        for c in range(6):
            p.append(cyl("Can%d%d" % (row, c), 0.04, 0.16, (-0.33 + c * 0.13, -0.32, 0.95 + row * 0.3), cols[(row + c) % len(cols)], segs=8))
            p.append(box("Btn%d%d" % (row, c), (0.05, 0.02, 0.02), (-0.33 + c * 0.13, -0.42, 0.86 + row * 0.3), "M_NeonBlue"))
    done(r, p, "Props/VendingMachine")


def stop_sign():
    clear(); r = empty("RoadSign_Stop")
    p = [cyl("Pole", 0.035, 2.6, (0, 0, 1.3), "M_Metal")]
    import bmesh as bm_
    bm = _new_bm()
    s = 0.45
    pts = [(-s, 0, 0.0), (s, 0, 0.0), (0, 0, -s * 1.5)]
    front = [bm.verts.new((x, -0.03, z)) for x, _, z in pts]
    back = [bm.verts.new((x, 0.0, z)) for x, _, z in pts]
    bm.faces.new(front)
    bm.faces.new(back[::-1])
    for i in range(3):
        j = (i + 1) % 3
        bm.faces.new((front[i], back[i], back[j], front[j]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    p.append(_mesh_obj("Sign", bm, "M_SignRed", (0, -0.05, 2.75), (0, 0, 0), None))
    p.append(text("Text", "STOP", 0.16, (0, -0.09, 2.55), "M_SignWhite"))
    done(r, p, "Props/RoadSign_Stop")


def tree(name, seed, h):
    clear(); r = empty(name)
    rnd = random.Random(seed)
    p = [cyl("Trunk", 0.16, h, (0, 0, h / 2), "M_Bark", r2=0.1, segs=8)]
    for i in range(4):
        p.append(ico("Leaf%d" % i, rnd.uniform(1.0, 1.5), (rnd.uniform(-0.6, 0.6), rnd.uniform(-0.6, 0.6), h + rnd.uniform(-0.3, 0.8)),
                     "M_Leaves", subdiv=1, jitter=0.15, seed=seed + i))
    done(r, p, "Props/" + name)


def utility_pole():
    clear(); r = empty("UtilityPole")
    p = [cyl("Pole", 0.15, 9.0, (0, 0, 4.5), "M_Concrete", r2=0.1, segs=10),
         box("Cross1", (1.8, 0.1, 0.1), (0, 0, 8.4), "M_MetalDark"),
         box("Cross2", (1.4, 0.1, 0.1), (0, 0, 7.8), "M_MetalDark"),
         cyl("Transformer", 0.25, 0.7, (0, 0.3, 6.8), "M_Concrete", segs=10)]
    for x in (-0.8, -0.3, 0.3, 0.8):
        p.append(cyl("Insulator", 0.04, 0.12, (x, 0, 8.5), "M_PlasticWhite", segs=6))
    done(r, p, "Props/UtilityPole")


def planter():
    clear(); r = empty("Planter")
    p = [box("Box", (1.6, 0.6, 0.5), (0, 0, 0.25), "M_ConcreteDark"),
         box("Soil", (1.5, 0.5, 0.05), (0, 0, 0.48), "M_Bark")]
    for i in range(3):
        p.append(ico("Bush%d" % i, 0.32, (-0.5 + i * 0.5, 0, 0.7), "M_Leaves", subdiv=1, jitter=0.2, seed=i))
    done(r, p, "Props/Planter")


def fountain():
    clear(); r = empty("Fountain")
    p = [cyl("Basin", 2.5, 0.6, (0, 0, 0.3), "M_Concrete", segs=24),
         cyl("Water", 2.3, 0.05, (0, 0, 0.55), "M_GlassClear", segs=24),
         cyl("Column", 0.35, 1.6, (0, 0, 1.2), "M_Concrete", segs=12),
         cyl("Bowl", 0.9, 0.2, (0, 0, 2.0), "M_Concrete", segs=16, r2=1.0)]
    done(r, p, "Props/Fountain")


def bus_stop():
    clear(); r = empty("BusStop")
    p = [box("Roof", (3.2, 1.6, 0.1), (0, 0, 2.5), "M_MetalDark"),
         box("Back", (3.0, 0.05, 1.6), (0, 0.7, 1.4), "M_GlassClear"),
         box("Bench", (2.4, 0.4, 0.06), (0, 0.4, 0.45), "M_Metal"),
         box("Ad", (1.0, 0.06, 1.6), (1.45, 0.0, 1.3), "M_NeonPink")]
    for sx in (-1.5, 1.5):
        p.append(box("Post", (0.08, 0.08, 2.5), (sx, 0.7, 1.25), "M_MetalDark"))
    p.append(cyl("SignPole", 0.04, 2.6, (-2.0, -0.4, 1.3), "M_Metal"))
    p.append(cyl("SignDisc", 0.28, 0.04, (-2.0, -0.42, 2.5), "M_Awning", rot=(90, 0, 0), segs=16))
    done(r, p, "Props/BusStop")


def shop_shelf(seed):
    clear(); r = empty("ShopShelf")
    rnd = random.Random(seed)
    p = [box("Back", (2.0, 0.05, 1.9), (0, 0.27, 0.95), "M_MetalDark")]
    for sx in (-1, 1):
        p.append(box("Side%d" % sx, (0.04, 0.6, 1.9), (sx * 1.0, 0, 0.95), "M_MetalDark"))
    cols = ("M_MilitaryGreen", "M_GunMetal", "M_HazardYellow", "M_SignRed", "M_Awning", "M_Sand")
    for i in range(4):
        z = 0.15 + i * 0.5
        p.append(box("Shelf%d" % i, (2.0, 0.55, 0.04), (0, 0, z), "M_Metal"))
        x = -0.9
        while x < 0.8:
            w = rnd.uniform(0.15, 0.35)
            hgt = rnd.uniform(0.15, 0.35)
            p.append(box("Item", (w, 0.3, hgt), (x + w / 2, -0.02, z + 0.02 + hgt / 2), rnd.choice(cols)))
            x += w + 0.05
    done(r, p, "Props/ShopShelf")


def shop_counter():
    clear(); r = empty("ShopCounter")
    p = [box("Counter", (2.6, 0.8, 1.0), (0, 0, 0.5), "M_WoodLight"),
         box("Top", (2.7, 0.9, 0.05), (0, 0, 1.02), "M_MetalDark"),
         box("Register", (0.45, 0.35, 0.18), (0.6, 0.0, 1.13), "M_PlasticDark"),
         box("RegScreen", (0.35, 0.03, 0.22), (0.6, 0.1, 1.35), "M_Screen"),
         box("Display", (0.9, 0.5, 0.35), (-0.6, 0.0, 1.22), "M_GlassClear"),
         box("Stripe", (2.6, 0.02, 0.08), (0, -0.41, 0.85), "M_NeonBlue")]
    done(r, p, "Props/ShopCounter")


def weapon_rack():
    clear(); r = empty("WeaponRack")
    p = [box("Board", (2.4, 0.08, 1.4), (0, 0.0, 1.5), "M_LobbyPanel"),
         box("Glow", (2.4, 0.09, 0.03), (0, 0.0, 2.2), "M_NeonBlue")]
    for i in range(3):
        z = 1.1 + i * 0.4
        p.append(box("Gun%d" % i, (1.0 - i * 0.2, 0.06, 0.1), (0, -0.07, z), "M_GunMetal"))
        p.append(box("Hook%d" % i, (0.05, 0.1, 0.05), (-0.3, -0.05, z - 0.07), "M_Metal"))
    done(r, p, "Props/WeaponRack")


def office_desk():
    clear(); r = empty("OfficeDesk")
    p = [box("Top", (1.6, 0.8, 0.05), (0, 0, 0.75), "M_WoodLight"),
         box("Monitor", (0.6, 0.04, 0.38), (0, 0.2, 1.05), "M_PlasticDark"),
         box("Screen", (0.55, 0.01, 0.33), (0, 0.175, 1.05), "M_Screen"),
         box("Stand", (0.06, 0.06, 0.2), (0, 0.24, 0.86), "M_PlasticDark"),
         box("Keyboard", (0.45, 0.15, 0.02), (0, -0.1, 0.785), "M_PlasticDark"),
         box("ChairSeat", (0.5, 0.5, 0.08), (0, -0.65, 0.48), "M_Fabric"),
         box("ChairBack", (0.5, 0.06, 0.55), (0, -0.88, 0.8), "M_Fabric"),
         cyl("ChairPole", 0.03, 0.4, (0, -0.65, 0.24), "M_MetalDark")]
    for sx in (-1, 1):
        p.append(box("Leg%d" % sx, (0.05, 0.7, 0.73), (sx * 0.75, 0, 0.365), "M_MetalDark"))
    done(r, p, "Props/OfficeDesk")


def reception_desk():
    clear(); r = empty("ReceptionDesk")
    p = [box("Front", (3.4, 0.4, 1.1), (0, -0.4, 0.55), "M_WallInterior"),
         box("Top", (3.6, 1.0, 0.06), (0, -0.2, 1.13), "M_WoodLight"),
         box("Logo", (1.6, 0.02, 0.2), (0, -0.61, 0.75), "M_NeonBlue"),
         box("Monitor", (0.5, 0.04, 0.32), (0.8, 0.0, 1.35), "M_PlasticDark")]
    done(r, p, "Props/ReceptionDesk")


def office_plant():
    clear(); r = empty("OfficePlant")
    p = [cyl("Pot", 0.22, 0.45, (0, 0, 0.225), "M_PlasticWhite", r2=0.18),
         ico("Leaves", 0.45, (0, 0, 0.9), "M_Leaves", subdiv=1, jitter=0.25, seed=3, scale=(1, 1, 1.4))]
    done(r, p, "Props/OfficePlant")


# ---------- Apartment ----------

def apartment_room():
    clear(); r = empty("Apartment_Room")
    W, D, H, t = 9.0, 7.0, 2.8, 0.2
    p = [box("Floor", (W, D, 0.1), (0, 0, -0.05), "M_FloorWood"),
         box("Ceiling", (W, D, 0.1), (0, 0, H + 0.05), "M_WallInterior"),
         box("WallBack", (W, t, H), (0, D / 2 + t / 2, H / 2), "M_WallInterior"),
         box("WallL", (t, D, H), (-W / 2 - t / 2, 0, H / 2), "M_WallInterior"),
         box("BaseBack", (W, 0.03, 0.1), (0, D / 2 - 0.015, 0.05), "M_Wood"),
         box("BaseL", (0.03, D, 0.1), (-W / 2 + 0.015, 0, 0.05), "M_Wood")]
    # Right wall with window (x = +W/2)
    wx = W / 2 + t / 2
    p += [box("WallR_Low", (t, D, 0.9), (wx, 0, 0.45), "M_WallInterior"),
          box("WallR_High", (t, D, 0.6), (wx, 0, H - 0.3), "M_WallInterior"),
          box("WallR_A", (t, 1.75, 1.3), (wx, -D / 2 + 0.875, 1.55), "M_WallInterior"),
          box("WallR_B", (t, 1.75, 1.3), (wx, D / 2 - 0.875, 1.55), "M_WallInterior"),
          box("Window", (0.05, 3.5, 1.3), (wx, 0, 1.55), "M_GlassClear"),
          box("WindowFrameH", (0.08, 3.5, 0.06), (wx - 0.05, 0, 1.55), "M_PlasticWhite"),
          box("WindowFrameV", (0.08, 0.06, 1.3), (wx - 0.05, 0, 1.55), "M_PlasticWhite"),
          box("Sill", (0.25, 3.7, 0.05), (wx - 0.12, 0, 0.92), "M_PlasticWhite")]
    # Front wall with entrance door gap at x=-3 (y = -D/2)
    fy = -D / 2 - t / 2
    door_x, door_w, door_h = -3.0, 1.1, 2.15
    left_w = (door_x - door_w / 2) - (-W / 2)
    right_w = W / 2 - (door_x + door_w / 2)
    p += [box("WallF_L", (left_w, t, H), (-W / 2 + left_w / 2, fy, H / 2), "M_WallInterior"),
          box("WallF_R", (right_w, t, H), (W / 2 - right_w / 2, fy, H / 2), "M_WallInterior"),
          box("WallF_Top", (door_w, t, H - door_h), (door_x, fy, door_h + (H - door_h) / 2), "M_WallInterior"),
          box("Door", (door_w - 0.06, 0.06, door_h - 0.03), (door_x, fy - 0.12, door_h / 2), "M_Door"),
          box("DoorHandle", (0.12, 0.05, 0.03), (door_x + 0.4, fy - 0.17, 1.0), "M_Metal"),
          box("Genkan", (1.6, 1.2, 0.02), (door_x, -D / 2 + 0.6, 0.01), "M_Tile"),
          box("CeilLight", (1.2, 1.2, 0.04), (0, 0, H - 0.02), "M_LampGlow")]
    done(r, p, "Props/Apartment_Room")


def bed():
    clear(); r = empty("Bed")
    p = [box("Frame", (1.2, 2.1, 0.3), (0, 0, 0.15), "M_Wood"),
         box("Mattress", (1.1, 2.0, 0.22), (0, 0, 0.41), "M_FabricWhite"),
         box("Blanket", (1.14, 1.3, 0.06), (0, -0.33, 0.54), "M_Fabric"),
         box("Pillow", (0.7, 0.4, 0.14), (0, 0.72, 0.58), "M_FabricWhite"),
         box("Headboard", (1.2, 0.08, 0.6), (0, 1.02, 0.6), "M_Wood")]
    done(r, p, "Props/Bed")


def pc_desk():
    clear(); r = empty("PCDesk")
    p = [box("Top", (1.5, 0.7, 0.04), (0, 0, 0.74), "M_WoodLight"),
         box("Monitor", (0.62, 0.04, 0.38), (0, 0.18, 1.06), "M_PlasticDark"),
         box("Screen", (0.58, 0.01, 0.34), (0, 0.155, 1.06), "M_Screen"),
         box("Stand", (0.06, 0.06, 0.2), (0, 0.22, 0.86), "M_PlasticDark"),
         box("Keyboard", (0.44, 0.15, 0.02), (0, -0.1, 0.77), "M_PlasticDark"),
         box("Tower", (0.2, 0.45, 0.45), (0.55, 0.05, 0.225), "M_PlasticDark"),
         box("TowerGlow", (0.01, 0.4, 0.02), (0.44, 0.05, 0.4), "M_NeonBlue"),
         box("ChairSeat", (0.5, 0.5, 0.08), (0, -0.6, 0.48), "M_FabricRed"),
         box("ChairBack", (0.5, 0.06, 0.6), (0, -0.84, 0.82), "M_FabricRed"),
         cyl("ChairPole", 0.03, 0.4, (0, -0.6, 0.24), "M_MetalDark")]
    for sx in (-1, 1):
        p.append(box("Leg%d" % sx, (0.05, 0.6, 0.72), (sx * 0.7, 0, 0.36), "M_MetalDark"))
    done(r, p, "Props/PCDesk")


def bookshelf():
    clear(); r = empty("Bookshelf")
    rnd = random.Random(5)
    p = [box("Back", (1.0, 0.03, 1.8), (0, 0.15, 0.9), "M_Wood")]
    for sx in (-1, 1):
        p.append(box("Side", (0.03, 0.32, 1.8), (sx * 0.5, 0, 0.9), "M_Wood"))
    cols = ("M_SignRed", "M_Awning", "M_Leaves", "M_HazardYellow", "M_FabricWhite", "M_Fabric")
    for i in range(5):
        z = 0.02 + i * 0.44
        p.append(box("Shelf", (1.0, 0.32, 0.03), (0, 0, z), "M_Wood"))
        if i < 4:
            x = -0.45
            while x < 0.4:
                w = rnd.uniform(0.03, 0.07)
                hh = rnd.uniform(0.22, 0.34)
                p.append(box("Book", (w, 0.22, hh), (x + w / 2, 0, z + 0.015 + hh / 2), rnd.choice(cols)))
                x += w + 0.005
    done(r, p, "Props/Bookshelf")


def kitchen():
    clear(); r = empty("Kitchenette")
    p = [box("Counter", (2.4, 0.6, 0.88), (0, 0, 0.44), "M_PlasticWhite"),
         box("Top", (2.45, 0.65, 0.04), (0, 0, 0.9), "M_MetalDark"),
         box("Sink", (0.5, 0.4, 0.02), (-0.5, 0, 0.925), "M_Metal"),
         box("Stove", (0.55, 0.45, 0.02), (0.6, 0, 0.925), "M_PlasticDark"),
         box("UpperCab", (2.4, 0.35, 0.6), (0, 0.13, 1.9), "M_PlasticWhite"),
         box("Fridge", (0.7, 0.65, 1.7), (1.65, 0, 0.85), "M_PlasticWhite"),
         box("FridgeHandle", (0.03, 0.04, 0.5), (1.4, -0.34, 1.1), "M_Metal")]
    done(r, p, "Props/Kitchenette")


def small_props():
    clear(); r = empty("Rug")
    done(r, [box("Rug", (2.4, 1.7, 0.02), (0, 0, 0.01), "M_Fabric")], "Props/Rug")
    clear(); r = empty("Nightstand")
    done(r, [box("Body", (0.45, 0.4, 0.5), (0, 0, 0.25), "M_Wood"),
             box("Lamp", (0.15, 0.15, 0.25), (0, 0, 0.62), "M_LampGlow")], "Props/Nightstand")
    clear(); r = empty("Sofa")
    done(r, [box("Base", (1.8, 0.85, 0.42), (0, 0, 0.21), "M_Fabric"),
             box("Back", (1.8, 0.2, 0.45), (0, 0.33, 0.62), "M_Fabric"),
             box("ArmL", (0.18, 0.85, 0.25), (-0.81, 0, 0.55), "M_Fabric"),
             box("ArmR", (0.18, 0.85, 0.25), (0.81, 0, 0.55), "M_Fabric"),
             box("Cushion", (1.4, 0.6, 0.1), (0, -0.05, 0.47), "M_FabricWhite")], "Props/Sofa")
    clear(); r = empty("TVStand")
    done(r, [box("Stand", (1.6, 0.4, 0.45), (0, 0, 0.225), "M_Wood"),
             box("TV", (1.3, 0.06, 0.75), (0, 0.05, 0.9), "M_PlasticDark"),
             box("TVScreen", (1.22, 0.01, 0.68), (0, 0.015, 0.9), "M_Screen")], "Props/TVStand")


for fn in (street_lamp, bench, trash_bins, vending_machine, stop_sign, utility_pole, planter, fountain, bus_stop,
           shop_counter, weapon_rack, office_desk, reception_desk, office_plant, apartment_room, bed, pc_desk,
           bookshelf, kitchen, small_props):
    fn()
tree("Tree_A", 11, 2.6)
tree("Tree_B", 22, 3.2)
shop_shelf(3)
