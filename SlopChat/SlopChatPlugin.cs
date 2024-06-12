using BepInEx;
using HarmonyLib;
using Reptile;
using SlopChat.Patches;
using System.IO;
using UnityEngine;

namespace SlopChat
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class SlopChatPlugin : BaseUnityPlugin
    {
        public static SlopChatPlugin Instance { get; private set; }
        public ChatAssets Assets { get; private set; }
        public ChatConfig ChatConfig { get; private set; }

        private void Awake()
        {
            Instance = this;
            Assets = new ChatAssets(Path.GetDirectoryName(Info.Location));
            ChatConfig = new ChatConfig();
            Patch();
            StageManager.OnStagePostInitialization += StageManager_OnStagePostInitialization;
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void Patch()
        {
            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
            InputPatch.Patch(harmony);
        }

        private static void StageManager_OnStagePostInitialization()
        {
            var chatGameObject = new GameObject("Chat Controller");
            chatGameObject.AddComponent<ChatController>();
        }

        public string SanitizeInput(string text)
        {
            text = text.Replace("\n", "");
            if (text.Length > ChatConfig.MaxCharacters)
            {
                text = text.Substring(0, ChatConfig.MaxCharacters);
            }
            return text;
        }

        public string SanitizeMessage(string text)
        {
            text = SanitizeInput(text);
            return text.Trim();
        }

        public bool ValidMessage(string text)
        {
            if (text.IsNullOrWhiteSpace()) return false;
            return true;
        }
    }
}
