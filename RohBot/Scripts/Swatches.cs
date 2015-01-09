using GameAPI;
using GameAPI.BudgetBoy;

namespace Games.RohBot
{
    public class Swatches
    {
        public readonly SwatchIndex ClearColor;
        public readonly SwatchIndex White;
        
        public Swatches(PaletteBuilder palette)
        {
            ClearColor = palette.Add(0x111111, 0x111111, 0x111111);
            White = palette.Add(0xffffff, 0xffffff, 0xffffff);
        }
    }
}
