# SwordArt Blender helper library.
# Loaded inside Blender via:  exec(open(r"C:/Users/yachi/SwordArt/Tools/Blender/sao_lib.py").read())
# Conventions: 1 BU = 1 m, Z up, models face -Y (exported to Unity +Z forward).
import bpy, bmesh, math, random
from mathutils import Vector, Matrix, Euler

ROOT = "C:/Users/yachi/SwordArt/Assets/_Project/Generated/"

# name: (rgb, metallic, roughness, emission_strength)
PALETTE = {
    # real world
    "M_Plaster": ((0.86, 0.83, 0.76), 0.0, 0.85, 0), "M_PlasterWarm": ((0.80, 0.64, 0.52), 0.0, 0.85, 0),
    "M_PlasterBlue": ((0.62, 0.70, 0.77), 0.0, 0.85, 0), "M_Concrete": ((0.55, 0.55, 0.53), 0.0, 0.9, 0),
    "M_ConcreteDark": ((0.33, 0.33, 0.34), 0.0, 0.9, 0), "M_Asphalt": ((0.12, 0.12, 0.13), 0.0, 0.95, 0),
    "M_Sidewalk": ((0.60, 0.58, 0.56), 0.0, 0.9, 0), "M_RoadLine": ((0.95, 0.95, 0.90), 0.0, 0.8, 0),
    "M_Glass": ((0.18, 0.26, 0.34), 0.3, 0.05, 0), "M_GlassClear": ((0.6, 0.75, 0.85), 0.0, 0.05, 0),
    "M_Trim": ((0.24, 0.24, 0.26), 0.2, 0.6, 0), "M_Metal": ((0.62, 0.64, 0.67), 0.8, 0.35, 0),
    "M_MetalDark": ((0.18, 0.18, 0.2), 0.7, 0.45, 0), "M_Wood": ((0.52, 0.36, 0.21), 0.0, 0.7, 0),
    "M_WoodLight": ((0.78, 0.63, 0.45), 0.0, 0.65, 0), "M_Roof": ((0.22, 0.26, 0.32), 0.1, 0.7, 0),
    "M_Door": ((0.33, 0.24, 0.17), 0.0, 0.6, 0), "M_Awning": ((0.12, 0.42, 0.68), 0.0, 0.7, 0),
    "M_SignRed": ((0.85, 0.12, 0.12), 0.0, 0.5, 0), "M_SignWhite": ((0.95, 0.95, 0.95), 0.0, 0.5, 0),
    "M_LampGlow": ((1.0, 0.86, 0.6), 0.0, 0.5, 6), "M_NeonBlue": ((0.2, 0.7, 1.0), 0.0, 0.4, 5),
    "M_NeonPink": ((1.0, 0.3, 0.6), 0.0, 0.4, 5), "M_Fabric": ((0.28, 0.33, 0.48), 0.0, 0.95, 0),
    "M_FabricRed": ((0.55, 0.15, 0.15), 0.0, 0.95, 0), "M_FabricWhite": ((0.9, 0.9, 0.92), 0.0, 0.95, 0),
    "M_Bark": ((0.33, 0.24, 0.15), 0.0, 0.95, 0), "M_Leaves": ((0.28, 0.52, 0.24), 0.0, 0.9, 0),
    "M_FloorWood": ((0.55, 0.40, 0.27), 0.0, 0.55, 0), "M_WallInterior": ((0.90, 0.88, 0.84), 0.0, 0.9, 0),
    "M_Screen": ((0.05, 0.2, 0.35), 0.0, 0.2, 3), "M_PlasticWhite": ((0.9, 0.9, 0.9), 0.0, 0.4, 0),
    "M_PlasticDark": ((0.1, 0.1, 0.11), 0.0, 0.5, 0), "M_Grass": ((0.3, 0.5, 0.22), 0.0, 0.95, 0),
    "M_Tile": ((0.85, 0.85, 0.82), 0.0, 0.3, 0),
    # characters
    "M_Skin": ((0.95, 0.78, 0.66), 0.0, 0.7, 0), "M_Hair": ((0.08, 0.06, 0.05), 0.0, 0.6, 0),
    "M_Shirt": ((0.25, 0.35, 0.55), 0.0, 0.9, 0), "M_Pants": ((0.14, 0.14, 0.18), 0.0, 0.9, 0),
    "M_Shoes": ((0.1, 0.1, 0.1), 0.0, 0.6, 0), "M_Eyes": ((0.05, 0.05, 0.08), 0.0, 0.3, 0),
    "M_Outfit": ((0.32, 0.34, 0.28), 0.0, 0.85, 0), "M_Accent": ((1.0, 0.45, 0.15), 0.0, 0.4, 4),
    "M_Gear": ((0.16, 0.16, 0.15), 0.3, 0.6, 0),
    # weapons
    "M_GunMetal": ((0.14, 0.14, 0.15), 0.85, 0.35, 0), "M_Polymer": ((0.2, 0.2, 0.21), 0.0, 0.6, 0),
    "M_GunAccent": ((0.55, 0.45, 0.32), 0.3, 0.5, 0), "M_GunGlow": ((0.3, 1.0, 0.8), 0.0, 0.3, 6),
    # FullDive
    "M_DeviceWhite": ((0.92, 0.93, 0.95), 0.2, 0.25, 0), "M_DeviceDark": ((0.12, 0.13, 0.16), 0.5, 0.3, 0),
    "M_DeviceGlow": ((0.3, 0.85, 1.0), 0.0, 0.2, 8),
    # GGO
    "M_Sand": ((0.76, 0.6, 0.42), 0.0, 0.95, 0), "M_SandDark": ((0.6, 0.46, 0.31), 0.0, 0.95, 0),
    "M_Rock": ((0.5, 0.42, 0.35), 0.0, 0.9, 0), "M_RustMetal": ((0.45, 0.26, 0.15), 0.5, 0.75, 0),
    "M_MilitaryGreen": ((0.3, 0.33, 0.24), 0.2, 0.7, 0), "M_ContainerRed": ((0.5, 0.16, 0.1), 0.4, 0.6, 0),
    "M_ContainerBlue": ((0.12, 0.25, 0.4), 0.4, 0.6, 0), "M_ConcreteRuin": ((0.58, 0.55, 0.5), 0.0, 0.95, 0),
    "M_Sandbag": ((0.6, 0.53, 0.38), 0.0, 0.95, 0), "M_HazardYellow": ((0.85, 0.65, 0.1), 0.2, 0.6, 0),
    "M_GlowOrange": ((1.0, 0.5, 0.1), 0.0, 0.4, 6), "M_GlowCyan": ((0.2, 0.9, 1.0), 0.0, 0.4, 6),
    "M_LobbyFloor": ((0.12, 0.12, 0.14), 0.6, 0.3, 0), "M_LobbyPanel": ((0.22, 0.23, 0.26), 0.5, 0.45, 0),
}


