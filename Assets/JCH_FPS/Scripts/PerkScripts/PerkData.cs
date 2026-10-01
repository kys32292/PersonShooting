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
