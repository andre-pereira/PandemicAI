// File: PawnAnimator.cs
using UnityEngine;
using UnityEngine.UI;
using static OPEN.PandemicAI.Enums;

namespace OPEN.PandemicAI
{
    public class PawnAnimator : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] public RoleCardDisplay roleCard;
        [SerializeField] Transform animationCanvas;   

        GameObject movingPawn;
        GameObject flyLine;
        Player model;

        public void Init(Player p) => model = p;

        public void CreateMovingPawn(Vector3? offset = null)
        {
            if (movingPawn) Destroy(movingPawn);

            City cur = model.CurrentCity;
            Vector3 pos = cur.transform.position + (Vector3)(offset ?? Vector2.zero);

            movingPawn = Instantiate(GameGUI.Assets.pawnPrefab,
                                     pos,
                                     cur.transform.rotation,
                                     animationCanvas);

            var pawn = movingPawn.GetComponent<Pawn>();
            pawn.CanMove = true;
            pawn.SetRoleAndPlayer(model);
            movingPawn.GetComponent<Outline>().enabled = true;

            cur.RemovePawn(model);
            cur.Draw();
        }

        public void DestroyMovingPawn()
        {
            if (!movingPawn) return;

            Destroy(movingPawn);
            movingPawn = null;

            City city = model.CurrentCity;
            city.AddPawn(model);
            city.Draw();
        }

        public void CreateFlyPreviewLine(int toCityId)
        {
            DestroyFlyLine();

            City from = model.CurrentCity;
            City to = CityDrawer.CityScripts[toCityId];

            flyLine = new GameObject("Line-FlyPreview");
            flyLine.transform.SetParent(animationCanvas, false);

            LineRenderer lr = flyLine.AddComponent<LineRenderer>();
            lr.sortingLayerName = "Animation";
            lr.material = GameGUI.Assets.lineMaterial;
            lr.startColor = lr.endColor = roleCard.RoleCardData.roleColor;
            lr.startWidth = lr.endWidth = 0.1f;
            lr.positionCount = 2;
            lr.SetPosition(0, from.transform.position);
            lr.SetPosition(1, to.transform.position);
        }

        public void DestroyFlyLine()
        {
            if (flyLine) Destroy(flyLine);
            flyLine = null;
        }

        public bool IsPreviewActive => movingPawn || flyLine;
    }
}
