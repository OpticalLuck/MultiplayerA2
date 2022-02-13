#pragma once
#include <stdio.h>
#include <map>
#include <string.h>
#include <winsock2.h>
#include <string>
#include <vector>
#include <queue>


#define BUFFERSIZE  1024 * 2
#define PORT_NUMBER 7890
#define MAX_LOBBY_ID 10
#define MAX_USER_PER_LOBBY 10
#pragma comment(lib, "Ws2_32.lib")

struct UserData
{
	std::string Name;
	int LobbyID = -1;
	
	UserData(std::string name) : Name(name) { }
	UserData(std::string name, int lobbyid) : Name(name), LobbyID(lobbyid) { }
};
class Server
{
public:
	bool Init(int argc, char** argv);
	bool Update();
	void Shutdown();
	
	//Messaging
	void Send_To_All(fd_set ReadFds, const char* data);
	void Send_To_One(fd_set ReadFds, const char* data, SOCKET clientSocket, SOCKET targetSocket);
	void Send_To_Self(const char* data, SOCKET clientSocket);
private:
	
	void SendConnectNotice(fd_set ReadFds, const char* data);
	//json::JSON GetPlayerData(SOCKET targetSocket, bool InLobby, bool IsConnected = true);
	void Disconnect(SOCKET targetSocket, int returnCode);
	void LeaveLobby(SOCKET targetSocket);
	void ProcessPacket(const char* data, SOCKET clientSocket);
	void ProcessCommands(const char* commandName, SOCKET clientSocket);
private:
	int          Port = PORT_NUMBER;
	WSADATA      WsaData;
	SOCKET       ServerSocket;
	SOCKADDR_IN  ServerAddr;

	unsigned int Index;
	int          ClientLen = sizeof(SOCKADDR_IN);
	SOCKET       ClientSocket;
	SOCKADDR_IN  ClientAddr;

	fd_set       ReadFds, TempFds;
	TIMEVAL      Timeout; // struct timeval timeout;

	char         Message[BUFFERSIZE];
	int          Return;

	std::map<int, UserData> SocketUserDataMap;
	std::map<int, std::vector<int>> LobbyMap;

	std::queue<int> AvailableLobbyIDs;
};

