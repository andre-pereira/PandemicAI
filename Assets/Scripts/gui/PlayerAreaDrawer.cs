using System.Collections.Generic;
using UnityEngine;

namespace OPEN.PandemicAI
{
    public class PlayerAreaDrawer : MonoBehaviour
    {
        [SerializeField] public List<PlayerPanel> panels;

        public static List<PlayerPanel> Panels { get; private set; }

        public void Awake()
        {
            Panels = panels;
        }

        public void OnEnable()
        {
            GUIEvents.DrawPlayerAreas += Draw;
            GUIEvents.DrawPlayerArea += DrawPlayerArea;
        }

        private void DrawPlayerArea(PlayerPanel panel)
        {
            panel.Draw();
        }

        public void Draw()
        {
            foreach (var p in Panels) p.Draw();
        }

        public static PlayerPanel PlayerPadForPlayer(Player player)
        {
            foreach (PlayerPanel playerPad in Panels)
            {
                if (playerPad.Model == player) return playerPad;
            }
            return null;
        }

        public static PlayerPanel CurrentPlayerPad() => PlayerPadForPlayer(GameRoot.State.CurrentPlayer);

        public static void Init(List<Player> players)
        {
            Panels[0].Init(players[0], players[1]);
            Panels[1].Init(players[1], players[0]);
        }
    }
}