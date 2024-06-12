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
using SlopChat.Packets;
using System.Numerics;

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

        public enum NetworkStates
        {
            None,
            Server,
            Client,
            LookingForHost
        }
        public string CurrentInput = "";

        public ChatStates CurrentChatState = ChatStates.None;
        public NetworkStates CurrentNetworkState = NetworkStates.None;

        public Dictionary<uint, ChatPlayer> ChatPlayersById = new();

        private ChatAssets _assets;
        private ChatConfig _config;

        private GameObject _chatUI;
        private TextMeshProUGUI _inputLabel;
        private TextMeshProUGUI _playersLabel;
        private ChatHistory _history;

        private float _caretTimer = 0f;
        private float _caretTime = 0.5f;

        private float _heartBeatTimer = 0f;
        private float _heartBeatTime = 0.2f;
        private float _heartBeatTimeout = 5f;

        private ISlopCrewAPI _slopAPI;
        private uint _hostId = uint.MaxValue;
        private float _hostTimer = 0f;
        private float _hostTimeout = 5f;

        private float _chatHistoryTimer = 0f;
        private float _chatHistoryTime = 1f;

        private void Awake()
        {
            _slopAPI = APIManager.API;
            _slopAPI.OnCustomPacketReceived += OnPacketReceived;
            Core.OnUpdate += CoreUpdate;

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
            _playersLabel.text = "";
            EnterChatState(ChatStates.Default);
            CurrentNetworkState = NetworkStates.LookingForHost;
        }

        private void OnPacketReceived(uint playerId, string guid, byte[] data)
        {
            var packet = PacketFactory.DeserializePacket(guid, data, playerId);
            if (packet == null) return;
            switch (packet.GUID)
            {
                case HeartbeatPacket.kGUID:
                    if (_slopAPI.PlayerIDExists(playerId) == false)
                        break;
                    var heartBeat = packet as HeartbeatPacket;
                    if (!ChatPlayersById.TryGetValue(playerId, out var player))
                    {
                        player = new ChatPlayer();
                        ChatPlayersById[playerId] = player;
                    }
                    player.NetworkState = heartBeat.NetworkState;
                    player.LastHeartBeat = DateTime.Now;
                    player.Name = _slopAPI.GetPlayerName(playerId);
                    player.Id = playerId;
                    player.HostId = heartBeat.HostId;
                    break;

                case ChatHistoryPacket.kGUID:
                    if (_slopAPI.PlayerIDExists(playerId) == false)
                        break;
                    if (CurrentNetworkState != NetworkStates.Client)
                        break;
                    if (_hostId != playerId)
                        break;
                    if (!ChatPlayersById.TryGetValue(playerId, out var chPlayer))
                        break;
                    if (chPlayer.NetworkState != NetworkStates.Server)
                        break;
                    var chatHistory = packet as ChatHistoryPacket;
                    _history.Entries = chatHistory.Entries;
                    _history.UpdateLabel();
                    break;

                case MessagePacket.kGUID:
                    if (_slopAPI.PlayerIDExists(playerId) == false)
                        break;
                    if (CurrentNetworkState != NetworkStates.Server)
                        break;
                    if (!ChatPlayersById.TryGetValue(playerId, out var chatPlayer))
                        break;
                    if (_slopAPI.PlayerIDExists(chatPlayer.HostId) == true)
                        break;
                    if (chatPlayer.NetworkState != NetworkStates.Client)
                        break;
                    var messagePacket = packet as MessagePacket;
                    if (!SlopChatPlugin.Instance.ValidMessage(messagePacket.Message))
                        break;
                    _history.Append(new ChatHistory.Entry()
                    {
                        PlayerName = _slopAPI.GetPlayerName(playerId),
                        PlayerId = playerId,
                        Message = SlopChatPlugin.Instance.SanitizeMessage(messagePacket.Message)
                    });
                    SendChatHistory();
                    break;
            }
        }

        private void UpdatePlayers()
        {
            var newDict = new Dictionary<uint, ChatPlayer>();
            foreach(var player in ChatPlayersById)
            {
                var lastHeartBeatTime = (DateTime.Now - player.Value.LastHeartBeat).TotalSeconds;
                if (lastHeartBeatTime <= _heartBeatTimeout && _slopAPI.PlayerIDExists(player.Key) == true)
                {
                    newDict[player.Key] = player.Value;
                }
            }
            ChatPlayersById = newDict;

            var playersText = "Players in Text Chat:\n";
            if (CurrentNetworkState == NetworkStates.Server)
                playersText += "<color=yellow>[HOST]";
            playersText += $"<color=white>{_slopAPI.PlayerName}\n";
            foreach (var player in ChatPlayersById)
            {
                if (CurrentNetworkState == NetworkStates.Client && _hostId == player.Key)
                    playersText += "<color=yellow>[HOST]";
                playersText += $"<color=white>{player.Value.Name}\n";
            }
            _playersLabel.text = playersText;
        }

        private void SendChatHistory()
        {
            var chatHistory = new ChatHistoryPacket();
            chatHistory.Entries = _history.Entries;
            PacketFactory.SendPacket(chatHistory, _slopAPI);
        }

        private void Heartbeat()
        {
            UpdatePlayers();
            var packet = new HeartbeatPacket()
            {
                NetworkState = CurrentNetworkState,
                HostId = _hostId
            };
            PacketFactory.SendPacket(packet, _slopAPI);
        }

        private void NetworkUpdate()
        {
            if (!_slopAPI.Connected)
            {
                CurrentNetworkState = NetworkStates.LookingForHost;
                _hostId = uint.MaxValue;
                _hostTimer = 0f;
            }
            _heartBeatTimer += Core.dt;
            if (_heartBeatTimer >= _heartBeatTime)
            {
                _heartBeatTimer = 0f;
                Heartbeat();
            }

            switch (CurrentNetworkState)
            {
                case NetworkStates.LookingForHost:
                    NetworkLookingForHostUpdate();
                    break;

                case NetworkStates.Server:
                    NetworkServerUpdate();
                    break;

                case NetworkStates.Client:
                    NetworkClientUpdate();
                    break;
            }
        }

        private void NetworkClientUpdate()
        {
            if (_slopAPI.PlayerIDExists(_hostId) == false)
            {
                LookForHost();
                return;
            }
            if (!ChatPlayersById.TryGetValue(_hostId, out var player))
            {
                LookForHost();
                return;
            }
            if (player.NetworkState != NetworkStates.Server)
            {
                LookForHost();
                return;
            }
        }

        private void NetworkServerUpdate()
        {
            _chatHistoryTimer += Core.dt;
            if (_chatHistoryTimer > _chatHistoryTime)
            {
                _chatHistoryTimer = 0f;
                SendChatHistory();
                NetworkServerCheckForOtherServers();
            }
        }

        private void NetworkServerCheckForOtherServers()
        {
            var myId = uint.MaxValue;
            var lowestHostId = uint.MaxValue;
            foreach(var player in ChatPlayersById)
            {
                if (_slopAPI.PlayerIDExists(player.Key) == false)
                    continue;
                if (player.Value.NetworkState == NetworkStates.Client)
                {
                    if (_slopAPI.PlayerIDExists(player.Value.HostId) == false && player.Value.HostId < myId && player.Value.HostId != uint.MaxValue)
                        myId = player.Value.HostId;
                }
                else if (player.Value.NetworkState == NetworkStates.Server)
                {
                    if (player.Key != uint.MaxValue && player.Key < lowestHostId)
                        lowestHostId = player.Key;
                }
            }

            if (myId != uint.MaxValue && lowestHostId != uint.MaxValue)
            {
                if (lowestHostId < myId)
                {
                    ConnectToHost(lowestHostId);
                }
            }
        }

        private void NetworkLookingForHostUpdate()
        {
            _hostId = uint.MaxValue;
            _hostTimer += Core.dt;

            ChatPlayer lowestIdHost = null;
            foreach(var player in ChatPlayersById)
            {
                if (_slopAPI.PlayerIDExists(player.Key) == false)
                    continue;
                if (player.Value.NetworkState != NetworkStates.Server)
                    continue;
                if (lowestIdHost == null)
                    lowestIdHost = player.Value;
                else
                {
                    if (player.Key < lowestIdHost.Id)
                        lowestIdHost = player.Value;
                }
            }

            if (lowestIdHost != null)
            {
                ConnectToHost(lowestIdHost.Id);
            }

            if (_hostTimer > _hostTimeout)
            {
                HostChat();
            }
        }

        private void LookForHost()
        {
            _hostTimer = 0f;
            CurrentNetworkState = NetworkStates.LookingForHost;
        }

        private void ConnectToHost(uint playerId)
        {
            _hostId = playerId;
            CurrentNetworkState = NetworkStates.Client;
        }

        private void HostChat()
        {
            _chatHistoryTimer = 0f;
            _hostId = uint.MaxValue;
            CurrentNetworkState = NetworkStates.Server;
        }

        private bool CanEnterTypingState()
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

        public void EnterChatState(ChatStates newState)
        {
            if (CurrentChatState == newState) return;

            _caretTimer = 0f;
            CurrentInput = "";
            _inputLabel.text = "";
            _playersLabel.transform.gameObject.SetActive(false);

            if (CurrentChatState == ChatStates.Typing)
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

            CurrentChatState = newState;
        }

        private void SendChatMessage(string text)
        {
            var plugin = SlopChatPlugin.Instance;
            text = plugin.SanitizeMessage(text);
            if (!plugin.ValidMessage(text)) return;
            if (CurrentNetworkState == NetworkStates.Server)
            {
                var entry = new ChatHistory.Entry() { PlayerName = _slopAPI.PlayerName, PlayerId = uint.MaxValue, Message = text };
                _history.Append(entry);
                SendChatHistory();
            }
            else if (CurrentNetworkState == NetworkStates.Client)
            {
                var packet = new MessagePacket()
                {
                    Message = text
                };
                PacketFactory.SendPacket(packet, _slopAPI);
            }
        }

        private void DefaultUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Tab) && CanEnterTypingState())
                EnterChatState(ChatStates.Typing);
        }

        private void TypingUpdate()
        {
            var gameInput = Core.Instance.GameInput;
            var enabledMaps = gameInput.GetAllCurrentEnabledControllerMapCategoryIDs(0);
            if (enabledMaps.controllerMapCategoryIDs.Length > 0 || !_chatUI.activeInHierarchy)
            {
                EnterChatState(ChatStates.Default);
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
                    EnterChatState(ChatStates.Default);
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
                            if (CurrentNetworkState != NetworkStates.Server && CurrentNetworkState != NetworkStates.Client)
                                return;
                            SendChatMessage(CurrentInput);
                            EnterChatState(ChatStates.Default);
                            return;
                        }
                        else
                        {
                            CurrentInput += c;
                        }
                    }
                }
                CurrentInput = SlopChatPlugin.Instance.SanitizeInput(CurrentInput);
                _inputLabel.text = $"<color=#87e5e5>Say</color> : {CurrentInput}";
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

        private void ChatUpdate()
        {
            switch (CurrentChatState)
            {
                case ChatStates.Default:
                    DefaultUpdate();
                    break;
                case ChatStates.Typing:
                    TypingUpdate();
                    break;
            }
        }

        private void CoreUpdate()
        {
            ChatUpdate();   
            NetworkUpdate();
        }

        private void OnDestroy()
        {
            _slopAPI.OnCustomPacketReceived -= OnPacketReceived;
            Core.OnUpdate -= CoreUpdate;
            EnterChatState(ChatStates.Default);
        }
    }
}
