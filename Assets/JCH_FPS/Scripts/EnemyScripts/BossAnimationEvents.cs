// 11/15 - 보스는 Spitter와 같은 Monster01 클립(Shoot, Die)을 쓰는데, 그 클립에 Spitter용 애니메이션 이벤트가 들어 있음
// 받는 함수가 없으면 "AnimationEvent has no receiver" 에러가 나므로 빈 함수로 받아서 무시함
// (보스의 공격/사망 처리는 BossEnemy가 직접 함) - Tools/Boss 메뉴가 보스 모델의 Animator 오브젝트에 붙임
using UnityEngine;

public class BossAnimationEvents : MonoBehaviour
{
    public void Spitter_Attack() { }

    public void Spitter_Die() { }
}
