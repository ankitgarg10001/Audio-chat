This is a solution for audio call using
http://www.lumisoft.ee/lswww/download/downloads/Examples/

Client
It detects your audio devices(Input and output) and gives you an option to choose one
you can test your device settings with test button and refresh devices, in case configuration is changed(Like removed headphones).
Shows recieved and sent packets and bytes

It gives various options to make calls:
	Via server: server connects 2 clients(which you can see in right pane)
	direct assess: you can directly connect to client via his IP address
	Groupcast: send audio to all selected clients
	
Server:
	manages connection between various clients via TCP messages so that they can enable disable controls based on calling status, show other client lists, test connection to server.