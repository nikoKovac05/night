using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    public float viewDistance = 6f;
    public float viewAngle = 90f;
    public int rayCount = 30;
    public string playerTag = "Player";
    public string wallTag = "wall";
    public Color idleVisionColor = new Color(1f, 0.8f, 0.1f, 0.2f);
    public Color detectedVisionColor = new Color(1f, 0.15f, 0.1f, 0.3f);

    private readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[32];
    private EnemyMovement enemyMovement;
    private Transform player;
    private bool playerVisible;
    private Mesh visionMesh;
    private MeshRenderer visionRenderer;
    private Material visionMaterial;
    private Vector3[] visionVertices;
    private int[] visionTriangles;

    void Awake()
    {
        enemyMovement = GetComponent<EnemyMovement>();
        FindPlayer();
        CreateVisionVisual();
    }

    void Update()
    {
        if (player == null)
        {
            FindPlayer();
        }

        playerVisible = CanSeePlayer();

        if (enemyMovement != null)
        {
            enemyMovement.SetChaseTarget(playerVisible ? player : null);
        }

        UpdateVisionVisual();
    }

    void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            player = playerObject.transform;
            return;
        }

        playerMovementPc playerController = FindFirstObjectByType<playerMovementPc>();
        if (playerController != null)
        {
            player = playerController.transform;
        }
    }

    bool CanSeePlayer()
    {
        if (player == null)
        {
            return false;
        }

        Vector2 offset = (Vector2)player.position - (Vector2)transform.position;
        float distance = offset.magnitude;
        if (distance > viewDistance || distance == 0f)
        {
            return false;
        }

        Vector2 direction = offset / distance;
        if (Vector2.Angle(transform.right, direction) > viewAngle * 0.5f)
        {
            return false;
        }

        int hitCount = Physics2D.RaycastNonAlloc(
            transform.position,
            direction,
            hitBuffer,
            distance
        );

        RaycastHit2D closestHit = default;
        float closestDistance = float.PositiveInfinity;
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = hitBuffer[i].collider;
            if (hitCollider == null || hitCollider.transform.root == transform.root)
            {
                continue;
            }

            if (!IsWallCollider(hitCollider) && !IsPlayerCollider(hitCollider))
            {
                continue;
            }

            if (hitBuffer[i].distance < closestDistance)
            {
                closestHit = hitBuffer[i];
                closestDistance = hitBuffer[i].distance;
            }
        }

        return closestHit.collider != null && IsPlayerCollider(closestHit.collider);
    }

    bool IsPlayerCollider(Collider2D targetCollider)
    {
        return (player != null &&
                (targetCollider.transform == player || targetCollider.transform.IsChildOf(player))) ||
               HasTagInParents(targetCollider, playerTag);
    }

    bool IsWallCollider(Collider2D targetCollider)
    {
        return HasTagInParents(targetCollider, wallTag);
    }

    bool HasTagInParents(Collider2D targetCollider, string tagToFind)
    {
        Transform current = targetCollider.transform;
        while (current != null)
        {
            if (current.CompareTag(tagToFind))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    void CreateVisionVisual()
    {
        GameObject visual = new GameObject("Vision Field");
        visual.transform.SetParent(transform, false);

        MeshFilter meshFilter = visual.AddComponent<MeshFilter>();
        visionRenderer = visual.AddComponent<MeshRenderer>();
        visionMesh = new Mesh { name = "Enemy Vision Field" };
        visionMesh.MarkDynamic();
        meshFilter.sharedMesh = visionMesh;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            visionRenderer.enabled = false;
            return;
        }

        visionMaterial = new Material(shader);
        visionMaterial.color = idleVisionColor;
        if (visionMaterial.HasProperty("_Surface"))
        {
            visionMaterial.SetFloat("_Surface", 1f);
            visionMaterial.SetFloat("_Blend", 0f);
            visionMaterial.SetFloat("_ZWrite", 0f);
            visionMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            visionMaterial.renderQueue = 3000;
        }

        visionRenderer.sharedMaterial = visionMaterial;
        SpriteRenderer enemySprite = GetComponent<SpriteRenderer>();
        if (enemySprite != null)
        {
            visionRenderer.sortingLayerID = enemySprite.sortingLayerID;
            visionRenderer.sortingOrder = enemySprite.sortingOrder - 1;
        }
    }

    void UpdateVisionVisual()
    {
        if (visionMesh == null || visionRenderer == null || !visionRenderer.enabled)
        {
            return;
        }

        int count = Mathf.Clamp(rayCount, 2, 128);
        if (visionVertices == null || visionVertices.Length != count + 1)
        {
            visionVertices = new Vector3[count + 1];
            visionTriangles = new int[(count - 1) * 3];
            for (int i = 0; i < count - 1; i++)
            {
                int triangle = i * 3;
                visionTriangles[triangle] = 0;
                visionTriangles[triangle + 1] = i + 1;
                visionTriangles[triangle + 2] = i + 2;
            }
        }

        visionVertices[0] = Vector3.zero;
        Vector2 origin = transform.position;
        float halfAngle = Mathf.Clamp(viewAngle, 0f, 360f) * 0.5f;
        for (int i = 0; i < count; i++)
        {
            float fraction = i / (float)(count - 1);
            float angle = Mathf.Lerp(-halfAngle, halfAngle, fraction);
            Vector2 localDirection = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );
            Vector2 worldDirection = transform.TransformDirection(localDirection);
            float distance = DistanceToWall(origin, worldDirection);
            visionVertices[i + 1] = localDirection * distance;
        }

        visionMesh.Clear();
        visionMesh.vertices = visionVertices;
        visionMesh.triangles = visionTriangles;
        visionMesh.RecalculateBounds();
        visionMaterial.color = playerVisible ? detectedVisionColor : idleVisionColor;
    }

    void OnDestroy()
    {
        if (visionMesh != null)
        {
            Destroy(visionMesh);
        }

        if (visionMaterial != null)
        {
            Destroy(visionMaterial);
        }
    }

    Vector2 DirectionFromAngle(float angle)
    {
        float currentAngle = transform.eulerAngles.z + angle;

        return new Vector2(
            Mathf.Cos(currentAngle * Mathf.Deg2Rad),
            Mathf.Sin(currentAngle * Mathf.Deg2Rad)
        );
    }

    public bool IsPlayerVisible()
    {
        return playerVisible;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Vector3 origin = transform.position;
        int count = Mathf.Max(2, rayCount);
        Vector3 previousPoint = origin;

        for (int i = 0; i < count; i++)
        {
            float fraction = i / (float)(count - 1);
            float angle = Mathf.Lerp(-viewAngle * 0.5f, viewAngle * 0.5f, fraction);
            Vector2 direction = DirectionFromAngle(angle);
            float distance = DistanceToWall(origin, direction);
            Vector3 point = origin + (Vector3)(direction * distance);

            Gizmos.DrawLine(origin, point);
            if (i > 0)
            {
                Gizmos.DrawLine(previousPoint, point);
            }

            previousPoint = point;
        }
    }

    float DistanceToWall(Vector2 origin, Vector2 direction)
    {
        int hitCount = Physics2D.RaycastNonAlloc(
            origin,
            direction,
            hitBuffer,
            viewDistance
        );

        float closestDistance = viewDistance;
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = hitBuffer[i].collider;
            if (hitCollider != null &&
                hitCollider.transform.root != transform.root &&
                IsWallCollider(hitCollider) &&
                hitBuffer[i].distance < closestDistance)
            {
                closestDistance = hitBuffer[i].distance;
            }
        }

        return closestDistance;
    }
}