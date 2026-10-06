using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("����ġ, ����")]
    public float currentEXP = 0;
    public float maxEXP = 100;
    public int currentLeve = 1;

    [Header("Ư�� �����ͺ��̽�")]
    [SerializeField] private List<PerkData> allPerks;

    [Header("UI ����")]
    [SerializeField] private PerkUIController perkUI;

    [Header("����ġ UI")]
    [SerializeField] private Image expBar;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text expValueText;

    private PlayerMove playerPrefab;

    public bool isPaused = false;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }

        perkUI = GetComponent<PerkUIController>();

        perkUI.CloseUI();

        RefreshEXPUI();
    }

    public void GameStarted()
    {
        playerPrefab = FindObjectOfType<PlayerMove>();
    }

    public void AddEXP(float exp)
    {
        currentEXP += exp;

        if(currentEXP >= maxEXP)
        {
            LevelUp();
        }

        RefreshEXPUI();
    }

    public void LevelUp()
    {
        currentLeve++;
        currentEXP -= maxEXP;
        maxEXP = maxEXP * 1.2f;

        ShowPerkSelection();
    }

    private void RefreshEXPUI()
    {
        if (expBar != null)
        {
            expBar.fillAmount = currentEXP / maxEXP;
        }

        if (levelText != null)
        {
            levelText.text = "Lv. " + currentLeve;
        }

        if (expValueText != null)
        {
            expValueText.text = Mathf.FloorToInt(currentEXP) + " / " + Mathf.CeilToInt(maxEXP);
        }
    }

    private void ShowPerkSelection()
    {
        // 1. ��ü Ư�� ��� �� �ߺ� ���� 3�� �̱�
        List<PerkData> selectedPerks = GetRandomPerks(3);

        // 2. UI ����
        if (perkUI != null)
        {
            perkUI.DisplayPerks(selectedPerks);
        }
    }

    // �ߺ� ���� 3�� �̱� ����
    private List<PerkData> GetRandomPerks(int count)
    {
        List<PerkData> tempList = new List<PerkData>(allPerks);
        List<PerkData> result = new List<PerkData>();

        for (int i = 0; i < count; i++)
        {
            if (tempList.Count == 0) break;

            int randomIndex = Random.Range(0, tempList.Count);
            result.Add(tempList[randomIndex]);

            // �̹� ���� �׸��� �ӽ� ����Ʈ���� �����Ͽ� �ߺ� ����
            tempList.RemoveAt(randomIndex);
        }

        return result;
    }

    // �÷��̾ Ư���� �������� �� ȣ���
    public void SelectPerk(PerkData perk)
    {
        PlayerMove player = FindObjectOfType<PlayerMove>();
        if (player != null)
        {
            player.ApplyPerk(perk);
        }

        // ���� �簳 �� UI �ݱ�
        Time.timeScale = 1f;
        perkUI.CloseUI();
    }

    public void GameOver()
    {

    }

    private void ResetData()
    {
        currentEXP = 0;
        maxEXP = 100;
        currentLeve = 1;
    }
}
