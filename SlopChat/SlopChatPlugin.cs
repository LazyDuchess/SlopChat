using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using Reptile;
using SlopChat.Patches;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SlopChat
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("SlopCrew.Plugin", BepInDependency.DependencyFlags.HardDependency)]
    public class SlopChatPlugin : BaseUnityPlugin
    {
        public static SlopChatPlugin Instance { get; private set; }
        public ChatAssets Assets { get; private set; }
        public ChatConfig ChatConfig { get; private set; }

        private void Awake()
        {
            var slopConfig = Chainloader.PluginInfos["SlopCrew.Plugin"].Instance.Config;
            var hostConfig = slopConfig.First(x => x.Key.Section == "Server" && x.Key.Key == "Host");
            if (hostConfig.Value.BoxedValue == hostConfig.Value.DefaultValue)
            {
                Logger.LogError("SlopChat is not allowed on the official SlopCrew server.");
                return;
            }
            try
            {
                Instance = this;
                Assets = new ChatAssets(Path.GetDirectoryName(Info.Location));
                ChatConfig = new ChatConfig();
                Patch();
                StageManager.OnStagePostInitialization += StageManager_OnStagePostInitialization;
                ChatCommands.Initialize();
                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} {PluginInfo.PLUGIN_VERSION} is loaded!");
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to load {PluginInfo.PLUGIN_NAME} {PluginInfo.PLUGIN_VERSION}!{Environment.NewLine}{e}");
            }
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
            text = text.Replace("\t", "");
            text = text.Replace("\r", "");
            if (text.Length > ChatConfig.MaxCharacters)
            {
                text = text.Substring(0, ChatConfig.MaxCharacters);
            }
            return text;
        }

        public string SanitizeName(string text)
        {
            text = SanitizeInput(text);
            text = text.Trim();
            text = TMPFilter.CloseAllTags(TMPFilter.FilterTags(text, ChatConfig.ChatCriteria));
            return text;
        }

        public string SanitizeMessage(string text, string censor)
        {
            text = SanitizeInput(text);
            text = text.Trim();
            text = TMPFilter.CloseAllTags(TMPFilter.FilterTags(text, ChatConfig.ChatCriteria));
            if (ChatConfig.FilterProfanity)
            {
                if (ProfanityFilter.TMPContainsProfanity(text))
                    return censor;
            }
            return text;
        }

        public bool ValidMessage(string text)
        {
            text = SanitizeMessage(text, ProfanityFilter.CensoredMessage);
            if (text.IsNullOrWhiteSpace()) return false;
            return true;
        }
    }
}
