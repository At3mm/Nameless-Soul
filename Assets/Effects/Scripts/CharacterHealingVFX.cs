using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterHealth))]
public sealed class CharacterHealingVFX : MonoBehaviour
{
    public GameObject effectPrefab;
    [Min(0.05f)] public float duration = 0.75f;
    public Vector3 localOffset = Vector3.zero;
    private CharacterHealth health;
    private GameObject activeEffect;
    private float remaining;
    public bool IsPlaying => activeEffect != null;

    private void Awake() { StatusEffectDefaults.Configure(this); health = GetComponent<CharacterHealth>(); }
    private void OnEnable()
    {
        if (health == null) health = GetComponent<CharacterHealth>();
        health.Healed += OnHealed;
    }
    private void OnDisable()
    {
        if (health != null) health.Healed -= OnHealed;
        Clear();
    }
    private void OnHealed(float amount)
    {
        if (effectPrefab == null || amount <= 0f) return;
        Clear();
        activeEffect = Instantiate(effectPrefab, transform, false);
        activeEffect.transform.localPosition = localOffset;
        var sprite = activeEffect.GetComponent<SpriteRenderer>();
        var character = GetComponent<SpriteRenderer>();
        if (sprite != null && character != null)
        {
            sprite.sortingLayerID = character.sortingLayerID;
            sprite.sortingOrder = character.sortingOrder + 2;
        }
        remaining = Mathf.Max(0.05f, duration);
    }
    private void Update()
    {
        if (activeEffect == null) return;
        remaining -= Time.deltaTime;
        if (remaining <= 0f) Clear();
    }
    private void Clear()
    {
        if (activeEffect != null) { activeEffect.SetActive(false); Destroy(activeEffect); }
        activeEffect = null;
    }
}
