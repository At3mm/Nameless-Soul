using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared flat, square-pixel visual language for boss and overhead bars.</summary>
public static class PixelEnemyBarStyle
{
    public static readonly Color Blood = new Color(.68f,.075f,.10f);
    public static readonly Color Loss = new Color(.85f,.42f,.26f);
    public static readonly Color Stagger = new Color(.91f,.91f,.86f);
    public static readonly Color Track = new Color(.055f,.045f,.045f,.96f);
    public static readonly Color Rim = new Color(.44f,.42f,.35f);

    public static void Boss(DragonEnemyHUD hud)
    {
        if(hud.healthFill==null)return;
        var root=hud.healthFill.transform.parent as RectTransform;
        root.anchorMin=root.anchorMax=new Vector2(.5f,1);root.pivot=new Vector2(.5f,1);
        root.anchoredPosition=new Vector2(0,-10);root.sizeDelta=new Vector2(640,0);
        var dragon=root.Find("DragonFrame");if(dragon!=null)dragon.gameObject.SetActive(false);
        var track=root.Find("Track");
        if(track!=null){Rect((RectTransform)track,new Vector2(0,-48),new Vector2(600,12));Paint(track.GetComponent<Image>(),Track);}
        Frame(root,"Pixel health rim",new Vector2(0,-48),new Vector2(604,16));
        Rect(hud.damageTrail.rectTransform,new Vector2(0,-48),new Vector2(600,12));Paint(hud.damageTrail,Loss);
        Rect(hud.healthFill.rectTransform,new Vector2(0,-48),new Vector2(600,12));Paint(hud.healthFill,Blood);
        if(hud.enemyName!=null)
        {
            Rect(hud.enemyName.rectTransform,new Vector2(0,-23),new Vector2(600,26));
            hud.enemyName.font=GameFont.Current;hud.enemyName.fontSize=24;hud.enemyName.alignment=TextAnchor.MiddleLeft;
            hud.enemyName.color=new Color(.91f,.87f,.75f);hud.enemyName.raycastTarget=false;
            hud.enemyName.resizeTextForBestFit=true;hud.enemyName.resizeTextMinSize=16;hud.enemyName.resizeTextMaxSize=24;
        }
        if(hud.healthValue!=null)hud.healthValue.gameObject.SetActive(false);
        hud.effectFPS=12;
    }
    public static void World(EnemyHealthBar bar)
    {
        var root=bar.transform as RectTransform;if(root==null)return;
        root.sizeDelta=new Vector2(64,8);
        SetChild(root,"Shadow",new Vector2(1,-1),new Vector2(66,10),new Color(0,0,0,.55f));
        SetChild(root,"Frame",Vector2.zero,new Vector2(66,10),new Color(.08f,.07f,.065f));
        SetChild(root,"Rim",Vector2.zero,new Vector2(64,8),Rim);
        SetChild(root,"Track",Vector2.zero,new Vector2(62,6),Track);
        var track=root.Find("Track");if(track==null)return;
        foreach(var name in new[]{"DamageTrail","Fill"})
        {
            var child=track.Find(name) as RectTransform;if(child==null)continue;
            child.anchorMin=Vector2.zero;child.anchorMax=Vector2.one;child.pivot=new Vector2(0,.5f);
            child.offsetMin=child.offsetMax=Vector2.zero;Paint(child.GetComponent<Image>(),name=="Fill"?Blood:Loss);
        }
        var highlight=track.Find("Fill/Highlight");if(highlight!=null)highlight.gameObject.SetActive(false);
        highlight=track.Find("Highlight");if(highlight!=null)highlight.gameObject.SetActive(false);
    }
    private static void SetChild(Transform root,string name,Vector2 position,Vector2 size,Color color)
    {var t=root.Find(name) as RectTransform;if(t!=null){Rect(t,position,size);Paint(t.GetComponent<Image>(),color);}}
    public static void Rect(RectTransform rect,Vector2 position,Vector2 size)
    {rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=size;rect.anchoredPosition=position;}
    public static void Paint(Image image,Color color)
    {
        if(image==null)return;image.sprite=null;image.color=color;image.raycastTarget=false;
        if(image.GetComponent<PixelBarCorners>()==null)image.gameObject.AddComponent<PixelBarCorners>();
    }
    public static Image Frame(Transform parent,string name,Vector2 position,Vector2 size)
    {
        var child=parent.Find(name);var image=child!=null?child.GetComponent<Image>():null;
        if(image==null){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);image=go.GetComponent<Image>();}
        Rect(image.rectTransform,position,size);Paint(image,Rim);image.transform.SetAsFirstSibling();
        var inset=image.transform.Find("Inset");
        if(inset==null){var go=new GameObject("Inset",typeof(RectTransform),typeof(Image));go.transform.SetParent(image.transform,false);inset=go.transform;}
        float border=size.y<=5?1:2;
        Rect((RectTransform)inset,Vector2.zero,size-Vector2.one*border*2);Paint(inset.GetComponent<Image>(),Track);
        return image;
    }
}
