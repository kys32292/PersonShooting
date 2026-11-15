// 11/15 - 화면 흔들림 (보스 돌진 피격 등)
// CameraShake.Shake(세기, 시간)으로 어디서든 호출. 메인 카메라에 컴포넌트가 없으면 자동으로 붙음.
// 회전은 PlayerMove가 매 프레임 덮어쓰므로 건드리지 않고 localPosition만 잠깐 흔든 뒤 원래 위치로 되돌림
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    private float amplitude = 0f;
    private float duration = 0f;
    private float timer = 0f;

    private Vector3 basePosition;
    private bool isShaking = false;

    public static void Shake(float amplitude, float duration)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        CameraShake shake = cam.GetComponent<CameraShake>();
        if (shake == null)
        {
            shake = cam.gameObject.AddComponent<CameraShake>();
        }

        shake.Begin(amplitude, duration);
    }

    private void Begin(float _amplitude, float _duration)
    {
        if (!isShaking)
        {
            basePosition = transform.localPosition;
            isShaking = true;
            amplitude = _amplitude;
            duration = _duration;
            timer = 0f;
            return;
        }

        // 흔들리는 중에 다시 호출되면 더 센 쪽을 이어서 사용
        float remaining = duration - timer;
        if (_amplitude >= amplitude || _duration > remaining)
        {
            amplitude = Mathf.Max(_amplitude, amplitude);
            duration = Mathf.Max(_duration, remaining);
            timer = 0f;
        }
    }

    private void LateUpdate()
    {
        if (!isShaking) return;

        if (GameManager.Instance != null && GameManager.Instance.isPaused)
        {
            transform.localPosition = basePosition;
            return;
        }

        timer += Time.deltaTime;

        if (timer >= duration)
        {
            transform.localPosition = basePosition;
            isShaking = false;
            return;
        }

        float strength = amplitude * (1f - timer / duration); // 점점 약해짐
        transform.localPosition = basePosition + Random.insideUnitSphere * strength;
    }

    private void OnDisable()
    {
        if (isShaking)
        {
            transform.localPosition = basePosition;
            isShaking = false;
        }
    }
}
