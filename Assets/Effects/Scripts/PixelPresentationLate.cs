using UnityEngine;

[DefaultExecutionOrder(32000)]
public sealed class PixelPresentationLate : MonoBehaviour
{
    public PixelPresentation owner;
    private void LateUpdate(){if(owner!=null)owner.Present();}
}
