using UnityEngine;
using UnityEngine.AI;
using Random = System.Random;

public sealed class CorridorPropSet : ScriptableObject
{
    [Tooltip("Minimum and maximum number of props in a 48 m section.")]
    [SerializeField] private Vector2Int propsPerSection = new Vector2Int(5, 7);
    [SerializeField] private Vector2 storageScale = new Vector2(1.35f, 1.6f);
    [SerializeField] private Vector2 smallPropScale = new Vector2(2.2f, 2.7f);
    [SerializeField] private GameObject[] storageProps;
    [SerializeField] private GameObject[] smallProps;
    [SerializeField] private GameObject[] wallProps;

    public void Populate(Transform parent, int sectionIndex, int runSeed, float sectionLength, float sectionWidth = 48f)
    {
        foreach (Transform child in parent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        var random = new Random(unchecked(runSeed ^ (sectionIndex * -1640531527)));
        int minimum = Mathf.Clamp(propsPerSection.x, 2, 8);
        int maximum = Mathf.Clamp(propsPerSection.y, minimum, 8);
        int count = random.Next(minimum, maximum + 1);
        int rows = (count + 1) / 2;
        float spacing = sectionLength / rows;
        int leftStorageSlot = random.Next(rows) * 2;
        int rightStorageSlot = random.Next(count / 2) * 2 + 1;
        int wallSlot = random.Next(3) == 0 ? random.Next(count) : -1;

        for (int i = 0; i < count; i++)
        {
            bool isStorage = i == leftStorageSlot || i == rightStorageSlot;
            bool onWall = i == wallSlot && !isStorage;
            var choices = onWall ? wallProps : isStorage ? storageProps : smallProps;
            if (choices == null || choices.Length == 0)
                continue;

            var prefab = choices[random.Next(choices.Length)];
            if (prefab == null)
                continue;

            float x = -sectionLength * 0.5f + (i / 2 + 0.5f) * spacing;
            int side = i % 2 == 0 ? -1 : 1;
            Vector2 scaleRange = isStorage ? storageScale : smallPropScale;
            Place(prefab, parent, random, x, spacing, side, onWall, scaleRange, sectionWidth);
        }
    }

    private static float FloorDepth(Random random, int side, float halfDepth, float halfWidth)
    {
        // Leave 8 m through the centre and keep Main's spawn lanes at Z -20 and +14 clear.
        float innerMinimum = 4f + halfDepth;
        float innerMaximum = (side < 0 ? 18f : 12f) - halfDepth;
        float outerMinimum = (side < 0 ? 22f : 16.5f) + halfDepth;
        float outerMaximum = halfWidth - 1.5f - halfDepth;
        bool useOuter = outerMaximum >= outerMinimum &&
            (innerMaximum < innerMinimum || random.Next(2) == 0);
        float depth = useOuter
            ? Range(random, outerMinimum, outerMaximum)
            : Range(random, innerMinimum, innerMaximum);
        return side * depth;
    }

    private static float Range(Random random, float min, float max)
    {
        return Mathf.Lerp(min, max, (float)random.NextDouble());
    }

    private static void Place(GameObject prefab, Transform parent, Random random, float x,
        float spacing, int side, bool onWall, Vector2 scaleRange, float sectionWidth)
    {
        var mount = new GameObject(prefab.name).transform;
        mount.SetParent(parent, false);
        var model = Instantiate(prefab, mount);
        model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        foreach (var body in model.GetComponentsInChildren<Rigidbody>())
            body.isKinematic = true;

        // Measure at the prefab's original scale before fitting the prop into its placement cell.
        var renderers = model.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers)
            bounds.Encapsulate(renderer.bounds);
        bounds.center -= mount.position;

        float yaw = onWall ? side * 90f : random.Next(4) * 90f;
        bool swapAxes = Mathf.RoundToInt(yaw / 90f) % 2 != 0;
        float width = swapAxes ? bounds.size.z : bounds.size.x;
        float depth = swapAxes ? bounds.size.x : bounds.size.z;

        if (!onWall)
        {
            float scale = Range(random, Mathf.Max(1f, scaleRange.x), Mathf.Max(1f, scaleRange.y));
            float innerDepth = side < 0 ? 14f : 8f;
            float outerDepth = sectionWidth * 0.5f - 1.5f - (side < 0 ? 22f : 16.5f);
            float availableDepth = Mathf.Max(innerDepth, outerDepth) - 0.4f;
            scale = Mathf.Min(scale, (spacing - 1.5f) / width, availableDepth / depth, 9.5f / bounds.size.y);
            model.transform.localScale *= scale;
            bounds.center *= scale;
            bounds.size *= scale;
            width *= scale;
            depth *= scale;
        }

        // Kit pivots vary. Align the visible bottom or the back of the fixture to its support.
        Vector3 anchor = bounds.center;
        if (onWall)
            anchor.x = bounds.min.x;
        else
            anchor.y = bounds.min.y;
        model.transform.localPosition = -anchor;

        float jitter = Mathf.Min(0.6f, Mathf.Max(0f, (spacing - width) * 0.5f - 0.75f));
        x += Range(random, -jitter, jitter);
        float z = onWall ? side * sectionWidth * 0.5f : FloorDepth(random, side, depth * 0.5f, sectionWidth * 0.5f);
        float height = onWall ? Range(random, 3.3f, 5.8f) : 0f;
        mount.SetLocalPositionAndRotation(new Vector3(x, height, z), Quaternion.Euler(0f, yaw, 0f));

        if (!onWall)
        {
            // Treat the cargo or loaded rack as solid cover for characters, projectiles and sight checks.
            foreach (var collider in model.GetComponentsInChildren<Collider>())
                collider.enabled = false;
            mount.gameObject.layer = Mathf.Max(0, LayerMask.NameToLayer("Ground"));
            var solid = mount.gameObject.AddComponent<BoxCollider>();
            solid.center = bounds.center - anchor;
            solid.size = bounds.size;

            var obstacle = mount.gameObject.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = solid.center;
            obstacle.size = solid.size;
            obstacle.carveOnlyStationary = false;
            obstacle.carving = true;
        }
    }
}
