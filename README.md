# CallAdminSystem CS2
Allows players to report another user who is breaking the community rules, this report is sent as an embed message to Discord so that administrators can respond.

https://github.com/user-attachments/assets/fd49799b-bc89-4d4d-8b9a-627670620c80

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
| `CustomDomain` | You can replace it with your domain if you want, the connect.php file is available in the main branch.  |**YES** |
| `MentionRoleID` | You must have the discord developer mode activated, right click on the role and copy its ID. |**YES** |
| `ReportCommand` | Sets the command with which players can report other users. |**YES** |
| `ClaimCommand` | Sets the command with which administrators can claim reports from that server. |**YES** |
| `ClaimCommandFlag` | Sets the permission that is needed to use the claim command |**YES** |
| `CommandCooldownSeconds` | Cooling down time for the user to be able to use the command again (in seconds). |**YES** |
| `MinimumPlayers` | Minimum players that must be connected to be able to use the command. |**YES** |

## Configuration example
```
{
  "WebhookUrl": "https://discord.com/api/webhooks/xxxxx/xxxxxxxxx,
  "IPandPORT": "45.235.99.18:27025",
  "CustomDomain": "https://crisisgamer.com/redirect/connect.php",
  "MentionRoleID": "1111767358881681519",
  "ReportCommand": [ "css_call", "css_report" ],
  "ClaimCommand": "css_claim",
  "ClaimCommandFlag": "@css/generic",
  "CommandCooldownSeconds": 60,
  "MinimumPlayers": 2,
  "ConfigVersion": 1
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
```

# Lang configuration

In the 'lang' folder, you'll find various files. For instance, 'es.json' is designated for the Spanish language. You're welcome to modify this file to better suit your style and language preferences. The language utilized depends on your settings in 'core.json' of CounterStrikeSharp.

# Custom domain configuration

To configure CustomDomain you must first upload the “connect.php” file to your web hosting, after you have done this step you must place the url of this file in the configuration file. It should look like this `https://domain.com/redirect/connect.php` (EXAMPLE URL). In case you don't have a web hosting you can leave the default url.
You can download the **`connect.php`** file directly from here: [Download connect.php](https://raw.githubusercontent.com/wiruwiru/CallAdminSystem-CS2/main/connect.php). 
> **Note:** Right-click the link and select "Save link as..." to download the file directly.

# Commands
`!report` `!call`  - Report a player command.

`!claim`  - Command to take reports on the server. | You need to have the default @css/generic permission to be able to use the command.

## TO-DO
- [x] Change configuration file location
- [ ] Move the reasons to configuration
- Any recommendations to help improve the plugin

###
This project is a modification of [ReportSystem](https://github.com/PhantomYopta/-Discord-cs2-ReportSystem)
