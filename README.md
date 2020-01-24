# GameServer
I will do the documentation later

# [Server](GameServerV1/Server/MainServer.cs)
**Types on MainServer/Client**

| Type | Value | Description |
| --- |--- | --- |
| **Default Types** |
| `TYPE_NonPack` |` 0 `| If ur pack brouken or http |
| **SingUP Types** |
| `TYPE_SingUpS` | `1 `| Send pack for SingUP |
| `TYPE_SingUpR` | `2 `| Get pack with User info |
| **Login Types** |
| `TYPE_LogInS` | `3 `| Send pack for login |
| `TYPE_LogInR` | `4 `| Get pack with User info  |
| `TYPE_LogOut` | `10 `|Send Info about logout from server  |
|**CreateRoom Types** |
| `TYPE_CreateRoomS` | `5 `| Send pack for Create Rooom |
| `TYPE_CreateRoomR` | `6 `| Get pack with Room info  |
|**Update Room Types** |
| `TYPE_update_rules` | `7 `| Get pack with new Rules data |
| `TYPE_update_users` | `8 `| Get pack with new Users data in Room  |
|**Update Room Types** |
| `TYPE_i_wanna_info` | `9`| Send pack to get info about Rules  |
| `TYPE_i_wanna_users` | `11`|Send pack to get info All Users in room  |
| `TYPE_i_newUser` | `12`| Send pack with User Info |

## [class MainServer](GameServerV1/Server/MainServer.cs#L<38>)
### Loigic Class
MainServer - This class is responsible for new customers and carries out their registration / login and lets them into the room or creates it.

```
  -user connect by Tcp
  -user AcceptTcpClient
    -new Thread()
      -user send request on Login/SingUp
        -user send request on Join/Create Room
          -new Thread()
            -Create new RoomServer on Next Active port 
      
```

### API Documentation

**Public Parametrs**

| Type | Name | Description | Default Value |
| --- |--- | --- | --- |
|`const string`|`BD_SOURCE_USERS` `{get; set;}` | Link to Data Base SQL |`lint to bd`|
|`float`|`IsTimer` `{set;}`| `in sec` waint data from Client | `60`sec |
|`static bool`|`_status` `{get; set;}`|  Bool Server Available  for new connect| `true` |

**Private Parametrs**

| Type | Name | Description | Default Value |
| --- |--- | --- | --- |
| `static int` | `portroomnow` | Link to Data Base SQL | `PORT` |
| `static int` | `PORT` | Link to Data Base SQL | `9000`|
| `static int` | `DEFPACSIZE` | Data Size | `8*1024`byte |
| `static TcpListener` | `listener` | Link to Data Base SQL | on Any Ip / and defaulte port |
| `List<TcpClient>` | `clients` | pool client | `{null}` |
| `List<RoomServer>` | `rooms` | pool room | `{null}` |
| `static BinaryFormatter` | `binFormatter` | for Serialization | `BinaryFormatter` |

**Private Methods**

| Name | Description |
| --- | --- | 
| ` void Channel()` | Thread for Client | 
| `User SingUp()` | Just SingUp | 
| `User LogIn()` | Just LogIn | 
| `void setUserStatys()` | Set user status into SQL  | 
| `void closeConnect()` | Close connect with `TcpClient` |
| `void sendDictionary()` | Send `Dictionary<>` to `NetworkStream` |
| `Dictionary<> ByteToDictionary()` | Convert `Byte[]` to `Dictionary<>` |
| `RoomServer FindActiveRoom()` |Find and `return` free `RoomServer` |
| `void CreateNewRoom()` | Create new `RoomServer` if `FindActiveRoom()` returned `null` |









