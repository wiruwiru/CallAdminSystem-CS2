# CallAdminSystem CS2
Allows players to report another user who is breaking the community rules, this report is sent as an embed message to Discord so that administrators can respond.
## Installation
1. Install [CounterStrike Sharp](https://github.com/roflmuffin/CounterStrikeSharp) and [Metamod:Source](https://www.sourcemm.net/downloads.php/?branch=master)

2. Download [CallAdminSystem.zip](https://github.com/wiruwiru/CallAdminSystem-CS2/releases) from the releases section.

3. Unzip the archive and upload it to the game server

4. Start the server and wait for the config.json file to be generated.

5. Complete the configuration file with the parameters of your choice.

6. Write possible reporting reasons in reasons.txt.

# Config
| Parameter | Description | Required     |
| :------- | :------- | :------- |
| `WebhookUrl` | You must create it in the channel where you will send the notices. |**YES** |
| `IPandPORT` | Replace with the IP address of your server. |**YES** |
| `CustomDomain` | You can replace it with your domain if you want, the connect.php file is available in the main branch  |**YES** |
| `MentionRoleID` | You must have the discord developer mode activated, right click on the role and copy its ID. |**YES** |

## Configuration example
```
{
    WebhookUrl = "https://discord.com/api/webhooks/xxxxx/xxxxxxxxx,
    IPandPORT = "45.235.99.18:27025",
    CustomDomain = "https://crisisgamer.com/redirect/connect.php",
    MentionRoleID = "1111767358881681519"
}
```

## Reasons example
You must write one reason per line
```
Toxicity
Player Insults
AFK (Away From Keyboard)
Aimbot
Wallhack
Multi-Hack
Chat/Voice Spam
Map Bug
Other Reasons
```

# Lang configuration

In the 'lang' folder, you'll find various files. For instance, 'es.json' is designated for the Spanish language. You're welcome to modify this file to better suit your style and language preferences. The language utilized depends on your settings in 'core.json' of CounterStrikeSharp.

# Commands
`!report` `!call`  - Report a player command.

`!claim`  - Command to take reports on the server. | You need to have the @css/generic permission to use it.

## TO-DO
- Change configuration file location
- Add support for translations in ChatMenus titles

###
This project is a modification of [ReportSystem](https://github.com/PhantomYopta/-Discord-cs2-ReportSystem)