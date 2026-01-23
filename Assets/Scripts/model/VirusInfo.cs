using UnityEngine;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    /// <summary>
    /// Stores information about a virus, including its name, color, and icon.
    /// </summary>
    [CreateAssetMenu(fileName = "New Virus Icon", menuName = "Icons/Virus Icon")]
    public class VirusInfo : ScriptableObject
    {
        /// <summary>
        /// The name of the virus.
        /// </summary>
        public VirusName virusName;

        /// <summary>
        /// The color representing the virus.
        /// </summary>
        public Color virusColor;

        /// <summary>
        /// The artwork/icon representing the virus.
        /// </summary>
        public Sprite artwork;
    }
}
