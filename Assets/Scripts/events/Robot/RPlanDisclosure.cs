using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    internal class RPlanDisclosure : PlayerEvent
    {
        private Plan plan;
        private string condition;
        private string color;
        private const string CSAFEGUARDCUBESUPPLY = "SafeguardCubeSupply";
        private const string CSAFEGUARDOUTBREAK = "SafeguardOutbreak";
        private const string CSHAREKNOWLEDGEILLENDINCITY = "ShareKnowledgeIllEndInCity";
        private const string CSHAREKNOWLEDGEGIVEYOUACARD = "ShareKnowledgeGiveYouACard";
        private const string CSHAREKNOWLEDGEGIVEYOUACARDSETCOMPLETE = "ShareKnowledgeGiveYouACardSetComplete";
        private const string CSHAREKNOWLEDGETAKECARDFROMYOU = "ShareKnowledgeTakeCardFromYou";
        private const string CSHAREKNOWLEDGETAKECARDFROMYOUCOMPLETE = "ShareKnowledgeTakeCardFromYouComplete";
        private const string CFINDCURE = "FindCure";
        private const string CFINDCURECOMPLETE = "FindCureComplete";
        private const string CMANAGINGDISEASE = "ManagingDisease";

        public RPlanDisclosure(Player player, Plan plan, Player partner) : base(player)
        {
            this.plan = plan;
            color = plan.TargetColor.ToString();
            switch (plan.PlanPriority)
            {
                case Plan.PlanPriorities.ShareKnowledge:
                    bool iHaveTheCard = _player.Hand.Contains(plan.TargetCity);
                    VirusName targetColor = CityDrawer.CityScripts[plan.TargetCity].CityCard.VirusInfo.virusName;
                    color = targetColor.ToString();
                    if (plan.ActionQueue.Any(a => a.Type == ActionType.Share))
                    {
                        if (iHaveTheCard)
                        {
                            switch (targetColor)
                            {
                                case VirusName.Red:
                                    if (partner.RedCardsInHand.Count == 3)
                                        condition = CSHAREKNOWLEDGEGIVEYOUACARDSETCOMPLETE;
                                    else
                                        condition = CSHAREKNOWLEDGEGIVEYOUACARD;
                                    break;
                                case VirusName.Yellow:
                                    if (partner.YellowCardsInHand.Count == 3)
                                        condition = CSHAREKNOWLEDGEGIVEYOUACARDSETCOMPLETE;
                                    else
                                        condition = CSHAREKNOWLEDGEGIVEYOUACARD;
                                    break;
                                case VirusName.Blue:
                                    if (partner.BlueCardsInHand.Count == 3)
                                        condition = CSHAREKNOWLEDGEGIVEYOUACARDSETCOMPLETE;
                                    else
                                        condition = CSHAREKNOWLEDGEGIVEYOUACARD;
                                    break;
                            }
                        }
                        else
                        {
                            switch (targetColor)
                            {
                                case VirusName.Red:
                                    if (_player.RedCardsInHand.Count == 3)
                                        condition = CSHAREKNOWLEDGETAKECARDFROMYOUCOMPLETE;
                                    else
                                        condition = CSHAREKNOWLEDGETAKECARDFROMYOU;
                                    break;
                                case VirusName.Yellow:
                                    if (_player.YellowCardsInHand.Count == 3)
                                        condition = CSHAREKNOWLEDGETAKECARDFROMYOUCOMPLETE;
                                    else
                                        condition = CSHAREKNOWLEDGETAKECARDFROMYOU;
                                    break;
                                case VirusName.Blue:
                                    if (_player.BlueCardsInHand.Count == 3)
                                        condition = CSHAREKNOWLEDGETAKECARDFROMYOUCOMPLETE;
                                    else
                                        condition = CSHAREKNOWLEDGETAKECARDFROMYOU;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        condition = CSHAREKNOWLEDGEILLENDINCITY;
                    }
                    break;
                case Plan.PlanPriorities.FindCure:
                    if (plan.ActionQueue.Any(a => a.Type == ActionType.FindCure))
                        condition = CFINDCURECOMPLETE;
                    else condition = CFINDCURE;
                    break;
                case Plan.PlanPriorities.SafeguardOutbreak:
                    condition = CSAFEGUARDOUTBREAK;
                    break;
                case Plan.PlanPriorities.SafeguardCubeSupply:
                    condition = CSAFEGUARDCUBESUPPLY;
                    break;
                case Plan.PlanPriorities.ManagingDisease:
                    condition = CMANAGINGDISEASE;
                    break;
            }

        }

        public override Dictionary<string, object> GetLogInfo()
        {
            return new Dictionary<string, object>
            {
                { "condition", condition },
                { "city", plan.TargetCity },
                { "color", color }
            };
        }
    }
}