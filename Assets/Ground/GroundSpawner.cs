using System.Collections.Generic;
using UnityEngine;

public class PlatformSpawner2D : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;

    [Header("Prefabs")]
    [SerializeField] private GameObject groundPrefab;
    [SerializeField] private GameObject springGroundPrefab;
    [SerializeField] private GameObject thornGroundPrefab;
    [SerializeField] private GameObject moveGroundPrefab;

    [Header("Spawn Area")]
    [SerializeField] private float xMargin = 0.6f;
    [SerializeField] private float spawnAhead = 12f;
    [SerializeField] private float destroyBelow = 8f;

    [Header("Step (Reachability)")]
    [SerializeField] private float minStepY = 1.2f;
    [SerializeField] private float maxStepY = 2.6f;
    [SerializeField] private float maxStepX = 3.0f;

    [Header("Chances")]
    [Range(0f, 1f)]
    [SerializeField] private float springChance = 0.15f;

    [Range(0f, 1f)]
    [SerializeField] private float thornChance = 0.08f;

    [Range(0f, 1f)]
    [SerializeField] private float moveChance = 0.12f;

    [Header("Cooldown (avoid consecutive spawns)")]
    [SerializeField] private int springCooldownCount = 3;
    [SerializeField] private int thornCooldownCount = 4;
    [SerializeField] private int moveCooldownCount = 2;

    [Header("Initial Platforms")]
    [SerializeField] private int initialCount = 12;
    [SerializeField] private float initialStartY = -2f;
    [SerializeField] private float initialEndY = 12f;

    private readonly List<GameObject> spawned = new();

    private float nextSpawnY;
    private float lastX;

    private int springCooldownLeft;
    private int thornCooldownLeft;
    private int moveCooldownLeft;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
    }

    private void Start()
    {
        if (initialCount > 0)
        {
            float y = initialStartY;
            lastX = 0f;

            for (int i = 0; i < initialCount; i++)
            {
                float t = (initialCount == 1) ? 1f : (float)i / (initialCount - 1);
                y = Mathf.Lerp(initialStartY, initialEndY, t);

                float x = GetNextX(lastX);
                SpawnPlatform(new Vector2(x, y), forceGround: true);
                lastX = x;
            }

            nextSpawnY = initialEndY;
        }
        else
        {
            nextSpawnY = transform.position.y;
            lastX = transform.position.x;
        }
    }

    private void Update()
    {
        if (targetCamera == null) return;

        float camTopY = GetCameraTopY();
        float targetMaxY = camTopY + spawnAhead;

        while (nextSpawnY < targetMaxY)
        {
            float dy = Random.Range(minStepY, maxStepY);
            nextSpawnY += dy;

            float x = GetNextX(lastX);
            SpawnPlatform(new Vector2(x, nextSpawnY), forceGround: false);
            lastX = x;
        }

        Cleanup();
    }

    private void SpawnPlatform(Vector2 pos, bool forceGround)
    {
        GameObject prefab = ChoosePrefab(forceGround);
        if (prefab == null) return;

        var go = Instantiate(prefab, pos, Quaternion.identity);
        spawned.Add(go);
    }

    private GameObject ChoosePrefab(bool forceGround)
    {
        if (forceGround) return groundPrefab;

        // cooldown消化
        if (springCooldownLeft > 0) springCooldownLeft--;
        if (thornCooldownLeft > 0) thornCooldownLeft--;
        if (moveCooldownLeft > 0) moveCooldownLeft--;

        bool canSpring = springGroundPrefab != null && springCooldownLeft <= 0;
        bool canThorn = thornGroundPrefab != null && thornCooldownLeft <= 0;
        bool canMove = moveGroundPrefab != null && moveCooldownLeft <= 0;

        // 優先順は「トゲ→動く→バネ→通常」にしています（好みで変えてOK）
        if (canThorn && Random.value < thornChance)
        {
            thornCooldownLeft = thornCooldownCount;
            return thornGroundPrefab;
        }

        if (canMove && Random.value < moveChance)
        {
            moveCooldownLeft = moveCooldownCount;
            return moveGroundPrefab;
        }

        if (canSpring && Random.value < springChance)
        {
            springCooldownLeft = springCooldownCount;
            return springGroundPrefab;
        }

        return groundPrefab;
    }

    private float GetNextX(float prevX)
    {
        float left = GetCameraLeftX() + xMargin;
        float right = GetCameraRightX() - xMargin;

        float x = Random.Range(left, right);

        // 直前足場からの到達可能範囲にクランプ
        float reachLeft = prevX - maxStepX;
        float reachRight = prevX + maxStepX;

        x = Mathf.Clamp(x, reachLeft, reachRight);
        x = Mathf.Clamp(x, left, right);

        return x;
    }

    private void Cleanup()
    {
        float camBottomY = GetCameraBottomY();
        float limitY = camBottomY - destroyBelow;

        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            var go = spawned[i];
            if (go == null)
            {
                spawned.RemoveAt(i);
                continue;
            }

            if (go.transform.position.y < limitY)
            {
                Destroy(go);
                spawned.RemoveAt(i);
            }
        }
    }

    private float GetCameraTopY()
    {
        float halfH = targetCamera.orthographicSize;
        return targetCamera.transform.position.y + halfH;
    }

    private float GetCameraBottomY()
    {
        float halfH = targetCamera.orthographicSize;
        return targetCamera.transform.position.y - halfH;
    }

    private float GetCameraLeftX()
    {
        float halfH = targetCamera.orthographicSize;
        float halfW = halfH * targetCamera.aspect;
        return targetCamera.transform.position.x - halfW;
    }

    private float GetCameraRightX()
    {
        float halfH = targetCamera.orthographicSize;
        float halfW = halfH * targetCamera.aspect;
        return targetCamera.transform.position.x + halfW;
    }
}