using UnityEngine;

namespace World
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D))]
    public sealed class NpcStationary : MonoBehaviour
    {
        private void Awake() { Apply(); }
        public void Apply()
        {
            var body=GetComponent<Rigidbody2D>();
            if(body.bodyType!=RigidbodyType2D.Static){body.linearVelocity=Vector2.zero;body.angularVelocity=0;}
            body.gravityScale=0;
            body.constraints=RigidbodyConstraints2D.FreezeAll;
            body.bodyType=RigidbodyType2D.Static;
        }
    }
}
