using UnityEngine;

/// <summary>Right-side archive showing the important items collected by the player.</summary>
public sealed class ImportantItemCollectionUI : MonoBehaviour
{
    private readonly bool[] previousState = new bool[4];
    private readonly float[] flashUntil = new float[4];

    private Texture2D alloyIcon;
    private Texture2D scene1GlassesIcon;
    private Texture2D scene1PieceIcon;
    public bool StoreScene1Piece(Texture2D icon){if(IsScene2||!icon||scene1PieceIcon)return false;scene1PieceIcon=icon;flashUntil[1]=Time.unscaledTime+1.2f;return true;}
    public bool StoreScene1Glasses(Texture2D icon){if(IsScene2||!icon||scene1GlassesIcon)return false;scene1GlassesIcon=icon;flashUntil[0]=Time.unscaledTime+1.2f;return true;}
    private Texture2D alloy2Icon;
    private bool hasCp1;
    private bool hasScene2Key;
    private Texture2D nailIcon;
    private Texture2D tapeIcon;
    private MeshFilter tapeSource;
    public MeshFilter TapeSource=>tapeSource;
    public Texture2D TapeIcon=>tapeIcon;
    public void RecordTapeSource(MeshFilter source){if(HasTape)tapeSource=source;}
    public bool ConsumeTape(){if(!IsScene2||!tapeIcon)return false;Destroy(tapeIcon);tapeIcon=null;tapeSource=null;return true;}
    private bool hasWood;
    public bool HasWood => hasWood;
    public bool HasTape => tapeIcon != null;
    public bool CollectWood(){if(!IsScene2||hasWood)return false;hasWood=true;return true;}
    public bool HasNail => nailIcon != null;
    public Texture2D NailIcon => nailIcon;
    public bool ConsumeNail(){if(!IsScene2||!nailIcon)return false;Destroy(nailIcon);nailIcon=null;return true;}
    public bool HasScene2Key => hasScene2Key;
    public bool HasCp1 => hasCp1;
    public bool CollectCp1(){if(!IsScene2||hasCp1)return false;hasCp1=true;return true;}
    public bool CollectScene2Key(){if(!IsScene2||hasScene2Key)return false;hasScene2Key=true;return true;}
    public bool ConsumeCp1(){if(!IsScene2||!hasCp1)return false;hasCp1=false;return true;}
    public bool HasAlloy2 => alloy2Icon != null;
    public bool HasAlloy => alloyIcon != null;
    public Texture2D AlloyIcon=>alloyIcon;
    public Texture2D Alloy2Icon=>alloy2Icon;
    public bool ConsumeAlloy2(){if(!IsScene2||!alloy2Icon)return false;Destroy(alloy2Icon);alloy2Icon=null;return true;}
    public bool ConsumeAlloy(){if(!IsScene2||!alloyIcon)return false;Destroy(alloyIcon);alloyIcon=null;return true;}
    public int VisibleSlotCount=>4;
    // Keep existing saved pickup item IDs; only the four repair parts have UI slots.
    public static int DisplaySlotForItem(int item){return item==0?0:item==1?1:item==4?2:item==5?3:-1;}
    public Texture2D VisibleIcon(int index){if(!IsScene2)return index==0?scene1GlassesIcon:index==1?scene1PieceIcon:null;return index==0?alloyIcon:index==1?alloy2Icon:index==2?nailIcon:index==3?tapeIcon:null;}
    public Rect SlotScreenRect(int index){if(index<0||index>=VisibleSlotCount)return Rect.zero;float scale=Mathf.Clamp(Screen.height/900f,.75f,1.15f),size=58*scale,gap=12*scale;int count=VisibleSlotCount;float top=(Screen.height-(count*size+(count-1)*gap))*.5f+index*(size+gap);return new Rect(Screen.width-size-22*scale,Screen.height-top-size,size,size);}

    private bool IsScene2 => string.Equals(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Replace(" ",""),"scene2",System.StringComparison.OrdinalIgnoreCase);
    public bool StoreAlloy(Texture2D icon){return StoreAlloyInSlot(0,icon);}
    public bool StoreAlloyInSlot(int slot,Texture2D icon)
    {
        if(!IsScene2 || !icon || slot<0 || slot>5)return false;
        // Legacy callers can still grant records/keys; they never occupy a repair slot.
        if(slot==2||slot==3){bool accepted=slot==2?CollectCp1():CollectScene2Key();if(accepted)Destroy(icon);return accepted;}
        if(slot==0){if(alloyIcon)return false;alloyIcon=icon;}
        else if(slot==1){if(alloy2Icon)return false;alloy2Icon=icon;}
        if(slot==4){if(nailIcon)return false;nailIcon=icon;}
        if(slot==5){if(tapeIcon)return false;tapeIcon=icon;}
        flashUntil[DisplaySlotForItem(slot)]=Time.unscaledTime+1.2f;return true;
    }
    private void OnDestroy(){if(alloyIcon)Destroy(alloyIcon);if(alloy2Icon)Destroy(alloy2Icon);if(nailIcon)Destroy(nailIcon);if(tapeIcon)Destroy(tapeIcon);}
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
        if(IsScene2)return new[]{HasAlloy,HasAlloy2,HasNail,HasTape};
        return new[]
        {
            scene1GlassesIcon!=null,
            scene1PieceIcon!=null,
            IsScene2 ? HasCp1 : pictureSketch != null && pictureSketch.IsCollected,
            IsScene2 ? HasScene2Key : KeyPickupHandler.HasCollected
        };
    }

    private void OnGUI()
    {
        if (terminal != null && !terminal.IsGameStarted) return;

        float scale = Mathf.Clamp(Screen.height / 900f, .75f, 1.15f);
        float size = 58f * scale;
        float gap = 12f * scale;
        int count=VisibleSlotCount;
        float totalHeight = count * size + (count-1) * gap;
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
            Texture2D icon=VisibleIcon(i);
            if(icon && (!IsScene2 || (!Scene2PlateInstallation.IsDraggingSlot(i) && !Scene2NailInstallation.IsDraggingSlot(i))))
                GUI.DrawTexture(new Rect(slot.x+6*scale,slot.y+6*scale,slot.width-12*scale,slot.height-12*scale),icon,ScaleMode.ScaleToFit,true);
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
