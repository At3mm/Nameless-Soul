using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

[CreateAssetMenu(menuName="Game/UI/Pixel Menu Button Style")]
public sealed class PixelMenuButtonStyle : ScriptableObject
{
    public Sprite background;
    public Color normal = new Color(.7f,.72f,.65f);
    public Color selected = new Color(1f,1f,.92f);
    public Color pressed = new Color(.55f,.52f,.4f);
    public Color disabled = new Color(.45f,.45f,.4f,.4f);
    public Color text = new Color(1f,.91f,.7f);
    public Color shadow = new Color(0,0,0,.9f);
    public Vector2 shadowOffset = new Vector2(1,-2);
    private static PixelMenuButtonStyle cached;
    public static PixelMenuButtonStyle Current => cached != null ? cached : (cached=Resources.Load<PixelMenuButtonStyle>("UI/PixelMenuButtonStyle"));

    public static PixelMenuButtonSkin Apply(Button button)
    {
        if(button==null || Current==null)return null;
        var skin=button.GetComponent<PixelMenuButtonSkin>();
        if(skin==null)skin=button.gameObject.AddComponent<PixelMenuButtonSkin>();
        skin.style=Current;skin.Refresh();return skin;
    }
    public static void Select(Button button,bool selected)
    {
        var skin=Apply(button);if(skin!=null){skin.useMenuSelection=true;skin.selected=selected;skin.Refresh();}
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        cached=null;SceneManager.sceneLoaded-=SkinScene;SceneManager.sceneLoaded+=SkinScene;
    }
    private static void SkinScene(Scene scene,LoadSceneMode mode)
    {
        foreach(var root in scene.GetRootGameObjects())
            foreach(var button in root.GetComponentsInChildren<Button>(true))Apply(button);
    }
}
