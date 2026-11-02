using UnityEngine;

/// <summary>Right-side archive showing the four important memories collected by the player.</summary>
public sealed class ImportantItemCollectionUI : MonoBehaviour
{
    private readonly bool[] previousState = new bool[4];
    private readonly float[] flashUntil = new float[4];

    private FirstPersonPlayer player;
    private FamilyPiecePuzzleController familyPiece;
    private PictureInspectController pictureSketch;
    private WorldStartTerminal terminal;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForScene()
    {
        if (FindObjectOfType<ImportantItemCollectionUI>() != null) return;
        new GameObject("Important Item Collection UI").AddComponent<ImportantItemCollectionUI>();
    }

    private void Awake()
    {
        CacheReferences();
        bool[] state = ReadState();
        for (int i = 0; i < state.Length; i++) previousState[i] = state[i];
    }

    private void Update()
    {
        if (player == null || familyPiece == null || pictureSketch == null || terminal == null)
            CacheReferences();

        bool[] state = ReadState();
        for (int i = 0; i < state.Length; i++)
        {
            if (state[i] && !previousState[i]) flashUntil[i] = Time.unscaledTime + 1.2f;
            previousState[i] = state[i];
        }
    }

    private void CacheReferences()
    {
        if (player == null) player = FindObjectOfType<FirstPersonPlayer>();
        if (familyPiece == null) familyPiece = FindObjectOfType<FamilyPiecePuzzleController>();
        if (pictureSketch == null) pictureSketch = FindObjectOfType<PictureInspectController>();
        if (terminal == null) terminal = FindObjectOfType<WorldStartTerminal>();
    }

    private bool[] ReadState()
    {
        return new[]
        {
            player != null && player.HasFilm,
            familyPiece != null && familyPiece.IsCollected,
            pictureSketch != null && pictureSketch.IsCollected,
            KeyPickupHandler.HasCollected
        };
    }

    private void OnGUI()
    {
        // The right-side archive is now a concrete Canvas hierarchy created in
        // Edit Mode. Keep this legacy renderer only as a fallback for old scenes.
        if (GameObject.Find("RightInventoryPanel") != null) return;
        if (terminal != null && !terminal.IsGameStarted) return;

        float scale = Mathf.Clamp(Screen.height / 900f, .75f, 1.15f);
        float size = 58f * scale;
        float gap = 12f * scale;
        float totalHeight = 4f * size + 3f * gap;
        float x = Screen.width - size - 22f * scale;
        float startY = (Screen.height - totalHeight) * .5f;

        bool[] state = ReadState();
        for (int i = 0; i < state.Length; i++)
        {
            Rect slot = new Rect(x, startY + i * (size + gap), size, size);
            bool collected = state[i];
            bool flashing = Time.unscaledTime < flashUntil[i];
            Color border = collected ? new Color(.34f, 1f, .64f, 1f) : new Color(.58f, .22f, .82f, .72f);
            if (flashing) border = Color.Lerp(border, new Color(.92f, .60f, 1f, 1f), Mathf.PingPong(Time.unscaledTime * 7f, 1f));

            Fill(slot, collected ? new Color(.32f, .10f, .46f, .72f) : new Color(.035f, .012f, .065f, .68f));
            if (flashing)
                DrawFrame(Expand(slot, 4f * scale), new Color(.75f, .32f, 1f, .45f), 2f * scale);
            DrawFrame(slot, border, (collected ? 2f : 1f) * scale);
        }
    }

    private static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    private static void DrawFrame(Rect rect, Color color, float thickness)
    {
        Fill(new Rect(rect.x, rect.y, rect.width, thickness), color);
        Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        Fill(new Rect(rect.x, rect.y, thickness, rect.height), color);
        Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private static Rect Expand(Rect rect, float amount)
    {
        return new Rect(rect.x - amount, rect.y - amount, rect.width + amount * 2f, rect.height + amount * 2f);
    }
}