def material(name):
    m = bpy.data.materials.get(name)
    if m:
        return m
    rgb, metal, rough, emis = PALETTE.get(name, ((0.8, 0.8, 0.8), 0.0, 0.7, 0))
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = (*rgb, 1)
    b.inputs["Metallic"].default_value = metal
    b.inputs["Roughness"].default_value = rough
    if emis:
        b.inputs["Emission Color"].default_value = (*rgb, 1)
        b.inputs["Emission Strength"].default_value = emis
    m.diffuse_color = (*rgb, 1)
    return m


def clear():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for me in list(bpy.data.meshes):
        if me.users == 0:
            bpy.data.meshes.remove(me)


def _link(obj, parent=None):
    bpy.context.scene.collection.objects.link(obj)
    if parent is not None:
        set_parent(obj, parent)
    return obj


def set_parent(obj, parent):
    mw = obj.matrix_world.copy()
    obj.parent = parent
    obj.matrix_world = mw


def empty(name, loc=(0, 0, 0), parent=None):
    e = bpy.data.objects.new(name, None)
    e.empty_display_size = 0.1
    e.location = loc
    _link(e)
    bpy.context.view_layer.update()
    if parent is not None:
        set_parent(e, parent)
    return e


def _mesh_obj(name, bm, mat, loc, rot, parent):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    if mat:
        me.materials.append(material(mat) if isinstance(mat, str) else mat)
    o = bpy.data.objects.new(name, me)
    o.location = loc
    o.rotation_euler = Euler([math.radians(a) for a in rot])
    _link(o)
    bpy.context.view_layer.update()
    if parent is not None:
        set_parent(o, parent)
    return o


def _new_bm():
    bm = bmesh.new()
    bm.loops.layers.uv.new("UVMap")
    return bm


