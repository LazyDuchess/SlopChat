using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Reptile;

namespace SlopChat.Patches
{
    [HarmonyPatch(typeof(Player))]
    internal static class PlayerPatch
    {
        [HarmonyPatch(nameof(Player.LateUpdateAnimation))]
        [HarmonyPostfix]
        private static void LateUpdateAnimation_Postfix(Player __instance)
        {
            if (!SlopChatPlugin.Instance.ChatConfig.PhoneOutWhileTyping) return;
            if (!__instance.gameObject.activeSelf) return;
            if (__instance.isAI) return;
            if (__instance.phone.IsOn) return;
            var chatController = ChatController.Instance;
            if (chatController == null) return;
            if (chatController.CurrentState != ChatController.ChatStates.Typing) return;
            if (__instance.spraycanState != Player.SpraycanState.NONE) return;

            __instance.anim.speed = 0f;
            __instance.anim.SetFloat(__instance.phoneDirectionXHash, -__instance.phoneDirBone.localRotation.x * 2.5f + __instance.customPhoneHandValue.x);
            __instance.anim.SetFloat(__instance.phoneDirectionYHash, -__instance.phoneDirBone.localRotation.y * 2.5f + __instance.customPhoneHandValue.y);
            __instance.anim.Update(Core.dt);
            __instance.anim.speed = 1f;
        }

        [HarmonyPatch(nameof(Player.UpdateLookAt))]
        [HarmonyPostfix]
        private static void UpdateLookAt_Postfix(Player __instance)
        {
            if (!SlopChatPlugin.Instance.ChatConfig.PhoneOutWhileTyping) return;

            if (__instance.isAI) return;

            var chatController = ChatController.Instance;

            if (chatController == null) return;
            if (chatController.CurrentState != ChatController.ChatStates.Typing) return;

            __instance.characterVisual.phoneActive = __instance.phoneLayerWeight >= 1f;
        }

        [HarmonyPatch(nameof(Player.UpdateHoldProps))]
        [HarmonyPrefix]
        private static bool UpdateHoldProps_Prefix(Player __instance)
        {
            if (!SlopChatPlugin.Instance.ChatConfig.PhoneOutWhileTyping) return true;

            var chatController = ChatController.Instance;

            if (chatController == null) return true;

            if (__instance.isAI) return true;

            if (chatController.CurrentState != ChatController.ChatStates.Typing) return true;

            __instance.UpdateSprayCanShake();

            __instance.characterVisual.SetPhone(true);
            __instance.phoneLayerWeight += __instance.grabPhoneSpeed * Core.dt;
            if (__instance.phoneLayerWeight >= 1f)
            {
                __instance.phoneLayerWeight = 1f;
            }
            __instance.anim.SetLayerWeight(3, __instance.phoneLayerWeight);

            return false;
        }
    }
}
