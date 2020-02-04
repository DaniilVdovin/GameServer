# GameServer
Doc on WIKI

![Datagram](/diagram.jpg)

***

Oke now i show how use server API in you'r Unity project
# Client
* [Initialization Server](API-Client#initialization)
* [Sing Up and Log In](API-Client#sing-up--log-in)
* [Joint and Create Room](API-Client#room)
* [Rules Room](API-Client#rules)
## Initialization
First step declare variable CtServer   
```cs
private CtServer server;
```

Two step. Initialization variable server on _9000_ Port and additional **event listener** 
```cs
server = new CtServer(ip, 9000);

server.OnChangeUser += Server_OnChangeUser;
server.OnError += Server_OnError;
server.OnNewRoom += Server_OnNewRoom;

private void Server_OnNewRoom(object sender, EventArgs e){}

private void Server_OnError(object sender, string e){}

private void Server_OnChangeUser(object sender, EventArgs e){}

```
## Sing Up / Log In
Need SingUp or LogIn, for example:
```cs
void SingUp(string name, string email, string password, string leng);
/*
 *string name = User Name (min 4 symbol)
 *string email = User Email (@gmail.com and other)
 *string password = User Password (min 6 symbol)
 *string leng = Code countries (For example ru, us and other) 
 *
 */

public void LogIn(string email, string password);
/*
 *string email = User Email (@gmail.com and other)
 *string password = User Password (min 6 symbol)
 */
```
If user wanna sing up but they have account on the server, user well be auto log in on the server.

After Log In or Sing Up, `MainServer` Send to client data about User

If user Log in or Sing Up you can find open or create new room. If user wanna exit just call `Logout();`
```cs
public void Logout();
```
`Logout();` don't have argument because used internal variable `CurrentUser` 
## Room
If user wanna create Quick Match Just need call `CreateRoom()`
```cs
public void CreateRoom()
```
`CreateRoom()`don't have argument because used internal variable `CurrentUser`. After create room or join `MainServer` send data about room in internal variable `CurrentRoom`, You can get all info about Current Room.

After create room you can activate **event listner** `Server_OnUpdate`
For example:
```cs
server.CurrentRoom.OnUpdate += Server_OnUpdate;
```
this you can udpate user position
```cs
 private void Server_OnUpdate(object sender, User[] e)
 {
    foreach (User user in e)
    {
        if (!con(enemy, user))
        {
            var en = Instantiate(enPref);
            en.name = user.uId;
            en.transform.position = user.position;
            enemy.Add(en);
        }
     }
}
```

Perfect we get data about current user from  `MainServer` and get simple data about current room. Now we need get room rules.
## Rules
Rules have all data about room and players:

|Type | Name | Default Value| Description |
|-----|------|--------------|-------------|
| int | `BlueUser` | `5` | How much Player can be in Blue command |
| int | `RedUser` | `5` | How much Player can be in Red command |
| int | `Alive` | `1` | Available the Room now or no |
| int | `BlueScore` | `0` | Kills or other score for Blue command |
| int | `RedScore` | `0` | Kills or other score for Red command |
| int | `MatchState` | `0` | info about state match (0 - Waiting players,1-Play) |
| int | `timer` | `0` | Just match timer |

Well we get Rules and basic info about Room, We need get All users from the Room. You need use variable `CurrentRoom`.

for example `CurrentRoom` have only two methods:
```cs
public void GetNewRules();
public void GetNewUsers();
```
Don't have arguments and don't `return`. Just call `GetNewUsers()` if you wanna get users data.

#### >WARNING: `GetNewUsers()` all user data, not from you'r command/group because it's used for get firs table or other UI

#### Good Luck.







