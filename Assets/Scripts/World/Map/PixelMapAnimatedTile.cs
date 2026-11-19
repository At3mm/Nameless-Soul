using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName="Game/World/Pixel Animated Tile")]
public sealed class PixelMapAnimatedTile : TileBase
{
    public Sprite[] frames;
    public Tile.ColliderType colliderType=Tile.ColliderType.None;
    public override void GetTileData(Vector3Int position,ITilemap tilemap,ref TileData data)
    {
        data.transform=Matrix4x4.identity;data.color=Color.white;data.colliderType=colliderType;
        if(frames!=null&&frames.Length>0)data.sprite=frames[0];
    }
    public override bool GetTileAnimationData(Vector3Int position,ITilemap tilemap,ref TileAnimationData data)
    {
        if(frames==null||frames.Length==0)return false;
        data.animatedSprites=frames;
        var map=tilemap.GetComponent<Tilemap>();
        data.animationSpeed=PixelAnimationSettings.FPS/Mathf.Max(.01f,map!=null?map.animationFrameRate:1);
        data.animationStartTime=0;
        return true;
    }
}
