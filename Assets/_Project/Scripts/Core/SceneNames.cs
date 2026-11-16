namespace SwordArt.Core
{
    /// <summary>Scene names used by the <see cref="SceneLoader"/>. Must match Build Settings.</summary>
    public static class SceneNames
    {
        public const string MainMenu = "MainMenu";
        public const string City = "City";
        public const string Apartment = "Apartment";
        public const string GGO = "GGO_Arena";

        /// <summary>True for scenes that belong to the real world (position is saved there).</summary>
        public static bool IsRealWorld(string scene) => scene == City || scene == Apartment;
    }

    /// <summary>Well-known spawn point identifiers.</summary>
    public static class SpawnIds
    {
        /// <summary>Use the saved real-world position if it belongs to the loaded scene.</summary>
        public const string Saved = "Saved";
        public const string Default = "Default";
        public const string CityApartmentEntrance = "ApartmentEntrance";
        public const string ApartmentDoor = "ApartmentDoor";
        public const string ApartmentFullDive = "FullDive";
        public const string GGOLobby = "GGOLobby";
    }

    /// <summary>Which kind of world a scene represents; drives UI theme and pause menu options.</summary>
    public enum WorldType
    {
        Menu,
        RealWorld,
        Virtual
    }
}
