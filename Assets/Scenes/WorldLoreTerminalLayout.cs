using UnityEngine;

public static class WorldLoreTerminalLayout
{
    public const float Width=520f;
    public const float Height=270f;
    public const float Left=14f;
    public const float Bottom=16f;
    public static float Scale(int screenHeight){return Mathf.Clamp(screenHeight/900f,.78f,1.18f);}
    public static Rect Frame(int screenHeight)
    {
        float scale=Scale(screenHeight);
        return new Rect(Left*scale,screenHeight-(Height+Bottom)*scale,Width*scale,Height*scale);
    }
}
