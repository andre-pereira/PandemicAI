using TMPro;
using UnityEngine;

namespace OPEN.PandemicAI
{
    /// <summary>
    /// Very small wrapper around the TMP text so the presenter doesn’t touch TMP directly.
    /// </summary>
    public class InstructionPresenter : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI label;

        public void Show(string msg) => label.text = msg;
    }
}
