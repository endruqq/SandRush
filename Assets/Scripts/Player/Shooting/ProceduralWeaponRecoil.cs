using UnityEngine;

public class ProceduralWeaponRecoil : MonoBehaviour
{
    [Header("Recoil Angles")]
    [SerializeField] private float _restingX = -89.98f;
    [SerializeField] private float _recoilTargetX = -94.39f;

    [Header("Recoil Durations (in seconds)")]
    [Tooltip("Czas trwania odskoku broni w górę.")]
    [SerializeField] private float _recoilDuration = 0.08f;
    
    [Tooltip("Czas powrotu broni do pozycji spoczynkowej.")]
    [SerializeField] private float _recoveryDuration = 0.25f;

    private enum RecoilState { Idle, Kick, Recover }
    private RecoilState _state = RecoilState.Idle;

    private float _currentX;
    private float _startAngle;
    private float _timeInState;
    private bool _isSubscribed = false;

    private void Start()
    {
        _currentX = _restingX;
        ApplyRotation(_currentX);
        
        TrySubscribe();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Update()
    {
        // Próba ponownej subskrypcji, jeśli gracz nie był zainicjalizowany w Start/OnEnable
        if (!_isSubscribed)
        {
            TrySubscribe();
        }

        if (_state == RecoilState.Idle) return;

        _timeInState += Time.deltaTime;

        if (_state == RecoilState.Kick)
        {
            float t = _timeInState / _recoilDuration;
            if (t >= 1f)
            {
                t = 1f;
                _state = RecoilState.Recover;
                _timeInState = 0f;
                _startAngle = _currentX;
            }
            // Używamy płynnego przejścia (SmoothStep) dla delikatnego początku i końca odskoku
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            _currentX = Mathf.Lerp(_startAngle, _recoilTargetX, smoothT);
        }
        else if (_state == RecoilState.Recover)
        {
            float t = _timeInState / _recoveryDuration;
            if (t >= 1f)
            {
                t = 1f;
                _state = RecoilState.Idle;
            }
            // Płynny powrót
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            _currentX = Mathf.Lerp(_startAngle, _restingX, smoothT);
        }

        ApplyRotation(_currentX);
    }

    public void TriggerRecoil()
    {
        _state = RecoilState.Kick;
        _timeInState = 0f;
        _startAngle = _currentX; // Zaczynamy od obecnego kąta, zapobiegając "teleportacji" przy szybkim strzelaniu
    }

    private void TrySubscribe()
    {
        if (_isSubscribed) return;

        if (Player.Instance != null && Player.Instance.Shooting != null)
        {
            Player.Instance.Shooting.OnShoot += TriggerRecoil;
            _isSubscribed = true;
        }
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed) return;

        if (Player.Instance != null && Player.Instance.Shooting != null)
        {
            Player.Instance.Shooting.OnShoot -= TriggerRecoil;
        }
        _isSubscribed = false;
    }

    private void ApplyRotation(float xRot)
    {
        Vector3 localEuler = transform.localEulerAngles;
        localEuler.x = xRot;
        transform.localEulerAngles = localEuler;
    }
}
