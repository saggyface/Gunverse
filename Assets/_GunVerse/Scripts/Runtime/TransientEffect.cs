using UnityEngine;

namespace GunVerse
{
    public sealed class TransientEffect : MonoBehaviour
    {
        private float lifetime;
        private float remainingLifetime;
        private Vector3 startScale;
        private Vector3 endScale;
        private Vector3 velocity;
        private float gravity;
        private bool isInitialized;

        public void Initialize(float lifetime, Vector3 startScale, Vector3 endScale, Vector3 velocity, float gravity)
        {
            this.lifetime = Mathf.Max(0.01f, lifetime);
            remainingLifetime = this.lifetime;
            this.startScale = startScale;
            this.endScale = endScale;
            this.velocity = velocity;
            this.gravity = gravity;
            isInitialized = true;

            transform.localScale = startScale;
        }

        private void Update()
        {
            if (!isInitialized)
            {
                return;
            }

            remainingLifetime -= Time.deltaTime;
            if (remainingLifetime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            float progress = 1f - remainingLifetime / lifetime;
            transform.localScale = Vector3.Lerp(startScale, endScale, progress);

            velocity += Vector3.down * (gravity * Time.deltaTime);
            transform.position += velocity * Time.deltaTime;
        }
    }
}
