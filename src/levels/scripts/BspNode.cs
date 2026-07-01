using System.Collections.Generic;
using Godot;

public class BspNode(Rect2I region)
{
    public Rect2I Region = region;
    public Rect2I Room;
    public BspNode Left;
    public BspNode Right;

    public bool IsLeaf => Left == null && Right == null;

    public IEnumerable<BspNode> Leaves()
    {
        if (IsLeaf) { yield return this; yield break; }
        if (Left != null) foreach (var n in Left.Leaves()) yield return n;
        if (Right != null) foreach (var n in Right.Leaves()) yield return n;
    }
}