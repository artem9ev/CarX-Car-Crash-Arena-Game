using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Фазы матча. PostCombat — экран результатов после истечения таймера боя.
/// Переход обратно в лобби намеренно не реализован — подключить отдельно позже.
/// </summary>
public enum MatchPhase : byte
{
    Combat = 0,
    PostCombat = 1
}

/// <summary>
/// Серверный таймер матча. Помести на in-scene placed NetworkObject (как ScoreManager) —
/// один экземпляр на сцену.
///
/// Логика: сервер тикает _timeRemaining в Update, при достижении нуля переключает _phase
/// в PostCombat. Клиенты только читают NetworkVariable — своей логики отсчёта не ведут,
/// чтобы не рассинхронизироваться с сервером.
/// </summary>
public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance { get; private set; }

    [Header("Настройки матча")]
    [SerializeField, Min(1f)] private float _matchDuration = 300f;

    [Tooltip("Сколько секунд держим экран результатов, прежде чем можно будет перейти в лобби (переход пока не реализован)")]
    [SerializeField, Min(0f)] private float _postCombatDuration = 15f;

    private readonly NetworkVariable<float> _timeRemaining = new NetworkVariable<float>();
    private readonly NetworkVariable<MatchPhase> _phase = new NetworkVariable<MatchPhase>(MatchPhase.Combat);

    /// <summary>Оставшееся время текущей фазы в секундах. Актуально только во время Combat.</summary>
    public float TimeRemaining => _timeRemaining.Value;
    public MatchPhase Phase => _phase.Value;

    /// <summary>Стреляет на любом клиенте (и хосте) при смене фазы матча.</summary>
    public event UnityAction<MatchPhase> OnPhaseChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _timeRemaining.Value = _matchDuration;
            _phase.Value = MatchPhase.Combat;
        }

        _phase.OnValueChanged += HandlePhaseChanged;

        // Применяем текущее состояние сразу — важно для клиентов, подключившихся
        // уже после того, как матч перешёл в PostCombat (late join).
        HandlePhaseChanged(_phase.Value, _phase.Value);
    }

    public override void OnNetworkDespawn()
    {
        _phase.OnValueChanged -= HandlePhaseChanged;
    }

    private void Update()
    {
        if (!IsServer) return;
        if (_phase.Value != MatchPhase.Combat) return;

        _timeRemaining.Value -= Time.deltaTime;
        if (_timeRemaining.Value <= 0f)
        {
            _timeRemaining.Value = 0f;
            StartPostCombat();
        }
    }

    private void StartPostCombat()
    {
        if (!IsServer) return;

        _phase.Value = MatchPhase.PostCombat;

        // TODO: через _postCombatDuration секунд здесь должен начаться переход в лобби
        // (NetworkManager.SceneManager.LoadScene(...) и т.п.) — намеренно не реализовано.
    }

    private void HandlePhaseChanged(MatchPhase oldPhase, MatchPhase newPhase)
    {
        OnPhaseChanged?.Invoke(newPhase);
    }
}