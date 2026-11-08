using UnityEngine;

public static class WorldLoreTerminalLayout
{
    public const float Width=520f;
    public const float Height=270f;
    public const float Left=14f;
    public const float Bottom=16f;
    // Keep the terminal at the same relative size as the 1600x900 design.
    // Do not cap the upper end: standalone builds commonly run at 1080p/1440p,
    // where a fixed maximum makes the terminal look progressively smaller.
    public static float Scale(int screenHeight){return Mathf.Max(.78f,screenHeight/900f);}
    public static Rect Frame(int screenHeight)
    {
        float scale=Scale(screenHeight);
        return new Rect(Left*scale,screenHeight-(Height+Bottom)*scale,Width*scale,Height*scale);
    }
}
