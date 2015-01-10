using System.Collections;
using System.Collections.Generic;
using GameAPI;
using GameAPI.BudgetBoy;
using MiniJSON;

namespace Games.RohBot
{
    public class LoginStage : Stage
    {
        enum LoginState
        {
            EnterUsername,
            EnterPassword,
            LoginSent,
            LoginSuccess,
            LoginFailed
        }
        
        private Main _game;
        
        private Text _title;
        private Text _label;
        private Text _input;
        
        private VirtualKeyboard _keyboard;
        private string _keyboardStr;
        
        private LoginState _state;
        private string _username;
        private string _password;
        
        private WebSocket Socket
        {
            get { return _game.Socket; }
            set { _game.Socket = value; }
        }
        
        public LoginStage(Main game)
            : base(game)
        {
            _game = game;
            
            var font = Graphics.GetImage("Resources", "font");
            var color = _game.Swatches.White;
            
            _title = Add(new Text(font, color), 0);
            _title.Value = "RohBot";
            _title.Position = (Graphics.Size / 2) - (_title.Size / 2) + new Vector2i(0, 64);
            
            _label = Add(new Text(font, color), 0);
            
            _input = Add(new Text(font, color), 0);
            
            _keyboard = Add(new VirtualKeyboard(font, color), 0);
            _keyboard.Position = new Vector2i((Graphics.Size.X / 2) - (_keyboard.Width / 2), 16);
            _keyboard.Pressed += KeyboardPressed;
            
            Socket.MessageReceived = message =>
            {
                var obj = (Dictionary<string, object>)Json.Deserialize(message);
                var type = (string)obj["Type"];

                if (type == "authResponse")
                {
                    _username = (string)obj["Name"];
                    
                    var success = (bool)obj["Success"];
                    ChangeState(success ? LoginState.LoginSuccess : LoginState.LoginFailed);
                }
            };
            
            /*_username = "Arcade";
            ChangeState(LoginState.EnterPassword);
            _keyboardStr = "password";*/
            
            ChangeState(LoginState.EnterUsername);
            
            OnUpdate(); // workaround for flicker on the first frame
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
                    
                switch (_state)
                {
                    case LoginState.EnterUsername:
                        _username = _keyboardStr;
                        ChangeState(LoginState.EnterPassword);
                        break;
                        
                    case LoginState.EnterPassword:
                        _password = _keyboardStr;
                        
                        Socket.Send(Json.Serialize(new Dictionary<string, object>
                        {
                            { "Type", "auth" },
                            { "Method", "login" },
                            { "Username", _username },
                            { "Password", _password }
                        }));
                        
                        ChangeState(LoginState.LoginSent);
                        break;
                }
                
                _keyboardStr = "";
                return;
            }
            
            _keyboardStr += ch;
        }
        
        private void ChangeState(LoginState newState)
        {
            _input.IsVisible = false;
            
            _keyboard.IsVisible = false;
            _keyboard.IsActive = false;
            
            _keyboardStr = "";
            
            switch (newState)
            {
                case LoginState.EnterUsername:
                    _label.Value = "Username:";
                    
                    _input.IsVisible = true;
                
                    _keyboard.IsVisible = true;
                    _keyboard.IsActive = true;
                    
                    _keyboardStr = "";
                    break;
                    
                case LoginState.EnterPassword:
                    _label.Value = "Password:";
                    
                    _input.IsVisible = true;
                
                    _keyboard.IsVisible = true;
                    _keyboard.IsActive = true;
                    
                    _keyboardStr = "";
                    break;
                    
                case LoginState.LoginSent:
                    _label.Value = "Logging in...";
                    break;
                    
                case LoginState.LoginSuccess:
                    _label.Value = string.Format("Logged in as {0}!", _username);
                    StartCoroutine(LoginSuccessCoroutine);
                    break;
                    
                case LoginState.LoginFailed:
                    _label.Value = "Login failed!";
                    StartCoroutine(LoginFailedCoroutine);
                    break;
            }
            
            _state = newState;
        }
        
        protected override void OnUpdate()
        {
            base.OnUpdate();
            Dispatcher.RunAll();
            
            switch (_state)
            {
                case LoginState.EnterUsername:
                case LoginState.EnterPassword:
                    _input.Value = _keyboardStr;
                    _input.Position = (Graphics.Size / 2) - new Vector2i(_input.Size.X / 2, 0);
                    
                    _label.Position = (Graphics.Size / 2) + new Vector2i(-_label.Size.X, 16);
                    break;
                    
                case LoginState.LoginSent:
                case LoginState.LoginSuccess:
                case LoginState.LoginFailed:
                    _label.Position = (Graphics.Size / 2) - (_label.Size / 2);
                    break;
            }
        }
        
        protected override void OnEnter()
        {
            Debug.Log("LoginStage entered");
            Graphics.SetClearColor(_game.Swatches.ClearColor);
        }
        
        private IEnumerator LoginSuccessCoroutine()
        {
            yield return Delay(1);
            
            _game.SetStage(new ChatStage(_game));
        }

        private IEnumerator LoginFailedCoroutine()
        {
            yield return Delay(2);
            
            _game.SetStage(new LoginStage(_game));
        }
    }
}
