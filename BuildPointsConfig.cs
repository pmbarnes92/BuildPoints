using System;
using System.IO;
using UnityEngine;

namespace BuildPoints
{
    /// <summary>
    /// Loads the mod-wide *default* settings from
    /// GameData/BuildPoints/PluginData/settings.cfg. This file is meant to
    /// be hand-edited — by you, or by anyone installing the mod — and
    /// nothing in-game ever writes to it except to create it with built-in
    /// defaults on first run.
    ///
    /// It only supplies the starting values a save is seeded with when
    /// first created; from then on each save keeps its own independent
    /// copy (BuildPointsScenario.Settings), so editing this file later
    /// doesn't retroactively change a save already in progress unless the
    /// player explicitly hits "Reset to Global Defaults" in the in-game
    /// Settings window.
    /// </summary>
    public static class BuildPointsConfig
    {
        public static BuildPointsSettingsValues Defaults { get; private set; }

        private static string ConfigPath =>
            Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "BuildPoints", "PluginData", "settings.cfg");

        /// <summary>Loads once; safe to call from anywhere that touches the defaults.</summary>
        public static void EnsureLoaded()
        {
            if (Defaults != null) return;
            Load();
        }

        /// <summary>
        /// (Re-)reads settings.cfg from disk into Defaults, discarding any
        /// previous in-memory copy. Called at bootstrap, and again by
        /// "Reset to Global Defaults" so that action picks up any hand
        /// edits made since the game started.
        /// </summary>
        public static void Load()
        {
            Defaults ??= new BuildPointsSettingsValues();

            try
            {
                if (!File.Exists(ConfigPath))
                {
                    // First run: write the built-in defaults out so the file
                    // exists and is there to hand-edit (or share, or diff)
                    // afterward.
                    Save();
                    return;
                }

                ConfigNode file = ConfigNode.Load(ConfigPath);
                ConfigNode node = file?.GetNode("BuildPointsSettings") ?? file;
                if (node != null) Defaults.Load(node);
            }
            catch (Exception e)
            {
                Debug.LogError("[BuildPoints] Failed to load settings.cfg, using built-in defaults: " + e);
            }
        }

        /// <summary>Writes Defaults out to settings.cfg. Only happens on first run, to create the file.</summary>
        private static void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(ConfigPath);
                if (dir != null) Directory.CreateDirectory(dir);

                ConfigNode root = new ConfigNode();
                ConfigNode node = root.AddNode("BuildPointsSettings");
                Defaults.Save(node);
                root.Save(ConfigPath);
            }
            catch (Exception e)
            {
                Debug.LogError("[BuildPoints] Failed to save settings.cfg: " + e);
            }
        }
    }
}
