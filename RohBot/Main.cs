using GameAPI;
using GameAPI.BudgetBoy;
using ResourceLibrary;

namespace Games.RohBot
{
    [GameInfo(
        Title = "RohBot",
        AuthorName = "Rohan",
        AuthorContact = "steamcommunity.com/id/rohans",
        UpdateRate = 30
    )]
    [GraphicsInfo(Width = 256, Height = 192)]
    public class Main : Game
    {
        public WebSocket Socket { get; set; }
        public Swatches Swatches { get; private set; }
        
        public void ResetSocket()
        {
            if (Socket == null)
                return;
                
            Socket.Dispose();
            Socket = null;
        }
        
        protected override void OnReset()
        {
            ResetSocket();
            SetStage(new ConnectStage(this));
        }
        
        protected override void OnLoadPalette(PaletteBuilder builder)
        {
            Swatches = new Swatches(builder);
        }
        
        protected override void OnDispose()
        {
            ResetSocket();
        }
    }
}
