#include "Server.h"

int main(int argc, char** argv)
{
	Server* chatserver = new Server();
	if (!chatserver->Init(argc, argv))
	{
		printf("Can't Initialize Server!");
		return 0;
	}

	bool NoError = true;
	while (NoError)
		NoError = chatserver->Update();
	
	chatserver->Shutdown();
	return 1;
}