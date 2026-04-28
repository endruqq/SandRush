using UnityEngine;
using System.Collections;

public class BossController : MonoBehaviour
{
    [Header("Boss Stats")]
    [SerializeField] private float _maxHealth = 1000f;
    private float _currentHealth;

    [Header("Attack Settings")]
    [Tooltip("Zasięg od bossa w którym budzi się i rozpoczyna walkę jak Gracz podejdzie")]
    [SerializeField] private float _activationRadius = 25f;
    [Tooltip("Ile czasu Boss nic nie robi przed kolejnym atakiem piorunów")]
    [SerializeField] private float _idleDuration = 5f;
    [Tooltip("Ile piorunów ma spawnować w jednym ataku (ile na raz)")]
    [SerializeField] private int _lightningStrikesCount = 3;
    [Tooltip("Opóźnienie między uderzeniami. Ustaw na 0, jeśli chcesz by wszystkie _lightningStrikesCount spadły dokładnie w tej samej milisekundzie!")]
    [SerializeField] private float _strikeSpawnDelay = 0.3f;
    [Tooltip("Promień dookoła BOSSA w którym pioruny zaczną spadać z nieba (ustaw duże by pokryć arenę)")]
    [SerializeField] private float _attackRadius = 15f;
    [Tooltip("Ile % rzuconych piorunów ma mieć złośliwego AIMBOTA na gracza doganiając go w biegu? (0 = totalny deszcz chaosu w ciemno na arenie, 1 = cała wiązka perfekcyjnie skumulowana 3 metry od gracza)")]
    [Range(0f, 1f)]
    [SerializeField] private float _playerBias = 0.7f;

    [Header("Dependencies")]
    [SerializeField] private Animator _animator;
    [Tooltip("Mechanika samego piorunu (przeciągnij swój utworzony Prefab LightningStrike)")]
    [SerializeField] private GameObject _lightningStrikePrefab;
    [SerializeField] private HitFlash _hitFlash;
    [Tooltip("Nazwa parametru Triggera w Twoim Animatorze (np. Attack)")]
    [SerializeField] private string _attackAnimTrigger = "Attack";

    [Header("Effects")]
    [SerializeField] private string _deathSound = "event:/Boss_Death";
    [SerializeField] private string _attackSound = "event:/Boss_Attack";

    private bool _isDead = false;
    private bool _isActivated = false;
    private float _idleTimer;
    private bool _isAttacking = false;
    private Player _cachedPlayer;

    public float HealthPercent => _maxHealth > 0 ? _currentHealth / _maxHealth : 0;

    private void Awake()
    {
        _currentHealth = _maxHealth;
        _idleTimer = _idleDuration;
        if (_hitFlash == null) _hitFlash = GetComponent<HitFlash>();
    }

