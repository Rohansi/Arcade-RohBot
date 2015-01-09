using System.Collections;
using System.Collections.Generic;
using GameAPI;
using GameAPI.BudgetBoy;

namespace Games.RohBot
{
    public class ConnectStage : Stage
    {
        private Main _game;
        private WebSocket _socket;
        
        private Text _text;
        
        public ConnectStage(Main game)
            : base(game)
        {
            _game = game;
            
            var font = Graphics.GetImage("Resources", "font");
            
            _text = Add(new Text(font, _game.Swatches.White), 0);
            _text.Value = "Connecting...";
            _text.Position = (Graphics.Size / 2) - (_text.Size / 2);
            
            _socket = new WebSocket("fpp.literallybrian.com", 12000);
            
            _socket.Connected = ConnectedHandler;
            _socket.Disconnected = exception => ConnectedHandler(false);
        }
        
        private void ConnectedHandler(bool success)
        {
            if (success)
                StartCoroutine(ConnectedCoroutine);
            else
                StartCoroutine(FailedCoroutine);
            
            _socket.Disconnected = exception =>
            {
                _game.SetStage(new DisconnectedStage(_game));
            };
        }
        
        private IEnumerator ConnectedCoroutine()
        {
            yield return Delay(1.0);
            
            _text.Value = "Connected!";
            _text.Position = (Graphics.Size / 2) - (_text.Size / 2);
            
            yield return Delay(0.5);
            
            var socket = _socket;
            _socket = null;
            
            _game.SetStage(new LoginStage(_game, socket));
        }
        
        private IEnumerator FailedCoroutine()
        {
            yield return Delay(1.0);
            
            _text.Value = "Couldn't connect!";
            _text.Position = (Graphics.Size / 2) - (_text.Size / 2);
            
            yield return Delay(10.0);
            
            _game.SetStage(new ConnectStage(_game));
        }
        
        protected override void OnEnter()
        {
            Debug.Log("ConnectStage entered");
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
        }
        
        protected override void OnRender()
        {
            base.OnRender();
        }
    }
}
