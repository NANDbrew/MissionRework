using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;

namespace MissionRework
{
    public class ModSettings : UnityModManager.ModSettings, IDrawable
    {
        // place settings here

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }

        public void OnChange() { }
    }

    internal static class Main
    {
        public static ModSettings settings;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            settings = UnityModManager.ModSettings.Load<ModSettings>(modEntry);

            // uncomment if using settings
            //modEntry.OnGUI = OnGUI;
            //modEntry.OnSaveGUI = OnSaveGUI;

            return true;
        }

        static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Draw(modEntry);
        }

        static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }
    }
}