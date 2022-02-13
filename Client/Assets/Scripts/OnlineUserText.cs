using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class OnlineUserText : MonoBehaviour
{
    public GameObject Name;
    public TMP_Text NameText;
    public int UserSocket { get; private set; }
    public int Lobby { get; private set; }
    public void SetName(UserData data)
    {
        Name.name = data.UserName;
        NameText.text = data.UserName + "[" + data.UserHandle + "]";
        UserSocket = data.UserHandle;
        Lobby = data.Lobby;
    }
}
