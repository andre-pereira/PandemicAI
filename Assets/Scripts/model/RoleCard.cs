using UnityEngine;

namespace OPEN.PandemicAI
{
    /// <summary>
    /// Represents a role card in the game, storing role name, description, color, and artwork.
    /// </summary>
    [CreateAssetMenu(fileName = "New Role Card", menuName = "Cards/Role Card")]
    public class RoleCard : ScriptableObject
    {
        public string roleName;

        public string roleText;

        public Color roleColor;

        public Sprite roleArtwork;
    }
}
