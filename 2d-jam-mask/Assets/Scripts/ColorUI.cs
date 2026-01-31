using UnityEngine;
using UnityEngine.UI;

public class ColorUI : MonoBehaviour
{
    [SerializeField] private Image selectedColour;
    [SerializeField] private Image otherColour;




    private void OnSwitch(Color color,Color color2)
    {
        selectedColour.color = color;
        otherColour.color = color2;
    }

}
