using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Player
{
    public string UID;

    public int wins = 0;
    public int losses = 0;

    public Player(string uid, int wins, int losses)
    {
        this.UID = uid;
        this.wins = wins;
        this.losses = losses;
    }
}