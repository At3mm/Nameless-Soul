using UnityEngine;
using UnityEngine.UI;

/// <summary>Active status timers and pending buildup, above the health HUD.</summary>
[DisallowMultipleComponent]
public sealed class PlayerStatusHUD : MonoBehaviour
{
    PlayerVitalsHUD hud;
    StatusHUDSettings settings;
    RectTransform root;
    readonly GameObject[] slots=new GameObject[9];
    readonly Image[] icons=new Image[9], covers=new Image[9], bars=new Image[9];
    Sprite white;
    void Start()
    {
        hud=GetComponent<PlayerVitalsHUD>();settings=Resources.Load<StatusHUDSettings>("DefaultStatusHUD");
        if(hud==null || hud.healthFill==null || settings==null){enabled=false;return;}
        white=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f),1);
        var go=new GameObject("Status effects above health",typeof(RectTransform));root=go.GetComponent<RectTransform>();root.SetParent(hud.healthFill.transform.parent,false);
        root.pivot=Vector2.zero;root.sizeDelta=new Vector2(252,30);
        for(int i=0;i<9;i++)
        {
            var background=Image(root,"Status "+i,Vector2.zero,new Vector2(26,30),new Color(.04f,.04f,.04f,.85f));slots[i]=background.gameObject;
            icons[i]=Image(background.transform,"Icon",new Vector2(1,4),new Vector2(24,24),Color.white);icons[i].preserveAspect=true;
            covers[i]=Image(background.transform,"Time veil",new Vector2(1,4),new Vector2(24,24),new Color(0,0,0,.68f));covers[i].sprite=white;covers[i].type=UnityEngine.UI.Image.Type.Filled;covers[i].fillMethod=UnityEngine.UI.Image.FillMethod.Vertical;covers[i].fillOrigin=0;
            bars[i]=Image(background.transform,"Duration or buildup",new Vector2(1,1),new Vector2(24,2),Color.white);bars[i].sprite=white;bars[i].type=UnityEngine.UI.Image.Type.Filled;bars[i].fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;
            slots[i].SetActive(false);
        }
    }
    void LateUpdate()
    {
        if(root==null)return;
        var healthRect=hud.healthFill.rectTransform;
        root.anchorMin=healthRect.anchorMin;root.anchorMax=healthRect.anchorMax;
        root.anchoredPosition=healthRect.anchoredPosition+new Vector2(-healthRect.pivot.x*healthRect.rect.width,(1-healthRect.pivot.y)*healthRect.rect.height+10);
        var target=hud.target;var health=target!=null?target.GetComponent<CharacterHealth>():null;
        var buffs=target!=null?target.GetComponent<CharacterBuffs>():null;var debuffs=target!=null?target.GetComponent<CharacterDebuffs>():null;
        int visible=0;
        for(int i=0;i<8;i++)
        {
            float remaining=i<5?(buffs!=null?buffs.GetRemaining((CharacterBuffs.Kind)i):0):(debuffs!=null?debuffs.GetRemaining((CharacterDebuffs.Kind)(i-5)):0);
            float duration=i<5?(buffs!=null?buffs.GetDuration((CharacterBuffs.Kind)i):0):(debuffs!=null?debuffs.GetDuration((CharacterDebuffs.Kind)(i-5)):0);
            float buildup=i>=5 && debuffs!=null?debuffs.GetBuildup((CharacterDebuffs.Kind)(i-5))/Mathf.Max(1,debuffs.buildupThreshold):0;
            bool active=health!=null && !health.isDead && (remaining>0 || buildup>0);
            slots[i].SetActive(active);if(!active)continue;
            ((RectTransform)slots[i].transform).anchoredPosition=new Vector2(visible++*28,0);
            icons[i].sprite=i<5?settings.buffs[i]:settings.debuffs[i-5];
            icons[i].color=remaining>0?Color.white:new Color(1,1,1,.55f);
            covers[i].fillAmount=remaining>0?1-Mathf.Clamp01(remaining/Mathf.Max(.01f,duration)):0;
            bars[i].fillAmount=remaining>0?Mathf.Clamp01(remaining/Mathf.Max(.01f,duration)):Mathf.Clamp01(buildup);
            bars[i].color=i<5?new Color(.9f,.77f,.32f):i==5?new Color(1,.35f,.12f):i==6?new Color(.5f,.9f,.25f):new Color(.65f,.5f,1);
        }
        bool soul=health!=null&&!health.isDead&&target.GetComponent<PlayerController>()!=null&&SoulConsumption.Active;
        slots[8].SetActive(soul);
        if(soul){((RectTransform)slots[8].transform).anchoredPosition=new Vector2(visible*28,0);icons[8].sprite=SoulConsumption.Settings.statusIcon;icons[8].color=Color.white;covers[8].fillAmount=0;bars[8].fillAmount=1;bars[8].color=new Color(.75f,.35f,1);}
    }
    static Image Image(Transform parent,string name,Vector2 pos,Vector2 size,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=Vector2.zero;r.anchoredPosition=pos;r.sizeDelta=size;
        var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return image;
    }
    void OnDestroy(){if(root!=null)Destroy(root.gameObject);if(white!=null)Destroy(white);}
}