    private void Update()
    {
        if (_isDead || _isAttacking) return;

        // Leniwa weryfikacja cache
        if (_cachedPlayer == null) _cachedPlayer = FindFirstObjectByType<Player>();

        // System Aktywacji (Zaczyna walkę dopiero jak gracz wejdzie w dany zasięg)
        if (!_isActivated)
        {
            if (_cachedPlayer != null)
            {
                float distToPlayer = Vector3.Distance(transform.position, _cachedPlayer.transform.position);
                if (distToPlayer <= _activationRadius)
                {
                    _isActivated = true;
                    Debug.Log("BOSS FIGHT STARTED!");
                }
            }
            return; // Kończymy pętlę dopóki nie zostanie aktywowany
        }

        // Odliczanie do ataku "Machając Ogonem"
        _idleTimer -= Time.deltaTime;
        
        if (_idleTimer <= 0)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;

        if (_animator != null)
        {
            _animator.SetTrigger(_attackAnimTrigger);
        }

        if (!string.IsNullOrEmpty(_attackSound))
        {
            FMODHelper.PlayOneShot(_attackSound, transform.position);
        }

        // Czekamy chwilę aż Twoja animacja "macha ogonem" dojdzie do odpowiedniej klatki przed zrespieniem piorunów!
        yield return new WaitForSeconds(0.8f);

        if (_lightningStrikePrefab != null)
        {
            // Pętla tworząca burzę
            for (int i = 0; i < _lightningStrikesCount; i++)
            {
                if (_isDead) break; 
                
                Vector3 targetPos;

                // Sprawdzamy zgodnie ze sztuczną inteligencją - czy obecny piorun skupia się tylko na pościgu gracza?
                bool huntPlayer = Random.value <= _playerBias;

                if (huntPlayer)
                {
                    // Ciasny promień wokół wciąż biegnącego gracza (pod nogi)
                    Vector2 rand = Random.insideUnitCircle * 4f;
                    targetPos = _cachedPlayer.transform.position + new Vector3(rand.x, 0, rand.y);
                }
                else
                {
                    // Ślepy rzut gdziekolwiek po całej arenie (buduje w tle wizualny deszcz chaosu)
                    Vector2 rand = Random.insideUnitCircle * _attackRadius;
                    targetPos = transform.position + new Vector3(rand.x, 0, rand.y);
                }

                // NIEPRZEKRACZALNA GRANICA: Upewniamy się, że Piorun NIE ZAATAKUJE poza ustaloną ogromną Strefą Bossa,
                // dzięki temu możesz uciec poza zasięg i tam się chować przed obrażeniami.
                Vector3 distanceToBoss = targetPos - transform.position;
                distanceToBoss.y = 0;
                
                if (distanceToBoss.magnitude > _attackRadius)
                {
                    targetPos = transform.position + distanceToBoss.normalized * _attackRadius;
                }

                // Rzutujemy sztuczny promień by dopasować Y na pofalowanej ziemi
                if (Physics.Raycast(targetPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 15f))
                {
                    targetPos = hit.point + Vector3.up * 0.05f;
                }

                // Generowanie gotowego pioruna w nowym miejscu
                Instantiate(_lightningStrikePrefab, targetPos, Quaternion.identity);
                
                // Opóźnienie pomiędzy kolejnymi piorunami (przy 0 spadnie cała chmara naraz!)
                if (_strikeSpawnDelay > 0)
                {
                    yield return new WaitForSeconds(_strikeSpawnDelay);
                }
            }
        }

        // Odczekaj jeszcze moment by boss "ochłonął" by nie ruszył timera od nowa od razu
        yield return new WaitForSeconds(1.5f);

        _idleTimer = _idleDuration;
        _isAttacking = false;
    }

    // Wykorzystywane przez Bullet.cs rykoszetem (SendMessageUpwards)
    public void TakeDamage(float amount)
    {
        TakeDamage(amount, Vector3.zero);
    }

    public void TakeDamage(float amount, Vector3 hitDirection)
    {
        if (_isDead) return;

        _currentHealth -= amount;

        // Feedback wizualny trafienia
        if (_hitFlash != null) _hitFlash.Flash();
        if (ScreenFlash.Instance != null) ScreenFlash.Instance.Flash();

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        _isDead = true;

        // Przerywa w locie ataki i burze jeśli zdechł w trackie 
        StopAllCoroutines();

        if (HitStopManager.Instance != null)
        {
            HitStopManager.Instance.TriggerSlowMo(0.7f, 0.4f); // Większe slow-mo na śmierć bossa
        }
        
        Player.TriggerHeavyCameraShake();
        Player.TriggerHeavyCameraShake(); // Podwójne dla epickości

        if (!string.IsNullOrEmpty(_deathSound))
        {
            FMODHelper.PlayOneShot(_deathSound, transform.position);
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(4f); // Zostaw model bossa przez chwilę zanim zupełnie zniknie
        gameObject.SetActive(false);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Strefa rażenia w trakcie ataku
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);
        UnityEditor.Handles.color = new Color(0.2f, 0.8f, 1f, 0.5f);
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, _attackRadius);

        // Strefa inicjacji walki (uruchomienia bossa)
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.2f);
        UnityEditor.Handles.color = new Color(1f, 0.2f, 0.2f, 0.4f);
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, _activationRadius);
    }
#endif
}
