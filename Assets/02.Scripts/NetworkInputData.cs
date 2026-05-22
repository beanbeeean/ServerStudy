using Fusion;
using UnityEngine;

// 퓨전 전용 입력 구조체 (NetworkInput)
public struct NetworkInputData : INetworkInput
{
    public Vector2 movementInput; // 이동 입력 (WASD)

    public NetworkBool isRunning;
    public NetworkBool killInput;
}