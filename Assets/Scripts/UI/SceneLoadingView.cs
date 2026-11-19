using UnityEngine;
using UnityEngine.UI;

/// <summary>Pixel loading overlay hosted by the persistent scene fader.</summary>
public sealed class SceneLoadingView : MonoBehaviour
{
    private RectTransform fill;
    private Text percentage;
    public float Progress { get; private set; }
    public bool IsVisible => gameObject.activeSelf;
    public void Build()
    {
        var background=gameObject.AddComponent<Image>();background.color=new Color(.025f,.035f,.025f);background.raycastTarget=true;
        var rect=GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        Label("Loading title","ĐANG TẢI",new Vector2(0,25),new Vector2(800,100),64,new Color(.88f,.76f,.51f));
        Image("Progress track",new Vector2(0,-80),new Vector2(640,8),new Color(.16f,.19f,.12f));
        fill=Image("Progress",new Vector2(-320,-80),new Vector2(0,8),new Color(.83f,.70f,.43f)).rectTransform;fill.pivot=new Vector2(0,.5f);
        percentage=Label("Percentage","0%",new Vector2(0,-140),new Vector2(200,50),30,new Color(.68f,.71f,.58f));
        SetProgress(0);gameObject.SetActive(false);
    }
    public void Show() { gameObject.SetActive(true); transform.SetAsLastSibling(); SetProgress(0); }
    public void SetProgress(float value)
    {
        Progress=Mathf.Clamp01(value);fill.sizeDelta=new Vector2(Mathf.Floor(Progress*640/4)*4,8);
        percentage.text=Mathf.FloorToInt(Progress*100)+"%";
    }
    public void Hide() { gameObject.SetActive(false); }
    private Image Image(string name,Vector2 position,Vector2 size,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(transform,false);
        var rect=go.GetComponent<RectTransform>();rect.anchoredPosition=position;rect.sizeDelta=size;
        var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return image;
    }
    private Text Label(string name,string value,Vector2 position,Vector2 size,int fontSize,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(transform,false);
        var rect=go.GetComponent<RectTransform>();rect.anchoredPosition=position;rect.sizeDelta=size;
        var text=go.GetComponent<Text>();text.font=GameFont.Current;text.fontSize=fontSize;text.text=value;text.color=color;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;return text;
    }
}
