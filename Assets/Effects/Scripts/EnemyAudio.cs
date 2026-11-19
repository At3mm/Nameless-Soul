using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyAudio : MonoBehaviour
{
    public EnemyAudioProfile profile;
    public bool ReplacesDefaultWeaponSwing => profile != null && profile.replaceDefaultWeaponSwing;
    private float lastAttack=-100,lastHurt=-100;
    private int variation;
    private bool deathPlayed;
    private void OnEnable(){lastAttack=lastHurt=-100;deathPlayed=false;}
    public void Attack(string skill)
    {
        if(!Ready()||BossPhaseController.Protects(gameObject)||Time.time-lastAttack<profile.attackInterval)return;
        var clips=profile.attacks;var cue=profile.skills.Find(entry=>entry.skillName==skill);
        if(cue!=null&&cue.clips!=null&&cue.clips.Length>0)clips=cue.clips;
        if(Emit(clips,false))lastAttack=Time.time;
    }
    public void Hurt()
    {
        if(!Ready()||BossPhaseController.Protects(gameObject)||Time.time-lastHurt<profile.hurtInterval)return;
        if(Emit(profile.hurt,false))lastHurt=Time.time;
    }
    public void Death(){if(!deathPlayed&&Ready()){deathPlayed=true;Emit(profile.death,true);}}
    public void PhaseTransition(){if(Ready())Emit(profile.phaseTransition,true);}
    private bool Ready()=>isActiveAndEnabled&&profile!=null&&Application.isPlaying&&Time.timeScale>0;
    private bool Emit(AudioClip[] clips,bool critical)
    {
        if(clips==null||clips.Length==0)return false;
        var clip=clips[variation%clips.Length];
        if(!EnemyAudioPool.Play(clip,transform.position,profile.volume,profile.pitch,critical))return false;
        variation++;return true;
    }
}
