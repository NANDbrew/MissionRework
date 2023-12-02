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
        private static GameObject sortButton;
        private static GameObject backButton;

        public static int currentPortIndex;
        public static Dictionary<int, List<Good>> portLists = new Dictionary<int, List<Good>>();

        [HarmonyPatch(typeof(MissionListUI))]
        private class MissionListUIPatches
        {
            [HarmonyPostfix]
            [HarmonyPatch("EnablePortMissionUI")]
            public static void EnablePortMissionUIPatch()
            {
                Refs.SetPlayerControl(false);
            }

            [HarmonyPostfix]
            [HarmonyPatch("DisablePortMissionUI")]
            public static void DisablePortMissionUIPatch()
            {
                Refs.SetPlayerControl(true);
            }

            [HarmonyPrefix]
            [HarmonyPatch("ToggleMenu")]
            public static bool ToggleMenuPatch(MissionListUI __instance, bool ___UIActive)
            {
                sortButton.SetActive(false);
                backButton.SetActive(false);

                return true;
            }
        }

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

                Good component = other.GetComponent<Good>();

                List<Good> missionGoodsInArea = portLists[currentPortIndex];

                if ((bool)component && !missionGoodsInArea.Contains(component) && !(component.GetMissionIndex() == -1))
                {
                    missionGoodsInArea.Add(component);
                }

            }

            [HarmonyPostfix]
            [HarmonyPatch("OnTriggerExit")]
            public static void OnTriggerExitPatch(IslandMarketWarehouseArea __instance, Collider other, IslandMarket ___market)
            {
                currentPortIndex = ___market.GetPortIndex();
                List<Good> missionGoodsInArea = portLists[currentPortIndex];

                Good component = other.GetComponent<Good>();
                if ((bool)component && missionGoodsInArea.Contains(component))
                {
                    missionGoodsInArea.Remove(component);
                }
            }
        }

        [HarmonyPatch(typeof(MissionDetailsUI))]
        private static class MissionDetailsUiPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("Start")]
            public static void StartPatch(MissionDetailsUI __instance, GameObject ___UI)
            {
                foreach (Transform item in ___UI.transform.parent)
                {
                    if (item.name == "current mission buttons")
                    {
                        foreach (Transform item2 in item)
                        {
                            if (item2.name == "page buttons")
                            {
                                GameObject backButtons = item2.Find("mission button (back)").gameObject;
                                GameObject gameObject = GameObject.Instantiate(backButtons);
                                gameObject.transform.parent = item;
                                backButtons.transform.parent = item;
                                gameObject.transform.localPosition = new Vector3(backButtons.transform.localPosition[0], 0.22f, 0.03f); // -0.654f, -0.336f, 0.022f
                                gameObject.transform.localRotation = backButtons.transform.localRotation;
                                gameObject.transform.localScale = backButtons.transform.localScale;
                                gameObject.name = "sort button";
                                GameObject buttonGameobject = gameObject.GetComponentInChildren<GoPointerButton>().gameObject;
                                GameObject.Destroy(buttonGameobject.GetComponent<GoPointerButton>());
                                GPMissionTypeButton button = buttonGameobject.AddComponent<GPMissionTypeButton>();
                                button.text = gameObject.GetComponentInChildren<TextMesh>();
                                button.UpdateText();
                                MissionListUI.instance.SetPrivateField("closeCooldown", 0f);
                                sortButton = gameObject;
                                backButton = backButtons;
                                gameObject.SetActive(false);
                            }
                        }
                    }
                }
            }


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
                            for (int i = 0; i < missionGoodsInArea.Count;)
                            {
                                if (___currentMission == missionGoodsInArea[i].GetAssignedMission())
                                {
                                    missionGoodsInArea[i].Deliver();
                                    missionGoodsInArea.Remove(missionGoodsInArea[i]);
                                }
                                else i++;
                            }
                            if (!(___currentMission.GetDeliveredCount() < ___currentMission.goodCount))
                            {
                                MissionListUI.instance.RefreshList();
                            }
                        }
                        __instance.InvokePrivateMethod("UpdateTexts");
                        MissionListUI.instance.GetPrivateField<GameObject>("book").SetActive(false);
                        MissionListUI.instance.GetPrivateField<PortDude>("currentPortDude").ActivateMissionListUI(false);
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
            private static void UpdateTextsPatch(ref bool ___clickable, ref TextMesh ___buttonText, Mission ___currentMission, ref TextMesh ___amount)
            {
                int missionPortIndex = ___currentMission.destinationPort.portIndex;
                List<Good> missionGoodsInArea = portLists[currentPortIndex];


                if (___currentMission.missionIndex != -1 && missionGoodsInArea.Count > 0 && (MissionListUI.instance.GetPrivateField<PortDude>("currentPortDude") != null))
                {
                    ___amount.text = ___currentMission.GetDeliveredCount() + " / " + ___amount.text;
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
                        if (currentMissionGoodsInArea > 0)
                        {
                            if (currentMissionGoodsInArea < ___currentMission.goodCount) ___buttonText.text = "Deliver\n" + "( " + currentMissionGoodsInArea + " )";
                            if (currentMissionGoodsInArea >= ___currentMission.goodCount) ___buttonText.text = "Deliver all";
                            ___clickable = true;
                            return;
                        }
                        ___buttonText.text = "(no goods\nin area)";
                        ___clickable = false;
                        return;
                    }
                    ___buttonText.text = "(wrong port)";
                    ___clickable = false;
                }
            }
        }
        [HarmonyPatch(typeof(PortDude))]
        private static class ActivateMissionListUIPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch("ActivateMissionListUI")]
            public static bool ActivateMissionListUI(PortDude __instance, bool openEconomyUI)
            {
                if (!openEconomyUI)
                {
                    GPMissionTypeButton.currentPort = __instance.GetPort();
                    GPMissionTypeButton.portDude = __instance;
                    sortButton.SetActive(true);
                    backButton.SetActive(true);
/*                    if (portLists[currentPortIndex].Count > 0)
                    {
                        GPMissionTypeButton.missionType = MissionType.Active;
                        //sortButton.GetComponent<GPMissionTypeButton>().OnActivate();
                        //sortButton.GetComponent<GPMissionTypeButton>().text.text = "Showing: Active";
                    }
                    else
                    {
                        GPMissionTypeButton.missionType = MissionType.Available;
                        //MissionListUI.instance.EnablePortMissionUI(___port.GetMissions(0, false), ___missionTable, __instance);
                    }*/
                    GPMissionTypeButton.UpdateMissions();
                    //sortButton.GetComponent<GPMissionTypeButton>().UpdateText();
                    return false;
                }
                return true;
            }

            [HarmonyPostfix]
            [HarmonyPatch("DeactivateMissionListUI")]
            public static void DeactivateMissionListUI(PortDude __instance)
            {
                GPMissionTypeButton.currentPort = null;
                GPMissionTypeButton.portDude = null;
                sortButton.SetActive(false);
                backButton.SetActive(false);
            }

            [HarmonyPrefix]
            [HarmonyPatch("OnTriggerEnter")]
            public static bool OnTriggerEnter()
            {
                return false;
            }
        }
    }


    // cheaty testing features
/*    [HarmonyPatch(typeof(GoPointer), "PickUpItem")]
    internal static class BedPickupPatch
    {
        public static void Postfix(PickupableItem item)
        {
            item.big = false;
        }
    }*/
}
