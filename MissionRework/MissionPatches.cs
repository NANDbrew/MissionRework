using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityModManagerNet;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using SailwindModdingHelper;
using System.ComponentModel;
using static UnityModManagerNet.UnityModManager;
using System.Net.Mail;
//using UnityEngine.TextRenderingModule;

namespace MissionRework
{
    internal class MissionPatches
    {
        //internal static List<Good> missionGoodsInArea = new List<Good>();
        public static int currentPortIndex;
        // <int port, List<Good> goodsInArea>
        public static Dictionary<int, List<Good>> portLists = new Dictionary<int, List<Good>>();
        
        [HarmonyPatch(typeof(IslandMarketWarehouseArea))]
        private static class MissionGoodRegistrationPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch("Awake")]
            public static void Register(IslandMarketWarehouseArea __instance, IslandMarket ___market)
            {
                currentPortIndex = ___market.GetPortIndex();
                if (!portLists.ContainsKey(currentPortIndex))
                {
                    portLists.Add(currentPortIndex, new List<Good>());

                }
            }

            [HarmonyPostfix]
            [HarmonyPatch("OnTriggerEnter")]
            public static void OnTriggerEnterPatch(IslandMarketWarehouseArea __instance, Collider other, IslandMarket ___market)
            {
                currentPortIndex = ___market.GetPortIndex();

                if (!portLists.ContainsKey(currentPortIndex))
                {
                    portLists.Add(currentPortIndex, new List<Good>());

                }
                Utilities.Log(Utilities.LogType.Log, "Current port: " + ___market.GetPortName() + " (" + ___market.GetPortIndex() + ")");

                Good component = other.GetComponent<Good>();

                List<Good> missionGoodsInArea = portLists[currentPortIndex];

                if ((bool)component && !missionGoodsInArea.Contains(component) && !(component.GetMissionIndex() == -1))
                {
                    missionGoodsInArea.Add(component);
                    Utilities.Log(Utilities.LogType.Log, "Good in area: " + component);

                }
                //currentPortIndex = ___market.GetPortIndex();
                //AddGood(component);
                Utilities.Log(Utilities.LogType.Log, "list '" + currentPortIndex + "' contains " + missionGoodsInArea.Count.ToString() + " items");
                //Utilities.Log(Utilities.LogType.Log, "Object entered area: " + other.name);
                //Utilities.Log(Utilities.LogType.Log, "Object entered area: " + other.GetComponent<Good>().GetMissionIndex());

            }

            [HarmonyPostfix]
            [HarmonyPatch("OnTriggerExit")]
            public static void OnTriggerExitPatch(IslandMarketWarehouseArea __instance, Collider other, IslandMarket ___market)
            {
                Utilities.Log(Utilities.LogType.Log, "Object exited area: " + other.name);
                currentPortIndex = ___market.GetPortIndex();
                List<Good> missionGoodsInArea = portLists[currentPortIndex];

                Good component = other.GetComponent<Good>();
                if ((bool)component && missionGoodsInArea.Contains(component))
                {
                    missionGoodsInArea.Remove(component);
                }
                //currentPortIndex = -1;
                //Utilities.Log(Utilities.LogType.Log, "list contains " + missionGoodsInArea.Count.ToString() + " items");

            }
        }

        [HarmonyPatch(typeof(IslandMarketWarehouseArea), "Awake")]
        private static class AwakePatch
        {
            public static void postfix(IslandMarketWarehouseArea __instance)
            {
                //missionGoodsInArea = new List<Good>();
            }
        }

        [HarmonyPatch(typeof(MissionDetailsUI))]
        private static class MissionDetailsUiPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("ClickButton")]
            public static bool ClickButton(MissionDetailsUI __instance, bool ___clickable, bool ___mapZoomedIn, ref Mission ___currentMission, ref GameObject ___UI)
            {
                int missionPortIndex = ___currentMission.destinationPort.portIndex;
                List<Good> missionGoodsInArea = portLists[missionPortIndex];

                if (___clickable && !___mapZoomedIn)
                {                
                    if (___currentMission.missionIndex == -1)
                    {
                        PlayerMissions.AcceptMission(___currentMission);
                        MissionListUI.instance.DisplayMissions(___currentMission.originPort.GetMissions(0, MissionListUI.instance.worldMissions));
                        ___UI.SetActive(value: false);
                        __instance.InvokePrivateMethod("UpdateTexts");
                        return false;
                    }
                    if (___currentMission.missionIndex != -1 && missionGoodsInArea.Count > 0)
                    {
                        if (currentPortIndex == missionPortIndex)
                        {
                            foreach (var component in missionGoodsInArea)
                            {
                                if (___currentMission == component.GetAssignedMission())
                                {
                                    component.Deliver();
                                    missionGoodsInArea.Remove(component);

                                }
                            }
                            ///___currentMission.EndMission();
                            MissionListUI.instance.RefreshList();
                            __instance.InvokePrivateMethod("UpdateTexts");
                        }
                        //NotificationUi.instance.ShowNotification("You are at the wrong port!");
                        MissionListUI.instance.RefreshList();
                        __instance.InvokePrivateMethod("UpdateTexts");
                        ___UI.SetActive(value: false);

                        if (PlayerMissions.missions[___currentMission.missionIndex] is null)
                        {
                            MissionListUI.instance.RefreshList();
                            ___UI.SetActive(value: false);
                        }
                        return false;
                    }
                    else
                    {
                        PlayerMissions.AbandonMission(___currentMission.missionIndex);
                        MissionListUI.instance.RefreshList();
                        ___UI.SetActive(value: false);
                    }                
                }
                return false;
            }

            [HarmonyPostfix]
            [HarmonyPatch("UpdateTexts")]
            private static void UpdateTextsPatch(ref bool ___clickable, ref TextMesh ___buttonText, Mission ___currentMission)
            {
                int missionPortIndex = ___currentMission.destinationPort.portIndex;
                List<Good> missionGoodsInArea = portLists[currentPortIndex];


                if (___currentMission.missionIndex != -1 && missionGoodsInArea.Count > 0)
                {
                    if (currentPortIndex == missionPortIndex)
                    {
                        int currentMissionGoodsInArea = 0;
                        foreach (var component in missionGoodsInArea)
                        {
                            if (___currentMission == component.GetAssignedMission())
                            {
                                currentMissionGoodsInArea++;
                            }
                        }                        
                        ___buttonText.text = "Deliver" + "\n( " + ___currentMission.GetDeliveredCount() + " / " + ___currentMission.goodCount + " )";

                        ___clickable = true;
                        return;
                    }
                    ___buttonText.text = "Wrong port!";
                    ___clickable = false;
                }
            }
        }
    }


    // cheaty testing features
    [HarmonyPatch(typeof(GoPointer), "PickUpItem")]
    internal static class BedPickupPatch
    {
        public static void Postfix(PickupableItem item)
        {
            item.big = false;
            Utilities.Log(Utilities.LogType.Log, "set 'big' to false on: " + item.name);
        }
    }
}
