using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CreatureUIManager : MonoBehaviour
{
    [Header("UI References")]
    public Canvas uiCanvas;
    public GameObject statsPanel;
    public Slider hungerBar;
    public Slider loveBar;
    public TextMeshProUGUI hungerText;
    public TextMeshProUGUI loveText;
    public TextMeshProUGUI stateText;

    void Awake()
    {
        // Initialize static UI references for all creature UI canvases
        CreatureUICanvas.uiCanvas = uiCanvas;
        CreatureUICanvas.statsPanel = statsPanel;
        CreatureUICanvas.hungerBar = hungerBar;
        CreatureUICanvas.loveBar = loveBar;
        CreatureUICanvas.hungerText = hungerText;
        CreatureUICanvas.loveText = loveText;
        CreatureUICanvas.stateText = stateText;
        
        // Initially hide the stats panel
        if (statsPanel != null)
        {
            statsPanel.SetActive(false);
        }
    }
} 