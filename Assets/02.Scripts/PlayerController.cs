using Fusion;
using TMPro;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 10f; // 뛰기 속도 변수 추가
    [SerializeField] private float killRange = 1.5f; // 킬 가능 거리
    [SerializeField] private TMP_Text myJob; //본인 직업 확인
    [SerializeField] private float KillCooldownTime = 10f;
    private NetworkTransform _nt;

    //Player색 
    [Networked] public Color PlayerColor { get; set; }
    //네트워크 동기화 변수: 임포스터 여부와 생존 여부
    [Networked] public NetworkBool IsImposter { get; set; }
    [Networked] public NetworkBool IsDead { get; set; }
    [Networked] public TickTimer KillTimer { get; set; }
    private MeshRenderer _meshRenderer;
    private ChangeDetector _changeDetector;
    private void Awake()
    {
        _nt = GetComponent<NetworkTransform>();
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        if (HasInputAuthority && IsDead)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                StartSpectating();
            }

        }
    }

    private void UpdateJobUI()
    {
        if (!HasInputAuthority || myJob == null) return;

        if (IsImposter)
        {
            myJob.text = "Imposter";
            myJob.color = Color.red;
        }
        else
        {
            myJob.text = "Crew";
            myJob.color = Color.white;
        }
    }

    public override void Spawned()
    {
        _meshRenderer.material.color = PlayerColor;

        if (HasInputAuthority)
        {
            GameObject uiTextObject = GameObject.Find("JobText");

            if (uiTextObject != null)
            {
                myJob = uiTextObject.GetComponent<TMP_Text>();
                UpdateJobUI();
            }

            CameraFollow camFollow = Camera.main.GetComponent<CameraFollow>();
            if (camFollow != null)
            {
                camFollow.SetTarget(this.transform);
                Debug.Log("카메라 타겟 설정 완료!");
            }
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }

    // [추가] IsDead 변수가 동기화될 때 화면에 그리는(Render) 역할
    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(IsImposter))
            {
                UpdateJobUI();
            }
        }

        if (IsDead && _meshRenderer.enabled)
        {
            _meshRenderer.enabled = false;

            if (HasInputAuthority)
            {
                StartSpectating();
            }
        }
    }
    public override void FixedUpdateNetwork()
    {
        // 1. 죽은 상태라면 조작 불가 (빠져나감)
        if (IsDead || GameManager.Instance.IsGameOver) return;

        // GetInput을 통해 서버로 전송된 내 입력을 꺼내옴
        if (GetInput(out NetworkInputData data))
        {
            // 이동 로직 (기존과 동일)
            float currentSpeed = data.isRunning ? runSpeed : walkSpeed;
            data.movementInput.Normalize();
            Vector3 moveVector = new Vector3(data.movementInput.x, 0, data.movementInput.y);
            transform.Translate(moveVector * currentSpeed * Runner.DeltaTime);

            // [추가] 킬 로직 (서버 권위 검증)
            if (data.killInput && IsImposter)
            {
                // HasStateAuthority: 이 코드를 실행하는 곳이 서버(호스트)인지 확인!
                if (HasStateAuthority)
                {
                    // 타이머가 실행된 적이 없거나, 이미 만료되었을 때만 킬 시도 가능
                    if (KillTimer.ExpiredOrNotRunning(Runner))
                    {
                        TryKillCrewmate();
                    }
                }
            }
        }
    }
    // 서버에서만 실행되는 킬 판정 함수
    private void TryKillCrewmate()
    {
        // 씬 내의 다른 플레이어들을 찾아서 거리 비교
        foreach (var otherPlayer in FindObjectsOfType<PlayerController>())
        {
            // 자기 자신이거나, 죽었거나, 다른 임포스터면 패스
            if (otherPlayer == this || otherPlayer.IsDead || otherPlayer.IsImposter) continue;

            float distance = Vector3.Distance(transform.position, otherPlayer.transform.position);

            // 거리가 1.5m 이내라면 킬 성공!
            if (distance <= killRange)
            {
                otherPlayer.IsDead = true; // 상대방을 죽임 (모든 클라이언트에 동기화됨)
                KillTimer = TickTimer.CreateFromSeconds(Runner, KillCooldownTime); //킬에 성공했다면, 서버에서 타이머를 시작시킵니다.
                Debug.Log($"서버 판정: Player {otherPlayer.Object.InputAuthority}가 죽었습니다.");
                break; // 한 번에 한 명만 죽임
            }
        }
    }
    private void StartSpectating()
    {
        PlayerController[] allPlayer = FindObjectsOfType<PlayerController>();
        System.Collections.Generic.List<PlayerController> alivePlayers =
        new System.Collections.Generic.List<PlayerController>();

        foreach (var p in allPlayer)
        {
            if (!p.IsDead && p != this)
            {
                alivePlayers.Add(p);
            }
        }

        if (alivePlayers.Count > 0)
        {
            PlayerController targetToSpectate =
            alivePlayers[UnityEngine.Random.Range(0, alivePlayers.Count)];

            CameraFollow CamFollow = Camera.main.GetComponent<CameraFollow>();
            if (CamFollow != null)
            {
                CamFollow.SetTarget(targetToSpectate.transform);
            }
        }
        else // 생존자가 0명
        {
            Debug.Log("생존자가 없습니다");
        }
    }
}