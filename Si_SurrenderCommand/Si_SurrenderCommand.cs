﻿/*
 Silica Surrender Command Mod
 Copyright (C) 2023-2024 by databomb
 
 * Description *
 For Silica listen servers, provides a command (!surrender) which
 each team's commander can use to have their team give up early.
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

#if NET6_0
using Il2Cpp;
#endif

using HarmonyLib;
using MelonLoader;
using Si_SurrenderCommand;
using SilicaAdminMod;
using System;
using UnityEngine;
using System.Collections.Generic;

[assembly: MelonInfo(typeof(SurrenderCommand), "Surrender Command", "1.2.7", "databomb", "https://github.com/data-bomb/Silica")]
[assembly: MelonGame("Bohemia Interactive", "Silica")]
[assembly: MelonOptionalDependencies("Admin Mod")]

namespace Si_SurrenderCommand
{
    public class SurrenderCommand : MelonMod
    {
        public static bool IsCommander(Player thePlayer)
        {
            if (thePlayer == null)
            {
                return false;
            }

            Team theTeam = thePlayer.Team;
            if (theTeam == null)
            {
                return false;
            }

            MP_Strategy strategyInstance = GameObject.FindObjectOfType<MP_Strategy>();
            Player teamCommander = strategyInstance.GetCommanderForTeam(theTeam);

            if (teamCommander == thePlayer)
            {
                return true;
            }

            return false;
        }

        public override void OnLateInitializeMelon()
        {
            HelperMethods.CommandCallback surrenderCallback = Command_Surrender;
            HelperMethods.RegisterPlayerCommand("surrender", surrenderCallback, true);
        }

        public static void Command_Surrender(Player? callerPlayer, String args)
        {
			if (callerPlayer == null)
			{
				HelperMethods.SendChatMessageToPlayer(callerPlayer, HelperMethods.chatPrefix, " Console not supported.");
				return;
			}

            // check if game on-going
            if (!GameMode.CurrentGameMode.GameOngoing)
            {
                HelperMethods.SendChatMessageToPlayer(callerPlayer, HelperMethods.chatPrefix, " Can't surrender. Game not started.");
                return;
            }

            // check if we are actually a commander
            bool isCommander = IsCommander(callerPlayer);

            if (!isCommander)
            {
                // notify player on invalid usage
                HelperMethods.ReplyToCommand_Player(callerPlayer, ": only commanders can use !surrender");
                return;
            }

            // if they're a commander then immediately take action
            Team surrenderTeam = callerPlayer.Team;
            Surrender(surrenderTeam, callerPlayer);
        }

        public static void Surrender(Team team, Player player)
        {
            // notify all players
            HelperMethods.ReplyToCommand_Player(player, "used !surrender to end");

            // find all construction sites we should destroy form the team that's surrendering
            RemoveConstructionSites(team);

            // destroy all structures on team that's surrendering
            RemoveStructures(team);

            // and destroy all units (especially the queen)
            RemoveUnits(team);
        }

        public static void RemoveConstructionSites(Team team)
        {
            List<ConstructionSite> sitesToDestroy = new List<ConstructionSite>();

            foreach (ConstructionSite constructionSite in ConstructionSite.ConstructionSites)
            {
                if (constructionSite == null || constructionSite.Team == null)
                {
                    continue;
                }

                if (constructionSite.Team != team)
                {
                    continue;
                }

                sitesToDestroy.Add(constructionSite);
            }

            foreach (ConstructionSite constructionSite in sitesToDestroy)
            {
                constructionSite.Deinit(false);
            }
        }

        public static void RemoveStructures(Team team)
        {
            for (int i = 0; i < team.Structures.Count; i++)
            {
                if (team.Structures[i] == null)
                {
                    MelonLogger.Warning("Found null structure during surrender command.");
                    continue;
                }

                team.Structures[i].DamageManager.SetHealth01(0.0f);
            }
        }

        public static void RemoveUnits(Team team)
        {
            for (int i = 0; i < team.Units.Count; i++)
            {
                if (team.Units[i] == null)
                {
                    MelonLogger.Warning("Found null unit during surrender command.");
                    continue;
                }

                if (team.Units[i].IsDestroyed)
                {
                    continue;
                }

                team.Units[i].DamageManager.SetHealth01(0.0f);
            }
        }
    }
}