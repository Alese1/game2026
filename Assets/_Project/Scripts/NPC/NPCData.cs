using System.Collections.Generic;
using UnityEngine;

namespace SwordArt.NPC
{
    /// <summary>One conversation (sequence of lines).</summary>
    [System.Serializable]
    public class DialogueEntry
    {
        [TextArea(2, 4)] public List<string> lines = new List<string>();
    }

    /// <summary>
    /// Identity, look and dialogue of a city NPC.
    /// </summary>
    [CreateAssetMenu(menuName = "SwordArt/NPC/NPC Data", fileName = "NPC_")]
    public class NPCData : ScriptableObject
    {
        public string displayName = "Прохожий";
        [Tooltip("Short role shown under the name, e.g. 'Студентка'.")]
        public string role = "";
        public Color shirtColor = new Color(0.3f, 0.4f, 0.6f);
        public Color pantsColor = new Color(0.15f, 0.15f, 0.2f);
        public Color hairColor = new Color(0.1f, 0.08f, 0.06f);
        public Color skinColor = new Color(0.95f, 0.8f, 0.68f);
        [Min(0.5f)] public float walkSpeed = 1.4f;
        [Tooltip("Conversations cycle each time the player talks to the NPC.")]
        public List<DialogueEntry> dialogues = new List<DialogueEntry>();
    }
}
