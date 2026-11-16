# Asset Registry

Все модели созданы процедурно в Blender 5.2 (скрипты `Tools/Blender/`), 1 BU = 1 м, экспорт FBX,
модели смотрят в +Z Unity, pivot — у основания (у оружия — верх рукояти). Коллайдеры добавляют билдеры сцен.

## Модели (`Assets/_Project/Generated/`)

| Категория | Файлы | Скрипт | Примечания |
|---|---|---|---|
| Characters | `Character_Male`, `Character_Female`, `GGO_Soldier` | `models_characters.py` | Сегменты с суставами `Torso/Head/Arm_L/Arm_R/Leg_L/Leg_R` (процедурная анимация; Humanoid-риг можно добавить позже). Солдат: шлем с визором, жилет, рюкзак, наколенники |
| Weapons | `Weapon_Pistol`, `Weapon_AR`, `Weapon_Sniper` | `models_weapons.py` | Раздельные части: `Body`, `Barrel`, `Magazine`, `Grip`, `Stock`, `Sight/Scope`, `Slide` — готовы к анимации; светящиеся акценты |
| Props | `FullDive_Headset` | `models_weapons.py` | Двойное кольцо-обруч с визором и узлами, док-станция; объект `Glow` пульсирует |
| Buildings | `Building_Apartment`, `Building_Shop` (входной интерьер), `Building_Office` (входное лобби), `House_A/B`, `Building_Tower_A/B` | `models_city.py` | Окна, балконы, бак на крыше, вывески-3D-текст («SAKURA HEIGHTS», «ARMS & GEAR 24», «NISHIKAWA DATA») |
| Props (город) | `StreetLamp`, `Bench`, `TrashBins`, `VendingMachine`, `RoadSign_Stop`, `UtilityPole`, `Planter`, `Fountain`, `BusStop`, `Tree_A/B` | `models_props.py` | |
| Props (интерьеры) | `ShopShelf`, `ShopCounter`, `WeaponRack`, `OfficeDesk`, `ReceptionDesk`, `OfficePlant`, `Apartment_Room`, `Bed`, `Nightstand`, `PCDesk`, `Bookshelf`, `Kitchenette`, `Rug`, `Sofa`, `TVStand` | `models_props.py` | |
| Environment (GGO) | `GGO_Terrain` (260×260 м, холмы, берма), `Ruin_A/B/C`, `Container_Red/Blue`, `Crate`, `CrateStack`, `Barrier`, `SandbagWall`, `Rock_A/B/C`, `WreckedTank`, `Platform_Tower` (вышка с пандусом), `AntennaMast`, `Girders`, `Lobby_Hall` (3D-баннер BULLET OF BULLETS) | `models_ggo.py` | |
| Props (GGO) | `Terminal`, `TargetDummy` | `models_ggo.py` | |

## Материалы (`Assets/_Project/Art/Materials/`)

65 URP Lit материалов `M_*` из общей палитры `PALETTE` (`Tools/Blender/sao_lib.py`) — одинаковые имена
в Blender и Unity, привязка по имени. Эмиссивные: `M_LampGlow`, `M_NeonBlue/Pink`, `M_Screen`, `M_Accent`,
`M_GunGlow`, `M_DeviceGlow`, `M_GlowOrange/Cyan`. Прозрачный: `M_GlassClear`.

FX (`Art/Materials/FX/`): `FX_ParticleAdditive`, `FX_ParticleAlpha`, `FX_Tracer`, `FX_Shard`, `FX_ZoneRing`
(шейдер `SwordArt/FXUnlit`), `FX_EnergyDome` (шейдер `SwordArt/EnergyDome`).
Текстуры: `Art/Textures/FX_Soft.png`, `FX_Ring.png` (сгенерированы). Скайбоксы и Volume-профили: `Assets/_Project/Settings/`.

## Префабы
- `Prefabs/Characters/Bot.prefab` — бот турнира (NavMeshAgent, Health, Participant, BotController, Hitbox головы, bullet line, имя).

## Данные (`Assets/_Project/ScriptableObjects/`, `Resources/GameConfig.asset`)
- Weapons: `W_Pistol`, `W_AssaultRifle`, `W_SniperRifle`.
- Items: `E_Armor`, `C_Medkit`, `C_AmmoPack`, `ItemDatabase`.
- Economy: `EconomyConfig`, `Shop_AssaultRifle`, `Shop_SniperRifle`, `Shop_Armor`, `Shop_Medkit`, `Shop_AmmoPack`.
- NPC: `NPC_Ayano`, `NPC_Kenta`, `NPC_Misaki`, `NPC_Jun`, `NPC_Hana`, `NPC_Ryo`, `NPC_Yuki`.
- Bots: `Bot_Rifleman`, `Bot_Rusher`, `Bot_Sniper`, `Bot_Gunslinger`.

## Звук
Внешних аудиофайлов нет: `AudioService` синтезирует 19 эффектов (выстрелы трёх типов, перезарядка, щелчок
пустого магазина, хитмаркер, убийство, попадания, UI, монеты, отсчёт, «Link Start», лечение, предупреждение зоны)
и два эмбиента (город, ветер пустоши). Папки `Audio/Music|SFX|Ambient` зарезервированы под реальные клипы.

## Заглушки
- `WeaponPlaceholder` — коробочная модель оружия, если у `WeaponData` нет `modelPrefab` (сейчас у всех есть модели).
- Иконки товаров — текстовые глифы (AR/SNP/ARM/+), поле `icon` в SO оставлено для будущих спрайтов.
