using UnityEngine;
using TMPro; 

[RequireComponent(typeof(TMP_InputField))]
public class ClientInputScript : MonoBehaviour
{
    public int MaxBufferSize = 256;
    public TMP_InputField InputField;

    // Start is called before the first frame update
    void Start()
    {
        InputField = GetComponent<TMP_InputField>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!InputField.isFocused) return;

        if (Input.GetKeyDown(KeyCode.Return) && !(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) )
        {
            InputField.text = InputField.text.Trim();
            UIManager.Instance.SendMessagePacket();
        }
    }

    public int GetLength()
    {
        return InputField.text.Length;
    }

    public string GetText()
    {
        return InputField.text;
    }

    public void Clear()
    {
        InputField.text = string.Empty;
    }
}
