using UnityEngine;

[CreateAssetMenu(menuName = "Nameless Soul/Gameplay Tutorial")]
public sealed class GameplayTutorialSettings : ScriptableObject
{
    public string sceneName = "WastelandScene";
    public string areaId = "area1";
    [Min(1f)] public float hintDuration = 7f;
    [Min(0f)] public float hintSpacing = 3f;
    [Min(1f)] public float dashHintDistance = 5f;
    [Min(1f)] public float combatHintDistance = 6f;
    [Min(0.5f)] public float moveDistance = 2f;
    [Min(0f)] public float initialDelay = 3.5f;
    [Min(1f)] public float shrineHintDistance = 5f;
    public string move = "Dùng {Up} / {Left} / {Down} / {Right} để bước tiếp trên lối mòn.";
    public string dash = "Nhấn {Dash} để lướt. Giữ phím sau khi lướt để chạy; chú ý thanh Stamina.";
    public string attack = "Nhấn {Attack} để đánh thường. Quan sát khoảng cách trước khi ra đòn.";
    public string parry = "Nhấn {Parry} để đỡ đòn. Canh đúng lúc đối thủ ra đòn để phản đòn.";
    public string skill = "Đánh và phản đòn để tích Chiến ý (POS), rồi nhấn {Skill} để tung kỹ năng.";
    public string heal = "Bạn đang bị thương. Nhấn {Heal} để uống bình máu; cầu nguyện tại tượng để nạp lại.";
    public string shrine = "Đến gần tượng và nhấn {Interact}, chọn Cầu nguyện để lưu hành trình và hồi phục.";
    public string explore = "Tiếp tục khám phá. Khi gặp tượng thần, nhấn {Interact} và Cầu nguyện để lưu hành trình.";
}
