using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Reptile;
using TMPro;
using System.Linq;
using UnityEngine.Video;
using SlopCrew.API;

namespace SlopChat
{
    public class ChatController : MonoBehaviour
    {
        public static ChatController Instance { get; private set; }
        public enum ChatStates
        {
            None,
            Default,
            Typing
        }
        public string CurrentInput = "";
        public ChatStates CurrentState = ChatStates.None;

        private ChatAssets _assets;
        private ChatConfig _config;

        private GameObject _chatUI;
        private TextMeshProUGUI _inputLabel;
        private TextMeshProUGUI _playersLabel;
        private ChatHistory _history;

        private float _caretTimer = 0f;
        private float _caretTime = 0.5f;

        private ISlopCrewAPI _slopAPI;

        private void Awake()
        {
            _slopAPI = APIManager.API;

            _assets = SlopChatPlugin.Instance.Assets;
            _config = SlopChatPlugin.Instance.ChatConfig;

            Instance = this;
            var prefab = _assets.Bundle.LoadAsset<GameObject>("Chat UI");
            _chatUI = GameObject.Instantiate(prefab);
            var chatCanvas = _chatUI.transform.Find("Canvas");
            _inputLabel = chatCanvas.transform.Find("Chat Input").GetComponent<TextMeshProUGUI>();
            _playersLabel = chatCanvas.transform.Find("Chat Players").GetComponent<TextMeshProUGUI>();
            _history = chatCanvas.transform.Find("Chat History").gameObject.AddComponent<ChatHistory>();
            _chatUI.transform.SetParent(Core.Instance.UIManager.gameplay.transform);
            EnterState(ChatStates.Default);
        }

        private bool CanEnterChatState()
        {
            if (!_chatUI.activeInHierarchy) return false;
            var gameInput = Core.Instance.gameInput;
            var enabledMaps = gameInput.GetAllCurrentEnabledControllerMapCategoryIDs(0);
            return enabledMaps.controllerMapCategoryIDs.Contains(0) && enabledMaps.controllerMapCategoryIDs.Contains(6) && enabledMaps.controllerMapCategoryIDs.Length == 2;
        }

        private void EnableInputs()
        {
            var gameInput = Core.Instance.gameInput;
            gameInput.DisableAllControllerMaps(0);
            gameInput.EnableControllerMaps(BaseModule.IN_GAME_INPUT_MAPS, 0);
        }

        private void DisableInputs()
        {
            var gameInput = Core.Instance.gameInput;
            gameInput.DisableAllControllerMaps(0);
        }

        public void EnterState(ChatStates newState)
        {
            if (CurrentState == newState) return;

            _caretTimer = 0f;
            CurrentInput = "";
            _inputLabel.text = "";
            _playersLabel.transform.gameObject.SetActive(false);

            if (CurrentState == ChatStates.Typing)
            {
                InputUtils.PopInputBlocker();
                EnableInputs();
            }

            switch (newState)
            {
                case ChatStates.Default:
                    break;
                case ChatStates.Typing:
                    _playersLabel.transform.gameObject.SetActive(true);
                    InputUtils.PushInputBlocker();
                    DisableInputs();
                    break;
            }

            CurrentState = newState;
        }

        private void DefaultUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Tab) && CanEnterChatState())
                EnterState(ChatStates.Typing);
        }

        private void SendChatMessage(string playerName, string text)
        {
            var plugin = SlopChatPlugin.Instance;
            text = plugin.SanitizeMessage(text);
            if (!plugin.ValidMessage(text)) return;
            var entry = new ChatHistory.Entry() {PlayerName = playerName, Message = text };
            _history.Append(entry);
        }

        private void TypingUpdate()
        {
            var gameInput = Core.Instance.GameInput;
            var enabledMaps = gameInput.GetAllCurrentEnabledControllerMapCategoryIDs(0);
            if (enabledMaps.controllerMapCategoryIDs.Length > 0 || !_chatUI.activeInHierarchy)
            {
                EnterState(ChatStates.Default);
                gameInput.DisableAllControllerMaps(0);
                gameInput.EnableControllerMaps(enabledMaps.controllerMapCategoryIDs);
                return;
            }
            _caretTimer += Core.dt;
            InputUtils.PopInputBlocker();
            try
            {
                var inputThisFrame = Input.inputString;
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
                {
                    EnterState(ChatStates.Default);
                    return;
                }
                if (Input.GetKey(KeyCode.LeftControl))
                {
                    if (Input.GetKeyDown(KeyCode.V))
                        CurrentInput += GUIUtility.systemCopyBuffer;
                }
                else
                {
                    foreach (var c in inputThisFrame)
                    {
                        if (c == '\b')
                        {
                            if (CurrentInput.Length > 0)
                                CurrentInput = CurrentInput.Substring(0, CurrentInput.Length - 1);
                        }
                        else if ((c == '\n') || (c == '\r')) // enter/return
                        {
                            SendChatMessage(_slopAPI.PlayerName, CurrentInput);
                            EnterState(ChatStates.Default);
                            return;
                        }
                        else
                        {
                            CurrentInput += c;
                        }
                    }
                }
                CurrentInput = SlopChatPlugin.Instance.SanitizeInput(CurrentInput);
                _inputLabel.text = $"<color=blue>Say</color> : {CurrentInput}";
                if (_caretTimer >= _caretTime)
                {
                    _inputLabel.text += "<color=white>|</color>";
                    if (_caretTimer >= _caretTime * 2f)
                        _caretTimer = 0f;
                }
            }
            finally
            {
                InputUtils.PushInputBlocker();
            }
        }

        private void Update()
        {
            switch (CurrentState)
            {
                case ChatStates.Default:
                    DefaultUpdate();
                    break;
                case ChatStates.Typing:
                    TypingUpdate();
                    break;
            }
        }

        private void OnDestroy()
        {
            EnterState(ChatStates.Default);
        }
    }
}
