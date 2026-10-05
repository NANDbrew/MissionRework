using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
//using SailwindModdingHelper;
using System.ComponentModel;
using System.Net.Mail;
//using UnityEngine.TextRenderingModule;

namespace MissionRework
{
    internal class MissionPatches
    {
        private static GameObject sortButton;
        private static GameObject backButton;
        private static GameObject acceptCancelButton;

        public static int currentPortIndex;
        public static Dictionary<int, List<Good>> portLists = new Dictionary<int, List<Good>>();
        private static IslandMarket currentMarket;

        private static int CountMissionGoods(List<Good> goods, Mission mission)
        {
            int count = 0;
            foreach (Good good in goods)
            {
                if (good.GetAssignedMission() == mission)
                {
                    count++;
                }
            }
            return count;
        }

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
                acceptCancelButton.SetActive(false);
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
                currentMarket = ___market;

                if (!portLists.ContainsKey(currentPortIndex))
                {
                    portLists.Add(currentPortIndex, new List<Good>());
                }

                Good component = other.GetComponent<Good>();

                List<Good> missionGoodsInArea = portLists[currentPortIndex];

                if ((bool)component && !missionGoodsInArea.Contains(component) && component.GetMissionIndex() != -1)
                {
                    missionGoodsInArea.Add(component);
                }

            }

            [HarmonyPostfix]
            [HarmonyPatch("OnTriggerExit")]
            public static void OnTriggerExitPatch(IslandMarketWarehouseArea __instance, Collider other, IslandMarket ___market)
            {
                currentPortIndex = ___market.GetPortIndex();
                currentMarket = ___market;

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
                foreach (Transform item in ___UI.transform)
                {
                    foreach (Transform item2 in item)
                    {
                        if (item2.name == "accept/cancel button")
                        {
                            acceptCancelButton = item2.gameObject;
                        }

                    }
                }
            }


            [HarmonyPrefix]
            [HarmonyPatch("ClickButton")]
            public static bool ClickButton(MissionDetailsUI __instance, ref bool ___clickable, bool ___mapZoomedIn, ref Mission ___currentMission, ref GameObject ___UI, ref TextMesh ___buttonText)
            {
                int missionPortIndex = ___currentMission.destinationPort.portIndex;
                if (GPMissionTypeButton.missionType == MissionType.Available) return true;
                
                List<Good> missionGoodsInArea = portLists[missionPortIndex];

                if (!___mapZoomedIn && ___currentMission.missionIndex != -1)
                {
                    if (___clickable)
                    {
                        if (currentPortIndex == missionPortIndex && CountMissionGoods(missionGoodsInArea, ___currentMission) > 0)
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
                            int num = ___currentMission.goodCount - ___currentMission.GetDeliveredCount();
                            if (num > 0)
                            {
                                int num2 = num * 100;
                                ___currentMission.AbandonMission();
                                NotificationUi.instance.ShowNotification("Ended mission:\n" + ___currentMission.missionName + "\nReputation penalty: " + num2);
                                UISoundPlayer.instance.PlayWritingSound();
                                PlayerMissions.missions[___currentMission.missionIndex] = null;

                                MissionLog.instance.loggedMissions[0].totalRep -= num2;
                                MissionLog.instance.UpdateTexts();

                                //List<string> texts = new List<string>(MissionLog.instance.texts[0].text.Split(')'));
                                //texts.Insert(1, "/" + ___currentMission.goodCount + ")");
                                //MissionLog.instance.texts[0].text = string.Join("", texts);
                            }

                            MissionListUI.instance.RefreshList();
                            //___currentMission = null;
                            __instance.InvokePrivateMethod("UpdateTexts");
                            MissionListUI.instance.GetPrivateField<GameObject>("book").SetActive(false);
                            MissionListUI.instance.GetPrivateField<PortDude>("currentPortDude").ActivateMissionListUI(false);

                            //List<Good> list = (List<Good>)currentMarket.GetPrivateField("warehouseArea").GetPrivateField("goodsInArea");
                            foreach (Good good in portLists[currentPortIndex]) 
                            {
                                //good.GetComponent<Rigidbody>().WakeUp();
                                good.GetComponent<ShipItem>().GetItemRigidbody().InvokePrivateMethod("SetDynamicColTimer");
                            }
                        }
                        else
                        {
                            PlayerMissions.AbandonMission(___currentMission.missionIndex);
                            MissionListUI.instance.RefreshList();
                            MissionListUI.instance.GetPrivateField<GameObject>("book").SetActive(false);
                            MissionListUI.instance.GetPrivateField<PortDude>("currentPortDude").ActivateMissionListUI(false);

                            Debug.Log("Button Clicked!");

                            //___UI.SetActive(value: false);
                        }
                    }
                    else
                    {
                        ___buttonText.text = "Abandon";
                        ___clickable = true;
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


                if (___currentMission.missionIndex != -1 && (MissionListUI.instance.GetPrivateField<PortDude>("currentPortDude") != null))
                {
                    ___amount.text = ___currentMission.GetDeliveredCount() + " / " + ___amount.text;
                    int currentMissionGoodsInArea = CountMissionGoods(missionGoodsInArea, ___currentMission);

                    if (currentPortIndex == missionPortIndex)
                    {                    
                        if (currentMissionGoodsInArea > 0)
                        {
                            if (currentMissionGoodsInArea < ___currentMission.goodCount) ___buttonText.text = "Deliver\n" + "partial (" + currentMissionGoodsInArea + ")";
                            if (currentMissionGoodsInArea >= ___currentMission.goodCount) ___buttonText.text = "Deliver all";
                            ___clickable = true;
                        }
                        else if (Main.safety.Value)
                        {
                            ___buttonText.text = "(no goods\nin area)";
                            ___clickable = false;
                        }//return;
                    }
                    else if (Main.safety.Value)
                    {
                        ___buttonText.text = "(wrong port)";
                        ___clickable = false;
                    }
                    //return;
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
                    acceptCancelButton.SetActive(true);

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

        [HarmonyPatch(typeof(PlayerMissions))]
        private static class PlayerMissionsPatches
        {
            [HarmonyPrefix]
            [HarmonyPatch("AbandonMission")]
            public static void AbandonMissionPatch(int missionIndex, out string __state)
            {
                Mission mission = PlayerMissions.missions[missionIndex];
                int repPenalty = -100 * (mission.goodCount - mission.GetDeliveredCount());
                __state = "Abandoning mission:\n" + mission.missionName + "\n" + repPenalty + " reputation";
            }

            [HarmonyPostfix]
            [HarmonyPatch("AbandonMission")]
            public static void AbandonMissionNotificationPatch(string __state)
            {
                NotificationUi.instance.ShowNotification(__state);
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
