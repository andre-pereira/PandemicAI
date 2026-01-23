using OPEN.PandemicAI;
using UnityEngine;
using static OPEN.PandemicAI.Enums;
using TMPro;
using UnityEngine.UI;
using System.Threading;

public class AIPlayer : MonoBehaviour
{
    public int playerIndex;
    private Plan playerPlan;
    private Player player;
    private Player partner;
    public TextMeshProUGUI textbox;
    public Image ourPanel;
    public Image partnerPanel;
    private PandemicAI engine;
    private bool drawStep = false;

    private void Start()
    {
        engine = new PandemicAI();
    }

    public void Initialize(Player me, Player partnerPlayer)
    {
        ourPanel.color = me.GetRoleCard().roleColor;
        ourPanel.gameObject.SetActive(true);
        player = me;
        player.IsAIControlled = true;
        partner = partnerPlayer;
        playerPlan = null;
    }

    // Update is called once per frame
    void Update()
    {
        if (Timeline.Instance.GetQueueSize() > 0 || player == null) return;
        if (drawStep)
        {
            GUIEvents.RaiseDrawAll();
            //player.Panel.UpdateCardsState(CardGUIStates.None);
            drawStep = false;
        }

        Player currentPlayer = GameRoot.State.CurrentPlayer;

        if (player.Hand.Count > 6 && GameRoot.State.CurrentState == GameState.Discarding)
        {
            var cardToDiscard = engine.SelectCardToDiscard(player, partner);
            new DiscardCardAction(cardToDiscard, player.Panel).Execute();
            GUIEvents.RaiseDrawAll();
            return;
        }

        if (currentPlayer.Role == player.Role && GameRoot.State.CurrentState != GameState.Discarding)
        {
            if ((playerPlan == null || playerPlan.ActionQueue.Count == 0) && Timeline.Instance.GetQueueSize() == 0
            && player.ActionsRemaining > 0)
            {

                playerPlan = engine.PlanMove(player, partner);
                // print in textbox
                textbox.text = playerPlan.ToString();
                partnerPanel.gameObject.SetActive(false);
                ourPanel.gameObject.SetActive(true);
                GUIEvents.RaiseDrawAll();
            }

            if (playerPlan != null && playerPlan.ActionQueue.Count > 0)
            {
                if (GameRoot.Config.StepByStepSimulation)
                {
                    // Wait for key click in game to execute next action
                    if (Input.GetKeyDown(KeyCode.Space))
                    {
                        playerPlan.ExecuteFirstAction();
                        drawStep = true;
                    }
                }
                else
                {
                    playerPlan.ExecuteFirstAction();
                    //Thread.Sleep(1000); // Wait for 1 second to simulate action execution
                }
            }
        }
    }
}