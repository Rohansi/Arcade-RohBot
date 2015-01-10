using System.Collections;
using System.Collections.Generic;
using GameAPI;
using GameAPI.BudgetBoy;

namespace Games.RohBot
{
    public class DisconnectedStage : Stage
    {
        private Main _game;
        
        private Text _text;
        
        public DisconnectedStage(Main game)
            : base(game)
        {
            _game = game;
            
            var font = Graphics.GetImage("Resources", "font");
            
            _text = Add(new Text(font, _game.Swatches.White), 0);
            _text.Value = "Lost connection!";
            _text.Position = (Graphics.Size / 2) - (_text.Size / 2);
            
            StartCoroutine(ReconnectCoroutine);
        }
        
        private IEnumerator ReconnectCoroutine()
        {
            yield return Delay(10.0);
            
            _game.ResetSocket();
            _game.SetStage(new ConnectStage(_game));
        }
        
        protected override void OnEnter()
        {
            Debug.Log("DisconnectedStage entered");
            Graphics.SetClearColor(_game.Swatches.ClearColor);
        }
        
        protected override void OnUpdate()
        {
            base.OnUpdate();
            Dispatcher.RunAll();
        }
    }
}
