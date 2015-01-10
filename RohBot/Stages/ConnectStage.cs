using System.Collections;
using System.Collections.Generic;
using GameAPI;
using GameAPI.BudgetBoy;

namespace Games.RohBot
{
    public class ConnectStage : Stage
    {
        private Main _game;
        
        private Text _text;
        
        private WebSocket Socket
        {
            get { return _game.Socket; }
            set { _game.Socket = value; }
        }
        
        public ConnectStage(Main game)
            : base(game)
        {
            _game = game;
            
            var font = Graphics.GetImage("Resources", "font");
            
            _text = Add(new Text(font, _game.Swatches.White), 0);
            _text.Value = "Connecting...";
            _text.Position = (Graphics.Size / 2) - (_text.Size / 2);
            
            Socket = new WebSocket("fpp.literallybrian.com", 12000);
            
            Socket.Connected = ConnectedHandler;
            Socket.Disconnected = exception => ConnectedHandler(false);
        }
        
        private void ConnectedHandler(bool success)
        {
            if (success)
                StartCoroutine(ConnectedCoroutine);
            else
                StartCoroutine(FailedCoroutine);
            
            Socket.Disconnected = exception =>
            {
                _game.ResetSocket();
                _game.SetStage(new DisconnectedStage(_game));
            };
        }
        
        private IEnumerator ConnectedCoroutine()
        {
            yield return Delay(1.0);
            
            _text.Value = "Connected!";
            _text.Position = (Graphics.Size / 2) - (_text.Size / 2);
            
            yield return Delay(0.5);
            
            _game.SetStage(new LoginStage(_game));
        }
        
        private IEnumerator FailedCoroutine()
        {
            yield return Delay(1.0);
            
            _text.Value = "Couldn't connect!";
            _text.Position = (Graphics.Size / 2) - (_text.Size / 2);
            
            yield return Delay(10.0);
            
            _game.ResetSocket();
            _game.SetStage(new ConnectStage(_game));
        }
        
        protected override void OnEnter()
        {
            Debug.Log("ConnectStage entered");
            Graphics.SetClearColor(_game.Swatches.ClearColor);
        }
        
        protected override void OnUpdate()
        {
            base.OnUpdate();
            Dispatcher.RunAll();
        }
    }
}
