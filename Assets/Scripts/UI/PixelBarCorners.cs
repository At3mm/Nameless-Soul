using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Small clipped corners, preserving Filled images and sharp pixel rendering.</summary>
[DisallowMultipleComponent]
public sealed class PixelBarCorners : BaseMeshEffect
{
    public override void ModifyMesh(VertexHelper helper)
    {
        if(!IsActive())return;
        var rect=((RectTransform)transform).rect;
        var image=graphic as Image;
        bool horizontal=image!=null && image.type==Image.Type.Filled && image.fillMethod==Image.FillMethod.Horizontal;
        if(horizontal && image.fillAmount<=0){helper.Clear();return;}
        float radius=Mathf.Min(2f,rect.height*.3f,rect.width*.25f);
        if(radius<=0)return;
        var outline=new[]{
            new Vector2(rect.xMin+radius,rect.yMin),new Vector2(rect.xMax-radius,rect.yMin),
            new Vector2(rect.xMax,rect.yMin+radius),new Vector2(rect.xMax,rect.yMax-radius),
            new Vector2(rect.xMax-radius,rect.yMax),new Vector2(rect.xMin+radius,rect.yMax),
            new Vector2(rect.xMin,rect.yMax-radius),new Vector2(rect.xMin,rect.yMin+radius)};
        var input=new List<UIVertex>();helper.GetUIVertexStream(input);helper.Clear();
        var polygon=new List<UIVertex>(12);var clipped=new List<UIVertex>(12);
        for(int t=0;t+2<input.Count;t+=3)
        {
            polygon.Clear();polygon.Add(input[t]);polygon.Add(input[t+1]);polygon.Add(input[t+2]);
            // Sprite-less Images ignore fillAmount in Unity's base mesh. Clip it explicitly.
            for(int edge=0;edge<outline.Length+(horizontal?1:0)&&polygon.Count>0;edge++)
            {
                clipped.Clear();Vector2 start,end;
                if(edge<outline.Length){start=outline[edge];end=outline[(edge+1)%outline.Length];}
                else
                {
                    float x=image.fillOrigin==0?rect.xMin+rect.width*image.fillAmount:rect.xMax-rect.width*image.fillAmount;
                    start=new Vector2(x,image.fillOrigin==0?rect.yMin:rect.yMax);
                    end=new Vector2(x,image.fillOrigin==0?rect.yMax:rect.yMin);
                }
                var previous=polygon[polygon.Count-1];float before=Side(start,end,previous.position);
                foreach(var current in polygon)
                {
                    float after=Side(start,end,current.position);
                    if((before>=0)!=(after>=0))clipped.Add(Interpolate(previous,current,before/(before-after)));
                    if(after>=0)clipped.Add(current);
                    previous=current;before=after;
                }
                var swap=polygon;polygon=clipped;clipped=swap;
            }
            int offset=helper.currentVertCount;
            foreach(var vertex in polygon)helper.AddVert(vertex);
            for(int i=1;i+1<polygon.Count;i++)helper.AddTriangle(offset,offset+i,offset+i+1);
        }
    }
    private static float Side(Vector2 a,Vector2 b,Vector3 p)=>(b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x);
    private static UIVertex Interpolate(UIVertex a,UIVertex b,float t)
    {
        var result=a;result.position=Vector3.LerpUnclamped(a.position,b.position,t);
        result.uv0=Vector4.LerpUnclamped(a.uv0,b.uv0,t);result.uv1=Vector4.LerpUnclamped(a.uv1,b.uv1,t);
        result.color=Color32.Lerp(a.color,b.color,t);return result;
    }
}
