using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class UIManager : Singleton<UIManager>
{
    [Header("Start")]
    public GameObject StartMenu;
    public TMP_InputField NameInput;
    public TMP_Text ErrorMessage;

    [Header("ChatScreen")]
    public GameObject ChatScreen;
    public TMP_Text LobbyName;
    public RectTransform ChatMessages;
    public MessageFormat MessagePrefab;

    [Header("ChatInput")]
    public ClientInputScript ClientInput;
    public Button SendButton;

    [Header("InformationPanel")]
    public TMP_Text Status;
    public GameObject LobbyNamePrefab;
    public RectTransform LobbyList;
    public TMP_Text OnlineTitle;
    public RectTransform OnlineList;
    public OnlineUserText OnlineUserPrefab;
    void OnEnable()
    {
        NetworkManager.Instance.OnServerConnectListeners += OnServerConnect;    
        NetworkManager.Instance.OnPacketReceivedListeners += OnPacketReceived;    
    }

    void OnDisable()
    {
        NetworkManager.Instance.OnServerConnectListeners -= OnServerConnect;
        NetworkManager.Instance.OnPacketReceivedListeners -= OnPacketReceived;    
    }

    protected void CreateMessage(string Name, string Message)
    {
        Debug.Log("Creating Message from " + Name);
        var msg = Instantiate(MessagePrefab, ChatMessages);
        msg.Content.text = Message;
        msg.Name.text = Name;
        msg.Time.text = System.DateTime.Now.ToString("HH:mm:ss");
    }

    public void ConnectToServer()
    {
        if (NameInput.text.Length > 0 && NetworkManager.Instance.IsConnected == false)
        {
            NetworkManager.Instance.ConnectToServer();
            NetworkManager.Instance.ClientName = NameInput.text;
        }
    }

    public void SendMessagePacket()
    {
        if (ClientInput.GetLength() > 0)
        {
            if (!ProcessCommands())
            {
                if(NetworkManager.Instance.LobbyID < 0)
                    NetworkManager.Instance.SendToAll(ClientInput.GetText());
                else
                    NetworkManager.Instance.SendToLobby(ClientInput.GetText());
            }
            ClientInput.Clear();
        }
    }

    private bool ProcessCommands()
    {
        var text = ClientInput.GetText();
        if (!text.StartsWith("/")) return false;

        if (text.StartsWith("/msg "))
        {
            string targetString = text.Split(' ')[1];
            int target;
            string message = text.Substring(text.IndexOf(targetString) + targetString.Length + 1);

            if (int.TryParse(targetString, out target))
            {
                int userID = -1;
                foreach (OnlineUserText user in OnlineList.GetComponentsInChildren<OnlineUserText>())
                {
                    //Check if user is online/exist
                    if (user.UserSocket == target)
                        userID = user.UserSocket;
                }

                if (userID == NetworkManager.Instance.ClientID)
                {
                    CreateMessage("System", "Why message yourself only?");
                    return true;
                }

                if (userID <= 0) CreateMessage("System", "User Is Not Online Or Does Not Exist!");
                else NetworkManager.Instance.SendWhisper(target, message);
            }
            else
            {
                CreateMessage("System", "Use the ID instead of Name");
            }
            return true;
        }

        if (text.StartsWith("/createlobby"))
        {
            var textarr = text.Split(' ');
            if (textarr.Length > 1)
            {
                CreateMessage("System", "Wrong Syntax. Try /createlobby");
                return true;
            }

            Command command;
            command.CommandRequest = "CreateLobby";
            NetworkManager.Instance.SendData(JsonConvert.SerializeObject(command));

        }

        if (text.StartsWith("/leavelobby"))
        {
            var textarr = text.Split(' ');
            if(textarr.Length > 1)
            {
                CreateMessage("System", "Wrong Syntax. Try /leavelobby");
                return true;
            }
            Command command;
            command.CommandRequest = "LeaveLobby";
            NetworkManager.Instance.SendData(JsonConvert.SerializeObject(command));

        }
        if (text.StartsWith("/joinlobby "))
        {
            var textarr = text.Split(' ');
            if(textarr.Length > 2)
            {
                CreateMessage("System", "Incorrect Syntax. /joinlobby [LobbyNumber]");
                return true;
            }

            string targetLobby = text.Split(' ')[1];
            int lobbyid;
            string message = text.Substring(text.IndexOf(targetLobby));
            if(int.TryParse(targetLobby, out lobbyid))
            {
                UserData userdata;
                userdata.UserName = NetworkManager.Instance.ClientName;
                userdata.UserHandle = NetworkManager.Instance.ClientID;
                userdata.Lobby = lobbyid;
                userdata.IsConnected = true;

                JObject jobj = new JObject(new JProperty(UserData.Prefix, new JArray(JObject.Parse(JsonConvert.SerializeObject(userdata)))));

                NetworkManager.Instance.SendData(JsonConvert.SerializeObject(jobj));
            }
        }

        if (text.StartsWith("/shrug"))
        {
            var textarr = text.Split(' ');
            if(textarr.Length > 1)
            {
                CreateMessage("System", "Wrong Syntax. Try /shrug");
                return true;
            }

            if (NetworkManager.Instance.LobbyID < 0)
                NetworkManager.Instance.SendToAll("¯\\_(ツ)_/¯");
            else
                NetworkManager.Instance.SendToLobby("¯\\_(ツ)_/¯");
        }
        return true;
    }

    protected void OnServerConnect()
    {
        StartMenu.SetActive(false);
        ChatScreen.SetActive(true);

        Status.text = "Status: Connected";
    }

    protected void OnPacketReceived(byte[] data)
    {
        JObject jobject = JObject.Parse(Encoding.UTF8.GetString(data));

        if(jobject.ContainsKey(LoginData.Prefix))
        {
            //Send Login Data
            UserData userdata;
            userdata.UserName = NameInput.text;
            userdata.UserHandle = NetworkManager.Instance.ClientID;
            userdata.Lobby = -1;
            userdata.IsConnected = true;
            JObject jobj = new JObject(new JProperty(UserData.Prefix, new JArray(JObject.Parse(JsonConvert.SerializeObject(userdata)))));
            NetworkManager.Instance.SendData(jobj.ToString());

            Command command;
            command.CommandRequest = "GetUserList";
            NetworkManager.Instance.SendData(JsonConvert.SerializeObject(command));
        }
        //Send Message
        if (jobject.ContainsKey(MessageData.Prefix))
        {
            MessageData messageData = jobject[MessageData.Prefix].ToObject<MessageData>();

            CreateMessage(messageData.Sender, messageData.Message);
        }

        //Receiving Player Data
        if (jobject.ContainsKey(UserData.Prefix))
        {
            var juserdata = JArray.Parse(jobject[UserData.Prefix].ToString());

            if (juserdata.Count > 1)
            {
                Debug.Log("Creating Online List");
                UserData[] userData = jobject[UserData.Prefix].ToObject<UserData[]>();

                foreach (UserData user in userData)
                {
                    if (user.UserHandle != NetworkManager.Instance.ClientID)
                    {
                        var prefab = Instantiate(OnlineUserPrefab, OnlineList);
                        prefab.SetName(user);
                    }
                }
            }
            else if (juserdata.Count == 1)
            {
                Debug.Log("Updating Online List");

                UserData userData = jobject[UserData.Prefix].ToObject<UserData[]>()[0];
                if (userData.IsConnected == false)
                {
                    foreach (OnlineUserText onlineUser in OnlineList.GetComponentsInChildren<OnlineUserText>())
                    {
                        if(onlineUser.UserSocket == userData.UserHandle)
                        {
                            Destroy(onlineUser.gameObject);
                            OnlineTitle.text = "Online(" + (OnlineList.transform.childCount - 1) + ")";
                            return;
                        }
                    }
                }

                bool added = false;
                //Update 
                foreach (OnlineUserText onlineUser in OnlineList.GetComponentsInChildren<OnlineUserText>())
                {
                    if (onlineUser.UserSocket == userData.UserHandle)
                    {
                        added = true;
                        onlineUser.SetName(userData);
                    }
                }
                //Create List object if doesnt exist
                if (!added)
                {
                    var prefab = Instantiate(OnlineUserPrefab, OnlineList);
                    prefab.SetName(userData);
                }

                if(userData.Lobby >= 0 && userData.UserHandle == NetworkManager.Instance.ClientID)
                {
                    Debug.Log("Updating Client Lobby to " + userData.Lobby);
                    LobbyName.text = userData.Lobby > 0 ? "Lobby " + userData.Lobby : "All Chat";
                    NetworkManager.Instance.LobbyID = userData.Lobby;
                    foreach (MessageFormat message in ChatScreen.GetComponentsInChildren<MessageFormat>())
                    {
                        Destroy(message.gameObject);
                    }
                }
            }

            Command command;
            command.CommandRequest = "GetLobbyIDs";
            NetworkManager.Instance.SendData(JsonConvert.SerializeObject(command));

            OnlineTitle.text = "Online(" + OnlineList.transform.childCount + ")";
        }

        if (jobject.ContainsKey(LobbyIDs.Prefix))
        {
            Debug.Log("Updating Lobby List");
            foreach(LobbyIDs lobby in jobject[LobbyIDs.Prefix].ToObject<LobbyIDs[]>())
            {
                var lobbyObj = LobbyList.transform.Find("Lobby " + lobby.ID);
                if (lobbyObj == null)
                {
                    if (lobby.UserCount > 0)
                    {
                        var newobj = Instantiate(LobbyNamePrefab, LobbyList);
                        newobj.name = "Lobby " + lobby.ID;
                        newobj.GetComponent<TMP_Text>().text = "Lobby " + lobby.ID + "[" + lobby.UserCount + "]";
                    }
                }
                else
                {
                    if (lobby.UserCount > 0)
                    {
                        lobbyObj.name = "Lobby " + lobby.ID;
                        lobbyObj.GetComponent<TMP_Text>().text = "Lobby " + lobby.ID + "[" + lobby.UserCount + "]";
                    }
                    else
                    {
                        Destroy(lobbyObj.gameObject);
                    }
                }
            }
        }
    }

}
