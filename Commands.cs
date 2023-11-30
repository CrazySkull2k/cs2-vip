using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using System.Reflection;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Menu;
using static CounterStrikeSharp.API.Core.Listeners;
using System.Runtime.Intrinsics.Arm;
using static System.Runtime.InteropServices.JavaScript.JSType;

using Nexd.MySQL;
using System.Runtime.ExceptionServices;
using CounterStrikeSharp.API.Core.Attributes;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Entities;
using System.Threading;

namespace VIP
{
    public partial class VIP
    {

        [ConsoleCommand("css_addfakeplayer", "Add fake client for testing reservation slots")]
        public void addfakeplayer(CCSPlayerController? player, CommandInfo info)
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                ConnectedPlayers++;
                Server.PrintToConsole($"You successful add new fake player on server/debugmode. Now are connected players {ConnectedPlayers}");
            }
        }
        [ConsoleCommand("css_addfakeround", "Add fake round for testing weapons")]
        public void addfakeround(CCSPlayerController? player, CommandInfo info)
        {
            if (AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                Round = 5;
                Server.PrintToConsole($"You successful add new round on server/debugmode. Now are round {Round}");
            }
        }
        [ConsoleCommand("css_testvip", "Test VIP")]
        public void CommandTESTVIP(CCSPlayerController? player, CommandInfo info)
        {

            MySqlDb MySql = new MySqlDb(Config.DBHost, Config.DBUser, Config.DBPassword, Config.DBDatabase);

            MySqlQueryResult result = MySql!.Table("users_test_vip").Where(MySqlQueryCondition.New("steam_id", "=", player.SteamID.ToString())).Select();

            if (result.Rows == 0)
            {
                int TimeSec = 3600;
                var TimeToUTC = DateTime.UtcNow.AddSeconds(Convert.ToInt32(TimeSec)).GetUnixEpoch();
                var timeofvip = DateTime.UtcNow.AddSeconds(Convert.ToInt32(TimeSec)).GetUnixEpoch();
                MySqlQueryValue _Tvalues = new MySqlQueryValue()
                .Add("steam_id", $"{player.SteamID}")
                .Add("used", $"{timeofvip}");
                MySql.Table("users_test_vip").Insert(_Tvalues);

                var timeRemaining = DateTimeOffset.FromUnixTimeSeconds(TimeToUTC) - DateTimeOffset.UtcNow;
                var timeRemainingFormatted =
                $"{timeRemaining.Days}d {timeRemaining.Hours:D2}:{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";

                MySqlQueryValue values = new MySqlQueryValue()
                .Add("steam_id", $"{player.SteamID}")
                .Add("end", $"{timeofvip}")
                .Add("`group`", $"0");
                MySql.Table("users").Insert(values);
                var client = player.Index;
                IsVIP[client] = 1;


                player.PrintToChat($" {ChatColors.Lime}=========================================");
                player.PrintToChat($" {Config.Prefix} You use a TestVIP.");
                player.PrintToChat($" {Config.Prefix} Ending time is {ChatColors.Lime}{timeRemainingFormatted}{ChatColors.Default}.");
                player.PrintToChat($" {ChatColors.Lime}=========================================");
                Server.PrintToConsole($"VIP Plugin - Player {player.PlayerName} add new TEST VIP with steamid {player.SteamID}, end time is {timeRemainingFormatted}");
            }
            else
            {

                var timeRemaining = DateTimeOffset.FromUnixTimeSeconds(result.Get<int>(0, "used")) - DateTimeOffset.UtcNow;
                var timeRemainingFormatted =
                $"{timeRemaining.Days}d {timeRemaining.Hours:D2}:{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";

                player.PrintToChat($" {ChatColors.Lime}=========================================");
                player.PrintToChat($" {Config.Prefix} You cannot use anymore TestVIP.");
                player.PrintToChat($" {Config.Prefix} Ending time is {ChatColors.Lime}{timeRemainingFormatted}{ChatColors.Default}.");
                player.PrintToChat($" {ChatColors.Lime}=========================================");
            }
        }
        [ConsoleCommand("css_activator", "Activate VIP from Tokens")]
        public void CommandActivator(CCSPlayerController? player, CommandInfo info)
        {
            var token = info.ArgByIndex(1);
            if (token == null || token == "" || IsInt(token))
                return;
            if (is_vip(player))
            {
                player.PrintToChat($" {Config.Prefix} You are {ChatColors.Lime}VIP{ChatColors.Default}, you {ChatColors.Red}cannot activate{ChatColors.Default} this VIP!");
                return;
            }

            MySqlDb MySql = new MySqlDb(Config.DBHost, Config.DBUser, Config.DBPassword, Config.DBDatabase);

            MySqlQueryResult result = MySql!.Table("users_key_vip").Where(MySqlQueryCondition.New("token", "=", $"{token}")).Select();

            if (result.Rows == 1)
            {

                var TimeToUTC = DateTime.UtcNow.AddSeconds(Convert.ToInt32(result.Get<int>(0, "end"))).GetUnixEpoch();
                var timeofvip = result.Get<int>(0, "end");
                var group_int = result.Get<int>(0, "group");
                if (result.Get<int>(0, "end") == 0)
                {
                    timeofvip = 0;
                }
                else
                {
                    timeofvip = DateTime.UtcNow.AddSeconds(Convert.ToInt32(result.Get<int>(0, "end"))).GetUnixEpoch();
                }
                var client = player.Index;
                var timeRemaining = DateTimeOffset.FromUnixTimeSeconds(TimeToUTC) - DateTimeOffset.UtcNow;
                var timeRemainingFormatted =
                $"{timeRemaining.Days}d {timeRemaining.Hours:D2}:{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";

                MySqlQueryValue _Tvalues = new MySqlQueryValue()
                .Add("steam_id", $"{player.SteamID}")
                .Add("end", $"{timeofvip}")
                .Add("`group`", $"{group_int}");
                MySql.Table("users").Insert(_Tvalues);
                player.PrintToChat($" {ChatColors.Lime}=========================================");
                player.PrintToChat($" {Config.Prefix} You activated the {ChatColors.Lime}VIP{ChatColors.Default}.");
                if (result.Get<int>(0, "end") == 0)
                {
                    player.PrintToChat($" {Config.Prefix} Your VIP is {ChatColors.Lime}Forever{ChatColors.Default}.");
                }
                player.PrintToChat($" {Config.Prefix} Ending time is {ChatColors.Lime}{timeRemainingFormatted}{ChatColors.Default}.");
                player.PrintToChat($" {ChatColors.Lime}=========================================");
                IsVIP[client] = 1;
                MySql.Table("users_key_vip").Where($"token = '{token}'").Delete();
            }
            else
            {
                player.PrintToChat($" {ChatColors.Lime}=========================================");
                player.PrintToChat($" {Config.Prefix} This {ChatColors.Red}Token{ChatColors.Default} dosent exist.");
                player.PrintToChat($" {Config.Prefix} You type this {ChatColors.Red}{token}{ChatColors.Default}.");
                player.PrintToChat($" {ChatColors.Lime}=========================================");
            }
        }
        [ConsoleCommand("css_generatevip", "Generate new VIP token")]
        public void CommandGenerateVIP(CCSPlayerController? player, CommandInfo info)
        {
            if (player != null) return;
            var TimeSec = info.ArgByIndex(1);
            var Group = info.ArgByIndex(2);
            if (TimeSec == null || TimeSec == "" || !IsInt(TimeSec) || Group == null || Group == "" || !IsInt(Group))
            {
                Server.PrintToConsole($"==========================================");
                Server.PrintToConsole($" {Config.Prefix} You must add seconds: css_generatevip <SECONDS> <GROUP>, must be added in int.");
                Server.PrintToConsole($" {Config.Prefix} If you wanna give forever VIP: css_generatevip 0 0");
                Server.PrintToConsole($" {Config.Prefix} <------------> List's of Groups <------------>");
                Server.PrintToConsole($" {Config.Prefix} < Group '0' > {Config.GroupsNames.Group1} < Group '0' >");
                Server.PrintToConsole($" {Config.Prefix} < Group '1' > {Config.GroupsNames.Group2} < Group '1' >");
                Server.PrintToConsole($" {Config.Prefix} <------------> List's of Groups <------------>");
                Server.PrintToConsole($"==========================================");

                return;
            }
            var TimeToUTC = DateTime.UtcNow.AddSeconds(Convert.ToInt32(TimeSec)).GetUnixEpoch();
            var timeofvip = 0;
            if (TimeSec == "0")
            {
                timeofvip = 0;
            }
            else
            {
                timeofvip = DateTime.UtcNow.AddSeconds(Convert.ToInt32(TimeSec)).GetUnixEpoch();
            }

            var token = CreatePassword(20);
            var group_int = Group;

            var timeRemaining = DateTimeOffset.FromUnixTimeSeconds(TimeToUTC) - DateTimeOffset.UtcNow;
                var timeRemainingFormatted =
                $"{timeRemaining.Days}d {timeRemaining.Hours:D2}:{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";

            MySqlDb MySql = new MySqlDb(Config.DBHost, Config.DBUser, Config.DBPassword, Config.DBDatabase);
            MySqlQueryValue values = new MySqlQueryValue()
            .Add("token", token)
            .Add("end", $"{timeofvip}")
            .Add("`group`", group_int);
            MySql.Table("users_key_vip").Insert(values);

            Server.PrintToConsole($"==========================================");
            Server.PrintToConsole($"You generate new VIP Token");
            Server.PrintToConsole($"Token: {token}");
            Server.PrintToConsole($"Ending: {timeRemainingFormatted}");
            Server.PrintToConsole($"Group ID: {Group}");
            Server.PrintToConsole($"==========================================");



        }
        [ConsoleCommand("css_addvip", "Add new VIP")]
        public void CommandAddVIP(CCSPlayerController? player, CommandInfo info)
        {
            var Group = info.ArgByIndex(3);
            var SteamIDC = info.ArgByIndex(2);
            var TimeSec = info.ArgByIndex(1);
            if (!AdminManager.PlayerHasPermissions(player, "@css/root"))
            {
                player.PrintToChat($" {Config.Prefix} You are not admin..");
                return;
            }
            else if (SteamIDC == null || SteamIDC == "" || !IsInt(SteamIDC))
            {
                player.PrintToChat($" {Config.Prefix} You must add SteamID. Example {ChatColors.Lime}/addvip <Time in seconds> 77777777 <GROUP>{ChatColors.Default}, must be added in int.");
                player.PrintToChat($" {Config.Prefix} Or if you wanna add forever VIP type {ChatColors.Lime}/addvip 0 77777777{ChatColors.Default}.");
                return;
            }
            else if (TimeSec == null || TimeSec == "" || !IsInt(TimeSec))
            {
                player.PrintToChat($" {Config.Prefix} You must add Time in seconds. Example {ChatColors.Lime}/addvip <Time in seconds> 77777777 <GROUP>{ChatColors.Default}, must be added in int.");
                player.PrintToChat($" {Config.Prefix} Or if you wanna add forever VIP type {ChatColors.Lime}/addvip 0 77777777{ChatColors.Default}.");

                return;
            }
            else if (Group == null || Group == "" || !IsInt(Group))
            {
                player.PrintToChat($" {Config.Prefix} You must add Group (Exist: 0, 1). Example {ChatColors.Lime}/addvip <Time in seconds> 77777777 <GROUP>{ChatColors.Default}, must be added in int.");
                player.PrintToChat($" {Config.Prefix} Or if you wanna add forever VIP type {ChatColors.Lime}/addvip 0 77777777 0{ChatColors.Default}.");
                player.PrintToChat($" {Config.Prefix} {ChatColors.Lime}<------------>{ChatColors.Default} List's of Groups {ChatColors.Lime}<------------>");
                player.PrintToChat($" {Config.Prefix} {ChatColors.Lime}< Group '0' >{ChatColors.Default} {Config.GroupsNames.Group1} {ChatColors.Lime}< Group '0' >");
                player.PrintToChat($" {Config.Prefix} {ChatColors.Lime}< Group '1' >{ChatColors.Default} {Config.GroupsNames.Group2} {ChatColors.Lime}< Group '1' >");
                player.PrintToChat($" {Config.Prefix} {ChatColors.Lime}<------------>{ChatColors.Default} List's of Groups {ChatColors.Lime}<------------>");


                return;
            }
            else
            {
                var TimeToUTC = DateTime.UtcNow.AddSeconds(Convert.ToInt32(TimeSec)).GetUnixEpoch();
                var timeofvip = 0;
                if(TimeSec == "0")
                {
                    timeofvip = 0;
                }
                else
                {
                    timeofvip = DateTime.UtcNow.AddSeconds(Convert.ToInt32(TimeSec)).GetUnixEpoch();
                }

                var timeRemaining = DateTimeOffset.FromUnixTimeSeconds(TimeToUTC) - DateTimeOffset.UtcNow;
                var timeRemainingFormatted =
                $"{timeRemaining.Days}d {timeRemaining.Hours:D2}:{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";

                MySqlDb MySql = new MySqlDb(Config.DBHost, Config.DBUser, Config.DBPassword, Config.DBDatabase);
                MySqlQueryValue values = new MySqlQueryValue()
                .Add("steam_id", SteamIDC)
                .Add("end", $"{timeofvip}")
                .Add("`group`", Group);
                MySql.Table("users").Insert(values);
                player.PrintToChat($" {ChatColors.Lime}=========================================");
                player.PrintToChat($" {Config.Prefix} Player with steamid {ChatColors.Lime}{SteamIDC}{ChatColors.Default} has been added.");
                player.PrintToChat($" {Config.Prefix} Ending time is {ChatColors.Lime}{timeRemainingFormatted}{ChatColors.Default}.");
                player.PrintToChat($" {ChatColors.Lime}=========================================");
                Server.PrintToConsole($"VIP Plugin - Admin {player.PlayerName} add new VIP with steamid {SteamIDC}, end time is {timeRemainingFormatted}");

            }
        }
        [ConsoleCommand("css_vip", "Info about VIP")]
        public void CommandVIPInfo(CCSPlayerController? player, CommandInfo info)
        {
            MySqlDb MySql = new MySqlDb(Config.DBHost, Config.DBUser, Config.DBPassword, Config.DBDatabase);

            MySqlQueryResult result = MySql!.Table("users").Where(MySqlQueryCondition.New("steam_id", "=", player.SteamID.ToString())).Select();
            var status = "";
            var formating = "";
            int status_i = 0;
            if (result.Rows == 1)
            {
                var client = player.Index;
                var timeRemaining = DateTimeOffset.FromUnixTimeSeconds(result.Get<int>(0, "end")) - DateTimeOffset.UtcNow;
                var nowtimeis = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var timeRemainingFormatted =
                $"{timeRemaining.Days}d {timeRemaining.Hours}:{timeRemaining.Minutes:D2}:{timeRemaining.Seconds:D2}";
                status = $" {ChatColors.Green}Active";
                formating = $" {ChatColors.Green}{timeRemainingFormatted}";
                IsVIP[client] = 1;
                status_i = 1;
                if (result.Get<int>(0, "end") != 0)
                {
                    status = $" {ChatColors.Green}Active";
                }
                if (result.Get<int>(0, "end") == 0)
                {
                    formating = $" {ChatColors.Green}Never ending";
                }
                status_i = 1;
            }
            else
            {
                status = $" {ChatColors.Red} Inactive";
                status_i = 0;
            }
            player.PrintToChat($" {ChatColors.Green}==!-!=={ChatColors.Lime} VIP {ChatColors.Default}Status {ChatColors.Green}==!-!==");
            player.PrintToChat($" {ChatColors.Gold}» {ChatColors.Default}You have {status}{ChatColors.Default} VIP Status.");
            if(status_i == 1)
            {
                player.PrintToChat($" {ChatColors.Gold}» {ChatColors.Default}Your {ChatColors.Lime}VIP have time {formating}{ChatColors.Default}.");
                player.PrintToChat($" {ChatColors.Gold}» {ChatColors.Default}Your {ChatColors.Lime}VIP {ChatColors.Default}group {ChatColors.Green}{get_name_group(player)}{ChatColors.Default}.");
                player.PrintToChat($" {ChatColors.Gold}▼ {ChatColors.Lime}Yours command available for you{ChatColors.Gold} ▼");
                if (get_vip_group(player) >= Config.CommandOnGroup.Weapons)
                {
                    player.PrintToChat($" {ChatColors.Gold}► {ChatColors.Default}Selecting weapons : {ChatColors.Lime}/weapon {ChatColors.Default}1,2,3,4,5{ChatColors.Gold} ◄");
                }
                if (get_vip_group(player) >= Config.CommandOnGroup.Pack )
                {
                    player.PrintToChat($" {ChatColors.Gold}► {ChatColors.Default}Selecting package : {ChatColors.Lime}/pack {ChatColors.Default}1,2{ChatColors.Gold} ◄");
                }
                if (get_vip_group(player) >= Config.CommandOnGroup.Respawn || Config.RespawnAllowed)
                {
                    player.PrintToChat($" {ChatColors.Gold}► {ChatColors.Default}Respawn on spawn  : {ChatColors.Lime}/respawn{ChatColors.Gold} ◄");
                }
                player.PrintToChat($" {ChatColors.Gold}► {ChatColors.Default}Turn of auto wep. : {ChatColors.Lime}/guns_off{ChatColors.Gold} ◄");

            }
            player.PrintToChat($" {ChatColors.Green}==!-!=={ChatColors.Lime} VIP {ChatColors.Default}Status {ChatColors.Green}==!-!==");


        }
        [ConsoleCommand("css_respawn", "Command to respawn player")]

        
        public void CommandRespawn(CCSPlayerController? player, CommandInfo info)
        {
            var client = player.Index;
            if (Config.RespawnAllowed == false)
                return;
            if (IsVIP[client] == 0)
            {
                player.PrintToChat($" {Config.Prefix} {Config.TranslationClass.MustBeVIP}");
                return;
            }
            if (Config.CommandOnGroup.Respawn > get_vip_group(player))
            {
                player.PrintToChat($" {Config.Prefix} You must have VIP group {get_name_group(player)}");
                return;
            }
            if (Round < 3)
            {
                player.PrintToChat($" {Config.Prefix} {Config.TranslationClass.MustBeThird}");
                return;
            }
            if (RespawnUsed[client] == 1)
            {
                player.PrintToCenterHtml($" <font color='red'>You canno't use /respawn at this time</font>");
                return;
            }
                player.PrintToCenterHtml($" <font color='green'>You used /respawn</font>");
                Server.NextFrame(() =>
                {
                    player!.Respawn();
                });
                RespawnUsed[client] = 1;

        }
        [ConsoleCommand("css_guns_off", "Disable automatically weapons")]

        public void CommandGUNS_off(CCSPlayerController? player, CommandInfo info)
        {
            var client = player.Index;
            if (!player.IsValid || !player.PlayerPawn.IsValid)
            {
                return;
            }
            if (IsVIP[client] == 0)
            {
                player.PrintToChat($" {Config.Prefix} {Config.TranslationClass.MustBeVIP}");
                return;
            }

            if (LastUsed[client] >= 1)
            {
                LastUsed[client] = 0;
                player.PrintToCenter($"You turn off automatically weapon..");
            }
        }
        [ConsoleCommand("css_weapon", "Select a Weapon from commands")]

        public void SelectWeapon(CCSPlayerController? player, CommandInfo info)
        {
            var PackagesID = info.ArgByIndex(1);
            var client = player.Index;
            if (PackagesID == null || PackagesID == "" || !IsInt(PackagesID))
            {
                player.PrintToChat($" {Config.Prefix} Please select the weapon. Must be added in int.");
                player.PrintToChat($" {Config.Prefix} Weapons 1 (AK-47). Type:{ChatColors.Lime}/weapon 1{ChatColors.Default}, must be added in int.");
                player.PrintToChat($" {Config.Prefix} Weapons 2 (M4A1). Type:{ChatColors.Lime}/weapon 2{ChatColors.Default}, must be added in int.");
                player.PrintToChat($" {Config.Prefix} Weapons 3 (M4A1-S). Type:{ChatColors.Lime}/weapon 3{ChatColors.Default}, must be added in int.");
                player.PrintToChat($" {Config.Prefix} Weapons 4 (AWP). Type:{ChatColors.Lime}/weapon 4{ChatColors.Default}, must be added in int.");
                return;
            }
            if (!player.IsValid || !player.PlayerPawn.IsValid)
            {
                return;
            }
            if (!player.PawnIsAlive)
            {
                player.PrintToChat($" {Config.Prefix} {Config.TranslationClass.MustBeAlive}");
                return;
            }
            if (IsVIP[client] == 0)
            {
                player.PrintToChat($" {Config.Prefix} {Config.TranslationClass.MustBeVIP}");
                return;
            }
            if (Config.CommandOnGroup.Weapons > get_vip_group(player))
            {
                player.PrintToChat($" {Config.Prefix} You must have VIP group {get_name_group(player)}");
                return;
            }
            if (Disabled20Sec)
            {
                player.PrintToChat($" {Config.Prefix} {Config.TranslationClass.MustFirst20Sec}");
                return;
            }

            if (Round < 3)
            {
                player.PrintToChat($" {Config.Prefix} {Config.TranslationClass.MustBeThird}");
                return;
            }
            if (Used[client] == 1)
            {
                player.PrintToChat($" {Config.Prefix} {Config.TranslationClass.OnceUse}");
                return;
            }
            if (Convert.ToInt32(PackagesID) == 1)
            {
                Used[client] = 1;
                LastUsed[client] = 1;
                if (CheckIsHaveWeapon("ak47", player) == false)
                {
                    player.GiveNamedItem("weapon_ak47");
                }
                player.PrintToChat($" {Config.Prefix} {Config.TranslationClass.WeaponAK}");
            }
            else if (Convert.ToInt32(PackagesID) == 2)
            {
                Used[client] = 1;
                LastUsed[client] = 4;
                if (CheckIsHaveWeapon("m4a1", player) == false)
                {
                    player.GiveNamedItem("weapon_m4a1");
                }
                player.PrintToChat($" {Config.Prefix} {Config.TranslationClass.WeaponM4A1}");
            }
            else if (Convert.ToInt32(PackagesID) == 3)
            {
                Used[client] = 1;
                LastUsed[client] = 5;
                if (CheckIsHaveWeapon("m4a1_silencer", player) == false)
                {
                    player.GiveNamedItem("weapon_m4a1_silencer");
                }
                player.PrintToChat($" {Config.Prefix} {Config.TranslationClass.WeaponM4A1S}");
            }
            else if (Convert.ToInt32(PackagesID) == 4)
            {
                Used[client] = 1;
                LastUsed[client] = 6;
                if (CheckIsHaveWeapon("awp", player) == false)
                {
                    player.GiveNamedItem("weapon_awp");
                }
                player.PrintToChat($" {Config.Prefix} {Config.TranslationClass.WeaponAWP}");
            }
            else
            {
                Used[client] = 0;
                LastUsed[client] = 0;
                player.PrintToChat($" {Config.Prefix} Please select the weapon. Must be added in int.");
                player.PrintToChat($" {Config.Prefix} Weapons 1 (AK-47). Type:{ChatColors.Lime}/weapon 1{ChatColors.Default}, must be added in int.");
                player.PrintToChat($" {Config.Prefix} Weapons 2 (M4A1). Type:{ChatColors.Lime}/weapon 2{ChatColors.Default}, must be added in int.");
                player.PrintToChat($" {Config.Prefix} Weapons 3 (M4A1-S). Type:{ChatColors.Lime}/weapon 3{ChatColors.Default}, must be added in int.");
                player.PrintToChat($" {Config.Prefix} Weapons 4 (AWP). Type:{ChatColors.Lime}/weapon 3{ChatColors.Default}, must be added in int.");
            }

        }
        [ConsoleCommand("css_pack", "Select a packages of weapons")]

        public void PackagesWeapons(CCSPlayerController? player, CommandInfo info)
        {
            var PackagesID = info.ArgByIndex(1);
            var client = player.Index;
            if (PackagesID == null || PackagesID == "" || !IsInt(PackagesID))
            {
                player.PrintToChat($" {Config.Prefix} Current packages ids is {ChatColors.Lime}1{ChatColors.Default},{ChatColors.Lime}2{ChatColors.Default}. {ChatColors.Lime}/pack ID_PACK{ChatColors.Default}, must be added in int.");
                return;
            }
            if (!player.IsValid || !player.PlayerPawn.IsValid)
            {
                return;
            }
            if (!player.PawnIsAlive)
            {
                player.PrintToChat($" {Config.Prefix} {Config.TranslationClass.MustBeAlive}");
                return;
            }
            if (IsVIP[client] == 0)
            {
                player.PrintToChat($" {Config.Prefix} {Config.TranslationClass.MustBeVIP}");
                return;
            }
            if (Config.CommandOnGroup.Pack > get_vip_group(player))
            {
                player.PrintToChat($" {Config.Prefix} You must have VIP group {get_name_group(player)}");
                return;
            }
            if (Disabled20Sec)
            {
                player.PrintToChat($" {Config.Prefix} {Config.TranslationClass.MustFirst20Sec}");
                return;
            }

            if (Round < 3)
            {
                player.PrintToChat($" {Config.Prefix} {Config.TranslationClass.MustBeThird}");
                return;
            }
            if (Used[client] == 1)
            {
                player.PrintToChat($" {Config.Prefix} {Config.TranslationClass.OnceUse}");
                return;
            }
            if (Convert.ToInt32(PackagesID) == 1)
            {
                Used[client] = 1;
                LastUsed[client] = 2;
                player.PrintToChat($" {Config.Prefix} {Config.TranslationClass.Pack1}");
                // Weapons
                if (CheckIsHaveWeapon($"{Config.Pack1Settings.Pistol}", player) == false)
                {
                    player.GiveNamedItem($"weapon_{Config.Pack1Settings.Pistol}");
                }
                if (CheckIsHaveWeapon($"{Config.Pack1Settings.Gun}", player) == false)
                {
                    player.GiveNamedItem($"weapon_{Config.Pack1Settings.Gun}");
                }
                if (CheckIsHaveWeapon($"{Config.Pack1Settings.Acceroies}", player) == false)
                {
                    player.GiveNamedItem($"weapon_{Config.Pack1Settings.Acceroies}");

                }
                // Granades
                if (CheckIsHaveWeapon($"{Config.Pack1Settings.Acceroies_2}", player) == false)
                {
                    player.GiveNamedItem($"weapon_{Config.Pack1Settings.Acceroies_2}");
                }
                if (CheckIsHaveWeapon($"{Config.Pack1Settings.Acceroies_3}", player) == false)
                {
                    player.GiveNamedItem($"weapon_{Config.Pack1Settings.Acceroies_3}");
                }
                if (CheckIsHaveWeapon($"{Config.Pack1Settings.Acceroies_4}", player) == false)
                {
                    player.GiveNamedItem($"weapon_{Config.Pack1Settings.Acceroies_4}");
                }
            }
            else if (Convert.ToInt32(PackagesID) == 2)
            {
                Used[client] = 1;
                LastUsed[client] = 3;
                player.PrintToChat($" {Config.Prefix} {Config.TranslationClass.Pack2}");
                // Weapons
                if (CheckIsHaveWeapon($"{Config.Pack2Settings.Pistol}", player) == false)
                {
                    player.GiveNamedItem($"weapon_{Config.Pack2Settings.Pistol}");
                }
                if (CheckIsHaveWeapon($"{Config.Pack2Settings.Gun}", player) == false)
                {
                    player.GiveNamedItem($"weapon_{Config.Pack2Settings.Gun}");
                }
                if (CheckIsHaveWeapon($"{Config.Pack2Settings.Acceroies}", player) == false)
                {
                    player.GiveNamedItem($"weapon_{Config.Pack2Settings.Acceroies}");

                }
                // Granades
                if (CheckIsHaveWeapon($"{Config.Pack2Settings.Acceroies_2}", player) == false)
                {
                    player.GiveNamedItem($"weapon_{Config.Pack2Settings.Acceroies_2}");
                }
                if (CheckIsHaveWeapon($"{Config.Pack2Settings.Acceroies_3}", player) == false)
                {
                    player.GiveNamedItem($"weapon_{Config.Pack2Settings.Acceroies_3}");
                }
                if (CheckIsHaveWeapon($"{Config.Pack2Settings.Acceroies_4}", player) == false)
                {
                    player.GiveNamedItem($"weapon_{Config.Pack2Settings.Acceroies_4}");
                }
            }
            else
            {
                Used[client] = 0;
                LastUsed[client] = 0;
                player.PrintToChat($" {Config.Prefix} Current packages ids is {ChatColors.Lime}1{ChatColors.Default},{ChatColors.Lime}2{ChatColors.Default}. {ChatColors.Lime}/pack ID_PACK{ChatColors.Default}, must be added in int.");
            }
        }
    }
}
