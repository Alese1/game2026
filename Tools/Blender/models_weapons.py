# GGO weapons (separate parts: Body, Barrel, Magazine, Stock, Grip, Sight) + FullDive headset.
# Weapons face -Y in Blender (muzzle forward); origin at the top of the grip.
exec(open(r"C:/Users/yachi/SwordArt/Tools/Blender/sao_lib.py").read())

Y90 = (90, 0, 0)  # cylinder along Y


def assault_rifle():
    clear()
    r = empty("Weapon_AR")
    body = empty("Body", (0, 0, 0), r)
    box("Receiver", (0.06, 0.42, 0.11), (0, -0.05, 0.02), "M_Polymer", body)
    box("Rail", (0.03, 0.36, 0.015), (0, -0.06, 0.083), "M_GunMetal", body)
    box("Handguard", (0.066, 0.26, 0.078), (0, -0.38, 0.025), "M_GunMetal", body, taper=(0.85, 1.0))
    box("GlowStrip_L", (0.068, 0.2, 0.008), (0, -0.08, 0.045), "M_GunGlow", body)
    box("Ejector", (0.065, 0.06, 0.03), (0, 0.0, 0.05), "M_GunAccent", body)
    barrel = empty("Barrel", (0, -0.5, 0.03), r)
    cyl("BarrelTube", 0.012, 0.22, (0, -0.6, 0.03), "M_GunMetal", barrel, rot=Y90, segs=10)
    cyl("MuzzleBrake", 0.02, 0.06, (0, -0.73, 0.03), "M_GunMetal", barrel, rot=Y90, segs=8)
    mag = empty("Magazine", (0, -0.12, -0.03), r)
    box("Mag", (0.035, 0.07, 0.17), (0, -0.13, -0.1), "M_GunAccent", mag, rot=(-12, 0, 0))
    grip = empty("Grip", (0, 0.06, -0.03), r)
    box("GripMesh", (0.035, 0.05, 0.11), (0, 0.07, -0.08), "M_Polymer", grip, rot=(18, 0, 0))
    stock = empty("Stock", (0, 0.17, 0.0), r)
    box("StockMesh", (0.045, 0.24, 0.085), (0, 0.28, 0.0), "M_Polymer", stock, taper=(1.0, 1.0))
    box("ButtPad", (0.05, 0.03, 0.12), (0, 0.41, -0.01), "M_GunMetal", stock)
    sight = empty("Sight", (0, -0.1, 0.1), r)
    box("SightHousing", (0.034, 0.06, 0.045), (0, -0.1, 0.112), "M_GunMetal", sight)
    box("SightLens", (0.026, 0.005, 0.03), (0, -0.131, 0.115), "M_GunGlow", sight)
    return r


def pistol():
    clear()
    r = empty("Weapon_Pistol")
    body = empty("Body", (0, 0, 0), r)
    box("Frame", (0.03, 0.17, 0.03), (0, -0.05, 0.0), "M_Polymer", body)
    box("TriggerGuard", (0.012, 0.05, 0.03), (0, -0.02, -0.03), "M_Polymer", body)
    slide = empty("Slide", (0, -0.06, 0.035), r)
    box("SlideMesh", (0.032, 0.2, 0.04), (0, -0.06, 0.035), "M_GunMetal", slide)
    box("SlideGlow", (0.034, 0.08, 0.006), (0, -0.09, 0.045), "M_GunGlow", slide)
    box("FrontSight", (0.006, 0.008, 0.01), (0, -0.15, 0.06), "M_GunGlow", slide)
    box("RearSight", (0.02, 0.008, 0.01), (0, 0.03, 0.06), "M_GunGlow", slide)
    barrel = empty("Barrel", (0, -0.16, 0.035), r)
    cyl("BarrelTip", 0.009, 0.02, (0, -0.165, 0.035), "M_GunMetal", barrel, rot=Y90, segs=8)
    grip = empty("Grip", (0, 0.02, -0.02), r)
    box("GripMesh", (0.032, 0.05, 0.11), (0, 0.025, -0.065), "M_GunAccent", grip, rot=(16, 0, 0))
    mag = empty("Magazine", (0, 0.03, -0.12), r)
    box("MagBase", (0.03, 0.045, 0.012), (0, 0.04, -0.122), "M_GunMetal", mag)
    return r


