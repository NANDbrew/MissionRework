using HarmonyLib;
using SailwindModdingHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MissionRework
{
    internal enum MissionType
    {
        Available,
        Active,
        Last
    }
    internal class GPMissionTypeButton : GoPointerButton
    {
        public TextMesh text;

        public static Port currentPort;

        public static PortDude portDude;

        public static MissionType missionType = MissionType.Available;

        static string[] sortingStrings = new[]
        {
            "Available",
            "Active"
        };

        public override void OnActivate()
        {
            //missionType++;
            if (missionType == MissionType.Available)
            {
                missionType = MissionType.Active;
            }
            else missionType = MissionType.Available;
            UpdateMissions();
            UpdateText();
        }

        public static void UpdateMissions()
        {
            if (currentPort != null)
            {
                switch (GPMissionTypeButton.missionType)
                {
                    case MissionType.Active:
                    {
                        MissionListUI.instance.EnablePortMissionUI(PlayerMissions.missions, portDude.GetPrivateField<Transform>("missionTable"), portDude);
                        //MissionListUI.instance.SetPrivateField("currentPageCount", 1);
                        MissionListUI.instance.GetPrivateField<GameObject>("pageButtons").SetActive(false);
                        //MissionListUI.instance.GetPrivateField<GameObject>("sort button").SetActive(true);
                        //MissionDetailsUI.instance.GetPrivateField<GameObject>("UI").SetActive(true);
                        MissionListUI.instance.SetPrivateField("closeCooldown", 0f);
                        return;
                    }
                    case MissionType.Available:
                    {
                        //MissionListUI.instance.DisablePortMissionUI();
                        MissionListUI.instance.EnablePortMissionUI(currentPort.GetPrivateField<Mission[]>("missions"), portDude.GetPrivateField<Transform>("missionTable"), portDude);
                        MissionListUI.instance.ChangePage(0);
                        MissionListUI.instance.SetPrivateField("closeCooldown", 0f);
                        MissionListUI.instance.InvokePrivateMethod("UpdatePageCountText");

                        return;
                    }
                }
            }
        }

        public void UpdateText()
        {
            string missionTypeText = missionType.ToString();
            if ((int)missionType < sortingStrings.Length)
            {
                missionTypeText = sortingStrings[(int)missionType];
            }
            text.text = $"Showing: {missionTypeText}";
        }
    }
}
