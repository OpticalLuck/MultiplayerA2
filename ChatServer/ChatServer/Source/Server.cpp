#include "Server.h"

bool Server::Init(int argc, char** argv)
{
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
		if (TempFds.fd_array[Index] == ServerSocket)
		{
			// New connection requested by new client.
			ClientSocket = accept(ServerSocket, (SOCKADDR*)&ClientAddr, &ClientLen);
			FD_SET(ClientSocket, &ReadFds);
			printf("New Client Accepted : Socket Handle [%d]\n", (int)ClientSocket);

			//send_welcome_message(ClientSocket);
			//session_info_message(ReadFds, ClientSocket);
			//send_notice_message(ReadFds, ClientSocket);
		}
		else
		{
			// Something to read from socket.
			memset(Message, '\0', BUFFERSIZE);
			Return = recv(TempFds.fd_array[Index], Message, BUFFERSIZE, 0);
			if (0 == Return)
			{
				// Connection closed message has arrived.
				closesocket(TempFds.fd_array[Index]);
				printf("Connection closed :Socket Handle [%d]\n", (int)TempFds.fd_array[Index]);
				FD_CLR(TempFds.fd_array[Index], &ReadFds);
			}
			else if (0 > Return)
			{
				// recv() function returned error.
				closesocket(TempFds.fd_array[Index]);
				printf("Exceptional error :Socket Handle [%d]\n", (int)TempFds.fd_array[Index]);
				FD_CLR(TempFds.fd_array[Index], &ReadFds);
			}
			else
			{
				// Message recevied.
				if ('/' == Message[0])
				{
					//whisper_to_one(ReadFds, Message, Return, TempFds.fd_array[Index]);
				}
				else
				{
					//send_to_all(ReadFds, Message, Return);
				}
			}
		}
	}
}

void Server::Shutdown()
{
	WSACleanup();
}
