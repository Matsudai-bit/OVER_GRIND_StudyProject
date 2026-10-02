using System;
using UnityEngine;

namespace WheelSparkEffect
{
    /// <summary>One contact patch. +Y is the default ground normal; dimensions assume a 3 m radius.</summary>
    [DisallowMultipleComponent]
    public sealed class WheelSparkController : MonoBehaviour
    {
        [Serializable]
        public sealed class Layer
        {
            public ParticleSystem system;
            public float rate;
            public int impactCount;
            public bool followsSpeed = true;
            public float gravity = 1;
            public float speedMin;
            public float speedMax;
        }

        [Header("Art direction")]
        [Range(0, 2)] public float intensity = 1;
        [Min(0)] public float emissionMultiplier = 1;
        [Min(0)] public float speedMultiplier = 1;
        [Tooltip("Local-space direction, projected onto the ground.")]
        public Vector3 sparkDirection = Vector3.left;
        [Min(0.05f)] public float wheelRadius = 3;
        public Transform wheelSparkEmitter;

        [Header("Contact and motion")]
        public bool contact = true;
        [Range(0, 2)] public float contactStrength = 1;
        [Tooltip("Drive from a wheel controller, or disable to use Spark Direction / Speed Multiplier.")]
        public bool useWheelMotion;
        [Tooltip("World-space metres per second.")]
        public Vector3 linearVelocity = new Vector3(6, 0, 0);
        [Tooltip("World-space radians per second, not degrees. Negative Z throws sparks toward -X.")]
        public Vector3 angularVelocity = new Vector3(0, 0, -8);
        public Vector3 groundNormal = Vector3.up;
        [Min(0.1f)] public float referenceSurfaceSpeed = 18;
        [Range(0, 1)] public float chatter = 0.35f;
        public Layer[] layers = Array.Empty<Layer>();

        float elapsed;
        float impactCooldown;
        bool previouslyContacting;
        float previousStrength;
        public float CurrentDrive { get; private set; }

        void OnEnable()
        {
            elapsed = 0;
            previousStrength = contactStrength;
            previouslyContacting = false;
            foreach (var layer in layers)
                if (layer.system) layer.system.Play(false);
            Tick(0);
        }

        void Update() { Tick(Time.deltaTime); }

        /// <summary>Can also be advanced at exactly 1/60 s by editor validation without entering Play mode.</summary>
        public void Tick(float dt)
        {
            dt = Mathf.Max(0, dt);
            elapsed += dt;
            impactCooldown = Mathf.Max(0, impactCooldown - dt);
            var normal = groundNormal.sqrMagnitude > 0.0001f ? groundNormal.normalized : Vector3.up;
            Vector3 direction = Vector3.ProjectOnPlane(transform.TransformDirection(sparkDirection), normal);
            float motion = speedMultiplier;
            float rootScale = Mathf.Max(0.001f, Mathf.Abs(transform.lossyScale.x));
            if (useWheelMotion)
            {
                Vector3 slip = Vector3.ProjectOnPlane(linearVelocity + Vector3.Cross(angularVelocity, -normal * wheelRadius * rootScale), normal);
                // Serrated teeth can cut during rolling even when the ideal rigid-wheel slip tends to zero.
                float surfaceSpeed = Mathf.Max(slip.magnitude, angularVelocity.magnitude * wheelRadius * rootScale * 0.25f);
                motion *= Mathf.Clamp(surfaceSpeed / referenceSurfaceSpeed, 0, 2);
                if (slip.sqrMagnitude > 0.01f) direction = slip;
            }
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.ProjectOnPlane(transform.right, normal);
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.Cross(normal, Vector3.forward);
            if (wheelSparkEmitter)
            {
                wheelSparkEmitter.rotation = Quaternion.LookRotation(direction.normalized, normal);
                wheelSparkEmitter.localScale = Vector3.one * (wheelRadius / 3f);
            }
            bool active = contact && motion > 0.005f && intensity > 0 && contactStrength > 0 && emissionMultiplier > 0;
            CurrentDrive = active ? intensity * contactStrength * Mathf.Sqrt(motion) : 0;
            float pulse = 1 + chatter * (0.65f * Mathf.Sin(elapsed * 47f) + 0.35f * Mathf.Sin(elapsed * 83f + 0.7f));
            foreach (var layer in layers)
            {
                if (!layer.system) continue;
                var emission = layer.system.emission;
                emission.enabled = active;
                emission.rateOverTime = layer.rate * CurrentDrive * emissionMultiplier * pulse;
                var main = layer.system.main;
                float launchScale = (layer.followsSpeed ? Mathf.Max(0.05f, motion) : 1f) * wheelRadius / 3f * rootScale;
                main.startSpeed = new ParticleSystem.MinMaxCurve(layer.speedMin * launchScale, layer.speedMax * launchScale);
                main.gravityModifierMultiplier = layer.gravity * wheelRadius / 3f * rootScale;
                if (active && layer.system.isStopped) layer.system.Play(false);
            }
            if (active && (!previouslyContacting || contactStrength - previousStrength > 0.35f))
                TriggerImpact(Mathf.Clamp01(contactStrength) * Mathf.Clamp01(motion));
            previouslyContacting = active;
            previousStrength = contactStrength;
        }

        /// <summary>Explicit gameplay impact event. Rate limited; never clears already emitted particles.</summary>
        public void TriggerImpact(float strength = 1)
        {
            if (!contact || CurrentDrive <= 0 || impactCooldown > 0) return;
            impactCooldown = 0.085f;
            foreach (var layer in layers)
                if (layer.system && layer.impactCount > 0)
                    layer.system.Emit(Mathf.RoundToInt(layer.impactCount * Mathf.Clamp(strength, 0, 2) * intensity * emissionMultiplier));
        }

        public void SetContact(bool touching, float strength = 1)
        {
            contact = touching;
            contactStrength = Mathf.Max(0, strength);
            Tick(0);
        }

        void OnDisable()
        {
            foreach (var layer in layers)
                if (layer.system)
                {
                    var e = layer.system.emission;
                    e.enabled = false;
                }
        }

        void OnDrawGizmosSelected()
        {
            if (!wheelSparkEmitter) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(wheelSparkEmitter.position, wheelRadius * 0.045f);
            Gizmos.color = new Color(1, 0.45f, 0.04f);
            Gizmos.DrawRay(wheelSparkEmitter.position, wheelSparkEmitter.forward * wheelRadius);
        }
    }
}
