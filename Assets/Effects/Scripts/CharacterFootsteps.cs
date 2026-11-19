using UnityEngine;
using System.Collections.Generic;

/// <summary>Distance-based footsteps: ignores idle, dash, lunges, teleports and pause.</summary>
[DisallowMultipleComponent]
public sealed class CharacterFootsteps : MonoBehaviour
{
    public FootstepAudioSettings.Surface surface=FootstepAudioSettings.Surface.Dirt;
    PlayerController player;
    EnemyBehavior enemy;
    EnemyCombat combat;
    CharacterHealth health;
    PlayerDash dash;
    AnimationEventHandler action;
    PlayerStun stun;
    EnemyBreak broken;
    EliteOrcTargetedLeap leap;
    Rigidbody2D body;
    Vector3 previous;
    float distance,wait,idleTime;
    int variation;
    bool moving;
    readonly HashSet<Collider2D> poisonContacts=new HashSet<Collider2D>();
    FootstepAudioSettings.Surface lastSurface;
    public FootstepAudioSettings.Surface CurrentSurface
    {
        get
        {
            poisonContacts.RemoveWhere(c=>c==null || !c.enabled || !c.gameObject.activeInHierarchy);
            bool inWater=false;
            foreach(var contact in poisonContacts)
            {
                if(!contact.OverlapPoint(transform.position))continue;
                var poison=contact.GetComponentInParent<PoisonZone>();
                if(poison!=null && poison.isActiveAndEnabled)return FootstepAudioSettings.Surface.Poison;
                var zone=contact.GetComponentInParent<WaterSlowZone>();
                if(zone!=null && zone.isActiveAndEnabled)inWater=true;
            }
            if(inWater)return FootstepAudioSettings.Surface.Water;
            return surface;
        }
    }
    void OnTriggerEnter2D(Collider2D other){TrackPoison(other);}
    void OnTriggerStay2D(Collider2D other){TrackPoison(other);}
    void OnTriggerExit2D(Collider2D other){poisonContacts.Remove(other);}
    void TrackPoison(Collider2D other)
    {
        if(other.GetComponentInParent<PoisonZone>()!=null || other.GetComponentInParent<WaterSlowZone>()!=null)poisonContacts.Add(other);
    }
    void Awake()
    {
        player=GetComponent<PlayerController>();enemy=GetComponent<EnemyBehavior>();combat=GetComponent<EnemyCombat>();health=GetComponent<CharacterHealth>();
        dash=GetComponent<PlayerDash>();action=GetComponent<AnimationEventHandler>();stun=GetComponent<PlayerStun>();
        broken=GetComponent<EnemyBreak>();leap=GetComponent<EliteOrcTargetedLeap>();
        body=GetComponent<Rigidbody2D>();
    }
    void OnEnable(){previous=transform.position;distance=0;wait=0;idleTime=0;moving=false;variation=Random.Range(0,6);poisonContacts.Clear();lastSurface=surface;}
    void LateUpdate()
    {
        var position=transform.position;float moved=((Vector2)(position-previous)).magnitude;previous=position;
        if(Time.deltaTime<=0)return;
        wait=Mathf.Max(0,wait-Time.deltaTime);
        bool blocked=(health!=null && health.isDead) || (player!=null && !player.isActiveAndEnabled) ||
            (enemy!=null && (!enemy.isActiveAndEnabled || enemy.currentState==EnemyBehavior.OrcState.Attack)) ||
            (dash!=null && dash.isDashing) || (action!=null && action.isActionLocked) ||
            (stun!=null && stun.IsStunned) || (broken!=null && broken.IsBroken) ||
            (leap!=null && leap.IsLeaping) || moved>2;
        blocked |= combat!=null && combat.IsLunging;
        if(blocked){distance=0;moving=false;return;}
        var currentSurface=CurrentSurface;
        if(currentSurface!=lastSurface){moving=false;distance=0;lastSurface=currentSurface;}
        // Rigidbody2D advances on physics ticks, not every rendered frame.
        // Keep accumulated travel through intervening frames with no displacement.
        if(moved<.0001f){idleTime+=Time.deltaTime;if(idleTime>.15f){distance=0;moving=false;}return;}
        idleTime=0;
        bool running=player!=null?player.IsSprinting:(body!=null?body.linearVelocity.magnitude:moved/Time.deltaTime)>3.1f;
        distance+=moved;float spacing=running?.9f:1.35f;
        // Start the first step after a tiny real displacement, rather than a full stride.
        if(distance<(moving?spacing:.025f) || wait>0)return;
        FootstepSFX.Play(position,currentSurface,running,enemy!=null,variation++);
        moving=true;
        distance=0;wait=running?.12f:.22f;
    }
}
