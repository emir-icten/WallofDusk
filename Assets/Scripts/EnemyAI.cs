using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("Hedef")]
    public Transform target;          // Base binası

    [Header("Hareket Ayarları")]
    public float moveSpeed = 3f;
    public float stopDistance = 1.5f; // Base'e çok yapışmasın diye

    private void Update()
    {
        if (target == null) return;

        Vector3 dir = target.position - transform.position;
        dir.y = 0f; // Y eksenini sabit tut, düz zeminde yürü

        float sqrDist = dir.sqrMagnitude;
        if (sqrDist <= stopDistance * stopDistance)
            return; // yeterince yaklaştı, dur

        Vector3 moveDir = dir.normalized;

        transform.position += moveDir * moveSpeed * Time.deltaTime;

        // Yönünü hedefe çevir
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, 10f * Time.deltaTime);
        }
    }
}
