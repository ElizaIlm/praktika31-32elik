using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common2
{
    public class ViewModelGames
    {
        public Snakes SnakesPlayers { get; set; }
        public Snakes.Point Points { get; set; }
        public int Top = 0;
        public int IdSnake { get; set; }
    }
}
