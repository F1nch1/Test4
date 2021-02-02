using UnityEngine;
using TMPro;

public partial class UIStatsAndSkills : MonoBehaviour
{

    public KeyCode hotKey = KeyCode.T;
    public TextMeshProUGUI agilityText;
    public TextMeshProUGUI awarenessText;
    public TextMeshProUGUI charismaText;
    public TextMeshProUGUI intelligenceText;
    public TextMeshProUGUI resilienceText;
    public TextMeshProUGUI academiaText;
    public TextMeshProUGUI agricultureText;
    public TextMeshProUGUI animalHandlingText;
    public TextMeshProUGUI athleticsText;
    public TextMeshProUGUI cookingText;
    public TextMeshProUGUI craftingText;
    public TextMeshProUGUI firearmsText;
    public TextMeshProUGUI leadershipText;
    public TextMeshProUGUI mechanicsText;
    public TextMeshProUGUI meleeCombatText;
    public TextMeshProUGUI rangedCombatText;
    public TextMeshProUGUI survivalText;
    public TextMeshProUGUI tailoringText;
    public TextMeshProUGUI unarmedCombatText;
    public TextMeshProUGUI vehiclesText;
    public GameObject panel;
    void Update()
    {
        Player player = Player.localPlayer;
        if (player)
        {
            // hotkey (not while typing in chat, etc.)
            if (Input.GetKeyDown(hotKey) && !UIUtils.AnyInputActive())
                panel.SetActive(!panel.activeSelf);

            // only refresh the panel while it's active
            if (panel.activeSelf)
            {
                agilityText.text = player.traits.agility.ToString();
                awarenessText.text = player.traits.awareness.ToString();
                charismaText.text = player.traits.charisma.ToString();
                intelligenceText.text = player.traits.intelligence.ToString();
                resilienceText.text = player.traits.resilience.ToString();
                academiaText.text = player.bookskills.academia.ToString();
                agricultureText.text = player.bookskills.agriculture.ToString();
                animalHandlingText.text = player.bookskills.animal_handling.ToString();
                athleticsText.text = player.bookskills.athletics.ToString();
                cookingText.text = player.bookskills.cooking.ToString();
                craftingText.text = player.bookskills.crafting.ToString();
                firearmsText.text = player.bookskills.firearms.ToString();
                leadershipText.text = player.bookskills.leadership.ToString();
                mechanicsText.text = player.bookskills.mechanics.ToString();
                meleeCombatText.text = player.bookskills.melee_combat.ToString();
                rangedCombatText.text = player.bookskills.ranged_combat.ToString();
                survivalText.text = player.bookskills.survival.ToString();
                tailoringText.text = player.bookskills.tailoring.ToString();
                unarmedCombatText.text = player.bookskills.unarmed_combat.ToString();
                vehiclesText.text = player.bookskills.vehicles.ToString();


            }
        }
        else panel.SetActive(false);
    }
}
