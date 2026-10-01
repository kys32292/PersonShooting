using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMove : MonoBehaviour, IDamageable
{
    [Header("체력")]
    public float maxHP = 100f;
    private float currentHP = 0f;

    [Header("이동 속도")]
    [SerializeField]
    private float moveSpeed = 3f;

    private Rigidbody rb;
    private Collider col;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        mainCamera = Camera.main;

        currentHP = maxHP;
        currentAmmo = magazineSize;

        RefreshHPUI();
        RefreshAmmoUI(currentAmmo + " : " + magazineSize);
    }

    // Update is called once per frame
    void Update()
    {
        if(GameManager.Instance != null && GameManager.Instance.isPaused)
        {
            return;
        }

        MovePlayer(InputMoveKey());
        RotatePlayer();

        FireGun();
        CountFireRate();
        Reload();
    }

    // 이동 ------------------------------------

    [Header("점프 높이")]
    [SerializeField]
    private float jumpForce = 7f;

    void MovePlayer(Vector3 dir)
    {
        float yVelocity = rb.velocity.y;

        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            yVelocity = jumpForce;
        }

        rb.velocity = new Vector3(dir.x * moveSpeed, yVelocity, dir.z * moveSpeed); 
    }

    Vector3 InputMoveKey()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 inputDir = (transform.forward * vertical + transform.right * horizontal).normalized;

        return inputDir;
    }

    [Header("땅 레이어 설정")]
    [SerializeField]
    private LayerMask groundLayer;

    bool IsGrounded()
    {
        if (col == null) return false;

        // 1. 콜라이더의 가장 밑면(발바닥) 중심점 구하기
        Vector3 bottomCenter = new Vector3(col.bounds.center.x, col.bounds.min.y + 0.05f, col.bounds.center.z);

        // 2. 발바닥에서 아래로 0.1m만 Ray를 쏴서 바닥 레이어와 닿았는지 확인
        return Physics.Raycast(bottomCenter, Vector3.down, 0.1f, groundLayer);
    }

    // 좌우 회전 ----------------------------------

    [Header("마우스 감도")]
    public float mouseSensitivity = 3f;

    float xRot;
    float yRot;

    [Header("플레이어의 팔")]
    [SerializeField]
    private Transform playerArm;

    [Header("상, 하 제한 각도")]
    public float minAngle = -90f;
    public float maxAngle = 90f;

    void RotatePlayer()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRot -= mouseY;
        xRot = Mathf.Clamp(xRot, minAngle, maxAngle);

        playerArm.localRotation = Quaternion.Euler(xRot, 0, 0);

        yRot += mouseX;
        transform.rotation = Quaternion.Euler(0, yRot, 0);
    }

    // 총 발사 ----------------------------------------

    private Camera mainCamera;

    [Header("사거리")]
    [SerializeField]
    private float Range = 100f;

    [Header("데미지")]
    [SerializeField]
    private int damage = 10;

    [Header("발사 간격")]
    [SerializeField]
    private float fireRate = 0.2f;
    private float fireRateTime = 0f;

    [Header("탄창 크기")]
    [SerializeField]
    private int magazineSize = 50;
    private int currentAmmo = 0;

    [Header("장전 시간")]
    [SerializeField]
    private float reload = 2f;
    private float reloadTime = 0f;

    [Header("감지 레이어")]
    private LayerMask hitLayer;

    [Header("총알 오브젝트")]
    [SerializeField]
    private GameObject bulletPrefab;

    [Header("총알 발사 위치")]
    [SerializeField]
    Transform MuzzleTrans;

    [Header("총알 발사 수")]
    [SerializeField]
    private int bulletCount = 1;

    [SerializeField]
    private float spreadAngle = 60f;

    private bool canFire = true;

    private bool isReloading = false;

    void FireGun()
    {
        if (Input.GetButton("Fire1") && canFire && currentAmmo > 0)
        {
            currentAmmo--;

            Ray ray = mainCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1f));
            Vector3 targetPoint;

            if(Physics.Raycast(ray, out RaycastHit hit, 1000f)) // 1000의 거리에 레이케스트 발사
            {
                targetPoint = hit.point; // 부딫힐 시 그곳을 타깃으로
            }
            else
            {
                targetPoint = ray.GetPoint(Range); // 부딫히지 않을 시 최대 거리를 타깃으로
            }

            Vector3 fireDir = (targetPoint - MuzzleTrans.position).normalized; // 발사 방향

            /*
            Quaternion fireRot = Quaternion.LookRotation(fireDir);

            GameObject bullet = Instantiate(bulletPrefab, MuzzleTrans.position, fireRot);
            */

            for (int i = 0; i < bulletCount; i++) // 산탄
            {
                float angleOffset = 0f; // 기본 0

                if(bulletCount > 1) // 총알이 추가 될 경우 오프셋 값을 추가
                {
                    float step = spreadAngle / (bulletCount - 1);
                    angleOffset = -(spreadAngle / 2f) + (step * i);
                }

                Vector3 finalDir = Quaternion.Euler(0f, angleOffset, 0f) * fireDir;
                Quaternion fireRot = Quaternion.LookRotation(finalDir);

                GameObject bullet = Instantiate(bulletPrefab, MuzzleTrans.position, fireRot);
            }

            canFire = false;

            RefreshAmmoUI(currentAmmo + " : " + magazineSize);
        }
    }

    /*
    Vector3 CameraCenter()
    {
        return Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 1f)); // 카메라 정중앙 기준 1미터 앞
    }
    */

    private void Reload()
    {
        if (Input.GetKeyDown(KeyCode.R) && !isReloading)
        {
            canFire = false;
            isReloading = true;
        }

        if (isReloading)
        {
            RefreshAmmoUI("Reloading...");

            if(Timer(ref reloadTime, reload))
            {
                currentAmmo = magazineSize;
                isReloading = false;
                canFire = true;

                RefreshAmmoUI(currentAmmo + " : " + magazineSize);
            }
        }
    }

    void CountFireRate()
    {
        if (!canFire && !isReloading)
        {
            if(Timer(ref fireRateTime, fireRate))
            {
                canFire = true;
            }
        }
    }

    // 특성 적용 ------------------------------------------------

    [Header("특성 추가 수치 값")]
    private float bonusMoveSpeed = 0f;
    private float bonusJumpForce = 0f;
    private float bonusFireRate = 0f;
    public int bonusBulletCount = 0;

    public void ApplyPerk(PerkData perk)
    {
        switch (perk.perkType)
        {
            case PerkType.MoveSpeedUp:
                moveSpeed += perk.value;
                break;
            case PerkType.JumpForceUp:
                jumpForce = Mathf.Min(15f, jumpForce + perk.value);
                break;
            case PerkType.FireRate:
                fireRate = Mathf.Max(0.05f, fireRate - perk.value);
                break;
            case PerkType.DamageUp:
                damage += (int)perk.value;
                break;
            case PerkType.MagazineUp:

                break;
            case PerkType.ScatterShot:
                bulletCount += (int)perk.value;
                break;
        }
    }

    // UI -------------------------------------------------------

    [Header("HP 바")]
    [SerializeField]
    private Image hpBar;

    [Header("총알 Text")]
    [SerializeField]
    private TMP_Text ammoText;

    private void RefreshHPUI()
    {
        if (hpBar != null)
        {
            hpBar.fillAmount = currentHP / maxHP;
        }
    }

    private void RefreshAmmoUI(string printText)
    {
        if(ammoText != null)
        {
            ammoText.text = printText;
        }
    }

    // 데미지, 게임 오버 처리 ----------------------------------------

    public bool IsDead => currentHP <= 0;

    public void TakeDamage(float _damage)
    {
        if(IsDead) // 이미 죽었을 경우 중복 작동을 방지하기 위해
        {
            return;
        }

        currentHP -= _damage;
        currentHP = Mathf.Max(currentHP, 0); // currentHP 값이 음수로 내려가지 않게 하기 위함

        RefreshHPUI();

        if (IsDead) // 데미지를 받은 뒤 죽었을 경우 Die를 호출
        {
            Die();
        }
    }

    private void Die()
    {
        GameManager.Instance.GameOver();
    }


    // 시간 계산 -----------------------------------------

    private bool Timer(ref float timer, float time)
    {
        timer += Time.deltaTime;

        if (timer >= time)
        {
            timer = 0;

            return true;
        }

        return false;
    }
}
