#nullable enable

using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MineHazard : MonoBehaviour
{
    [Header("Detection")]
    public float detectionRange = 8f;
    public float killRange = 1.0f;

    [Header("Thrust")]
    public float thrustForce = 15f;
    public ForceMode thrustForceMode = ForceMode.Force;
    public float maxSpeed = 10f;

    [Header("Deceleration")]
    public float decelerationRate = 5f;

    [Header("Audio")]
    public float beepFarRange = 20f;
    public float beepNearRange = 4f;
    public float minBeepFrequency = 0.5f;
    public float maxBeepFrequency = 5f;

    [Header("Visualization")]
    public Color detectionRangeColor = Color.yellow;
    public Color killRangeColor = Color.red;
    public Color beepFarRangeColor = Color.cyan;
    public Color beepNearRangeColor = Color.blue;

    private Vector3 initialPosition = Vector3.zero;
    private Quaternion initialRotation = Quaternion.identity;
    private Rigidbody? mineRigidbody;
    private Entity? playerEntity;
    private Rigidbody? playerRigidbody;
    private float beepTimer = 0f;

    private void Awake()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        mineRigidbody = GetComponent<Rigidbody>();

        TryFindPlayer();
    }

    private void OnDestroy()
    {
        if (playerEntity != null)
        {
            playerEntity.OnKilled -= HandlePlayerKilled;
        }
    }

    private void TryFindPlayer()
    {
        if (playerEntity != null)
        {
            return;
        }

        GameObject? player = StarliftStatics.FindPlayer();
        if (player == null)
        {
            return;
        }

        if (player.TryGetComponent(out Rigidbody rb))
        {
            playerRigidbody = rb;
        }

        if (player.TryGetComponent(out Entity entity))
        {
            playerEntity = entity;
            playerEntity.OnKilled += HandlePlayerKilled;
        }
    }

    private void Update()
    {
        if (playerRigidbody == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, playerRigidbody.position);

        if (distance > beepFarRange)
        {
            beepTimer = 0f;
            return;
        }

        float t = Mathf.InverseLerp(beepFarRange, 0f, distance);
        float frequency = Mathf.Lerp(minBeepFrequency, maxBeepFrequency, t);
        float period = 1f / frequency;

        beepTimer += Time.deltaTime;
        if (beepTimer >= period)
        {
            beepTimer = 0f;

            string beepEvent = distance <= beepNearRange ? "play_mine_beep_near" : "play_mine_beep_far";
            AkUnitySoundEngine.PostEvent(beepEvent, gameObject);
        }
    }

    private void FixedUpdate()
    {
        if (mineRigidbody == null)
        {
            return;
        }

        if (playerRigidbody == null)
        {
            TryFindPlayer();
            if (playerRigidbody == null)
            {
                return;
            }
        }

        Vector3 playerPosition = playerRigidbody.position;
        float distance = Vector3.Distance(transform.position, playerPosition);

        if (distance <= killRange)
        {
            KillPlayer();
            return;
        }

        if (distance <= detectionRange)
        {
            Vector3 direction = (playerPosition - transform.position).normalized;
            mineRigidbody.AddForce(direction * thrustForce, thrustForceMode);
            mineRigidbody.linearVelocity = Vector3.ClampMagnitude(mineRigidbody.linearVelocity, maxSpeed);
        }
        else
        {
            mineRigidbody.linearVelocity = Vector3.MoveTowards(mineRigidbody.linearVelocity, Vector3.zero, decelerationRate * Time.fixedDeltaTime);
        }
    }

    private void KillPlayer()
    {
        if (playerEntity == null || !playerEntity.IsAlive)
        {
            return;
        }

        DamageEvent damageEvent = new();
        damageEvent.damageTarget = playerEntity.gameObject;
        damageEvent.damageSource = gameObject;
        damageEvent.damageType = DamageType.Explosion;

        playerEntity.Kill(damageEvent);
        gameObject.SetActive(false);
    }

    private void HandlePlayerKilled(DamageEvent damageEvent)
    {
        if (mineRigidbody != null)
        {
            mineRigidbody.linearVelocity = Vector3.zero;
            mineRigidbody.angularVelocity = Vector3.zero;
        }

        transform.SetPositionAndRotation(initialPosition, initialRotation);
        beepTimer = 0f;
        gameObject.SetActive(true);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = beepFarRangeColor;
        Gizmos.DrawWireSphere(transform.position, beepFarRange);

        Gizmos.color = beepNearRangeColor;
        Gizmos.DrawWireSphere(transform.position, beepNearRange);

        Gizmos.color = detectionRangeColor;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = killRangeColor;
        Gizmos.DrawWireSphere(transform.position, killRange);
    }
}
