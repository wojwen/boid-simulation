using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Allows for changing and displaying Boid count in a simulation.
/// </summary>
public class BoidCountController : MonoBehaviour
{
    /// <summary>Slider for selecting Boid count.</summary>
    [SerializeField] private Slider boidCountSlider;

    /// <summary>Slider for applying Boid count selected on the slider.</summary>
    [SerializeField] private Button applyButton;

    /// <summary>Text showing the value selected on the slider.</summary>
    [SerializeField] private TextMeshProUGUI sliderText;

    /// <summary>Simulation for which Boid count will be changed.</summary>
    [SerializeField] private BoidSimulation simulation;

    /// <summary>
    /// Displays initial Boid count.
    /// </summary>
    private void Start()
    {
        boidCountSlider.value = simulation.GetBoidCount();
    }

    /// <summary>
    /// Registers listeners.
    /// </summary>
    private void OnEnable()
    {
        applyButton.onClick.AddListener(SetBoidCount);
        boidCountSlider.onValueChanged.AddListener(SetSliderText);
    }

    /// <summary>
    /// Removes listeners.
    /// </summary>
    private void OnDisable()
    {
        applyButton.onClick.RemoveListener(SetBoidCount);
        boidCountSlider.onValueChanged.RemoveListener(SetSliderText);
    }

    /// <summary>
    /// 
    /// </summary>
    private void SetBoidCount()
    {
        var newBoidCount = (int)boidCountSlider.value;
        if (simulation.GetBoidCount() != newBoidCount)
            simulation.ChangeBoidCount(newBoidCount);
    }

    private void SetSliderText(float value)
    {
        sliderText.text = ((int)value).ToString();
    }
}