using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

public sealed class CorridorPropSet : ScriptableObject
{
    [Tooltip("Minimum and maximum number of props in a 48 m section.")]
    [SerializeField] private Vector2Int propsPerSection = new Vector2Int(2, 4);
    [SerializeField] private GameObject[] storageProps;
    [SerializeField] private GameObject[] smallProps;
    [SerializeField] private GameObject[] wallProps;

    public void Populate(Transform parent, int sectionIndex, int runSeed, float sectionLength)
    {
        foreach (Transform child in parent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        var random = new Random(unchecked(runSeed ^ (sectionIndex * -1640531527)));
        int minimum = Mathf.Clamp(propsPerSection.x, 1, 6);
        int maximum = Mathf.Clamp(propsPerSection.y, minimum, 6);
        int count = random.Next(minimum, maximum + 1);
        int storageSlot = random.Next(count);
        int wallSlot = count > 1 && random.Next(3) == 0 ? (storageSlot + 1) % count : -1;
        int side = random.Next(2) == 0 ? -1 : 1;
        float spacing = sectionLength / count;

        for (int i = 0; i < count; i++)
        {
            bool onWall = i == wallSlot;
            var choices = onWall ? wallProps : i == storageSlot ? storageProps : smallProps;
            if (choices == null || choices.Length == 0)
                continue;

            var prefab = choices[random.Next(choices.Length)];
            if (prefab == null)
                continue;
            float x = -sectionLength * 0.5f + (i + Range(random, 0.42f, 0.58f)) * spacing;
            float z = onWall ? side * 24f : FloorDepth(random, side);
            float height = onWall ? Range(random, 3.3f, 5.8f) : 0f;
            float yaw = onWall ? side * 90f : random.Next(2) * 180f + Range(random, -8f, 8f);
            Place(prefab, parent, new Vector3(x, height, z), yaw, onWall);
            side = -side;
        }
    }

    private static float FloorDepth(Random random, int side)
    {
        // Main's moving spawn points sweep two lanes at local Z -20 and +14.
        if (side < 0)
            return -Range(random, 10f, 16f);
        return random.Next(2) == 0 ? Range(random, 9.5f, 10.8f) : Range(random, 18.5f, 21.5f);
    }

    private static float Range(Random random, float min, float max)
    {
        return Mathf.Lerp(min, max, (float)random.NextDouble());
    }

    private static void Place(GameObject prefab, Transform parent, Vector3 position, float yaw, bool onWall)
    {
        var mount = new GameObject(prefab.name).transform;
        mount.SetParent(parent, false);
        var model = Instantiate(prefab, mount);
        model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        foreach (var body in model.GetComponentsInChildren<Rigidbody>())
            body.isKinematic = true;

        // Kit pivots vary. Align the visible bottom or the back of the fixture to its support.
        var renderers = model.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers)
            bounds.Encapsulate(renderer.bounds);
        bounds.center -= mount.position;
        Vector3 anchor = bounds.center;
        if (onWall)
            anchor.x = bounds.min.x;
        else
            anchor.y = bounds.min.y;

        model.transform.localPosition = -anchor;
        mount.SetLocalPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        if (!onWall)
        {
            var obstacle = mount.gameObject.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = bounds.center - anchor;
            obstacle.size = bounds.size;
            obstacle.carveOnlyStationary = false;
            obstacle.carving = true;
        }
    }
}
