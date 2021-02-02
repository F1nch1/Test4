using System.Text;
using UnityEngine;

[CreateAssetMenu(menuName = "uMMORPG Item/Curio", order = 999)]
public class CurioItem : UsableItem
{
    [Header("Curio")]
    public int usageExperience;

    // usage
    public override void Use(Player player, int inventoryIndex)
    {
        // always call base function too
        base.Use(player, inventoryIndex);

        // decrease amount
        ItemSlot slot = player.study.slots[inventoryIndex];
        slot.DecreaseAmount(1);
        player.study.slots[inventoryIndex] = slot;
    }

    // tooltip
    public override string ToolTip()
    {
        StringBuilder tip = new StringBuilder(base.ToolTip());
        tip.Replace("{USAGEEXPERIENCE}", usageExperience.ToString());
        return tip.ToString();
    }
}
