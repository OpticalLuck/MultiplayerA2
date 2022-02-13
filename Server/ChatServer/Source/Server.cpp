#include "Server.h"
#include <sstream>
#include "json.hpp"

namespace Utils {
	json::JSON GetPlayerData(SOCKET targetSocket, std::map<int, UserData> userDataMap, bool InLobby, bool IsConnected = true)
	{
		json::JSON jobject;
		jobject["UserData"] = json::Array();

		jobject["UserData"].append(json::Object());
		jobject["UserData"][0]["UserHandle"] = (int)targetSocket;
		jobject["UserData"][0]["UserName"] = userDataMap.at((int)targetSocket).Name;
		jobject["UserData"][0]["Lobby"] = InLobby ? userDataMap.at((int)targetSocket).LobbyID : 0;
		jobject["UserData"][0]["IsConnected"] = IsConnected;
		return jobject;
	}
}

bool Server::Init(int argc, char** argv)
{
	for (int i = 1; i <= MAX_LOBBY_ID; ++i)
	{
		AvailableLobbyIDs.push(i);
		LobbyMap[i];
	}

	if (2 == argc)
	{
		Port = atoi(argv[1]);
	}
	printf("Using port number : [%d]\n", Port);

	if (WSAStartup(MAKEWORD(2, 2), &WsaData) != 0)
	{
		printf("WSAStartup() error!\n");
		return false;
	}

	ServerSocket = socket(AF_INET, SOCK_STREAM, 0);
	if (INVALID_SOCKET == ServerSocket)
	{
		printf("socket() error\n");
		return false;
	}

	ServerAddr.sin_family = AF_INET;
	ServerAddr.sin_port = htons(Port);
	ServerAddr.sin_addr.s_addr = htonl(INADDR_ANY);

	if (SOCKET_ERROR == bind(ServerSocket, (SOCKADDR*)&ServerAddr,
		sizeof(ServerAddr)))
	{
		printf("bind() error\n");
		return false;
	}

	if (SOCKET_ERROR == listen(ServerSocket, 5))
	{
		printf("listen() error\n");
		return false;
	}

	FD_ZERO(&ReadFds);
	FD_SET(ServerSocket, &ReadFds);

	return true;
}

bool Server::Update()
{
	memset(Message, '\0', BUFFERSIZE);
	TempFds = ReadFds;
	Timeout.tv_sec = 5;
	Timeout.tv_usec = 0;

	if (SOCKET_ERROR == (Return = select(0, &TempFds, 0, 0, &Timeout)))
	{
		// Select() function returned error.
		printf("select() error\n");
		return false;
	}
	if (0 == Return)
	{
		// Select() function returned by timeout.
		printf("Select returned timeout.\n");
		//return false;
	}
	else if (0 > Return)
	{
		printf("Select returned error!\n");
		//return false;
	}


	for (Index = 0; Index < TempFds.fd_count; Index++)
	{
		// New connection requested by new client.
		if (TempFds.fd_array[Index] == ServerSocket)
		{
			ClientSocket = accept(ServerSocket, (SOCKADDR*)&ClientAddr, &ClientLen);
			FD_SET(ClientSocket, &ReadFds);
			printf("New Client Accepted : Socket Handle [%d]\n", (int)ClientSocket);

			//send_welcome_message(ClientSocket);
			//session_info_message(ReadFds, ClientSocket);
			//send_notice_message(ReadFds, ClientSocket);
			json::JSON jobj;
			jobj["LoginData"] = json::Object();
			jobj["LoginData"]["ID"] = (int)ClientSocket;
			std::stringstream ss;
			ss << jobj;
			Send_To_Self(ss.str().c_str(), (int)ClientSocket);

		}
		else
		{
			// Something to read from socket.
			memset(Message, '\0', BUFFERSIZE);
			Return = recv(TempFds.fd_array[Index], Message, BUFFERSIZE, 0);
			if (0 >= Return)
			{
				Disconnect(TempFds.fd_array[Index], Return);
			}
			else
			{
				ProcessPacket(Message, TempFds.fd_array[Index]);
			}
		}
	}
}

