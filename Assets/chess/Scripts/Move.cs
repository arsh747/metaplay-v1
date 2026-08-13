using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChessGame
{

[System.Serializable]
public class Move
{
    public Coord from;
    public Coord to;

    public Coord from2;
    public Coord to2;

    public int priority;

    // True if this move is an en passant capture (the captured pawn is
    // NOT on the "to" square - it's beside the "from" square)
    public bool isEnPassant = false;

    public Move(Coord from, Coord to, Coord from2 = null, Coord to2 = null){
        this.from = from;
        this.to = to;
        this.from2 = from2;
        this.to2 = to2;
    }

    public override string ToString(){
        return $"{from.ToString()} to {to.ToString()}";
    }
}
}
