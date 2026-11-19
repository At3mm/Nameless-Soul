using UnityEngine;

/// <summary>Shared font for authored UI and text created at runtime.</summary>
public static class GameFont
{
    private static Font cached;
    public static Font Current => cached != null ? cached : (cached = Resources.Load<Font>("Fonts/VT323-Regular"));
}
