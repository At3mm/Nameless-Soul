using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Expandable inventory presentation, ready to bind to an inventory data model.</summary>
public class InventoryGridView : MonoBehaviour
{
    public ScrollRect scrollRect;
    public GridLayoutGroup grid;
    public GameObject slotTemplate;
    public GameObject emptyMessage;
    [Min(1)] public int columns = 4;
    [Min(0), Tooltip("Số ô trống tối thiểu vẫn hiển thị khi chưa có đồ.")]
    public int minimumSlots = 12;
    [SerializeField, Min(0), Tooltip("Số vật phẩm hiển thị. Hệ thống túi đồ có thể gọi SetItemCount hoặc SetItems để cập nhật.")]
    private int itemCount;

    public int ItemCount => itemCount;
    public bool CanScroll => scrollRect != null && scrollRect.content != null &&
        scrollRect.viewport != null && scrollRect.content.rect.height > scrollRect.viewport.rect.height + 0.5f;

    private void OnEnable() { Refresh(); }

    public void SetItemCount(int count)
    {
        itemCount = Mathf.Max(0, count);
        Refresh();
    }

    public void SetItems(IReadOnlyList<Sprite> icons)
    {
        SetItemCount(icons != null ? icons.Count : 0);
        if (icons == null) return;
        for (int i = 0; i < icons.Count; i++) SetItemIcon(i, icons[i]);
    }

    public bool SetItemIcon(int index, Sprite icon)
    {
        if (scrollRect == null || scrollRect.content == null || index < 0 || index >= itemCount ||
            index >= scrollRect.content.childCount)
            return false;
        var image = scrollRect.content.GetChild(index).Find("Icon")?.GetComponent<Image>();
        if (image == null) return false;
        image.sprite = icon;
        image.gameObject.SetActive(icon != null);
        return true;
    }

    [ContextMenu("Refresh Inventory Layout")]
    public void Refresh()
    {
        if (scrollRect == null || scrollRect.content == null || grid == null || slotTemplate == null) return;
        itemCount = Mathf.Max(0, itemCount);
        var content = scrollRect.content;
        int count = Mathf.Max(itemCount, Mathf.Max(0, minimumSlots));
        float previousOffset = CanScroll ? content.anchoredPosition.y : 0f;
        for (int i = content.childCount - 1; i >= count; i--)
        {
            var child = content.GetChild(i).gameObject;
            child.SetActive(false);
            child.transform.SetParent(null, false);
            if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
        }
        for (int i = content.childCount; i < count; i++)
        {
            var slot = Instantiate(slotTemplate, content, false);
            slot.name = "Slot_" + i.ToString("D2");
            slot.SetActive(true);
        }
        for (int i = itemCount; i < content.childCount; i++)
        {
            var icon = content.GetChild(i).Find("Icon");
            if (icon != null) icon.gameObject.SetActive(false);
        }
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Mathf.Max(1, columns);
        int rows = Mathf.CeilToInt(count / (float)grid.constraintCount);
        float height = grid.padding.top + grid.padding.bottom + rows * grid.cellSize.y +
            Mathf.Max(0, rows - 1) * grid.spacing.y;
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        if (emptyMessage != null) emptyMessage.SetActive(itemCount == 0);
        if (scrollRect.verticalScrollbar != null) scrollRect.verticalScrollbar.gameObject.SetActive(CanScroll);
        var position = content.anchoredPosition;
        position.y = CanScroll
            ? Mathf.Clamp(previousOffset, 0f, height - scrollRect.viewport.rect.height) : 0f;
        content.anchoredPosition = position;
        if (scrollRect.verticalScrollbar != null)
        {
            float maxOffset = Mathf.Max(0f, height - scrollRect.viewport.rect.height);
            scrollRect.verticalScrollbar.SetValueWithoutNotify(maxOffset > 0f ? 1f - position.y / maxOffset : 1f);
            scrollRect.verticalScrollbar.size = height > 0f
                ? Mathf.Clamp01(scrollRect.viewport.rect.height / height) : 1f;
        }
        if (!CanScroll)
        {
            scrollRect.StopMovement();
        }
    }
}
