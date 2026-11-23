// 11/15 - 화면 흔들림 (보스 돌진 피격 등)
// CameraShake.Shake(세기, 시간)으로 어디서든 호출. 메인 카메라에 컴포넌트가 없으면 자동으로 붙음.
// 2026-11-23: 기존 보스 호출은 위치 흔들림을 유지하고 플레이어 피격은 명시한 카메라에 작은 각도도 더합니다.
using UnityEngine;

[DefaultExecutionOrder(10000)] // 2026-11-23: 일반 이동/시점 갱신 후 최종 카메라 오프셋을 적용합니다.
public class CameraShake : MonoBehaviour
{
    private float amplitude = 0f;
    private float duration = 0f;
    private float timer = 0f;

    private Vector3 basePosition;
    // 2026-11-23: 플레이어 시점은 카메라의 부모를 회전시키므로 카메라 자체의 로컬 각도만 잠깐 흔듭니다.
    private Quaternion baseRotation;
    private float rotationAmplitude;
    private bool isShaking = false;

    public static void Shake(float amplitude, float duration)
    {
        // 2026-11-23: 기존 보스/투사체 호출의 세기와 위치 전용 동작을 유지합니다.
        Shake(Camera.main, amplitude, duration, 0f);
    }

    // 2026-11-23: 플레이어/테스트 씬은 실제 사용하는 카메라를 전달하여 MainCamera 태그에만 의존하지 않습니다.
    public static void Shake(Camera cam, float amplitude, float duration, float rotationDegrees = 0f)
    {
        if (cam == null || duration <= 0f || (amplitude <= 0f && rotationDegrees <= 0f)) return;
        CameraShake shake = cam.GetComponent<CameraShake>();
        if (shake == null)
        {
            shake = cam.gameObject.AddComponent<CameraShake>();
        }

        // 2026-11-23: 카메라에 비활성화된 컴포넌트가 남아 있어도 LateUpdate가 실행되게 합니다.
        shake.enabled = true;
        shake.Begin(Mathf.Max(0f, amplitude), duration, Mathf.Max(0f, rotationDegrees));
    }

    // 2026-11-23: 중복 피격은 더 강한 위치/각도와 남은 시간을 유지하며 기준 자세를 다시 캡처하지 않습니다.
    private void Begin(float _amplitude, float _duration, float _rotationAmplitude)
    {
        if (!isShaking)
        {
            basePosition = transform.localPosition;
            baseRotation = transform.localRotation;
            isShaking = true;
            amplitude = _amplitude;
            rotationAmplitude = _rotationAmplitude;
            duration = _duration;
            timer = 0f;
            return;
        }

        // 흔들리는 중에 다시 호출되면 더 센 쪽을 이어서 사용
        float remaining = duration - timer;
        if (_amplitude >= amplitude || _duration > remaining || _rotationAmplitude > rotationAmplitude)
        {
            amplitude = Mathf.Max(_amplitude, amplitude);
            rotationAmplitude = Mathf.Max(_rotationAmplitude, rotationAmplitude);
            duration = Mathf.Max(_duration, remaining);
            timer = 0f;
        }
    }

    // 2026-11-23: 경과 시간을 적용 뒤 증가시켜 낮은 FPS에서도 최초 한 프레임의 흔들림은 보장합니다.
    private void LateUpdate()
    {
        if (!isShaking) return;

        if (GameManager.Instance != null && GameManager.Instance.isPaused)
        {
            RestorePose();
            return;
        }

        if (timer >= duration)
        {
            RestorePose();
            isShaking = false;
            return;
        }

        float strength = amplitude * (1f - timer / duration); // 점점 약해짐
        transform.localPosition = basePosition + Random.insideUnitSphere * strength;
        // 2026-11-23: 작은 pitch/yaw/roll로 먼 배경에서도 피격을 느끼게 하며 조작 회전값은 수정하지 않습니다.
        if (rotationAmplitude > 0f)
        {
            Vector3 angles = Random.insideUnitSphere * (rotationAmplitude * (1f - timer / duration));
            transform.localRotation = baseRotation * Quaternion.Euler(angles);
        }
        timer += Time.deltaTime;
    }

    // 2026-11-23: 위치 전용 보스 흔들림은 기존처럼 회전에 손대지 않습니다.
    private void RestorePose()
    {
        transform.localPosition = basePosition;
        if (rotationAmplitude > 0f) transform.localRotation = baseRotation;
    }

    // 2026-11-23: 비활성화/씬 종료 시 카메라에 오프셋이 남지 않도록 원래 자세로 복구합니다.
    private void OnDisable()
    {
        if (isShaking)
        {
            RestorePose();
            isShaking = false;
        }
    }
}
