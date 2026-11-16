# Segmented low-poly characters with joint pivots named for ProceduralCharacterAnimator.
# Pivots: Torso, Head, Arm_L, Arm_R, Leg_L, Leg_R. Faces -Y in Blender (= +Z in Unity).
exec(open(r"C:/Users/yachi/SwordArt/Tools/Blender/sao_lib.py").read())


def build_character(name, female=False, soldier=False):
    clear()
    root = empty(name, (0, 0, 0))
    shirt = "M_Outfit" if soldier else "M_Shirt"
    # Legs (pivot at the hip joint)
    for side, x in (("L", 0.095), ("R", -0.095)):
        leg = empty("Leg_" + side, (x, 0, 0.92), root)
        box("Thigh_" + side, (0.15, 0.16, 0.47), (x, 0, 0.69), "M_Pants", leg)
        box("Shin_" + side, (0.12, 0.13, 0.42), (x, 0, 0.26), "M_Pants" if not female else "M_Skin", leg)
        box("Shoe_" + side, (0.12, 0.27, 0.09), (x, -0.05, 0.045), "M_Shoes", leg)
        if soldier:
            box("KneePad_" + side, (0.13, 0.05, 0.12), (x, -0.08, 0.47), "M_Gear", leg)
            box("Boot_" + side, (0.135, 0.15, 0.2), (x, 0, 0.14), "M_Gear", leg)
    torso = empty("Torso", (0, 0, 0.95), root)
    box("Pelvis", (0.34 if not female else 0.36, 0.2, 0.2), (0, 0, 0.97), "M_Pants", torso)
    chest_w = 0.36 if female else 0.40
    box("Chest", (chest_w, 0.22, 0.52), (0, 0, 1.31), shirt, torso, taper=(1.06, 1.0))
    cyl("Neck", 0.055, 0.12, (0, 0, 1.6), "M_Skin", torso, segs=10)
    if female:
        cyl("Skirt", 0.2, 0.28, (0, 0, 0.86), "M_Pants", torso, segs=14, r2=0.17)
    if soldier:
        box("Vest", (chest_w + 0.04, 0.27, 0.36), (0, 0, 1.33), "M_Gear", torso)
        box("Pouch_L", (0.09, 0.06, 0.1), (0.1, -0.15, 1.22), "M_Outfit", torso)
        box("Pouch_R", (0.09, 0.06, 0.1), (-0.1, -0.15, 1.22), "M_Outfit", torso)
        box("Belt", (0.37, 0.23, 0.06), (0, 0, 1.06), "M_Gear", torso)
        box("Backpack", (0.3, 0.14, 0.34), (0, 0.18, 1.32), "M_Outfit", torso)
        box("AccentStrip", (0.3, 0.01, 0.025), (0, -0.137, 1.44), "M_Accent", torso)
    head = empty("Head", (0, 0, 1.6), torso)
    sphere("HeadMesh", 0.12, (0, -0.01, 1.74), "M_Skin", head, scale=(0.92, 1.0, 1.12), segs=16, rings=10)
    for sx in (1, -1):
        box("Eye", (0.03, 0.012, 0.022), (0.042 * sx, -0.122, 1.755), "M_Eyes", head)
    if soldier:
        sphere("Helmet", 0.14, (0, 0.0, 1.79), "M_Gear", head, scale=(1.0, 1.05, 0.85), segs=16, rings=8)
        box("Visor", (0.2, 0.04, 0.05), (0, -0.115, 1.765), "M_Accent", head)
        box("EarPiece", (0.26, 0.06, 0.07), (0, 0.0, 1.73), "M_Gear", head)
    else:
        sphere("Hair", 0.13, (0, 0.022, 1.78), "M_Hair", head, scale=(1.0, 1.04, 0.98), segs=16, rings=10)
        box("Fringe", (0.2, 0.05, 0.06), (0, -0.095, 1.83), "M_Hair", head)
        if female:
            box("LongHair", (0.24, 0.08, 0.36), (0, 0.075, 1.6), "M_Hair", head, taper=(1.0, 0.8))
    for side, x in (("L", 0.235), ("R", -0.235)):
        arm = empty("Arm_" + side, (x, 0, 1.52), torso)
        box("UpperArm_" + side, (0.11, 0.12, 0.33), (x, 0, 1.36), shirt, arm)
        box("Forearm_" + side, (0.09, 0.1, 0.28), (x, 0, 1.06), shirt if soldier else "M_Skin", arm)
        sphere("Hand_" + side, 0.05, (x, 0, 0.88), "M_Gear" if soldier else "M_Skin", arm, segs=8, rings=6)
        if soldier:
            box("ShoulderPad_" + side, (0.14, 0.15, 0.08), (x, 0, 1.53), "M_Gear", arm)
    return root


for nm, fem, sol in (("Character_Male", False, False), ("Character_Female", True, False), ("GGO_Soldier", False, True)):
    r = build_character(nm, fem, sol)
    print(export(r, "Characters/" + nm))
