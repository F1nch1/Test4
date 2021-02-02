using UnityEngine;
using UnityEngine.UI;

public partial class UIHealthMana : MonoBehaviour
{
    public GameObject panel;
    public Slider healthSlider;
    public Text healthStatus;
    public Slider manaSlider;
    public Text manaStatus;
    public Slider hydrationSlider;
    public Text hydrationStatus;
    public Slider nutritionSlider;
    public Text nutritionStatus;

    void Update()
    {
        Player player = Player.localPlayer;
        if (player != null)
        {
            panel.SetActive(true);
            healthSlider.value = player.health.Percent();
            healthStatus.text = player.health.current + " / " + player.health.max;

            manaSlider.value = player.endurance.Percent();
            manaStatus.text = player.endurance.current + " / " + player.endurance.max;

            hydrationSlider.value = player.hydration.Percent();
            hydrationStatus.text = player.hydration.current + " / " + player.hydration.max;

            nutritionSlider.value = player.nutrition.Percent();
            nutritionStatus.text = player.nutrition.current + " / " + player.nutrition.max;
        }
        else panel.SetActive(false);
    }
}
