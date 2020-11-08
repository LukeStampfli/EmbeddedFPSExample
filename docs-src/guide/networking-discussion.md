# Networking Discussion

This chapter contains general information about game networking.

## Networking Architectures

### Direct Peer to Peer
Direct peer to peer networking is an architecture in which clients are connected directly to each other. There are 2 popular architectures for direct P2P.
One uses a host. Clients will connect directly to the host and the host will get data from all the clients and redistribute data back to them. Another way is to connect each client with each other client and let each client update all other clients. Both architectures have advantages and disadvantages but the host-client architecture is far more popular.

### Relay Server
The relay server architecture is similar to the direct host-client P2P architecture. The only difference is that both the host and the clients connect to a central relay server. Data is sent in a similar way as in the host-client
P2P architecture but all data will be sent over the relay server. The relay server receives packages and redirects them to the host or the clients. The advantage of using a relay server architecture is that it works for anyone out of the box without having players to open ports. It's a very popular architecture for COOP games.

### NAT Punchthrough
NAT Punchthrough allows clients to create direct P2P connections without having to know the other clients ips. The basic idea is to open a port on each client’s NAT by connecting it to a master server and passing all the ips to the other clients while keeping the connection open which allows the clients to receive messages from other clients.
In practice it is far more complicated and not always stable or possible, that's why most of the time the master server is also a relay server which will act like a relay server if a connection fails.
So why use NAT Punchthrough at all? Direct connections between clients will lower the ping for the players and will also decrease your server bandwidth and cpu load by a lot. Battle.net was initially only powered by a single master server which created direct connections between clients.

### Client-Server/Authorative Server
The idea is similar to host-client direct P2P but the host is a dedicated server and not a client. Clients will connect to the server and send messages to the server and the host will redirect them to the clients. It is highly recommended to give full authority to the server. In some cases the client should have authority over certain actions though (More in the Chapter Authority).

### Room-based Client-Server
It is definitely the most popular type of Client-Server architecture. A room based server features multiple rooms and a way to distribute clients into them. Clients will only be updated about information in their current room.
The architecture is very easy to scale because the servers running the rooms don't have to send information to each other. Usually a single master server is used to distribute the players into rooms and to pass information between the servers.

### Seamless World Server
Seamless world servers are usually used in open world MMORPGs. Creating a working seamless world server system needs a lot of knowledge so I won't cover it in detail. But the basic idea is usually to slice your map into areas and have players connected to their current area and nearby areas and if the player passes a border authority is also passed to another server.

### Which Architecture to choose:
-Direct P2P: Local Lan-Party games
-Relay Server: COOP/casual games
-Nat Punchthrough: COOP/Casual games where saving money on servers is important and with an advanced networking programmer in the team.
-Client-Server: Prototypes or Lan-Party games
-Room-based Client-Server: Almost all games: MOBAs, MMORPGs, Shooters....
-Seamless World Server: Huge open world games with thousands of players on the same map.

## General Game Networking Concepts

### Ticks/Update
Most multiplayer games run logic like collisions and movement on a fixed tick rate. One reason for that is that it allows clients to predict movement accurately but it makes games overall more smooth and solves a lot
of nasty networking issues which otherwise need to be fixed. Running the server and the client at the same tick rate makes everything even easier. If the client and the server use the same tick rate then almost anything can be deterministically calculated and things like lag compensation can be done with tick numbers instead of timestamps.
When using unity make sure to set "Maximum Allowed Timestep" in the time settings to a high value, if you don't Unity will skip fixed frames if a lag occurs which brings everything out of sync.
You can also easily create your own fixed update by just adding time.DeltaTime to a float in Update and when it exceeds 1/YourDesiredFixedRate you subtract your fixed rate and call your own fixed update method. 

### Network Ids
A lot of games use network ids to keep track of objects. Usually there are two types of ids. Type ids describe what type an object is (a certain creature or an interactable object or a player) and Network ids are unique numbers for each object. The advantage of such a system is that it makes spawning/despawning and sending information to a specific object very easy. A basic breakdown of the implementation:
- Spawning: The server sends a spawn command including an NetworkId,TypeId and TypeSpecificSpawnInformation then both the server and clients spawn that object based on the parameters
- Despawning: Is very easy just send a despawn event including the NetworkId of the object and maybe TypeSpecificDespawnInformation
- Passing Information: This is where the pattern truly shines. You can send a NetworkId, an EventId and EventSpecificInformation to the client and the client can very easily redirect that information to the right object and execute a method and pass the EventSpecificInformation
as a parameter. Note that the EventIds can be object specific for instance a player could use the id 1 for a shooting event but a door could use it as an open/close event. This is crucial as it allows the pattern to use very low bandwidth while still being very flexible.

### Optimization
There are a lot of ways to optimize when it comes to game networking but it's always important to not over optimize things. As a rule of thumb code which runs every tick is worth to optimize everything else isn't unless it's causing notable performance problems. I'm just going to a list a few general tips:
- For numbers think about if you really need an int or if an ushort or a byte would also fulfil the purpose.
- Optimize base data types like quaternions, bool arrays, angles, vectors [Example](https://github.com/LestaAllmaron/DarkriftSerializationExtensions/blob/master/DarkriftSerializationExtensions/DarkriftSerializationExtensions/SerializationExtensions.cs) 
- Only use strings for names or text messages almost everything else can be serialized far more efficiently. Strings also include JSON strings don’t use JSON for real time games.
- Send as much has possible in one message and combine message information into one big message (Most games collect all information for a game tick and send it in 1 message). Sending a message is very expensive.