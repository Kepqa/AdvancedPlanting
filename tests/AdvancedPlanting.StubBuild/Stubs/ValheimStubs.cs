// Minimal stand-ins for the Valheim classes the plugin touches, as they look after
// publicizing assembly_valheim.dll. Compile-only; signatures are from memory of recent builds
// and must be confirmed against the real assembly.
#pragma warning disable IDE0060, CS1591, CS0649
using System;
using UnityEngine;

public class ZNetView : MonoBehaviour
{
    public static bool m_forceDisableInit;
}

public static class ZInput
{
    public static bool GetKeyDown(KeyCode key, bool logWarning = true) => false;
    public static bool GetKey(KeyCode key, bool logWarning = true) => false;
    public static float GetMouseScrollWheel() => 0f;
}

public class MessageHud
{
    public enum MessageType { TopLeft = 1, Center = 2 }
}

public class Localization
{
    public static Localization instance;
    public string Localize(string text) => text;
}

public class Hud
{
    public static bool IsPieceSelectionVisible() => false;
}

public class ItemDrop : MonoBehaviour
{
    public class ItemData
    {
        public class SharedData
        {
            public string m_name;
        }
        public SharedData m_shared = new SharedData();
    }
    public ItemData m_itemData = new ItemData();
}

public class Inventory
{
    public int CountItems(string name, int quality = -1, bool matchWorldLevel = true) => 0;
}

public class Heightmap : MonoBehaviour
{
    [Flags]
    public enum Biome
    {
        None = 0,
        Meadows = 1,
        Swamp = 2,
        Mountain = 4,
        BlackForest = 8,
        Plains = 16,
        AshLands = 32,
        DeepNorth = 64,
        Ocean = 256,
        Mistlands = 512,
    }

    public static Heightmap FindHeightmap(Vector3 point) => null;
    public static Biome FindBiome(Vector3 point) => Biome.None;
    public bool IsCultivated(Vector3 worldPos) => false;
}

public class ZoneSystem : MonoBehaviour
{
    public static ZoneSystem instance;
    public float m_waterLevel = 30f;
    public bool GetGroundHeight(Vector3 p, out float height) { height = 0f; return false; }
}

public static class Location
{
    public static bool IsInsideNoBuildLocation(Vector3 point) => false;
}

public static class PrivateArea
{
    public static bool CheckAccess(Vector3 point, float radius = 0f, bool flash = true, bool wardCheck = false) => true;
}

public class Piece : MonoBehaviour
{
    [Serializable]
    public class Requirement
    {
        public ItemDrop m_resItem;
        public int m_amount;
    }

    public string m_name;
    public bool m_cultivatedGroundOnly;
    public bool m_noInWater;
    public Requirement[] m_resources;
    public void SetInvalidPlacementHeightlight(bool enabled) { }
}

public class Plant : MonoBehaviour
{
    public float m_growRadius;
    public bool m_needCultivatedGround;
    public Heightmap.Biome m_biome;
    public LayerMask m_spaceMask;
}

public class Character : MonoBehaviour
{
    public void Message(MessageHud.MessageType type, string msg, int amount = 0, object icon = null) { }
}

public class Humanoid : Character
{
    public Inventory GetInventory() => null;
}

public class Player : Humanoid
{
    public enum RequirementMode { CanBuild, IsKnown, CanAlmostBuild }

    public static Player m_localPlayer;
    public GameObject m_placementGhost;
    public int m_placeRotation;

    public bool InPlaceMode() => false;
    public Piece GetSelectedPiece() => null;
    public bool HaveRequirements(Piece piece, RequirementMode mode) => false;
    public bool TakeInput() => true;
    public bool PlacePiece(Piece piece, Vector3 pos, Quaternion rot, bool doAttack) => false;
}
