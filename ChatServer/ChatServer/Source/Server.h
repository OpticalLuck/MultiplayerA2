#pragma once
#include <stdio.h>
#include <string.h>
#include <winsock2.h>

#define BUFFERSIZE  1024
#define PORT_NUMBER 7890

#pragma comment(lib, "Ws2_32.lib")

class Server
{
public:
	bool Init(int argc, char** argv);
	bool Update();
	void Shutdown();
	
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
};