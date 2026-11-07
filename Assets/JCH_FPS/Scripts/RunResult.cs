using System.Collections.Generic;

// 2026-11-07: 게임 오버 시 GameManager가 채워 두고 Ending 씬(EndingController)이 읽어가는 이번 판 기록.
// 씬이 바뀌어도 유지되도록 static으로 둔다.
public static class RunResult
{
    public static float SurvivalTime;        // 초 (일시정지/특성 선택 시간 제외)
    public static float Score;               // 이번 판에 얻은 경험치 총합
    public static int Level;
    public static List<PerkData> Perks = new List<PerkData>(); // 선택한 순서대로

    public static void Set(float survivalTime, float score, int level, List<PerkData> perks)
    {
        SurvivalTime = survivalTime;
        Score = score;
        Level = level;
        Perks = new List<PerkData>(perks);
    }
}
