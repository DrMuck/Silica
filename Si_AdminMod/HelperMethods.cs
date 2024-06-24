﻿/*
Silica Admin Mod
Copyright (C) 2023-2024 by databomb

* License *
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using MelonLoader;
using System;
using UnityEngine;


#if NET6_0
using Il2Cpp;
using Il2CppSteamworks;
using Il2CppDebugTools;
#else
using DebugTools;
using System.Collections.Generic;
using Steamworks;
using System.Reflection;
#endif

namespace SilicaAdminMod
{
    public static class HelperMethods
    {
        public static readonly float Timer_Inactive = -123.0f;
        public const string defaultColor = "<color=#DDE98C>";
        public const string chatPrefix = "<b>" + defaultColor + "[<color=#DFA725>SAM" + defaultColor + "]</b> ";

        public delegate void CommandCallback(Player? callerPlayer, String args);

        public static void RegisterAdminCommand(String adminCommand, CommandCallback adminCallback, Power adminPower, String? adminDescription = null)
        {
            AdminMethods.RegisterAdminCommand(adminCommand, adminCallback, adminPower, adminDescription);
        }

        public static void RegisterPlayerCommand(String playerCommand, CommandCallback commandCallback, bool hideFromChat)
        {
            PlayerMethods.RegisterPlayerCommand(playerCommand, commandCallback, hideFromChat);
        }
        public static void RegisterPlayerPhrase(String playerCommand, CommandCallback commandCallback, bool hideFromChat)
        {
            PlayerMethods.RegisterPlayerPhrase(playerCommand, commandCallback, hideFromChat);
        }

        public static bool UnregisterAdminCommand(String adminCommand)
        {
            return AdminMethods.UnregisterAdminCommand(adminCommand);
        }

        public static bool UnregisterPlayerCommand(String playerCommand)
        {
            return PlayerMethods.UnregisterPlayerCommand(playerCommand);
        }

        public static bool UnregisterPlayerPhrase(String playerPhrase)
        {
            return PlayerMethods.UnregisterPlayerPhrase(playerPhrase);
        }

        public static void ReplyToCommand(params string[] messages)
        {
            Player broadcastPlayer = FindBroadcastPlayer();
            broadcastPlayer.SendChatMessage(chatPrefix + String.Concat(messages), false);
        }

        public static void ReplyToCommand_Player(Player player, params string[] messages)
        {
            Player broadcastPlayer = FindBroadcastPlayer();
            broadcastPlayer.SendChatMessage(chatPrefix + GetTeamColor(player) + player.PlayerName + defaultColor + " " + String.Concat(messages), false);
        }

        public static void AlertAdminActivity(Player? adminPlayer, Player targetPlayer, string action)
        {
            Player broadcastPlayer = FindBroadcastPlayer();
            string adminName = (adminPlayer == null) ? "SERVER CONSOLE" : adminPlayer.PlayerName;
            broadcastPlayer.SendChatMessage(chatPrefix + GetAdminColor() + adminName + defaultColor + " " + action + " " + GetTeamColor(targetPlayer) + targetPlayer.PlayerName, false);
        }

        public static void AlertAdminAction(Player? adminPlayer, string action)
        {
            Player broadcastPlayer = FindBroadcastPlayer();
            string adminName = (adminPlayer == null) ? "SERVER CONSOLE" : adminPlayer.PlayerName;
            broadcastPlayer.SendChatMessage(chatPrefix + GetAdminColor() + adminName + defaultColor + " " + action, false);
        }

        public static void SendChatMessageToTeam(Team team, params string[] messages)
        {
            Player broadcastPlayer = FindBroadcastPlayer(team);

            for (int i = 0; i < Player.Players.Count; i++)
            {
                Player? player = Player.Players[i];
                if (player == null)
                {
                    continue;
                }

                if (player.Team == team)
                {
                    NetworkSendChat(player, broadcastPlayer, messages);
                }
            }
        }

        public static void SendChatMessageToPlayer(Player? player, params string[] messages)
        {
            // send to server console if null
            if (player == null)
            {
                DebugConsole.Log(String.Concat(messages), DebugConsole.LogLevel.Log);
                DebugConsole.Log("", DebugConsole.LogLevel.Log);
                return;
            }

            Player broadcastPlayer = FindBroadcastPlayer();
            NetworkSendChat(player, broadcastPlayer, messages);
        }
        
        public static void NetworkSendChat(Player recipient, Player sender, params string[] messages)
        {
            GameByteStreamWriter gameByteStreamWriter = GameByteStreamWriter.GetGameByteStreamWriter(0U, "Si_AdminMod::NetworkSendChat", true);
            gameByteStreamWriter.WriteByte((byte)ENetworkPacketType.ChatMessage);
            gameByteStreamWriter.WriteUInt64((ulong)sender.PlayerID);
            gameByteStreamWriter.WriteByte((byte)sender.PlayerChannel);
            gameByteStreamWriter.WriteString(String.Concat(messages));
            gameByteStreamWriter.WriteBool(false);

            SteamGameServerNetworking.SendP2PPacket(recipient.PlayerID, gameByteStreamWriter.GetByteData(), (uint)gameByteStreamWriter.GetByteDataSize(), EP2PSend.k_EP2PSendReliable, recipient.PlayerChannel);
        }

        public static Player FindBroadcastPlayer()
        {
            if (!NetworkGameServer.GetServerDedicated())
            {
                return NetworkGameServer.GetServerPlayer();
            }

            // not ideal but funnel messages through the first player for now
            if (Player.Players.Count > 0)
            {
                return Player.Players[0];
            }

            return NetworkGameServer.GetServerPlayer();
        }

        public static Player FindBroadcastPlayer(Team team)
        {
            if (!NetworkGameServer.GetServerDedicated())
            {
                return NetworkGameServer.GetServerPlayer();
            }

            // not ideal but funnel messages through the first player on the team for now
            for (int i = 0; i < Player.Players.Count; i++)
            {
                Player? player = Player.Players[i];
                if (player == null)
                {
                    continue;
                }

                if (player.Team == team)
                {
                    return player;
                }
            }

            return NetworkGameServer.GetServerPlayer();
        }

        public static void PrintError(Exception exception, string? message = null)
        {
            if (message != null)
            {
                MelonLogger.Msg(message);
            }
            string error = exception.Message;
            error += "\n" + exception.TargetSite;
            error += "\n" + exception.StackTrace;
            MelonLogger.Error(error);
        }

        public static Player? FindTargetPlayer(String sTarget)
        {
            Player? targetPlayer = null;
            int iTargetCount = 0;

            // loop through all players
            #if NET6_0
            Il2CppSystem.Collections.Generic.List<Player> players = Player.Players;
            #else
            List<Player> players = Player.Players;
            #endif

            int iPlayerCount = players.Count;

            for (int i = 0; i < iPlayerCount; i++)
            {
                if (players[i].PlayerName.Contains(sTarget))
                {
                    iTargetCount++;
                    targetPlayer = players[i];
                }
            }

            if (iTargetCount != 1)
            {
                targetPlayer = null;
            }

            return targetPlayer;
        }

        public static string GetAdminColor()
        {
            return "<color=#DFA725>";
        }

        private static string TeamColorTextFromIndex(SiConstants.ETeam teamIndex)
        {
            return teamIndex switch
            {
                SiConstants.ETeam.Alien => "<color=#54CC54>",
                SiConstants.ETeam.Centauri => "<color=#FF5454>",
                SiConstants.ETeam.Sol => "<color=#548DFF>",
                SiConstants.ETeam.Wildlife => "<color=#cc8800>",
                _ => "<color=#FFFFFF>",
            };
        }

        public static string GetTeamColor(Team team)
        {
            if (team == null)
            {
                return "<color=#FFFFFF>";
            }

            return TeamColorTextFromIndex((SiConstants.ETeam)team.Index);
        }

        public static string GetTeamColor(Player player)
        {
            if (player == null)
            {
                return "<color=#FFFFFF>";
            }

            Team? team = player.Team;
            if (team == null)
            {
                return "<color=#FFFFFF>";
            }

            int teamIndex = team.Index;
            return TeamColorTextFromIndex((SiConstants.ETeam)teamIndex);
        }

        public static Power PowerTextToPower(String powerText)
        {
            Power powers = Power.None;
            if (powerText.Contains('a'))
            {
                powers |= Power.Slot;
            }
            if (powerText.Contains('b'))
            {
                powers |= Power.Vote;
            }
            if (powerText.Contains('c'))
            {
                powers |= Power.Kick;
            }
            if (powerText.Contains('d'))
            {
                powers |= Power.Ban;
            }
            if (powerText.Contains('e'))
            {
                powers |= Power.Unban;
            }
            if (powerText.Contains('f'))
            {
                powers |= Power.Slay;
            }
            if (powerText.Contains('g'))
            {
                powers |= Power.Map;
            }
            if (powerText.Contains('h'))
            {
                powers |= Power.Cheat;
            }
            if (powerText.Contains('i'))
            {
                powers |= Power.Commander;
            }
            if (powerText.Contains('j'))
            {
                powers |= Power.Skip;
            }
            if (powerText.Contains('k'))
            {
                powers |= Power.End;
            }
            if (powerText.Contains('l'))
            {
                powers |= Power.Eject;
            }
            if (powerText.Contains('m'))
            {
                powers |= Power.Mute;
            }
            if (powerText.Contains('n'))
            {
                powers |= Power.MuteForever;
            }
            if (powerText.Contains('o'))
            {
                powers |= Power.Generic;
            }
            if (powerText.Contains('p'))
            {
                powers |= Power.Teams;
            }
            if (powerText.Contains('q'))
            {
                powers |= Power.Custom1;
            }
            if (powerText.Contains('r'))
            {
                powers |= Power.Custom2;
            }
            if (powerText.Contains('s'))
            {
                powers |= Power.Custom3;
            }
            if (powerText.Contains('t'))
            {
                powers |= Power.Custom4;
            }
            if (powerText.Contains('u'))
            {
                powers |= Power.Reserved1;
            }
            if (powerText.Contains('v'))
            {
                powers |= Power.Reserved2;
            }
            if (powerText.Contains('w'))
            {
                powers |= Power.Reserved3;
            }
            if (powerText.Contains('x'))
            {
                powers |= Power.Reserved4;
            }
            if (powerText.Contains('y'))
            {
                powers |= Power.Rcon;
            }
            if (powerText.Contains('z'))
            {
                powers |= Power.Root;
            }

            return powers;
        }

        public static void DestroyAllStructures(Team team)
        {
            if (team == null)
            {
                return;
            }

            for (int i = 0; i < team.Structures.Count; i++)
            {
                team.Structures[i].DamageManager.SetHealth01(0.0f);
            }
        }

        public static bool KickPlayer(Player playerToKick)
        {
            Player serverPlayer = NetworkGameServer.GetServerPlayer();

            if (playerToKick == serverPlayer)
            {
                return false;
            }

            #if NET6_0
            Il2CppSteamworks.CSteamID serverSteam = NetworkGameServer.GetServerID();
            Il2CppSteamworks.CSteamID playerSteam = playerToKick.PlayerID;
            #else
            Steamworks.CSteamID serverSteam = NetworkGameServer.GetServerID();
            Steamworks.CSteamID playerSteam = playerToKick.PlayerID;
            #endif

            int playerChannel = playerToKick.PlayerChannel;

            NetworkLayer.SendPlayerConnectResponse(ENetworkPlayerConnectType.Kicked, playerSteam, playerChannel, serverSteam);
            Player.RemovePlayer(playerSteam, playerChannel);
            NetworkLayer.SendPlayerConnect(ENetworkPlayerConnectType.Disconnected, playerSteam, playerChannel);

            return true;
        }

        public static GameObject? SpawnAtLocation(String name, Vector3 position, Quaternion rotation, int teamIndex = -1)
        {
            int prefabIndex = GameDatabase.GetSpawnablePrefabIndex(name);
            if (prefabIndex <= -1)
            {
                return null;
            }

            GameObject prefabObject = GameDatabase.GetSpawnablePrefab(prefabIndex);
            GameObject spawnedObject = Game.SpawnPrefab(prefabObject, null, true, true);

            if (spawnedObject == null)
            {
                return null;
            }

            Unit thisUnit = spawnedObject.GetComponent<Unit>();
            // unit
            if (thisUnit != null)
            {
                position.y += 3f;
                spawnedObject.transform.position = position;
                spawnedObject.transform.rotation = rotation;

                spawnedObject.transform.GetBaseGameObject().Teleport(position, rotation);

                if (teamIndex > -1)
                {
                    thisUnit.Team = Team.Teams[teamIndex];
                }

                return spawnedObject;
            }

            Structure thisStructure = spawnedObject.GetComponent<Structure>();
            // structure
            if (thisStructure != null)
            {
                spawnedObject.transform.position = position;
                spawnedObject.transform.rotation = rotation;

                if (teamIndex > -1)
                {
                    thisStructure.Team = Team.Teams[teamIndex];
                }

                return spawnedObject;
            }

            return null;
        }

        public static bool IsTimerActive(float time)
        {
            if (time >= 0.0f)
            {
                return true;
            }

            return false;
        }

        public static void StartTimer(ref float timer)
        {
            timer = 0.0f;
        }
    }
}