def box(name, size, loc, mat, parent=None, rot=(0, 0, 0), taper=None):
    """Axis aligned box with full extents `size`, centred at `loc`. taper=(sx, sy) scales the top face."""
    bm = _new_bm()
    bmesh.ops.create_cube(bm, size=1.0, calc_uvs=True)
    for v in bm.verts:
        v.co.x *= size[0]; v.co.y *= size[1]; v.co.z *= size[2]
        if taper and v.co.z > 0:
            v.co.x *= taper[0]; v.co.y *= taper[1]
    return _mesh_obj(name, bm, mat, loc, rot, parent)


def cyl(name, r, depth, loc, mat, parent=None, rot=(0, 0, 0), segs=12, r2=None):
    """Cylinder/cone along local Z, centred at loc."""
    bm = _new_bm()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segs, radius1=r,
                          radius2=r if r2 is None else r2, depth=depth, calc_uvs=True)
    return _mesh_obj(name, bm, mat, loc, rot, parent)


def sphere(name, r, loc, mat, parent=None, scale=(1, 1, 1), segs=12, rings=8, rot=(0, 0, 0)):
    bm = _new_bm()
    bmesh.ops.create_uvsphere(bm, u_segments=segs, v_segments=rings, radius=r, calc_uvs=True)
    for v in bm.verts:
        v.co.x *= scale[0]; v.co.y *= scale[1]; v.co.z *= scale[2]
    return _mesh_obj(name, bm, mat, loc, rot, parent)


def ico(name, r, loc, mat, parent=None, subdiv=1, scale=(1, 1, 1), jitter=0.0, seed=0):
    rnd = random.Random(seed)
    bm = _new_bm()
    bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=r, calc_uvs=True)
    for v in bm.verts:
        n = v.co.normalized()
        v.co += n * rnd.uniform(-jitter, jitter) * r
        v.co.x *= scale[0]; v.co.y *= scale[1]; v.co.z *= scale[2]
    return _mesh_obj(name, bm, mat, loc, (0, 0, 0), parent)


def torus(name, major, minor, loc, mat, parent=None, rot=(0, 0, 0), seg=32, mseg=8, arc=360):
    bm = _new_bm()
    uv = bm.loops.layers.uv.active
    rings = []
    steps = seg if arc >= 360 else seg + 1
    for i in range(steps):
        a = math.radians(arc) * i / seg
        ring = []
        for j in range(mseg):
            b = 2 * math.pi * j / mseg
            x = (major + minor * math.cos(b)) * math.cos(a)
            y = (major + minor * math.cos(b)) * math.sin(a)
            z = minor * math.sin(b)
            ring.append(bm.verts.new((x, y, z)))
        rings.append(ring)
    n = len(rings)
    for i in range(n if arc >= 360 else n - 1):
        r0, r1 = rings[i], rings[(i + 1) % n]
        for j in range(mseg):
            f = bm.faces.new((r0[j], r1[j], r1[(j + 1) % mseg], r0[(j + 1) % mseg]))
            for k, l in enumerate(f.loops):
                l[uv].uv = ((i + (k in (1, 2))) / n, (j + (k in (2, 3))) / mseg)
    return _mesh_obj(name, bm, mat, loc, rot, parent)


def plane_grid(name, sx, sy, nx, ny, height_fn, mat, parent=None):
    """Displaced grid (terrain). height_fn(x, y) -> z."""
    bm = _new_bm()
    uv = bm.loops.layers.uv.active
    verts = []
    for j in range(ny + 1):
        row = []
        for i in range(nx + 1):
            x = -sx / 2 + sx * i / nx
            y = -sy / 2 + sy * j / ny
            row.append(bm.verts.new((x, y, height_fn(x, y))))
        verts.append(row)
    for j in range(ny):
        for i in range(nx):
            f = bm.faces.new((verts[j][i], verts[j][i + 1], verts[j + 1][i + 1], verts[j + 1][i]))
            for l in f.loops:
                l[uv].uv = (l.vert.co.x / 8.0, l.vert.co.y / 8.0)
    return _mesh_obj(name, bm, mat, (0, 0, 0), (0, 0, 0), parent)


