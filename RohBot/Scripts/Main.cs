using GameAPI;
using GameAPI.BudgetBoy;
using ResourceLibrary;

namespace Games.RohBot
{
    [GameInfo(
        Title = "RohBot",
        AuthorName = "Rohan",
        AuthorContact = "rohan-singh@hotmail.com",
        UpdateRate = 30
    )]
    [GraphicsInfo(Width = 256, Height = 192)]
    public class Main : Game
    {
        public Swatches Swatches { get; private set; }
        
        protected override void OnReset()
        {
            SetStage(new ConnectStage(this));
        }
        
        protected override void OnLoadPalette(PaletteBuilder builder)
        {
            Swatches = new Swatches(builder);
        }
    }
}
