using UnityEngine;

// Gắn vào NPC_Quest trong Mission Lobby để khi người chơi bấm vào thì mở Bàn Tác Chiến Bunker
public class QuestOfficerInteract : MonoBehaviour
{
    private void OnMouseDown()
    {
        if (TacticalWarRoomManager.Instance != null)
        {
            TacticalWarRoomManager.Instance.OpenWarRoom();
        }
    }
}
