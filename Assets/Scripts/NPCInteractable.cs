using UnityEngine;

public class NPCInteractable : MonoBehaviour
{
    [SerializeField] private string npcName = "NPC";
    [TextArea] [SerializeField] private string[] dialogLines; // nhiều câu thay vì 1 câu
    [SerializeField] private Color highlightColor = new Color(1.3f, 1.3f, 1.3f);

    private SpriteRenderer _sr;
    private Color _originalColor;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _originalColor = _sr.color;
    }

    private void OnMouseEnter() => _sr.color = highlightColor;
    private void OnMouseExit() => _sr.color = _originalColor;

    private void OnMouseDown()
    {
        NPCDialogManager.Instance.StartDialog(npcName, dialogLines);
    }
}