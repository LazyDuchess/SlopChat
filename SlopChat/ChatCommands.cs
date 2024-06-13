using Reptile;
using SlopCrew.API;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlopChat
{
    public static class ChatCommands
    {
        public static void Initialize()
        {
            ChatController.OnSendMessage += OnSendMessage;
        }

        public static void OnSendMessage(SendMessageEventArgs e)
        {
            var text = e.Message.Trim().ToLowerInvariant();

            if (text[0] == '/')
                e.Cancel = true;
            else
                return;

            text = text.Substring(1);
            var args = text.Split(' ');

            var api = APIManager.API;
            var uiManager = Core.Instance.UIManager;

            switch (args[0])
            {
                case "hide":
                    ChatController.HideWhileNotTyping = true;
                    break;

                case "show":
                    ChatController.HideWhileNotTyping = false;
                    break;

                case "mute":
                    if (args.Length < 2)
                        uiManager.ShowNotification("Please provide a player ID to mute");
                    if (uint.TryParse(args[1], out var result))
                    {
                        if (api.PlayerIDExists(result) == true)
                        {
                            var playerName = TMPFilter.RemoveAllTags(api.GetPlayerName(result));
                            ChatController.MutedPlayers.Add(playerName);
                            uiManager.ShowNotification($"Muted player {playerName}");
                        }
                        else
                        {
                            uiManager.ShowNotification("A player with that ID doesn't exist");
                        }
                    }
                    else
                        uiManager.ShowNotification("Invalid ID");
                    break;

                case "unmute":
                    if (args.Length < 2)
                        uiManager.ShowNotification("Please provide a player ID to unmute");
                    if (uint.TryParse(args[1], out var unmuteResult))
                    {
                        if (api.PlayerIDExists(unmuteResult) == true)
                        {
                            var playerName = TMPFilter.RemoveAllTags(api.GetPlayerName(unmuteResult));
                            ChatController.MutedPlayers.Remove(playerName);
                            uiManager.ShowNotification($"Unmuted player {playerName}");
                        }
                        else
                        {
                            uiManager.ShowNotification("A player with that ID doesn't exist");
                        }
                    }
                    else
                        uiManager.ShowNotification("Invalid ID");
                    break;

                case "unmuteall":
                    ChatController.MutedPlayers.Clear();
                    uiManager.ShowNotification("Unmuted all players");
                    break;
            }
        }
    }
}
