public struct LoginData 
{
    public const string Prefix = "LoginData";
    public int ID;
}

public struct LobbyIDs
{
    public const string Prefix = "LobbyIDs";
    public int ID;
    public int UserCount;

}


public struct UserData
{
    public const string Prefix = "UserData";
    public int UserHandle;
    public string UserName;
    public int Lobby;
    public bool IsConnected;
}

public struct MessageData
{
    public const string Prefix = "Message";
    public const string ALL = "All";
    public const string SINGLE = "Single";
    public const string GROUP = "Group";
    public string Sender;
    public string Message;
    public string Mode;
    public int Target;
}

public struct Command
{
    public const string Prefix = "Command";
    public string CommandRequest;
}