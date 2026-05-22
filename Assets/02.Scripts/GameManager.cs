using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : NetworkBehaviour 
{
    public static GameManager Instance { get; private set; }
    public TMP_Text TimeOver;
    public GameObject resultpannel;
    public TMP_Text resultUiText;
    public Button okButton;
    public int lobbySceneIndex = 0;

    [Networked] public float GameTimer { get; set; } = 90f;
    [Networked] public NetworkBool IsGameOver { get; set; }

    public void Start()
    {
        resultUiText.text = "";
        resultpannel.SetActive(false);

        okButton.onClick.RemoveAllListeners();
        okButton.onClick.AddListener(OnClickReturnToLobby);
    }

    public override void Spawned()
    {
        Instance = this;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || IsGameOver)
        {
            return;
        }

        CheckWin();

        if (GameTimer > 0)
        {
            GameTimer -= Runner.DeltaTime;
        }
        else
        {
            TriggerGameOver("Crew WIN");
        }
    }

    public void TriggerGameOver(string resultMessage)
    {
        IsGameOver = true;
        Debug.Log($"게임종료 : {resultMessage}");

        Rpc_AnnounceResult(resultMessage);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_AnnounceResult(string message)
    {
        Debug.Log($"게임 종료 결과: {message}");

        // 모든 유저의 컴퓨터에서 UI가 켜지도록 코드 실행함
        if (resultpannel != null)
            resultpannel.SetActive(true);

        if (resultUiText != null)
            resultUiText.text = message;
    }

    private void CheckWin()
    {
        int aliveCrewCount = 0;
        int aliveImposterCount = 0;

        PlayerController[] allPlayers = FindObjectsOfType<PlayerController>();

        foreach (var player in allPlayers)
        {
            if (player.IsDead) continue;

            if (player.IsImposter)
                aliveImposterCount++;
            else
                aliveCrewCount++;
        }

        // 혼자 접속해 있거나 로딩 중일 때 즉시 게임이 끝나버리는 버그 방지
        if (aliveImposterCount + aliveCrewCount <= 1) return;

        if (aliveImposterCount >= aliveCrewCount && aliveImposterCount > 0)
        {
            TriggerGameOver("Imposter WIN");
        }

    }
    
    
    private async void OnClickReturnToLobby()
    {
        // 중복 클릭 방지 및 상태 표시
        if (okButton != null)
            okButton.interactable = false;

        if (resultUiText != null)
            resultUiText.text = "로비로 이동 중...";

        // Runner 객체가 파괴되기 전에 필요한 정보 백업
        int sceneToLoad = lobbySceneIndex;
        NetworkRunner currentRunner = Runner;

        if (currentRunner != null)
        {
            // 네트워크 연결 안전하게 종료
            await currentRunner.Shutdown();

            // 종료 후 잔여 객체가 씬에 남아있다면 완전히 파괴
            if (currentRunner != null && currentRunner.gameObject != null)
            {
                Destroy(currentRunner.gameObject);
            }
        }

        // 로비 씬으로 이동
        SceneManager.LoadScene(sceneToLoad);
    }
}
