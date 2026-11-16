using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using SwordArt.AI;
using SwordArt.Core;
using SwordArt.Economy;
using SwordArt.Inventory;
using SwordArt.NPC;
using SwordArt.Weapons;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SwordArt.EditorTools
{
    /// <summary>
    /// One-shot project setup: layers, URP material palette (shared with the Blender generator),
    /// FX materials, material remapping of generated models, all ScriptableObject game data and
    /// build settings. Safe to re-run: existing assets are updated in place.
    /// </summary>
    public static class ProjectSetup
    {
        public const string Root = "Assets/_Project/";
        public const string MaterialsDir = Root + "Art/Materials/";
        public const string FxDir = Root + "Art/Materials/FX/";
        public const string TexturesDir = Root + "Art/Textures/";
        public const string WeaponLayer = "FPWeapon";
        public const string PlayerLayer = "Player";
        public const string CharacterLayer = "Characters";
        private const string PalettePath = "Tools/Blender/sao_lib.py";

        [MenuItem("SwordArt/Setup/1. Create Materials, Data & Settings", priority = 1)]
        public static void RunAll()
        {
            EnsureFolders();
            EnsureLayer(WeaponLayer);
            EnsureLayer(PlayerLayer);
            EnsureLayer(CharacterLayer);
            CreatePalette();
            CreateFxMaterials();
            RemapGeneratedModels();
            CreateGameData();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup] Done.");
        }

        // ---------------- Folders & layers ----------------

        private static void EnsureFolders()
        {
            foreach (var dir in new[] { MaterialsDir, FxDir, TexturesDir, Root + "ScriptableObjects/Items", Root + "ScriptableObjects/Weapons",
                         Root + "ScriptableObjects/Economy", Root + "ScriptableObjects/NPC", Root + "ScriptableObjects/Bots", Root + "Resources" })
                Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }

        /// <summary>Adds a user layer if missing.</summary>
        public static int EnsureLayer(string layerName)
        {
            int existing = LayerMask.NameToLayer(layerName);
            if (existing >= 0) return existing;
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                var p = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(p.stringValue)) continue;
                p.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
            Debug.LogError("[ProjectSetup] No free layer slot for " + layerName);
            return -1;
        }

        // ---------------- Materials ----------------

        private static void CreatePalette()
        {
            if (!File.Exists(PalettePath))
            {
                Debug.LogError("[ProjectSetup] Palette file not found: " + PalettePath);
                return;
            }
            string text = File.ReadAllText(PalettePath);
            var rx = new Regex("\"(M_\\w+)\":\\s*\\(\\(([\\d.]+),\\s*([\\d.]+),\\s*([\\d.]+)\\),\\s*([\\d.]+),\\s*([\\d.]+),\\s*([\\d.]+)\\)");
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            int count = 0;
            foreach (Match m in rx.Matches(text))
            {
                string name = m.Groups[1].Value;
                var color = new Color(F(m, 2), F(m, 3), F(m, 4), 1f);
                float metallic = F(m, 5), roughness = F(m, 6), emission = F(m, 7);
                var mat = GetOrCreate(MaterialsDir + name + ".mat", lit);
                mat.SetColor("_BaseColor", color);
                mat.SetFloat("_Metallic", metallic);
                mat.SetFloat("_Smoothness", 1f - roughness);
                if (emission > 0f)
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color * emission * 0.5f);
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
                else
                {
                    mat.DisableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", Color.black);
                }
                if (name == "M_GlassClear") MakeTransparent(mat, 0.28f);
                EditorUtility.SetDirty(mat);
                count++;
            }
            Debug.Log($"[ProjectSetup] Palette materials: {count}");
        }

        private static float F(Match m, int g) => float.Parse(m.Groups[g].Value, System.Globalization.CultureInfo.InvariantCulture);

        private static Material GetOrCreate(string path, Shader shader)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader) mat.shader = shader;
            return mat;
        }

        private static void MakeTransparent(Material mat, float alpha)
        {
            var c = mat.GetColor("_BaseColor");
            c.a = alpha;
            mat.SetColor("_BaseColor", c);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        private static void CreateFxMaterials()
        {
            var soft = CreateTexture("FX_Soft", 64, (x, y) => { float d = Mathf.Clamp01(1f - new Vector2(x, y).magnitude); return d * d; });
            var ring = CreateTexture("FX_Ring", 256, (x, y) => { float d = new Vector2(x, y).magnitude; return Mathf.Clamp01(1f - Mathf.Abs(d - 0.96f) * 40f); });

            var particles = Shader.Find("SwordArt/FXUnlit");
            ParticleMat("FX_ParticleAdditive", particles, soft, true);
            ParticleMat("FX_ParticleAlpha", particles, soft, false);
            ParticleMat("FX_Tracer", particles, null, true);
            ParticleMat("FX_Shard", particles, null, true);
            var ringMat = ParticleMat("FX_ZoneRing", particles, ring, true);
            ringMat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.8f));

            var dome = GetOrCreate(FxDir + "FX_EnergyDome.mat", Shader.Find("SwordArt/EnergyDome"));
            dome.SetColor("_Color", new Color(0.35f, 0.75f, 1.8f, 1f));
            EditorUtility.SetDirty(dome);
        }

        private static Material ParticleMat(string name, Shader shader, Texture2D tex, bool additive)
        {
            var mat = GetOrCreate(FxDir + name + ".mat", shader);
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Intensity", additive ? 2f : 1f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Texture2D CreateTexture(string name, int size, System.Func<float, float, float> alpha)
        {
            string path = TexturesDir + name + ".png";
            if (!File.Exists(path))
            {
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size * 2f - 1f, v = (y + 0.5f) / size * 2f - 1f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha(u, v)));
                }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Remaps embedded FBX materials to the palette materials with the same name.</summary>
        [MenuItem("SwordArt/Setup/Remap Generated Model Materials", priority = 20)]
        public static void RemapGeneratedModels()
        {
            int n = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { Root + "Generated" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!(AssetImporter.GetAtPath(path) is ModelImporter importer)) continue;
                importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName, ModelImporterMaterialSearch.Everywhere);
                importer.SaveAndReimport();
                n++;
            }
            Debug.Log($"[ProjectSetup] Remapped materials for {n} models.");
        }

        // ---------------- Game data ----------------

        private static T Asset<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a != null) return a;
            a = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        private static GameObject Model(string relPath) => AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Generated/" + relPath + ".fbx");

        private static void CreateGameData()
        {
            string so = Root + "ScriptableObjects/";

            var pistol = Asset<WeaponData>(so + "Weapons/W_Pistol.asset");
            SetItem(pistol, "weapon_pistol", "Пистолет «Viper-9»", "Надёжный стартовый пистолет. Полуавтоматический огонь, быстрая перезарядка.", ItemType.Weapon, 1, new Color(0.95f, 0.6f, 0.3f));
            SetWeapon(pistol, WeaponType.Pistol, 22, 5, 70, 1.6f, 0.35f, false, 15, 4, 1.3f, 2f, 1.6f, 0.4f, 58, false, 1.05f,
                Model("Weapons/Weapon_Pistol"), new Vector3(0.16f, -0.15f, 0.42f), new Vector3(0f, -0.062f, 0.3f), new Vector3(0f, 0.035f, 0.17f), new Color(1f, 0.8f, 0.45f), 1.15f);

            var ar = Asset<WeaponData>(so + "Weapons/W_AssaultRifle.asset");
            SetItem(ar, "weapon_ar", "Штурмовая винтовка «AR-57 Strix»", "Автоматическая винтовка с коллиматором. Универсальна на средних дистанциях.", ItemType.Weapon, 1, new Color(0.3f, 1f, 0.8f));
            SetWeapon(ar, WeaponType.AssaultRifle, 18, 10, 120, 2.2f, 0.5f, true, 30, 4, 2.2f, 1.8f, 1.0f, 0.35f, 50, false, 0.95f,
                Model("Weapons/Weapon_AR"), new Vector3(0.2f, -0.23f, 0.42f), new Vector3(0f, -0.135f, 0.2f), new Vector3(0f, 0.03f, 0.76f), new Color(0.5f, 1f, 0.85f), 1f);

            var sniper = Asset<WeaponData>(so + "Weapons/W_SniperRifle.asset");
            SetItem(sniper, "weapon_sniper", "Снайперская винтовка «Longbow-X»", "Крупнокалиберная винтовка с оптикой ×4. Огромный урон, медленная стрельба.", ItemType.Weapon, 1, new Color(0.6f, 0.75f, 1f));
            SetWeapon(sniper, WeaponType.SniperRifle, 85, 0.9f, 300, 3.5f, 0.03f, false, 5, 4, 3.0f, 2.5f, 6f, 0.6f, 18, true, 0.85f,
                Model("Weapons/Weapon_Sniper"), new Vector3(0.21f, -0.24f, 0.4f), new Vector3(0f, -0.125f, 0.05f), new Vector3(0f, 0.035f, 1.08f), new Color(0.6f, 0.85f, 1f), 0.7f);

            var armor = Asset<EquipmentData>(so + "Items/E_Armor.asset");
            SetItem(armor, "equip_armor", "Бронежилет «Aegis»", "Лёгкая броня GGO: больше здоровья и снижение входящего урона.", ItemType.Equipment, 1, new Color(0.5f, 0.8f, 0.5f));
            armor.bonusMaxHealth = 25f;
            armor.damageReduction = 0.2f;
            EditorUtility.SetDirty(armor);

            var medkit = Asset<ConsumableData>(so + "Items/C_Medkit.asset");
            SetItem(medkit, "cons_medkit", "Аптечка «NanoHeal»", "Мгновенно восстанавливает 50 HP в GGO (клавиша H).", ItemType.Consumable, 5, new Color(1f, 0.4f, 0.45f));
            medkit.kind = ConsumableKind.Medkit;
            medkit.healAmount = 50f;
            EditorUtility.SetDirty(medkit);

            var ammo = Asset<ConsumableData>(so + "Items/C_AmmoPack.asset");
            SetItem(ammo, "cons_ammo", "Боекомплект «Ammo Box»", "Автоматически используется в начале матча: +2 запасных магазина к каждому оружию.", ItemType.Consumable, 5, new Color(1f, 0.8f, 0.3f));
            ammo.kind = ConsumableKind.AmmoPack;
            ammo.extraMagazines = 2;
            EditorUtility.SetDirty(ammo);

            var shopAr = Shop(so + "Economy/Shop_AssaultRifle.asset", "shop_ar", ar, 350);
            var shopSniper = Shop(so + "Economy/Shop_SniperRifle.asset", "shop_sniper", sniper, 650);
            var shopArmor = Shop(so + "Economy/Shop_Armor.asset", "shop_armor", armor, 300);
            var shopMedkit = Shop(so + "Economy/Shop_Medkit.asset", "shop_medkit", medkit, 40);
            var shopAmmo = Shop(so + "Economy/Shop_AmmoPack.asset", "shop_ammo", ammo, 30);

            var economy = Asset<EconomyConfig>(so + "Economy/EconomyConfig.asset");
            economy.startingCredits = 250;
            economy.starterItems = new List<ItemData> { pistol };
            economy.jobRewardPerTask = 15;
            economy.jobCompletionBonus = 30;
            economy.jobPerfectBonus = 20;
            economy.jobTasksPerShift = 6;
            economy.jobShiftDuration = 35f;
            economy.participationReward = 50;
            economy.rewardPerKill = 35;
            economy.winReward = 500;
            economy.podiumReward = 100;
            EditorUtility.SetDirty(economy);

            var db = Asset<ItemDatabase>(so + "Items/ItemDatabase.asset");
            db.items = new List<ItemData> { pistol, ar, sniper, armor, medkit, ammo };
            db.shopItems = new List<ShopItemData> { shopAr, shopSniper, shopArmor, shopMedkit, shopAmmo };
            EditorUtility.SetDirty(db);

            var config = Asset<GameConfig>(Root + "Resources/GameConfig.asset");
            config.itemDatabase = db;
            config.economy = economy;
            config.particleAdditive = AssetDatabase.LoadAssetAtPath<Material>(FxDir + "FX_ParticleAdditive.mat");
            config.particleAlpha = AssetDatabase.LoadAssetAtPath<Material>(FxDir + "FX_ParticleAlpha.mat");
            config.tracerMaterial = AssetDatabase.LoadAssetAtPath<Material>(FxDir + "FX_Tracer.mat");
            config.shardMaterial = AssetDatabase.LoadAssetAtPath<Material>(FxDir + "FX_Shard.mat");
            EditorUtility.SetDirty(config);

            CreateBotProfiles(so, pistol, ar, sniper);
            CreateNpcData(so);
        }

        private static void SetItem(ItemData item, string id, string name, string desc, ItemType type, int maxStack, Color accent)
        {
            item.id = id;
            item.displayName = name;
            item.description = desc;
            item.itemType = type;
            item.maxStack = maxStack;
            item.accentColor = accent;
            EditorUtility.SetDirty(item);
        }

        private static void SetWeapon(WeaponData w, WeaponType type, float damage, float fireRate, float range, float spread, float aimSpread,
            bool auto, int mag, int reserveMags, float reload, float headshot, float recoil, float recoilH, float aimFov, bool scope,
            float mobility, GameObject model, Vector3 view, Vector3 aim, Vector3 muzzle, Color tracer, float pitch)
        {
            w.weaponType = type;
            w.damage = damage;
            w.fireRate = fireRate;
            w.range = range;
            w.spread = spread;
            w.aimSpread = aimSpread;
            w.automatic = auto;
            w.magazineSize = mag;
            w.reserveMagazines = reserveMags;
            w.reloadTime = reload;
            w.headshotMultiplier = headshot;
            w.recoil = recoil;
            w.recoilHorizontal = recoilH;
            w.aimFov = aimFov;
            w.hasScope = scope;
            w.mobility = mobility;
            w.modelPrefab = model;
            w.viewOffset = view;
            w.aimOffset = aim;
            w.muzzleLocalPosition = muzzle;
            w.tracerColor = tracer;
            w.soundPitch = pitch;
            EditorUtility.SetDirty(w);
        }

        private static ShopItemData Shop(string path, string id, ItemData item, int price)
        {
            var s = Asset<ShopItemData>(path);
            s.id = id;
            s.displayName = item.displayName;
            s.description = item.description;
            s.itemType = item.itemType;
            s.item = item;
            s.price = price;
            s.quantity = 1;
            EditorUtility.SetDirty(s);
            return s;
        }

        private static void CreateBotProfiles(string so, WeaponData pistol, WeaponData ar, WeaponData sniper)
        {
            Bot(so + "Bots/Bot_Rifleman.asset", "Стрелок", ar, 100, 60, 120, 0.6f, 2.8f, 0.4f, 0.7f, new Vector2Int(3, 6), new Vector2(0.5f, 1.2f), 12, 35, 0.45f, 3.2f, 5.4f,
                new Color(0.33f, 0.36f, 0.28f), new Color(1f, 0.5f, 0.15f));
            Bot(so + "Bots/Bot_Rusher.asset", "Штурмовик", ar, 110, 50, 140, 0.5f, 3.2f, 0.38f, 0.55f, new Vector2Int(4, 8), new Vector2(0.4f, 0.9f), 5, 15, 0.3f, 3.6f, 6.2f,
                new Color(0.45f, 0.18f, 0.16f), new Color(1f, 0.25f, 0.2f));
            Bot(so + "Bots/Bot_Sniper.asset", "Снайпер", sniper, 90, 120, 90, 1.0f, 0.7f, 0.55f, 1.4f, new Vector2Int(1, 1), new Vector2(1.6f, 2.6f), 30, 90, 0.6f, 3.0f, 5.0f,
                new Color(0.62f, 0.55f, 0.4f), new Color(0.3f, 0.9f, 1f));
            Bot(so + "Bots/Bot_Gunslinger.asset", "Дуэлянт", pistol, 100, 55, 130, 0.55f, 2.6f, 0.45f, 0.6f, new Vector2Int(2, 4), new Vector2(0.4f, 0.8f), 7, 20, 0.4f, 3.5f, 6.0f,
                new Color(0.25f, 0.3f, 0.4f), new Color(0.9f, 0.3f, 1f));
        }

        private static void Bot(string path, string archetype, WeaponData weapon, float hp, float sight, float fov, float reaction, float spreadMul,
            float dmgMul, float aimTime, Vector2Int burst, Vector2 pause, float minD, float maxD, float cover, float walk, float run, Color outfit, Color accent)
        {
            var b = Asset<BotProfile>(path);
            b.archetype = archetype;
            b.weapon = weapon;
            b.maxHealth = hp;
            b.sightRange = sight;
            b.fieldOfView = fov;
            b.hearingRange = 45f;
            b.reactionTime = reaction;
            b.spreadMultiplier = spreadMul;
            b.damageMultiplier = dmgMul;
            b.aimTime = aimTime;
            b.burstSize = burst;
            b.burstPause = pause;
            b.preferredMinDistance = minD;
            b.preferredMaxDistance = maxD;
            b.coverHealthThreshold = cover;
            b.walkSpeed = walk;
            b.runSpeed = run;
            b.outfitColor = outfit;
            b.accentColor = accent;
            EditorUtility.SetDirty(b);
        }

        private static void CreateNpcData(string so)
        {
            Npc(so + "NPC/NPC_Ayano.asset", "Аяно", "Студентка", new Color(0.85f, 0.45f, 0.55f), new Color(0.2f, 0.22f, 0.35f), new Color(0.35f, 0.2f, 0.12f), 1.3f,
                new[] { "Привет! Ты тоже играешь в VRMMO? Говорят, после того инцидента с NerveGear всё стало гораздо безопаснее.",
                        "AmuSphere ограничивает сигналы, так что никакой опасности. Просто очень, очень реалистично." },
                new[] { "Слышала про Bullet of Bullets? Турнир в Gun Gale Online, где выживает только один.",
                        "Мой брат говорит, что там видно красную линию, по которой полетит пуля противника. Жутко, правда?" });
            Npc(so + "NPC/NPC_Kenta.asset", "Кэнта", "Продавец", new Color(0.15f, 0.42f, 0.68f), new Color(0.15f, 0.15f, 0.18f), new Color(0.1f, 0.08f, 0.06f), 1.2f,
                new[] { "Добро пожаловать в «Arms & Gear 24»! Мы продаём цифровые лицензии на снаряжение для GGO.",
                        "Купленное здесь сразу появится в вашем инвентаре, когда вы подключитесь через FullDive." },
                new[] { "Совет от продавца: аптечки дёшевы, а спасают чаще, чем кажется.",
                        "Если денег не хватает — в «Nishikawa Data» через дорогу всегда нужны люди на ввод данных." });
            Npc(so + "NPC/NPC_Misaki.asset", "Мисаки", "Офисный работник", new Color(0.92f, 0.92f, 0.95f), new Color(0.12f, 0.12f, 0.15f), new Color(0.12f, 0.08f, 0.05f), 1.4f,
                new[] { "Уф, ещё одна смена ввода данных... Платят немного, но стабильно.",
                        "Терминал для подработки — на первом этаже нашего офиса. Просто подойди к столу." },
                new[] { "Иногда думаю: может, тоже стать профи в GGO? Говорят, лучшие игроки зарабатывают реальные деньги." });
            Npc(so + "NPC/NPC_Jun.asset", "Дзюн", "Геймер", new Color(0.12f, 0.12f, 0.12f), new Color(0.25f, 0.25f, 0.3f), new Color(0.05f, 0.05f, 0.05f), 1.6f,
                new[] { "Ты в BoB заходишь? Главное — следи за зоной. Энергетический купол сжимается, и снаружи тебя медленно поджаривает.",
                        "И не стой на открытом месте, особенно против снайперов. Красная линия — это предупреждение, а не приглашение." },
                new[] { "Я коплю на снайперскую винтовку. Шестьсот пятьдесят кредитов... Ещё пара смен в офисе.",
                        "Кстати, Tab во время матча показывает всех участников. Удобно." });
            Npc(so + "NPC/NPC_Hana.asset", "Бабушка Хана", "Пенсионерка", new Color(0.55f, 0.4f, 0.6f), new Color(0.3f, 0.25f, 0.3f), new Color(0.8f, 0.8f, 0.8f), 0.9f,
                new[] { "Ах, молодёжь... Всё время в этих своих виртуальных мирах.",
                        "Не забывай есть и спать, внучок. Реальный мир тоже стоит того, чтобы в нём жить." },
                new[] { "В этом парке сакура цветёт каждую весну. Никакая игра такого не покажет." });
            Npc(so + "NPC/NPC_Ryo.asset", "Рё", "Курьер", new Color(0.95f, 0.65f, 0.15f), new Color(0.2f, 0.3f, 0.45f), new Color(0.3f, 0.15f, 0.08f), 1.8f,
                new[] { "Извини, спешу! Сегодня полно доставок — все заказывают новые AmuSphere.",
                        "Говорят, к турниру BoB регистрируется рекордное количество игроков." });
            Npc(so + "NPC/NPC_Yuki.asset", "Юки", "Школьница", new Color(0.95f, 0.95f, 0.98f), new Color(0.15f, 0.2f, 0.45f), new Color(0.1f, 0.05f, 0.03f), 1.3f,
                new[] { "Торговый автомат на углу — лучший! Попробуй «Ramune».",
                        "Мой персонаж в GGO — маленькая девочка с огромным пулемётом. Никто не воспринимает её всерьёз. До первого выстрела." });
        }

        private static void Npc(string path, string name, string role, Color shirt, Color pants, Color hair, float speed, params string[][] dialogues)
        {
            var n = Asset<NPCData>(path);
            n.displayName = name;
            n.role = role;
            n.shirtColor = shirt;
            n.pantsColor = pants;
            n.hairColor = hair;
            n.walkSpeed = speed;
            n.dialogues = new List<DialogueEntry>();
            foreach (var d in dialogues) n.dialogues.Add(new DialogueEntry { lines = new List<string>(d) });
            EditorUtility.SetDirty(n);
        }

        // ---------------- Build settings ----------------

        private static void ConfigureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var path in new[]
                     {
                         Root + "Scenes/Bootstrap/MainMenu.unity", Root + "Scenes/RealWorld/City.unity",
                         Root + "Scenes/Apartment/Apartment.unity", Root + "Scenes/GGO/GGO_Arena.unity"
                     })
                scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
