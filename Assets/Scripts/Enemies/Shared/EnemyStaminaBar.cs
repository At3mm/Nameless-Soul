using UnityEngine;
using UnityEngine.UI;

/// <summary>Small enemy stamina bar under the existing health bar.</summary>
public sealed class EnemyStaminaBar : MonoBehaviour
{
    private EnemyBreak source;
    private CharacterHealth health;
    private EnemyClassification classification;
    private RectTransform fill;
    private Image track;
    private Text label;
    private CanvasGroup group;
    private PixelEffectClock effectClock;
    private bool wasBroken;
    public static void Create(EnemyBreak source)
    {
        if (source.transform.Find("StaminaBar") != null) return;
        var root = new GameObject("StaminaBar", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        root.transform.SetParent(source.transform, false);
        var rect = root.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(64,16);
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingLayerName = "Overhead"; canvas.sortingOrder = 25;
        var bar = root.AddComponent<EnemyStaminaBar>(); bar.source = source;
        bar.health = source.GetComponent<CharacterHealth>(); bar.classification = source.GetComponent<EnemyClassification>();
        bar.group = root.GetComponent<CanvasGroup>(); bar.group.blocksRaycasts = false;
        PixelEnemyBarStyle.Frame(root.transform,"Pixel stagger rim",Vector2.zero,new Vector2(64,5));
        bar.track = MakeImage(root.transform,"Track",new Vector2(62,3),PixelEnemyBarStyle.Track);
        var inner = MakeImage(bar.track.transform,"Fill",Vector2.zero,PixelEnemyBarStyle.Stagger);
        bar.fill = inner.rectTransform; bar.fill.anchorMin = Vector2.zero; bar.fill.anchorMax = Vector2.one;
        bar.fill.offsetMin = bar.fill.offsetMax = Vector2.zero;
        var go = new GameObject("Break label", typeof(RectTransform), typeof(Text)); go.transform.SetParent(root.transform,false);
        bar.label = go.GetComponent<Text>(); bar.label.font = GameFont.Current;
        bar.label.fontSize = 11; bar.label.fontStyle = FontStyle.Bold; bar.label.alignment = TextAnchor.MiddleCenter;
        bar.label.color = PixelEnemyBarStyle.Stagger; bar.label.raycastTarget = false;
        var lr = bar.label.rectTransform; lr.anchoredPosition = new Vector2(0,-9); lr.sizeDelta = new Vector2(70,14);
    }
    private static Image MakeImage(Transform parent, string name, Vector2 size, Color color)
    {
        var go = new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false);
        var image = go.GetComponent<Image>(); PixelEnemyBarStyle.Paint(image,color); image.rectTransform.sizeDelta = size; return image;
    }
    private void LateUpdate()
    {
        bool visible = source != null && source.isActiveAndEnabled && health != null && !health.isDead && (classification == null || !classification.UsesDragonBar);
        group.alpha = visible ? 1 : 0; if (!visible) return;
        transform.position = source.transform.position + Vector3.up * source.barHeight; transform.rotation = Quaternion.identity;
        var scale = source.transform.lossyScale;
        transform.localScale = new Vector3(Mathf.Abs(scale.x)>.0001f?.01f/scale.x:.01f,Mathf.Abs(scale.y)>.0001f?.01f/scale.y:.01f,.01f);
        if(effectClock.Tick(Time.deltaTime,12) || wasBroken!=source.IsBroken || source.Ratio<=0 || source.Ratio>=1)fill.anchorMax = new Vector2(Mathf.Round(source.Ratio*62)/62,1);
        fill.GetComponent<Image>().enabled = source.Ratio > 0f;
        track.color = PixelEnemyBarStyle.Track;
        label.text = source.IsBroken ? "BREAK" : "";
        wasBroken=source.IsBroken;
    }
}
