# Architecture

Unity 6000.5.1f1 · URP 17.5 · Input System 1.19 · Cinemachine 3.1.7 · AI Navigation 2.0.13 · uGUI.
Один runtime-assembly (`Assembly-CSharp`), namespace `SwordArt.*`; редакторский код — `Assets/_Project/Scripts/Editor`.

## Composition root

`GameManager` (единственный глобальный синглтон, `DontDestroyOnLoad`) создаётся автоматически
`[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`, поэтому любую сцену можно запускать напрямую.
Он загружает `Resources/GameConfig` и владеет сервисами:

```
GameManager
 ├─ JsonSaveSystem   : ISaveSystem        — файл, .bak, карантин
 ├─ CurrencyService  : ICurrencyService   ┐ работают поверх ОДНОГО экземпляра SaveData
 ├─ InventoryService : IInventoryService  ┘ (нет дублирования валюты между сценами)
 ├─ ShopSystem                            — CanPurchase / TryPurchase (+ откат)
 ├─ GameInput (GameAction ×15)            — Input System actions в коде (+ слой симуляции для автотестов)
 ├─ AudioService                          — пул AudioSource, процедурные клипы, эмбиенс
 ├─ UIManager                             — канвасы, стек окон, Esc, темы, уведомления
 └─ SceneLoader                           — async-загрузка за экраном загрузки
```

Сцены сообщают о себе через `SceneContext` (WorldType Menu/RealWorld/Virtual, игрок, спавн):
он вызывает `GameManager.EnterScene` (старт сессии при прямом запуске сцены, смена темы UI и эмбиенса)
и ставит игрока на `SpawnPoint` или на сохранённую позицию.

## События

- `ICurrencyService.CreditsChanged(balance, delta, reason)` → HUD кредитов, уведомления.
- `IInventoryService.Changed`, `ShopSystem.Purchased` → автосохранение.
- `Health.Damaged / Died / Changed`, `Participant.Eliminated` → турнир, HUD, ИИ.
- `TournamentManager.StateChanged / CountdownTick / ParticipantEliminated / MatchFinished` → HUD, результат.
- `Hitscan.ShotFired` → «слух» ботов. `GameSettings.Changed`, `InputGate.Changed`.

## Системы

| Папка | Ключевые классы |
|---|---|
| Core | `GameManager`, `SceneLoader`, `SceneContext`, `GameInput`/`GameAction`, `InputGate` (блокировка ввода + курсор), `GameSettings`, `StateMachine`/`IState`, `GameConfig`, `AudioService` |
| SaveSystem | `SaveData`, `ProgressData`, `JsonSaveSystem` |
| Economy | `CurrencyService`, `ShopSystem`, `ShopItemData` (SO), `EconomyConfig` (SO) |
| Inventory | `ItemData` (SO), `EquipmentData`, `ConsumableData`, `ItemDatabase`, `InventoryService` |
| Weapons | `WeaponData` (SO), `PlayerWeaponController`, `WeaponPlaceholder` |
| Combat | `Health`/`IDamageable`, `Hitbox`, `Hitscan`, `SurfaceTag`, `CombatFX` (пулы частиц/трассеров/вспышек) |
| Player | `ThirdPersonController`, `ThirdPersonCameraRig` (+ CinemachineThirdPersonFollow), `FirstPersonController`, `ProceduralCharacterAnimator` |
| Interaction | `IInteractable`, `InteractableBase`, `Interactor`, `InteractableHighlight` |
| World | `SceneDoor`, `ShopCounter`, `JobTerminal`, `FullDiveDevice`, `BedSavePoint`, `InfoPoint`, `VendingMachine`, `SpawnPoint` |
| NPC | `NPCData` (SO), `NPCController` (Idle/Walking/Interacting), `CharacterAppearance` |
| AI | `BotController` (8 состояний), `BotProfile` (SO), `CoverPoint` |
| Tournament | `TournamentManager`, `Participant`, `SafeZone`/`ZonePhase`, `GGOPlayer`, `TournamentTerminal` |
| UI | `UIKit`, `UITheme`, `UIManager`, `UIWindow` (база модальных окон), `LoadingScreen`, `NotificationFeed`, `InteractionPrompt`, `CurrencyHUD`, `PauseMenu`, `SettingsWindow`, `DialogueWindow`, `ShopWindow`, `JobWindow`, `GGOHUD`, `ResultWindow`, `MainMenuController`, `WorldNameTag`, `MenuSpin` |

### Ввод и UI-окна
Контроллеры читают `GameManager.Input` и проверяют `InputGate.IsBlocked`. Любой `UIWindow.Open()` блокирует ввод,
освобождает курсор и попадает в стек `UIManager`; Esc закрывает верхнее окно, иначе открывает паузу.
`PauseMenu` ставит `Time.timeScale = 0`. UI анимируется на unscaled time.

### Персонажи
Сегментированные low-poly модели из Blender с опорными точками суставов (`Torso`, `Head`, `Arm_L/R`, `Leg_L/R`).
`ProceduralCharacterAnimator` даёт ходьбу/бег, дыхание, «боевую» позу и падение при смерти — без риггинга.
Цвета NPC/ботов — `MaterialPropertyBlock` поверх общих материалов (`M_Shirt`, `M_Pants`, `M_Outfit`…).

### Рендер
URP Forward, ACES, Bloom, Color Adjustments, Vignette (свои профили на каждую сцену). Освещение realtime
(без запекания), trilight ambient, процедурное небо, realtime reflection probe (OnAwake).
В GGO оружие рисуется overlay-камерой (`FPWeapon` слой) — нет клиппинга в стены.
Шейдеры: `SwordArt/EnergyDome` (купол), `SwordArt/FXUnlit` (частицы, трассеры, bullet line с вертексным цветом).

## Контент-пайплайн

1. **Blender** (через Unity MCP `blender_bridge`): скрипты `Tools/Blender/*.py` строят модели из примитивов
   (`sao_lib.py`: box/cyl/sphere/ico/torus/text/terrain, палитра материалов `PALETTE`), разворачивают геометрию
   (−Y → +Z), экспортируют FBX в `Assets/_Project/Generated/<Category>/`.
2. **Импорт**: `GeneratedModelPostprocessor` — scale 1, `bakeAxisConversion`, без анимаций/камер/света.
3. **Материалы**: `ProjectSetup` парсит ту же `PALETTE` из `sao_lib.py` и создаёт URP-материалы
   `Art/Materials/M_*.mat`, затем `SearchAndRemapMaterials` привязывает FBX к ним по имени.
4. **Данные**: `ProjectSetup` создаёт все SO (оружие, предметы, магазин, экономика, NPC, профили ботов, GameConfig).
5. **Сцены**: детерминированные билдеры (`CityBuilder`, `ApartmentBuilder`, `GGOBuilder`, `MainMenuBuilder`)
   запускаются вручную из меню **SwordArt/Build/...**; они расставляют модели с коллайдерами, свет, volume,
   NPC, интерактивные объекты, запекают NavMesh (asset рядом со сценой), создают префаб бота и укрытия.
   **SwordArt/Build/ALL** пересобирает всё с нуля. Ничего не перестраивается автоматически при запуске.

## Тестирование через MCP
Unity в фоне не выполняет player loop и отбрасывает ввод устройств, поэтому автотесты шагают кадры
`EditorApplication.Step()` на паузе и подают ввод через `GameAction.SimulatePress/SimulateHold/SimulateValue`.
`SwordArt/Dev/Keep Play Mode Alive` (по умолчанию выкл.) — продолжать Play Mode, когда редактор не в фокусе.