def sniper():
    clear()
    r = empty("Weapon_Sniper")
    body = empty("Body", (0, 0, 0), r)
    box("Receiver", (0.07, 0.4, 0.1), (0, -0.05, 0.02), "M_GunMetal", body)
    box("Chassis", (0.06, 0.3, 0.05), (0, -0.33, 0.0), "M_Polymer", body)
    box("GlowStrip", (0.072, 0.25, 0.008), (0, -0.05, 0.05), "M_GunGlow", body)
    barrel = empty("Barrel", (0, -0.5, 0.035), r)
    cyl("BarrelTube", 0.016, 0.78, (0, -0.64, 0.035), "M_GunMetal", barrel, rot=Y90, segs=12)
    box("MuzzleBrake", (0.055, 0.09, 0.04), (0, -1.05, 0.035), "M_GunMetal", barrel)
    cyl("BipodL", 0.006, 0.3, (0.02, -0.75, -0.0), "M_GunMetal", barrel, rot=(90, 0, 0), segs=6)
    cyl("BipodR", 0.006, 0.3, (-0.02, -0.75, -0.0), "M_GunMetal", barrel, rot=(90, 0, 0), segs=6)
    scope = empty("Scope", (0, -0.08, 0.12), r)
    cyl("ScopeTube", 0.024, 0.34, (0, -0.08, 0.125), "M_GunMetal", scope, rot=Y90, segs=12)
    cyl("ScopeFront", 0.033, 0.07, (0, -0.27, 0.125), "M_GunMetal", scope, rot=Y90, segs=12)
    cyl("ScopeLens", 0.029, 0.005, (0, -0.305, 0.125), "M_GunGlow", scope, rot=Y90, segs=12)
    box("ScopeMountA", (0.02, 0.02, 0.06), (0, -0.16, 0.085), "M_GunMetal", scope)
    box("ScopeMountB", (0.02, 0.02, 0.06), (0, 0.02, 0.085), "M_GunMetal", scope)
    mag = empty("Magazine", (0, -0.12, -0.04), r)
    box("Mag", (0.05, 0.1, 0.12), (0, -0.12, -0.08), "M_GunAccent", mag)
    grip = empty("Grip", (0, 0.07, -0.03), r)
    box("GripMesh", (0.04, 0.055, 0.12), (0, 0.08, -0.08), "M_Polymer", grip, rot=(18, 0, 0))
    stock = empty("Stock", (0, 0.2, 0), r)
    box("StockMesh", (0.05, 0.36, 0.11), (0, 0.34, -0.005), "M_Polymer", stock)
    box("CheekRest", (0.045, 0.14, 0.035), (0, 0.3, 0.07), "M_GunAccent", stock)
    box("ButtPad", (0.055, 0.03, 0.15), (0, 0.53, -0.01), "M_GunMetal", stock)
    return r


def fulldive_headset():
    clear()
    r = empty("FullDive_Headset")
    dock = empty("Dock", (0, 0, 0), r)
    cyl("DockBase", 0.15, 0.035, (0, 0, 0.0175), "M_DeviceDark", dock, segs=32)
    torus("DockGlow", 0.13, 0.006, (0, 0, 0.036), "M_DeviceGlow", dock, seg=40, mseg=6)
    head = empty("Headset", (0, 0, 0.12), r)
    torus("RingUpper", 0.1, 0.012, (0, 0, 0.16), "M_DeviceWhite", head, seg=40, mseg=8)
    torus("RingLower", 0.108, 0.01, (0, 0, 0.1), "M_DeviceWhite", head, rot=(-10, 0, 0), seg=40, mseg=8)
    torus("Glow", 0.104, 0.004, (0, 0, 0.13), "M_DeviceGlow", head, seg=40, mseg=6)
    for i in range(6):
        a = math.radians(i * 60 + 30)
        box("Spacer%d" % i, (0.02, 0.02, 0.06), (math.cos(a) * 0.104, math.sin(a) * 0.104, 0.13), "M_DeviceDark", head,
            rot=(0, 0, math.degrees(a)))
    box("Visor", (0.12, 0.02, 0.035), (0, -0.112, 0.125), "M_DeviceDark", head)
    box("VisorGlow", (0.1, 0.004, 0.008), (0, -0.123, 0.125), "M_DeviceGlow", head)
    for sx in (1, -1):
        sphere("Node", 0.018, (0.112 * sx, 0, 0.13), "M_DeviceGlow", head, segs=10, rings=6)
    return r


for fn, path in ((assault_rifle, "Weapons/Weapon_AR"), (pistol, "Weapons/Weapon_Pistol"),
                 (sniper, "Weapons/Weapon_Sniper"), (fulldive_headset, "Props/FullDive_Headset")):
    print(export(fn(), path))
