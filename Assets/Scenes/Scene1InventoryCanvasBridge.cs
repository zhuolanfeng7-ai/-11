using UnityEngine;
using UnityEngine.UI;

// Synchronize the collection state with the editable Scene1 Canvas slots.
public sealed class Scene1InventoryCanvasBridge:MonoBehaviour
{
    private ImportantItemCollectionUI inventory;
    private RectTransform panel;
    private readonly RectTransform[] slots=new RectTransform[4];
    private readonly RawImage[] icons=new RawImage[4];
    public static void Refresh(){var bridge=FindObjectOfType<Scene1InventoryCanvasBridge>();if(bridge)bridge.Sync();}
    public static Rect ScreenSlot(int index)
    {
        var bridge=FindObjectOfType<Scene1InventoryCanvasBridge>();if(!bridge||index<0||index>3)return Rect.zero;
        bridge.Sync();var slot=bridge.slots[index];if(!slot)return Rect.zero;
        var canvas=slot.GetComponentInParent<Canvas>();var camera=canvas&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
        var corners=new Vector3[4];slot.GetWorldCorners(corners);var a=RectTransformUtility.WorldToScreenPoint(camera,corners[0]);var b=RectTransformUtility.WorldToScreenPoint(camera,corners[2]);return Rect.MinMaxRect(a.x,a.y,b.x,b.y);
    }
    private void Awake(){inventory=GetComponent<ImportantItemCollectionUI>();Sync();}
    private void LateUpdate(){Sync();}
    private void Sync()
    {
        if(!inventory)return;
        if(!panel)
        {
            foreach(var rect in Resources.FindObjectsOfTypeAll<RectTransform>())if(rect.gameObject.scene==gameObject.scene&&rect.name=="RightInventoryPanel"){panel=rect;break;}
            if(!panel)return;
            string[] names={"GlassesInventoryEntry","CollectionSlot_Family","CollectionSlot_Sketch","CollectionSlot_Key"};
            var film=panel.Find("CollectionSlot_Film");if(film)film.gameObject.SetActive(false);
            for(int i=0;i<4;i++)
            {
                var slot=panel.Find(names[i]) as RectTransform;if(!slot)continue;slots[i]=slot;slot.gameObject.SetActive(true);slot.SetSiblingIndex(i);
                var icon=slot.GetComponentInChildren<RawImage>(true);
                if(!icon){var go=new GameObject("Collected Item Icon",typeof(RectTransform),typeof(RawImage));go.transform.SetParent(slot,false);icon=go.GetComponent<RawImage>();}
                icons[i]=icon;icon.raycastTarget=false;icon.uvRect=new Rect(0,0,1,1);icon.color=Color.white;
                var r=icon.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(48,27);r.localScale=Vector3.one;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        }
        for(int i=0;i<4;i++)
        {
            if(!slots[i]||!icons[i])continue;slots[i].gameObject.SetActive(true);
            var texture=inventory.VisibleIcon(i);
            icons[i].texture=texture;icons[i].gameObject.SetActive(texture);icons[i].enabled=texture;
            var outline=slots[i].GetComponent<Outline>();if(outline)outline.effectColor=texture?new Color(.34f,1,.64f):new Color(.58f,.22f,.82f,.72f);
        }
    }
}