void Server::Shutdown()
{
	WSACleanup();
}

void Server::Send_To_All(fd_set ReadFds, const char* data)
{
	for (int i = 1; i < ReadFds.fd_count; ++i)
	{
		send(ReadFds.fd_array[i], data, strlen(data), 0);
	}
}

void Server::Send_To_One(fd_set ReadFds, const char* data, SOCKET clientSocket, SOCKET targetSocket)
{
	int i;
	for (i = 1; i < ReadFds.fd_count; ++i)
	{
		if (ReadFds.fd_array[i] == targetSocket)
		{
			send(ReadFds.fd_array[i], data, strlen(data), 0);
			break;
		}
	}

	if (i == ReadFds.fd_count)
	{
		json::JSON obj = json::Object();
		obj["Error"] = "Failed to find the user you were trying to whisper to.";

		std::stringstream ss;
		ss << obj;
		send(clientSocket, ss.str().c_str(), ss.str().size(), 0);
	}
}

void Server::Send_To_Self(const char* data, SOCKET clientSocket)
{
	send(clientSocket, data, strlen(data), 0);
}

void Server::SendConnectNotice(fd_set ReadFds, const char* data)
{
	json::JSON jobject = Utils::GetPlayerData(ClientSocket, SocketUserDataMap, false);

	std::stringstream ss;
	ss << jobject;

	//Alert everyone but yourself
	for (int i = 1; i < ReadFds.fd_count; ++i)
	{
		if(ReadFds.fd_array[i] != ClientSocket)
			send(ReadFds.fd_array[i], ss.str().c_str(), ss.str().length(), 0);
	}
}

void Server::Disconnect(SOCKET targetSocket, int returnCode)
{
	LeaveLobby(targetSocket);

	std::stringstream ss;
	ss << Utils::GetPlayerData(targetSocket, SocketUserDataMap, false, false);
	printf("Send to All: %s", ss.str().c_str());
	Send_To_All(ReadFds, ss.str().c_str());
	SocketUserDataMap.erase((int)targetSocket);
	closesocket(targetSocket);
	
	
	switch (returnCode)
	{
	case 0: 
		printf("Connection closed :Socket Handle [%d]\n", (int)TempFds.fd_array[Index]);
		break;
	default:
		printf("Exceptional error :Socket Handle [%d]with Error Code [%d]\n", (int)TempFds.fd_array[Index], WSAGetLastError());
	}
	FD_CLR(targetSocket, &ReadFds);
}

void Server::LeaveLobby(SOCKET targetSocket)
{
	int lobbyid = SocketUserDataMap.at((int)targetSocket).LobbyID;

	LobbyMap[lobbyid].erase(std::remove(LobbyMap[lobbyid].begin(), LobbyMap[lobbyid].end(), (int)targetSocket), LobbyMap[lobbyid].end());

	if (LobbyMap[lobbyid].size() <= 0)
	{
		//LobbyMap.erase(lobbyid);
		AvailableLobbyIDs.push(lobbyid);
	}

	SocketUserDataMap.at((int)targetSocket).LobbyID = 0;
}

