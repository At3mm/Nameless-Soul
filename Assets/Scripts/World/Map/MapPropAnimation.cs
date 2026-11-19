using UnityEngine;

/// <summary>Map-only sprite playback. Doors open/close explicitly; ambient props loop.</summary>
public sealed class MapPropAnimation : MonoBehaviour
{
    public SpriteRenderer visual;
    public Sprite[] frames,openingFrames;
    public bool loop=true,deactivateOnComplete,startOpen;
    public Collider2D doorBlocker;
    private enum State { Closed,Opening,Opened,Closing }
    private State state;
    private float elapsed;
    public bool IsOpen=>state==State.Opened;
    public bool IsMoving=>state==State.Opening||state==State.Closing;
    private bool Door=>openingFrames!=null&&openingFrames.Length>0;
    private void OnEnable(){elapsed=0;state=Door&&startOpen?State.Opened:State.Closed;Draw();}
    private void Update(){elapsed+=Time.deltaTime;Draw();}
    [ContextMenu("Animation/Open")]
    public void Open(){if(!Door||state==State.Opened||state==State.Opening)return;state=State.Opening;elapsed=0;Draw();}
    [ContextMenu("Animation/Close")]
    public void Close(){if(!Door||state==State.Closed||state==State.Closing)return;state=State.Closing;elapsed=0;Draw();}
    public void Toggle(){if(state==State.Opened||state==State.Opening)Close();else Open();}
    public void Replay(){elapsed=0;state=State.Closed;if(!gameObject.activeSelf)gameObject.SetActive(true);Draw();}
    private void Draw()
    {
        if(visual==null||frames==null||frames.Length==0)return;
        int frame=Mathf.FloorToInt(elapsed*PixelAnimationSettings.FPS);
        if(Door&&(state==State.Opening||state==State.Closing))
        {
            if(frame>=openingFrames.Length){state=state==State.Opening?State.Opened:State.Closed;elapsed=0;frame=0;}
            else visual.sprite=openingFrames[state==State.Opening?frame:openingFrames.Length-1-frame];
        }
        if(state==State.Opened)visual.sprite=openingFrames[openingFrames.Length-1];
        else if(state==State.Closed)
        {
            if(!loop&&frame>=frames.Length&&deactivateOnComplete){gameObject.SetActive(false);return;}
            visual.sprite=frames[loop?frame%frames.Length:Mathf.Min(frame,frames.Length-1)];
        }
        if(doorBlocker!=null)doorBlocker.enabled=state!=State.Opened;
    }
}
