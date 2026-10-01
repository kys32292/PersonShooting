using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PerkUIController : MonoBehaviour
{
    [SerializeField] private GameObject uiPanel;
    [SerializeField] private List<Button> perkButtons; // 버튼 3개 연결
    [SerializeField] private List<TextMeshProUGUI> perkNameTexts; // 텍스트 3개 연결

    public void DisplayPerks(List<PerkData> perks)
    {
        uiPanel.SetActive(true);

        for (int i = 0; i < perkButtons.Count; i++)
        {
            if (i < perks.Count)
            {
                perkButtons[i].gameObject.SetActive(true);

                PerkData perk = perks[i];
                perkNameTexts[i].text = perk.perkName;

                // 버튼 클릭 이벤트 리스너 재설정
                perkButtons[i].onClick.RemoveAllListeners();
                perkButtons[i].onClick.AddListener(() => GameManager.Instance.SelectPerk(perk));
            }
            else
            {
                perkButtons[i].gameObject.SetActive(false);
            }
        }
    }

    public void CloseUI()
    {
        uiPanel.SetActive(false);
    }
}
