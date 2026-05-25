#nullable enable

using UnityEngine;
using UnityEngine.VFX;

public class GravityVectorField : MonoBehaviour
{
    [SerializeField] private int gridResolution = 16;
    [SerializeField] private float fieldRadius = 20f;
    [SerializeField] private float updateInterval = 0.1f;
    [SerializeField] private float meshSourceBoundaryMultiplier = 5f;

    [SerializeField] private VisualEffect? vfx;

    private Texture3D? _texture;
    private Color[]? _pixels;
    private float _timeSinceLastUpdate;

    private GravitySourceComponent[] _allSources = System.Array.Empty<GravitySourceComponent>();
    private bool _initialized;

    private void Start()
    {
        vfx ??= GetComponent<VisualEffect>();
        if (vfx == null)
        {
            Debug.LogWarning("GravityVectorField: no VisualEffect found.", this);
            return;
        }

        if (!vfx.HasTexture("GravityFieldTexture"))
        {
            Debug.LogError("GravityVectorField: VFX graph is missing exposed Texture3D property 'GravityFieldTexture'.", this);
            return;
        }

        _allSources = FindObjectsByType<GravitySourceComponent>(FindObjectsSortMode.None);
        System.Array.Sort(_allSources, (a, b) => b.priority.CompareTo(a.priority));

        int total = gridResolution * gridResolution * gridResolution;
        _pixels = new Color[total];

        _texture = new Texture3D(gridResolution, gridResolution, gridResolution, TextureFormat.RGBAHalf, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        _initialized = true;

        RebuildTexture(transform.position);
        UploadAndPush(transform.position);
    }

    private void LateUpdate()
    {
        if (!_initialized || vfx == null)
            return;

        Vector3 center = transform.position;
        vfx.SetVector3("GravityFieldCenter", center);

        _timeSinceLastUpdate += Time.deltaTime;

        if (_timeSinceLastUpdate < updateInterval)
            return;

        _timeSinceLastUpdate = 0f;

        RebuildTexture(center);
        UploadAndPush(center);
    }

    private Vector3 ComputeGravityAt(Vector3 worldPoint)
    {
        for (int i = 0; i < _allSources.Length; i++)
        {
            GravitySourceComponent source = _allSources[i];
            if (!source.isGravityEnabled)
                continue;

            bool inside;
            if (source is GravitySourceMesh meshSource)
            {
                if (meshSource.meshCollider == null) continue;
                float threshold = meshSource.maxDistanceToSurface * meshSourceBoundaryMultiplier;
                inside = (worldPoint - meshSource.meshCollider.ClosestPoint(worldPoint)).sqrMagnitude <= threshold * threshold;
            }
            else
            {
                Collider? col = source.GetComponent<Collider>();
                if (col == null || !col.isTrigger) continue;
                inside = (col.ClosestPoint(worldPoint) - worldPoint).sqrMagnitude < 0.001f;
            }

            if (inside)
                return source.GetGravityVector(worldPoint);
        }

        return Vector3.zero;
    }

    private void RebuildTexture(Vector3 center)
    {
        if (_pixels == null)
            return;

        int res = gridResolution;
        float diameter = fieldRadius * 2f;

        for (int z = 0; z < res; z++)
        {
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    Vector3 worldPoint = center + new Vector3(
                        (x + 0.5f) / res * diameter - fieldRadius,
                        (y + 0.5f) / res * diameter - fieldRadius,
                        (z + 0.5f) / res * diameter - fieldRadius
                    );

                    int index = x + y * res + z * res * res;

                    Vector3 gravVec = ComputeGravityAt(worldPoint);

                    if (gravVec.sqrMagnitude < 1e-6f)
                    {
                        _pixels[index] = new Color(0.5f, 0.5f, 0.5f, 0f);
                        continue;
                    }

                    Vector3 dir = gravVec.normalized;
                    _pixels[index] = new Color(
                        dir.x * 0.5f + 0.5f,
                        dir.y * 0.5f + 0.5f,
                        dir.z * 0.5f + 0.5f,
                        gravVec.magnitude
                    );
                }
            }
        }
    }

    private void UploadAndPush(Vector3 center)
    {
        if (_texture == null || _pixels == null || vfx == null)
            return;

        _texture.SetPixels(_pixels);
        _texture.Apply(updateMipmaps: false, makeNoLongerReadable: false);

        vfx.SetTexture("GravityFieldTexture", _texture);
        vfx.SetVector3("GravityFieldCenter", center);
        vfx.SetVector3("GravityFieldExtent", new Vector3(fieldRadius, fieldRadius, fieldRadius));
    }

    private void OnDestroy()
    {
        if (_texture != null)
            Destroy(_texture);
    }
}
