/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace MissionRework
{
    [HarmonyPatch(typeof(PlayerReputation))]
    internal static class ReputationPatcehs
    {
        [HarmonyPatch("UpdateReputation")]
        [HarmonyPostfix]
        public static void UpdateReputationPatch(ref int[] ___reputation)
        {
            if (!Main.moreMissions.Value) return;
            for (int i = 0; i < ___reputation.Length; i++)
            {
                int level = PlayerReputation.GetLevel(___reputation[i]);
                PlayerReputation.maxMissions[i] = level + 2;
                if (level >= 5) { PlayerReputation.maxMissions[i] = 99; }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerMissions))]
    internal static class PlayerMissionsPatches
    {
        [HarmonyPatch("AcceptMission")]
        [HarmonyPrefix]
        public static void AcceptMission(Mission mission)
        {
            if (!Main.moreMissions.Value) { return; }
            if (PlayerMissions.missions == null || PlayerMissions.missions.Length == 5)
            {
                Mission[] newMissions = new Mission[99];

                for (int i = 0; i < PlayerMissions.missions.Length; i++)
                {
                    newMissions[i] = PlayerMissions.missions[i];
                }
                PlayerMissions.missions = newMissions;
            }
        }
    }
}
*/