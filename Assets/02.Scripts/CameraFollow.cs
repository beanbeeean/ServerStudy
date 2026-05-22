using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target; // 카메라가 따라갈 대상
    public Vector3 offset = new Vector3(0, 10f, -8f); // 대상으로부터의 거리 (탑다운 뷰 느낌)
    public float smoothSpeed = 10f; // 카메라가 따라가는 부드러운 정도

    // 물리 이동(FixedUpdate)이 끝난 후 카메라가 따라가야 덜 떨립니다.
    private void LateUpdate()
    {
        if (target == null) return;

        // 목표 위치 계산
        Vector3 desiredPosition = target.position + offset;

        // 부드럽게 이동 (Lerp)
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            Time.deltaTime * smoothSpeed
        );

        // 항상 타겟을 바라보도록 회전 (선택 사항)
        transform.LookAt(target);
    }

    // 타겟을 변경하는 함수 (스폰될 때, 혹은 죽어서 관전할 때 호출)
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}