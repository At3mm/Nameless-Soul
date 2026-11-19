using UnityEngine;
using UnityEngine.UI;

public sealed class HealthPotionHUD : MonoBehaviour
{
    public PlayerVitalsHUD vitals;
    public Button useButton;
    public Image icon;
    public Image cooldownOverlay;
    public Text countText;
    public Text keyText;
    private PlayerHealthPotion potion;
    private SettingsMenuToggle pauseMenu;
    private void Awake()
    {
        pauseMenu=UnityEngine.Object.FindFirstObjectByType<SettingsMenuToggle>(FindObjectsInactive.Include);
        if (UnityEngine.EventSystems.EventSystem.current == null)
            new GameObject("UI Event System", typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
    }
    private void OnEnable() { if (useButton != null) useButton.onClick.AddListener(Use); MedicineQuickSlot.AlignCount(countText,keyText); }
    private void OnDisable()
    {
        if (useButton != null) useButton.onClick.RemoveListener(Use);
        if (cooldownOverlay != null) cooldownOverlay.enabled = false;
    }
    private void Use() { if (potion != null) potion.TryUse(); }
    private void LateUpdate()
    {
        var target = vitals != null ? vitals.target : null;
        if (potion == null || potion.gameObject != (target != null ? target.gameObject : null))
            potion = target != null ? target.GetComponent<PlayerHealthPotion>() : null;
        if (useButton != null) useButton.interactable = potion != null && potion.CanUse;
        if (countText != null) { countText.enabled=pauseMenu==null || !pauseMenu.IsOpen; countText.text = potion != null ? potion.Charges.ToString() : "0"; }
        if (keyText != null) keyText.text = GameControls.Display(GameControls.Action.Heal);
        if (icon != null) icon.color = potion != null && potion.Charges > 0 ? Color.white : new Color(0.4f, 0.4f, 0.4f, 1f);
        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = potion != null && potion.cooldown > 0f
                ? Mathf.Clamp01(potion.CooldownRemaining / potion.cooldown) : 0f;
            // A Filled Image without a sprite can render a full rectangle even
            // at zero fill. Hide it explicitly and mask only the bottle shape.
            cooldownOverlay.sprite = icon != null ? icon.sprite : null;
            cooldownOverlay.enabled = cooldownOverlay.sprite != null && cooldownOverlay.fillAmount > 0f;
        }
    }
}
