using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
public class FighterController : MonoBehaviour, IDamageable
{
    [Header("Settings")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Hit Stop Settings")]
    [SerializeField] private float lightHitStopDuration = 0.1f; // Czas zatrzymania przy lekkim uderzeni
    [SerializeField] private float heavyHitStopDuration = 0.3f; // Czas zatrzymania przy mocnym uderzeniu
    [SerializeField] private float heavyHitThreshold = 15f;

    [SerializeField] private CinemachineImpulseSource impulseSource; // Shake kamery
    [SerializeField] private float lightShakeForce = 0.5f; // Lekki wstrzas kamery 
    [SerializeField] private float heavyShakeForce = 2.0f; // Ciezki wstrzas kamery

    [Header("Block Settings")]
    [SerializeField] private GameObject blockEffectPrefab; // VFX Bloku
    [SerializeField] private Transform blockEffectPoint; // Miejsce efektu bloku
    [SerializeField] private float blockKnockback = 5f; // Odleglosc odepchniecia przy bloku
    [SerializeField] private float hitKnockback = 1.0f; // Odleglosc odepchniecia przy zadaniu obrazen
    [SerializeField] private float chipDamagePercentage = 0.1f; // Procent obrazen otrzymanych po bloku
    [Space(10)]
    [SerializeField] private GameObject bloodEffectPrefab; // VFX otrzymania obrazen
    [SerializeField] private Transform hitCenterPoint; // Miejsce efektu otrzymania obrazen

    [SerializeField] private float facingRotation = 90f; // Obrot postaci na scenie

    [Header("Combat References")]
    [SerializeField] private Hitbox attack1Hitbox; // Hitbox ataku 1
    [SerializeField] private Hitbox attack2Hitbox; // Hitbox ataku 2

    [Header("Environment Settings")]
    [SerializeField] private LayerMask wallLayer; // Warstwa niewidzialnych scian
    [SerializeField] private float wallCheckDistance = 1.5f;

    [Header("AI Agent")]
    public FighterAgent myAgent; // Agent AI przekazujacy nagrody i kary

    [Header("Movement Tweaks")]
    [SerializeField] private float rootMotionSpeedMultiplier = 1.0f; // Predkosc poruszania sie

    [Header("Pushbox Settings")]
    [SerializeField] private float minDistanceBetweenPlayers = 0.8f; // Srednica collidera postaci
    public FighterController opponent; // Przeciwnik

    [Header("Visuals")]
    [SerializeField] private Renderer characterRenderer; // Przypisz mesh postaci (SkinnedMeshRenderer)
    [SerializeField] private Material[] colorMaterials; // Tablica materia³ów (taka sama kolejnoœæ jak w Menu)

    // Stany postaci
    private float currentHealth; // Aktualne zdrowie
    private float moveInput; // Kierunek poruszania sie 
    private bool isBlocking; // Czy postac blokuje
    private bool canMove = true; // Czy postac moze sie ruszac
    private bool isPaused = false; // Pauza gry
    private bool canCombo = false; // Kontrola wykonania combo
    private bool isDead = false; // Czy postac zostala pokonana

    private Animator animator;
    public Rigidbody rb;

    // Hashe animacji dla wydajnosci
    private static readonly int AnimMove = Animator.StringToHash("Move");
    private static readonly int AnimAttack1 = Animator.StringToHash("Attack1");
    private static readonly int AnimAttack2 = Animator.StringToHash("Attack2");
    private static readonly int AnimBlock = Animator.StringToHash("Block");
    private static readonly int AnimHit = Animator.StringToHash("TakeHit");
    private static readonly int AnimDeath = Animator.StringToHash("Death");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        currentHealth = maxHealth;
        // Blokada rotacji i osi Z
        rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
    }

    private void LateUpdate()
    {
        // Korekta fizyki wykonywana po wszystkich innych ruchach
        PreventCrossOver();
    }


