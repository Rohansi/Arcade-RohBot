using System;
using System.Collections.Generic;
using GameAPI;
using GameAPI.BudgetBoy;
using MiniJSON;

namespace Games.RohBot
{
    public class ChatStage : Stage
    {
        private Main _game;
        private WebSocket _socket;
        
        private VirtualKeyboard _keyboard;
        private string _keyboardStr;
        
        private Text _input;
        
        private List<Text> _history;
        
        public ChatStage(Main game, WebSocket socket)
            : base(game)
        {
            _game = game;
            _socket = socket;
            
            var font = Graphics.GetImage("Resources", "font");
            var color = _game.Swatches.White;
            
            _keyboard = Add(new VirtualKeyboard(font, color), 0);
            _keyboard.Position = new Vector2i((Graphics.Size.X / 2) - (_keyboard.Width / 2), 16);
            _keyboard.Pressed += KeyboardPressed;
            
            _keyboardStr = "";
            
            _input = Add(new Text(font, color), 0);
            _input.Position = (Vector2i)_keyboard.Position + new Vector2i(0, _keyboard.Height + 10);
            
            var start = _input.Position + new Vector2i(0, 20);
            
            _history = new List<Text>();
            
            for (var i = 0; i < 10; i++)
            {
                var line = Add(new Text(font, color), 0);
                line.Position = start + new Vector2i(0, i * 10);
                
                _history.Add(line);
            }
                    
            _socket.Send(Json.Serialize(new Dictionary<string, object>
            {
                { "Type", "sendMessage" },
                { "Target", "home" },
                { "Content", "/join general" }
            }));
            
            _socket.MessageReceived = message =>
            {
                // ignore long messages, we dont need em
                if (message.Length > 16384)
                    return;
                    
                var obj = (Dictionary<string, object>)Json.Deserialize(message);
                var type = (string)obj["Type"];

                if (type != "message")
                    return;
                    
                var line = (Dictionary<string, object>)obj["Line"];
                var chat = (string)line["Chat"];
                
                if (chat != "general")
                    return;
                    
                var lineType = (string)line["Type"];
                var content = WebUtility.HtmlDecode((string)line["Content"]);
                
                if (lineType == "chat")
                {
                    var sender = WebUtility.HtmlDecode((string)line["Sender"]);
                    
                    if (sender.Length > 6)
                        sender = sender.Substring(0, 6);
                    
                    content = string.Format("{0}: {1}", sender, content);
                }
                
                content = content.Replace("\r", "").Replace("\n", "");
                
                AddLine(content);
            };
        }
        
        private void AddLine(string value)
        {
            var lineCount = 0;
            while (value.Length > 0)
            {
                if (lineCount >= 3)
                    break;
                    
                var part = value.Substring(0, Math.Min(value.Length, 26));
                value = value.Substring(Math.Min(value.Length, 26));
                
                for (var i = _history.Count - 1; i > 0; i--)
                {
                    _history[i].Value = _history[i - 1].Value;
                }
                
                _history[0].Value = part;
                lineCount++;
            }
        }
        
        private void KeyboardPressed(char ch)
        {
            if (ch == '\b')
            {
                if (_keyboardStr.Length == 0)
                    return;
                    
                _keyboardStr = _keyboardStr.Substring(0, _keyboardStr.Length - 1);
                return;
            }
            
            if (ch == '\r')
            {
                if (string.IsNullOrEmpty(_keyboardStr))
                    return;
                    
                _socket.Send(Json.Serialize(new Dictionary<string, object>
                {
                    { "Type", "sendMessage" },
                    { "Target", "general" },
                    { "Content", _keyboardStr }
                }));
                
                _keyboardStr = "";
                return;
            }
            
            if (_keyboardStr.Length < 26)
                _keyboardStr += ch;
        }
        
        protected override void OnEnter()
        {
            Debug.Log("ChatStage entered");
            Graphics.SetClearColor(_game.Swatches.ClearColor);
        }
        
        protected override void OnLeave()
        {
            if (_socket == null)
                return;
                
            _socket.Dispose();
            _socket = null;
        }
        
        protected override void OnUpdate()
        {
            base.OnUpdate();
            Dispatcher.RunAll();
            
            _input.Value = _keyboardStr;
        }
        
        protected override void OnRender()
        {
            base.OnRender();
        }
    }
}
