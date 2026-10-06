using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMove : MonoBehaviour, IDamageable
{
    [Header("ü��")]
    public float maxHP = 100f;
    private float currentHP = 0f;

    [Header("�̵� �ӵ�")]
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

    // �̵� ------------------------------------

    [Header("���� ����")]
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

    [Header("�� ���̾� ����")]
    [SerializeField]
    private LayerMask groundLayer;

    bool IsGrounded()
    {
        if (col == null) return false;

        // 1. �ݶ��̴��� ���� �ظ�(�߹ٴ�) �߽��� ���ϱ�
        Vector3 bottomCenter = new Vector3(col.bounds.center.x, col.bounds.min.y + 0.05f, col.bounds.center.z);

        // 2. �߹ٴڿ��� �Ʒ��� 0.1m�� Ray�� ���� �ٴ� ���̾�� ��Ҵ��� Ȯ��
        return Physics.Raycast(bottomCenter, Vector3.down, 0.1f, groundLayer);
    }

    // �¿� ȸ�� ----------------------------------

    [Header("���콺 ����")]
    public float mouseSensitivity = 3f;

    float xRot;
    float yRot;

    [Header("�÷��̾��� ��")]
    [SerializeField]
    private Transform playerArm;

    [Header("��, �� ���� ����")]
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

    // �� �߻� ----------------------------------------

    private Camera mainCamera;

    [Header("��Ÿ�")]
    [SerializeField]
    private float Range = 100f;

    [Header("������")]
    [SerializeField]
    private int damage = 10;

    [Header("�߻� ����")]
    [SerializeField]
    private float fireRate = 0.2f;
    private float fireRateTime = 0f;

    [Header("źâ ũ��")]
    [SerializeField]
    private int magazineSize = 50;
    private int currentAmmo = 0;

    [Header("���� �ð�")]
    [SerializeField]
    private float reload = 2f;
    private float reloadTime = 0f;

    [Header("���� ���̾�")]
    private LayerMask hitLayer;

    [Header("�Ѿ� ������Ʈ")]
    [SerializeField]
    private GameObject bulletPrefab;

    [Header("�Ѿ� �߻� ��ġ")]
    [SerializeField]
    Transform MuzzleTrans;

    [Header("�Ѿ� �߻� ��")]
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

            if(Physics.Raycast(ray, out RaycastHit hit, 1000f)) // 1000�� �Ÿ��� �����ɽ�Ʈ �߻�
            {
                targetPoint = hit.point; // �΋H�� �� �װ��� Ÿ������
            }
            else
            {
                targetPoint = ray.GetPoint(Range); // �΋H���� ���� �� �ִ� �Ÿ��� Ÿ������
            }

            Vector3 fireDir = (targetPoint - MuzzleTrans.position).normalized; // �߻� ����

            /*
            Quaternion fireRot = Quaternion.LookRotation(fireDir);

            GameObject bullet = Instantiate(bulletPrefab, MuzzleTrans.position, fireRot);
            */

            for (int i = 0; i < bulletCount; i++) // ��ź
            {
                float angleOffset = 0f; // �⺻ 0

                if(bulletCount > 1) // �Ѿ��� �߰� �� ��� ������ ���� �߰�
                {
                    float step = spreadAngle / (bulletCount - 1);
                    angleOffset = -(spreadAngle / 2f) + (step * i);
                }

                Vector3 finalDir = Quaternion.Euler(0f, angleOffset, 0f) * fireDir;
                Quaternion fireRot = Quaternion.LookRotation(finalDir);

                GameObject bullet = Instantiate(bulletPrefab, MuzzleTrans.position, fireRot);

                Bullet bulletComp = bullet.GetComponent<Bullet>();
                if (bulletComp != null)
                {
                    bulletComp.owner = gameObject;
                }
            }

            canFire = false;

            RefreshAmmoUI(currentAmmo + " : " + magazineSize);
        }
    }

    /*
    Vector3 CameraCenter()
    {
        return Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 1f)); // ī�޶� ���߾� ���� 1���� ��
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

    // Ư�� ���� ------------------------------------------------

    [Header("Ư�� �߰� ��ġ ��")]
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

    [Header("HP ��")]
    [SerializeField]
    private Image hpBar;

    [SerializeField]
    private TMP_Text hpText;

    [Header("�Ѿ� Text")]
    [SerializeField]
    private TMP_Text ammoText;

    private void RefreshHPUI()
    {
        if (hpBar != null)
        {
            hpBar.fillAmount = currentHP / maxHP;
        }

        if (hpText != null)
        {
            hpText.text = Mathf.CeilToInt(currentHP).ToString();
        }
    }

    private void RefreshAmmoUI(string printText)
    {
        if(ammoText != null)
        {
            ammoText.text = printText;
        }
    }

    // ������, ���� ���� ó�� ----------------------------------------

    public bool IsDead => currentHP <= 0;

    public void TakeDamage(float _damage)
    {
        if(IsDead) // �̹� �׾��� ��� �ߺ� �۵��� �����ϱ� ����
        {
            return;
        }

        currentHP -= _damage;
        currentHP = Mathf.Max(currentHP, 0); // currentHP ���� ������ �������� �ʰ� �ϱ� ����

        RefreshHPUI();

        if (IsDead) // �������� ���� �� �׾��� ��� Die�� ȣ��
        {
            Die();
        }
    }

    private void Die()
    {
        GameManager.Instance.GameOver();
    }


    // �ð� ��� -----------------------------------------

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
