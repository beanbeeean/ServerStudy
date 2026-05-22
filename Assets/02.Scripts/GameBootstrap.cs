using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine;

public class GameBootstrap : NetworkBehaviour, INetworkRunnerCallbacks
{
    [Header("Settings")]
    [SerializeField] private NetworkObject playerPrefab;

    // 플레이어 리스트 관리 (참조용)
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    public override void Spawned()
    {
        // 씬 오브젝트로 배치된 경우, Runner에 콜백 등록
        Runner.AddCallbacks(this);
    }

    // [중요] 씬 로딩이 완료된 후 호출됨
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer) return; // Host만 권한 가짐

        Debug.Log("Scene Load Done - Spawning Players...");

        // 이미 접속해 있는 모든 플레이어에 대해 캐릭터 생성
        List<PlayerController> spawnedList = new List<PlayerController>();

        foreach (var player in runner.ActivePlayers)
        {
            // SpawnPlayer가 생성된 PlayerController를 반환하도록 수정했다고 가정
            PlayerController pc = SpawnPlayer(runner, player);
            if (pc != null) spawnedList.Add(pc);
        }

        // [추가] 임포스터 랜덤 배정 (서버 권위)
        if (spawnedList.Count > 0)
        {
            int imposterIndex = Random.Range(0, spawnedList.Count);
            spawnedList[imposterIndex].IsImposter = true;
            Debug.Log($"서버: Player {spawnedList[imposterIndex].Object.InputAuthority}를 임포스터로 지정했습니다.");
        }
    }

    // SpawnPlayer 반환형을 PlayerController로 변경하여 활용
    private PlayerController SpawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        Vector3 spawnPos = new Vector3(Random.Range(-3, 3), 0, Random.Range(-3, 3));

        NetworkObject networkPlayerObject = runner.Spawn(
            playerPrefab, spawnPos, Quaternion.identity, player,
            (runner, obj) =>
            {
                PlayerController pc = obj.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.PlayerColor = rainbowColors[Random.Range(0, rainbowColors.Length)];
                }
            }
        );

        _spawnedCharacters.Add(player, networkPlayerObject);
        return networkPlayerObject.GetComponent<PlayerController>();
    }


    private readonly Color[] rainbowColors = new Color[]
    {
        Color.red, new Color(1f, 0.5f, 0f), Color.yellow,
        Color.green, Color.blue, new Color(0.29f, 0f, 0.51f), new Color(0.56f, 0f, 1f)
    };

    // 게임 도중 새로 들어온 플레이어 처리
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer) SpawnPlayer(runner, player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer && _spawnedCharacters.TryGetValue(player, out var networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }

    #region Unused Callbacks
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        data.movementInput.x = Input.GetAxisRaw("Horizontal");
        data.movementInput.y = Input.GetAxisRaw("Vertical");

        data.isRunning = Input.GetKey(KeyCode.LeftShift);

        data.killInput = Input.GetKey(KeyCode.F);


        input.Set(data); // 서버로 전송
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }
    // ... 나머지 인터페이스 메서드들 (공백으로 유지) ...
    #endregion
}