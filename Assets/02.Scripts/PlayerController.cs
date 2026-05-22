using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float killRange = 1.5f;
    [SerializeField] private TMP_Text myJob;
    [SerializeField] private float killCooldownTime = 10f;
    private NetworkTransform _nt;

    [Networked] public Color PlayerColor { get; set; }
    [Networked] public NetworkBool IsImposter { get; set; }
    [Networked] public NetworkBool IsDead { get; set; }
    [Networked] public TickTimer KillTimer { get; set; }

    private MeshRenderer _meshRenderer;
    private ChangeDetector _changeDetector;

    private void Awake()
    {
        _nt = GetComponent<NetworkTransform>();
        _meshRenderer = GetComponentInChildren<MeshRenderer>();
    }

    private void Update()
    {
        if (HasInputAuthority && IsDead)
        {
            if (Input.GetMouseButton(0) || Input.GetKeyDown(KeyCode.Space))
            {

                StartSpectating();
            }
        }
    }

    public override void Spawned()
    {
        _meshRenderer.material.color = PlayerColor;

        if (HasInputAuthority)
        {
            GameObject uiTextObject = GameObject.Find("JobText");
            myJob = uiTextObject.GetComponent<TMP_Text>();
            myJob.text = "";

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

            CameraFollow camFollow = Camera.main.GetComponent<CameraFollow>();
            if (camFollow != null)
            {
                camFollow.SetTarget(this.transform);
                Debug.Log("카메라 타겟 설정 완료!");
            }
        }

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

    }

    public override void Render()
    {
        if (IsDead && _meshRenderer.enabled)
        {
            _meshRenderer.enabled = false;
            if (HasInputAuthority)
            {
                // StartSpectating();
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (IsDead) return;
        // GetInput을 통해 해당 객체의 InputAuthority를 가진 유저의 입력을 가져옴
        if (GetInput(out NetworkInputData data))
        {
            data.movementInput.Normalize();
            Vector3 moveVector = new Vector3(data.movementInput.x, 0, data.movementInput.y);

            float currentSpeed = data.isRunning ? runSpeed : moveSpeed;

            // 단순 이동 구현 (NetworkTransform이 이 변화를 감지하고 동기화함)
            transform.Translate(moveVector * currentSpeed * Runner.DeltaTime);

            if (data.killInput && IsImposter)
            {
                if (KillTimer.ExpiredOrNotRunning(Runner))
                {
                    TryKillCrewmate();
                }
            }
        }
    }


    private void TryKillCrewmate()
    {
        foreach (var otherPlayer in FindObjectsOfType<PlayerController>())
        {
            if (otherPlayer == this || otherPlayer.IsDead || otherPlayer.IsImposter) continue;

            float distance = Vector3.Distance(transform.position, otherPlayer.transform.position);

            // 거리가 1.5m 이내라면 킬 성공!
            if (distance <= killRange)
            {
                otherPlayer.IsDead = true; // 상대방을 죽임 (모든 클라이언트에 동기화됨)
                KillTimer = TickTimer.CreateFromSeconds(Runner, killCooldownTime);
                Debug.Log($"서버 판정: Player {otherPlayer.Object.InputAuthority}가 죽었습니다.");
                break; // 한 번에 한 명만 죽임
            }
        }
    }

    private void StartSpectating()
    {
        PlayerController[] allPlayer = FindObjectsOfType<PlayerController>();
        List<PlayerController> alivePlayers = new List<PlayerController>();

        foreach (var p in allPlayer)
        {
            if (!p.IsDead && p != this)
            {
                alivePlayers.Add(p);
            }
        }
        
        if(alivePlayers.Count > 0)
        {
            PlayerController targetToSpectate = alivePlayers[Random.Range(0, alivePlayers.Count)];
            CameraFollow CamFollow = Camera.main.GetComponent<CameraFollow>();
            if(CamFollow != null)
            {
                CamFollow.SetTarget(targetToSpectate.transform);
            }

        }
        else
        {
            Debug.Log("생존자가 없습니다.");
        }
    }



}