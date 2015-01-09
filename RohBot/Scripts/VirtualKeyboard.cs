using System;
using System.Collections.Generic;
using System.Linq;
using GameAPI;
using GameAPI.BudgetBoy;

namespace Games.RohBot
{
    public class VirtualKeyboard : Entity
    {
        private static List<string> Layout = new List<string>
        {
            "" +    "q w e r t y u i o p  7 8 9",
            "" +    " a s d f g h j k l   4 5 6",
            "\x1E" + " z x c v b n m  ==  1 2 3",
            "" +    "    [       ]        0"
        };
        
        private static List<string> LayoutShift = new List<string>
        {
            "" +    "Q W E R T Y U I O P  & * (",
            "" +    " A S D F G H J K L   $ % ^",
            "\x1E" + " Z X C V B N M  ==  ! @ #",
            "" +    "    [       ]        )"
        };
        
        private int _charWidth;
        private int _charHeight;
        private List<Text> _rows;
        private bool _shift;
        
        public int Width
        {
            get { return _rows.Select(r => r.Size.X).Max(); }
        }
        
        public int Height
        {
            get { return _rows.Count * _charHeight; }
        }
        
        public new Vector2f Position
        {
            get { return base.Position; }
            set
            {
                foreach (var row in _rows)
                {
                    row.Position -= (Vector2i)base.Position;
                    row.Position += (Vector2i)value;
                }
                
                base.Position = value;
            }
        }
        
        public event Action<char> Pressed;
        
        public VirtualKeyboard(Image font, SwatchIndex color)
            : base()
        {
            var temp = new Text(font, color);
            _charWidth = temp.CharSize.X;
            _charHeight = temp.CharSize.Y;
            
            _rows = new List<Text>();
            
            base.Position = new Vector2f(0, 0);
            
            for (var i = 0; i < Layout.Count; i++)
            {
                var row = new Text(font, color);
                
                row.Value = Layout[i];
                row.Position = (Vector2i)(base.Position + new Vector2f(0, (Layout.Count - 1 - i) * _charHeight));
                
                _rows.Add(row);
            }
        }
        
        protected override void OnUpdate(double dt)
        {
            var cursorPos = (Vector2i)Stage.Controls.CursorPosition;
            var bounds = new RectI((Vector2i)Position, new Vector2i(Width, Height));
            
            if (!bounds.Intersects(cursorPos))
                return;
                
            // backspace
            if (Stage.Controls.B.JustReleased)
            {
                if (Pressed != null)
                    Pressed('\b');
                    
                return;
            }
            
            if (!Stage.Controls.A.JustReleased)
                return;
                
            cursorPos -= (Vector2i)Position;
            
            var x = cursorPos.X / _charWidth;
            var y = (_rows.Count - 1) - cursorPos.Y / _charHeight;
            
            if (y < 0 || y >= _rows.Count)
                return;
                
            var row = _rows[y].Value;
            
            if (x < 0 || x >= row.Length)
                return;
                
            var ch = row[x];
            
            if (ch == ' ') ch = '\0';
            
            // space
            if (y == 3 && (x >= 4 && x <= 12))
                ch = ' ';
                
            // enter
            if (y == 2 && (x >= 17 && x <= 18))
                ch = '\r';
                
            // shift
            if (y == 2 && x == 0)
            {
                SetShift(!_shift);
                return;
            }
                
            if (ch != '\0' && Pressed != null)
                Pressed(ch);
        }
        
        protected override void OnRender(Graphics graphics)
        {
            graphics.DrawPoint(_rows[0].SwatchIndex, 6, (Vector2i)Stage.Controls.CursorPosition);
            
            foreach (var row in _rows)
            {
                row.Render(graphics);
            }
        }
        
        private void SetShift(bool shift)
        {
            _shift = shift;
            var layout = _shift ? LayoutShift : Layout;
            
            for (var i = 0; i < _rows.Count; i++)
            {
                _rows[i].Value = layout[i];
            }
        }
    }
}
