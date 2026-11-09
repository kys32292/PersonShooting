// 11/09 - 파일 인코딩 CP949 -> UTF-8 변환 (한글 주석 깨짐 해결)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PerkType
{
    MoveSpeedUp,
    JumpForceUp,
    FireRate,
    MagazineUp,
    DamageUp,
    ScatterShot
}

[CreateAssetMenu(fileName = "NewPerData", menuName = "Perk/PerkData")]
public class PerkData : ScriptableObject
{
    public PerkType perkType;

    public string perkName; // 이름
    public string description; // 설명
    public Sprite icon;
    public float value;
}