    private void PreventCrossOver()
    {
        if (opponent == null) return;

        // Pozycja postaci
        Vector3 myPos = transform.position;
        Vector3 opPos = opponent.transform.position;

        // Obliczenie dystansu miedzy postaciami
        float distanceX = Mathf.Abs(myPos.x - opPos.x);

        // Rozepchniecie postaci jesli sa zbyt blisko siebie
        if (distanceX < minDistanceBetweenPlayers)
        {
            float centerPoint = (myPos.x + opPos.x) / 2f;
            float pushDir = (myPos.x < opPos.x) ? -1f : 1f;
            float newX = centerPoint + (pushDir * (minDistanceBetweenPlayers / 2f));
            transform.position = new Vector3(newX, myPos.y, myPos.z);
            // Zerowanie predkosci aby nie doszlo do konfliktu z silnikiem fizyki
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    private void Start()
    {
        transform.rotation = Quaternion.Euler(0, facingRotation, 0);
        if (attack1Hitbox != null) attack1Hitbox.Initialize(this.gameObject);
        if (attack2Hitbox != null) attack2Hitbox.Initialize(this.gameObject);
        if (impulseSource == null) impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    private void Update()
    {
        HandleAnimationLogic();
        if (transform.position.y < -10f)
        {
            rb.linearVelocity = Vector3.zero; // Wyzeruj pêd
            Die();
        }
    }

    public void SetMoveInput(float value)
    {
        if (!canMove)
        {
            moveInput = 0f;
            return;
        }
        moveInput = value;
    }


    private void HandleAnimationLogic()
    {
        if (isPaused) return;

        // Zatrzymanie postaci podczas blokowania, blok nadpisuej chodzenie, blokowanie ruchu podczas smierci lub otrzymania ciosu.
        if (!canMove || isBlocking)
        {
            animator.SetFloat(AnimMove, 0f);
            return;
        }
        // Przekazanie kierunku ruchu do animatora
        animator.SetFloat(AnimMove, moveInput);

        // Ustawienie kierunku obrotu postaci
        transform.rotation = Quaternion.Euler(0, facingRotation, 0);
    }


    private void OnAnimatorMove()
    {
        if (isPaused || Time.deltaTime < 0.001f) return;

        // Blokowanie slizgania sie i odbijania
        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        // Pobranie przesuniecia postaci w klatce
        Vector3 deltaMove = animator.deltaPosition;

        // Dodanie mnoznika predkosci podczas ruchu
        if (moveInput != 0)
        {
            deltaMove *= rootMotionSpeedMultiplier;
        }

        // Obliczenie nowej pozycji
        Vector3 nextPosition = rb.position + deltaMove;

        // Korekta osi Z
        nextPosition.z = 0;

        // Wykonanie ruchu
        rb.MovePosition(nextPosition);
    }


    public void PerformAttack()
    {
        if (isBlocking || !canMove) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // Kontrola wykonania ataku 2 w oknie combo przy wykonywaniu ataku 1
        if (stateInfo.IsName("Attack_1"))
        {
            // Combo Window
            if (canCombo)
            {
                animator.SetTrigger(AnimAttack2);
                canCombo = false;
            }
            return;
        }
        // Blokada Inputu ataku w trakcie wykonania animacji  2 ataku.
        else if (stateInfo.IsName("Attack_2"))
        {
            return;
        }
        // Start pierwszego ataku
        else
        {
            animator.SetTrigger(AnimAttack1);
        }
    }


    public void SetBlock(bool blocking)
    {
        if (!canMove && blocking) return;
        isBlocking = blocking;
        animator.SetBool(AnimBlock, isBlocking);
    }


    public bool TakeDamage(float damageAmount)
    {
        if (isDead) return false;

        // Sprawdzamy czy cios jest mocny (dla Shake i HitStop)
        bool isHeavy = damageAmount >= heavyHitThreshold;

        // ---------------- LOGIKA BLOKU ----------------
        if (isBlocking)
        {
            // Opcjonalnie: Minimalny wstrz¹s przy bloku (daje fajny feeling ciê¿aru)
            //if (impulseSource != null) impulseSource.GenerateImpulse(0.01f);

            HandleBlockReaction(damageAmount);
            if (currentHealth <= 0)
            {
                // Jeœli Chip Damage nas dobi³:

                // Opcjonalnie: Mocniejszy shake przy œmierci przez blok
                if (impulseSource != null) impulseSource.GenerateImpulse(heavyShakeForce);

                Die();
            }
            else
            {
                // Jeœli ¿yjemy po Chip Damage -> Standardowa reakcja bloku
                StopAllCoroutines();
                StartCoroutine(ApplyKnockback(blockKnockback, 0.1f));
                DisableAttack1();
                DisableAttack2();
            }

            return true; // Cios zablokowany
        }

        // ---------------- LOGIKA TRAFIENIA ----------------
        DisableAttack1();
        DisableAttack2();
        canCombo = false;

        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(0, currentHealth);
        
        /*
        if (BackgroundManager.Instance != null)
        {
            // Zawsze dodajemy plamê w tle przy trafieniu
            BackgroundManager.Instance.SpawnBackgroundSplat();
        }
        */

        // [AI] Kara za otrzymanie obrazen
        if (myAgent != null) myAgent.AddReward(-1.0f);

        // Odepchniêcie
        StartCoroutine(ApplyKnockback(hitKnockback, 0.15f));

        // VFX Krwi
        /*
        if (bloodEffectPrefab != null)
        {
            Vector3 spawnPos = hitCenterPoint != null ? hitCenterPoint.position : transform.position + Vector3.up;
            GameObject vfx = Instantiate(bloodEffectPrefab, spawnPos, Quaternion.identity);
            Destroy(vfx, 1.0f);
        }
        */

        // --- CAMERA SHAKE (Wstrz¹s Kamery) ---
        /*
        if (impulseSource != null && PlayerPrefs.GetInt("CameraShake", 1) == 1)
        {
            // Wybieramy si³ê w zale¿noœci od obra¿eñ
            float shakeForce = isHeavy ? heavyShakeForce : lightShakeForce;
            // Generujemy impuls (Mno¿y Default Velocity z komponentu przez shakeForce)
            impulseSource.GenerateImpulse(shakeForce);
        }
        */

        // Sprawdzenie œmierci
        if (currentHealth <= 0)
        {
            // Przy œmierci opcjonalnie: EKSTREMALNY wstrz¹s
            if (impulseSource != null) impulseSource.GenerateImpulse(heavyShakeForce * 2.0f);

            Die();
            // HitStop przy œmierci jest pomijany na rzecz Slow Motion z GameManagera
        }
        else
        {
            animator.SetTrigger(AnimHit);

            /*
            // --- HIT STOP (Zatrzymanie Czasu) ---
            if (GameManager.Instance != null)
            {
                float stopDuration = isHeavy ? heavyHitStopDuration : lightHitStopDuration;
                GameManager.Instance.DoHitStop(stopDuration);
            }
            */
        }

        return false;
    }

    /// <summary>
    /// Sprawdza, czy postaæ jest w trakcie wykonywania animacji ataku.
    /// </summary>
    public bool IsAttacking()
    {
        if (animator == null) return false;
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        // Sprawdzamy nazwy stanów (musz¹ pasowaæ do tych w Animatorze)
        return info.IsName("Attack_1") || info.IsName("Attack_2");
    }

    /// <summary>
    /// Logika redukcji obrazen przy bloku
    /// </summary>
    private void HandleBlockReaction(float rawDamage)
    {
        // Redukcja obrazen
        float reducedDamage = rawDamage * chipDamagePercentage;
        currentHealth -= reducedDamage;
        currentHealth = Mathf.Max(0, currentHealth);

        // [AI]Nagroda za blok
        if (myAgent != null)
        {
            myAgent.AddReward(1.5f);
            myAgent.LogBlock();
        }

        // VFX
        if (blockEffectPrefab != null && blockEffectPoint != null)
        {
            GameObject vfxInstance = Instantiate(blockEffectPrefab, blockEffectPoint.position, Quaternion.identity);
            Destroy(vfxInstance, 1.0f);
        }

        // Odrzut
        //StartCoroutine(BlockKnockbackRoutine());
    }

    // Plynne przesuniecie podczas bloku
    private IEnumerator BlockKnockbackRoutine()
    {
        float timer = 0f;
        float duration = 0.1f;

        while (timer < duration)
        {
            transform.Translate(Vector3.back * blockKnockback * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }
    }

    // Kontrola pozycji postaci podczas odepchniecia
    private IEnumerator ApplyKnockback(float force, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            // Sprawdzenie wystapienia sciany, aby nie wpychaæ postaci w œcianê
            Vector3 rayOrigin = transform.position + Vector3.up;
            Vector3 rayDirection = -transform.forward;
            float checkDistance = 0.2f;

            bool hitWall = Physics.Raycast(rayOrigin, rayDirection, checkDistance, wallLayer);

            if (!hitWall)
            {
                transform.Translate(Vector3.back * force * Time.deltaTime);
            }

            timer += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// Smierc postaci. Blokada sterowania i informacja dla GameManagera
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        //StartCoroutine(ApplyKnockback(blockKnockback, 1f));
        isDead = true;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.DisablePauseForMatchEnd();
        }
        SetCanMove(false);
        animator.SetTrigger(AnimDeath);
        DisableAttack1();
        DisableAttack2();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayerDied(this);
        }
    }

    /// <summary>
    /// [AI]Wykrywa trafienie we wroga. Wywo³ywana przez Hitbox.
    /// </summary>
    public void RegisterHit()
    {
        if (myAgent != null)
        {
            myAgent.LogHit();
        }
    }

    // Funkcje dla Animation Events
    // Attack_1
    public void EnableAttack1()
    {
        if (attack1Hitbox != null) attack1Hitbox.EnableHitbox();
    }

    public void DisableAttack1()
    {
        if (attack1Hitbox != null) attack1Hitbox.DisableHitbox();
    }

    // Attack_2
    public void EnableAttack2()
    {
        if (attack2Hitbox != null) attack2Hitbox.EnableHitbox();
    }

    public void DisableAttack2()
    {
        if (attack2Hitbox != null) attack2Hitbox.DisableHitbox();
    }

    // Zatrzymanie czasu przy uderzeniu
    private IEnumerator HitStopEffect(float duration)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        if (!isPaused) Time.timeScale = 1f;
    }

    /// <summary>
    /// Otwiera okno czasowe na Combo.
    /// </summary>
    public void OpenComboWindow()
    {
        canCombo = true;
    }

    /// <summary>
    /// Zamyka okno czasowe na Combo.
    /// </summary>
    public void CloseComboWindow()
    {
        canCombo = false;
    }

    /// <summary>
    /// Resetuje stan zdrowia i flagi.
    /// </summary>
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;
        isBlocking = false;
        animator.SetBool(AnimBlock, false);
        canMove = true;
        canCombo = false;
        DisableAttack1();
        DisableAttack2();
    }

    /// <summary>
    /// Resetuje stan animatora.
    /// </summary>
    public void ResetAnimator()
    {
        animator.Rebind();
        animator.Update(0f);
    }
    /// <summary>
    /// Wlacza lub wylacza poruszanie sie postaci.
    /// </summary>
    public void SetCanMove(bool state)
    {
        canMove = state;
        if (!state)
        {
            moveInput = 0;
            animator.SetFloat(AnimMove, 0);

            // Wymuœ opuszczenie gardy
            SetBlock(false);
        }
    }

    // Getter aktualnego zdrowia postaci
    public float GetCurrentHealth()
    {
        return currentHealth;
    }

    // Ustawienie koloru materialu
    public void SetColor(int colorIndex)
    {
        if (characterRenderer != null && colorMaterials != null && colorIndex < colorMaterials.Length)
        {
            characterRenderer.material = colorMaterials[colorIndex];
        }
    }

    // Getter zdrowia 
    public float GetMaxHealth()
    {
        return maxHealth;
    }

    public bool IsBlocking => isBlocking;
}
