using UnityEngine;

// The imported drawer uses local Z for height and local Y for its slide axis.
public static class DrawerUpShellRepair
{
    public const string ShellName="DrawerUp Solid Inner Panels";
    public static Transform Ensure(Transform drawer)
    {
        var existing=drawer.Find(ShellName);
        var source=drawer.GetComponent<MeshFilter>();var renderer=drawer.GetComponent<MeshRenderer>();
        if(!source||!source.sharedMesh||!renderer)return null;
        var bounds=source.sharedMesh.bounds;
        float thickness=Mathf.Min(bounds.size.x,Mathf.Min(bounds.size.y,bounds.size.z))*.04f;
        Material material=null;
        foreach(var candidate in renderer.sharedMaterials)
            if(candidate){material=candidate;break;}
        if(existing)
        {
            foreach(var panel in existing.GetComponentsInChildren<MeshRenderer>())
                if(panel.sharedMaterial!=material)panel.sharedMaterial=material;
            return existing;
        }
        var root=new GameObject(ShellName).transform;root.SetParent(drawer,false);
        // A small inset avoids flickering against surviving exterior polygons.
        AddPanel(root,"Front Inner Panel",new Vector3(bounds.center.x,bounds.min.y+thickness*.58f,bounds.center.z),new Vector3(bounds.size.x,thickness,bounds.size.z),material);
        AddPanel(root,"Back Panel",new Vector3(bounds.center.x,bounds.max.y-thickness*.58f,bounds.center.z),new Vector3(bounds.size.x,thickness,bounds.size.z),material);
        AddPanel(root,"Bottom Panel",new Vector3(bounds.center.x,bounds.center.y,bounds.min.z+thickness*.58f),new Vector3(bounds.size.x,bounds.size.y,thickness),material);
        return root;
    }
    private static void AddPanel(Transform parent,string name,Vector3 center,Vector3 size,Material material)
    {
        var panel=GameObject.CreatePrimitive(PrimitiveType.Cube);panel.name=name;
        panel.layer=parent.parent.gameObject.layer;panel.transform.SetParent(parent,false);
        panel.transform.localPosition=center;panel.transform.localScale=size;
        panel.GetComponent<MeshRenderer>().sharedMaterial=material;
    }
}