void Server::ProcessPacket(const char* data, SOCKET clientSocket)
{
	printf("Received: %s \n", data);
	json::JSON obj = obj.Load(data);
	if (obj.hasKey("UserData"))
	{
		for (int i = 0; i < obj.size(); i++)
		{
			if (SocketUserDataMap.find((int)clientSocket) == SocketUserDataMap.end()) //If key is not found
				SocketUserDataMap.insert(std::make_pair<int, UserData>((int)clientSocket, UserData(obj["UserData"][i]["UserName"].ToString()))); //Insert
			else
				SocketUserDataMap.at((int)clientSocket) = UserData(obj["UserData"][i]["UserName"].ToString(), obj["UserData"][i]["Lobby"].ToInt()); //Update

			if (obj["UserData"][i]["Lobby"].ToInt() > 0)
				LobbyMap[obj["UserData"][i]["Lobby"].ToInt()].push_back((int)clientSocket);

			printf("Name of Handle [%d] updated to [%s]\n", (int)clientSocket, obj["UserData"][i]["UserName"].ToString().c_str());
		}
		Send_To_All(ReadFds, Message);
	}
	if (obj.hasKey("Message"))
	{
		auto mode = obj["Message"]["Mode"].ToString();
		if (strcmp(mode.c_str(), "Single") == 0)
		{
			Send_To_One(ReadFds, data, clientSocket, obj["Message"]["Target"].ToInt());
			Send_To_Self(data, clientSocket);
		}
		else if (strcmp(mode.c_str(), "Group") == 0)
		{
			//sends to EVERYONE who is in that lobby id
			for (auto user : SocketUserDataMap)
			{
				if (user.second.LobbyID == SocketUserDataMap.at((int)clientSocket).LobbyID)
					Send_To_One(ReadFds, data, clientSocket, user.first);
			}
		}
		else
		{
			//sends to EVERYONE who is not in a lobby
			for (auto user : SocketUserDataMap)
			{
				if (user.second.LobbyID < 0)
					Send_To_One(ReadFds, data, clientSocket, user.first);
			}
		}
	}
	if (obj.hasKey("CommandRequest"))
	{
		ProcessCommands(obj["CommandRequest"].ToString().c_str(), clientSocket);
	}
}

void Server::ProcessCommands(const char* commandName, SOCKET clientSocket)
{
	if (strcmp(commandName,"GetUserList") == 0)
	{
		json::JSON obj;
		obj["UserData"] = json::Array();

		int i = 0;
		for (auto user : SocketUserDataMap)
		{
			obj["UserData"].append(json::Object());
			obj["UserData"][i]["UserHandle"] = user.first;
			obj["UserData"][i]["UserName"] = user.second.Name;
			obj["UserData"][i]["Lobby"] = user.second.LobbyID;
			obj["UserData"][i]["IsConnected"] = true;
			i++;
		}
		
		std::stringstream ss;
		ss << obj;

		printf("Sent to [%d]: %s \n", (int)clientSocket, ss.str().c_str());
		Send_To_Self(ss.str().c_str(), clientSocket);
	}
	if (strcmp(commandName, "CreateLobby") == 0)
	{
		int LobbyID = AvailableLobbyIDs.front();
		AvailableLobbyIDs.pop();

		LobbyMap[LobbyID].push_back((int)clientSocket);
		SocketUserDataMap.at((int)clientSocket).LobbyID = LobbyID;

		json::JSON jobject = Utils::GetPlayerData(clientSocket, SocketUserDataMap, true);;
		
		std::stringstream ss;
		ss << jobject;
		Send_To_All(ReadFds, ss.str().c_str());
	}
	if (strcmp(commandName, "LeaveLobby") == 0)
	{
		LeaveLobby(clientSocket);

		json::JSON jobject = Utils::GetPlayerData(clientSocket, SocketUserDataMap, false);
		
		std::stringstream ss;
		ss << jobject;
		printf("Leaving lobby: %s", ss.str().c_str());
		Send_To_All(ReadFds, ss.str().c_str());
	}
	if (strcmp(commandName, "GetLobbyIDs") == 0)
	{
		json::JSON jobject;
		jobject["LobbyIDs"] = json::Array();
		int i = 0;
		for (auto lobby : LobbyMap)
		{
			jobject["LobbyIDs"][i]["ID"] = lobby.first;
			jobject["LobbyIDs"][i]["UserCount"] = lobby.second.size();
			i++;
		}
		std::stringstream ss;
		ss << jobject;
		printf("Sending lobbyIDs: %s", ss.str().c_str());
		Send_To_Self(ss.str().c_str(), clientSocket);

	}
}
