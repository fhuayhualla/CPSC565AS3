using UnityEngine;
using TMPro;
using Antymology.Terrain;

// Using TMPro.
public class NestBlockCounterUI : MonoBehaviour
{
    public TextMeshProUGUI nestBlockCountText;

    void Update()
    {
        if (nestBlockCountText != null)
        {
            // Call the new CountNestBlocks method in WorldManager.
            nestBlockCountText.text = "Nest Blocks: " + WorldManager.Instance.CountNestBlocks();
        }
    }
}
