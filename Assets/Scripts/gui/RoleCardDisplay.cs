using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace OPEN.PandemicAI
{
    public class RoleCardDisplay : MonoBehaviour
    {
        public RoleCard RoleCardData;

        [SerializeField] private Image background;
        [SerializeField] private Image roleImage;
        [SerializeField] private TextMeshProUGUI roleName;
        [SerializeField] private TextMeshProUGUI roleText;

        public void Awake()
        {
            UpdateRoleUI();
        }

        private void UpdateRoleUI()
        {
            roleName.text = RoleCardData.roleName;
            roleText.text = RoleCardData.roleText;
            roleImage.sprite = RoleCardData.roleArtwork;
            background.color = RoleCardData.roleColor;
        }

        public void SetOutlineEnabled(bool enabled)
        {
            var outline = background.gameObject.GetComponent<Outline>();
            if (outline != null)
            {
                outline.enabled = enabled;
            }
            else
            {
                Debug.LogWarning("Outline component not found on background.");
            }
        }

        public Outline GetOutlineComponent()
        {
            return background.gameObject.GetComponent<Outline>();
        }
    }
}