def text(name, body, size, loc, mat, parent=None, rot=(90, 0, 0), depth=0.02, align='CENTER'):
    """3D text converted to a mesh (default Blender font, Latin only). Default faces -Y."""
    cu = bpy.data.curves.new(name, 'FONT')
    cu.body = body
    cu.size = size
    cu.extrude = depth
    cu.align_x = align
    cu.align_y = 'CENTER'
    o = bpy.data.objects.new(name, cu)
    o.location = loc
    o.rotation_euler = Euler([math.radians(a) for a in rot])
    bpy.context.scene.collection.objects.link(o)
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action='DESELECT')
    o.select_set(True)
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.convert(target='MESH')
    o = bpy.context.view_layer.objects.active
    o.name = name
    o.data.materials.clear()
    o.data.materials.append(material(mat))
    if parent is not None:
        set_parent(o, parent)
    return o


def window_grid(prefix, parent, face_y, x0, x1, z0, floors, floor_h, cols, win_w, win_h, frame_mat="M_Trim",
                glass="M_Glass", sill=0.6, inset=0.05, facing=-1):
    """Windows on a wall plane at y=face_y (facing=-1 means the wall faces -Y)."""
    objs = []
    span = (x1 - x0) / cols
    for f in range(floors):
        for c in range(cols):
            cx = x0 + span * (c + 0.5)
            cz = z0 + f * floor_h + sill + win_h / 2
            y = face_y + facing * 0.01
            objs.append(box("%sGlass_%d_%d" % (prefix, f, c), (win_w, 0.04, win_h), (cx, y - facing * inset, cz), glass))
            objs.append(box("%sFrameT_%d_%d" % (prefix, f, c), (win_w + 0.1, 0.08, 0.06), (cx, y, cz + win_h / 2), frame_mat))
            objs.append(box("%sFrameB_%d_%d" % (prefix, f, c), (win_w + 0.14, 0.12, 0.06), (cx, y + facing * 0.02, cz - win_h / 2), frame_mat))
    return objs


def window_grid_side(prefix, face_x, y0, y1, z0, floors, floor_h, cols, win_w, win_h, frame_mat="M_Trim",
                     glass="M_Glass", sill=0.6, facing=1):
    objs = []
    span = (y1 - y0) / cols
    for f in range(floors):
        for c in range(cols):
            cy = y0 + span * (c + 0.5)
            cz = z0 + f * floor_h + sill + win_h / 2
            x = face_x + facing * 0.01
            objs.append(box("%sGlass_%d_%d" % (prefix, f, c), (0.04, win_w, win_h), (x - facing * 0.05, cy, cz), glass))
            objs.append(box("%sFrame_%d_%d" % (prefix, f, c), (0.08, win_w + 0.1, 0.06), (x, cy, cz + win_h / 2), frame_mat))
    return objs


def join(objs, name, parent=None):
    """Joins mesh objects into one (keeps materials). Origin stays at the first object's origin."""
    objs = [o for o in objs if o and o.type == 'MESH']
    if not objs:
        return None
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.object.join()
    o = bpy.context.view_layer.objects.active
    o.name = name
    o.data.name = name
    if parent is not None:
        set_parent(o, parent)
    return o


def descendants(o):
    out = [o]
    for c in o.children:
        out += descendants(c)
    return out


def _turn_around(root):
    """Rotates the model 180 deg about Z by transforming vertices and conjugating object
    matrices, so pivots keep identity rotations (Blender -Y front -> +Y front)."""
    R = Matrix.Rotation(math.pi, 4, 'Z')
    Rinv = R.inverted()
    objs = descendants(root)
    worlds = {o: o.matrix_world.copy() for o in objs}
    for o in objs:
        if o.type == 'MESH':
            o.data.transform(R)
    for o in objs:
        o.matrix_world = R @ worlds[o] @ Rinv
    bpy.context.view_layer.update()


def export(root, rel_path):
    """Exports root + children to Generated/<rel_path>.fbx.
    Native Z-up axes + Unity's bakeAxisConversion give identity root/pivot rotations;
    models built facing -Y in Blender face +Z in Unity after _turn_around."""
    path = ROOT + rel_path + ".fbx"
    import os
    os.makedirs(os.path.dirname(path), exist_ok=True)
    _turn_around(root)
    bpy.ops.object.select_all(action='DESELECT')
    for o in descendants(root):
        o.select_set(True)
    bpy.context.view_layer.objects.active = root
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'EMPTY', 'MESH'},
                             apply_scale_options='FBX_SCALE_ALL', axis_forward='Y', axis_up='Z',
                             bake_space_transform=False, mesh_smooth_type='FACE', add_leaf_bones=False,
                             use_mesh_modifiers=True, use_custom_props=False, path_mode='AUTO')
    _turn_around(root)
    return path
