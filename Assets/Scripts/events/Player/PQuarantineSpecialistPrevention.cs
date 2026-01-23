using DG.Tweening;
using UnityEngine.UI;

namespace OPEN.PandemicAI
{
    internal class PQuarantineSpecialistPrevention : PlayerEvent
    {
        private int cityToInfect;

        public PQuarantineSpecialistPrevention(Player player, int cityToInfect) : base(player)
        {
            this.cityToInfect = cityToInfect;
        }

        public override float Act()
        {
            Outline outline = _panel.pawnAnimator.roleCard.GetOutlineComponent();
            Sequence sequence = AnimationTemplates.FadeOutline(outline).Play();
            return sequence.Duration();
        }
    }
}