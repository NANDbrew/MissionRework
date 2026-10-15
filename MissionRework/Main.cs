using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;

namespace MissionRework
{
    [BepInPlugin(GUID, NAME, VERSION)]
    //[BepInDependency("com.app24.sailwindmoddinghelper", "2.0.0")]

    internal class Main : BaseUnityPlugin
    {
        public const string GUID = "com.nandbrew.missionrework";
        public const string NAME = "Mission Rework";
        public const string VERSION = "1.0.3";

        //internal static Main instance;
        //internal static ManualLogSource logSource;

        internal static ConfigEntry<bool> safety;

        private void Awake()
        {
            //logSource = Logger;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), GUID);

            safety = Config.Bind("Options", "Safety", false, new ConfigDescription("\"Abandon\" button will read \"(wrong port)\" or \"(no goods in area)\" and must be clicked twice"));
        }
    }
}
