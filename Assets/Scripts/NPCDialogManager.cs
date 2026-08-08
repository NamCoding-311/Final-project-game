using UnityEngine;
using TMPro;

public class NPCDialogManager : MonoBehaviour
{
    public static NPCDialogManager Instance;

    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text contentText;

    private string[] _lines;
    private int _currentIndex;

    private void Awake()
    {
        Instance = this;
        dialogPanel.SetActive(false);
    }

    public void StartDialog(string npcName, string[] lines)
    {
        nameText.text = npcName;
        _lines = lines;
        _currentIndex = 0;
        dialogPanel.SetActive(true);
        ShowCurrentLine();
    }

    // Gọi hàm này khi click vào DialogBox
    public void OnDialogBoxClicked()
    {
        _currentIndex++;

        if (_currentIndex >= _lines.Length)
        {
            dialogPanel.SetActive(false); // hết câu -> đóng luôn
        }
        else
        {
            ShowCurrentLine();
        }
    }

    private void ShowCurrentLine()
    {
        contentText.text = _lines[_currentIndex];
    }
}