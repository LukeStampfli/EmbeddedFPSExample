
# Setting up the projects


## Project Structure with Darkrift
Before we start our project we should take a minute to think about our project structure.
We will create a classic FPS game in this tutorial with a server and a client application. The server application will have full control (authoritative server) over the game, so cheating will be impossible.

The client will be a unity game with a normal unity project. For the server we have 2 possibilities:
- Use an embedded server (We will also run the server in Unity)
- Use a standalone server (The server will be just a C# console application)


::: tip
 If you use an embedded server you can also create the server and the client in the same project. But you will have to make sure that the client build includes no server code and vice versa which makes it a bit complicated.
:::

### Reasons to go for an embedded server
- You can use a similar project structure on your client and server (Monobehaviors) 
- Access to Unity features like Physics, Navmesh or Collision detection.
- You can use graphical outputs for debugging or testing on your server.

### Reasons to go for a standalone server
- Less overhead then an embedded server (slightly increased performance)
- Easier to manage references and use external libraries (NuGet packages, C# 7 support...)
- Easier to containerize and deploy for production
- You can still use some Unity functions by referencing the UnityEngine.dll.

In the end standalone and embedded servers are pretty similar. We will go for an embedded server in this tutorial because we want to use the Unity collision detection and because I usually write standalone applications and want to try something different.

## Setup
- Create a repository or folder for the unity projects, name it something like EmbeddedFPSExample

## Setup the client
::: danger
This project uses new physics features which were introduced in Unity 2018.3 so you have to use 2018.3 or a newer version of Unity.
:::
- Create a new Unity project (I called mine EmbeddedFPSClient).
- In Edit -> Project Settings -> Player -> Other settings, make sure that Scripting Runtime Version is set to .Net 4.x equivalent.
- In Edit -> Project Settings -> Time set Fixed Timestep to 0.025 and Maximum Allowed Timestep to 100
- Head to the Asset Store and download the newest version of Darkrift 2.
- Create a basic folder structure (create a "Prefabs", "Scenes" and "Scripts" folder)
- Create a Scene "Main" in the scenes folder
## Setup the server
Repeat the setup from the client but name the project something like EmbeddedFPSServer

## Creating a folder Junction to share scripts

::: warning
I usually use a shared .dll file in a seperate project and not folder junction. Both are viable options to do, but if you plan to work on the project with a bigger team you should go for the .dll approach or use git submodules.
:::

Some scripts will be used by the client and the server, having a way to synchronize them saves time. There are many good ways to do that. For the sake of simplicity we will use a folder junction. A folder junction synchronizes all files inside a folder to another folder. 
- Create a "shared" folder inside the Scripts folder of the client project.

### Windows:
- open the cmd.exe
- type: ```mklink /J "Client shared folder" "server shared folder"```\
(Replace "Client/Server shared folder" with just the path, don't write the "")

### Mac OS X
- open the terminal
- type: ```ln -s "Client shared folder" "server shared folder"```\



This junction will share all changes that we make on the client **But not the other way around so we will edit scripts in the shared folders always in the clients folder.**

Now the setup is done and we can start to write some code :smiley: 