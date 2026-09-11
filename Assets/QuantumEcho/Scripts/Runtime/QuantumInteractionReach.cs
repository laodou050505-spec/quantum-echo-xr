using UnityEngine;

namespace QuantumEcho
{
    /// <summary>Reach the actual target surface without shooting one's own body or through a wall.</summary>
    public static class QuantumInteractionReach
    {
        static readonly RaycastHit[] Hits = new RaycastHit[64];

        public static bool CanReach(QuantumPlayerRig player, Transform target, float nearDistance, float rayDistance)
        {
            if (player == null || target == null || !target.gameObject.activeInHierarchy) return false;
            var origin = player.GroundPosition + Vector3.up * 0.96f;
            var surface = target.position;
            var surfaceDistance = float.PositiveInfinity;
            foreach (var collider in target.GetComponentsInChildren<Collider>())
            {
                if (!collider.enabled || collider.isTrigger) continue;
                var point = collider.ClosestPoint(origin);
                var distance = Vector3.Distance(origin, point);
                if (distance >= surfaceDistance) continue;
                surfaceDistance = distance;
                surface = point;
            }
            var centreNear = Vector3.ProjectOnPlane(target.position-player.GroundPosition, Vector3.up).magnitude < nearDistance;
            // A 0.96m avatar must be able to interact while its hull is touching
            // the crystal's hull; it should never have to enter that solid.
            if (centreNear || surfaceDistance <= 1.31f)
            {
                var offset = surface-origin;
                if (offset.sqrMagnitude < 0.0001f || !FirstExternalHit(new Ray(origin, offset.normalized),
                    offset.magnitude + 0.025f, player.transform, out var obstruction) || IsTarget(obstruction.transform, target)) return true;
            }
            return FirstExternalHit(player.AimRay, rayDistance, player.transform, out var hit) && IsTarget(hit.transform, target);
        }

        public static bool FirstExternalHit(Ray ray, float distance, Transform playerRoot, out RaycastHit closest)
        {
            // Query-only triggers (including the avatar penetration probe) never
            // intercept aiming. Keep the nearest real occluder after filtering
            // all of the player's controllers and imported model children.
            var count = Physics.RaycastNonAlloc(ray, Hits, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            var results = Hits;
            if (count == Hits.Length) { results = Physics.RaycastAll(ray, distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore); count = results.Length; }
            closest = default;
            var best = float.PositiveInfinity;
            for (var i=0; i<count; i++)
            {
                var candidate = results[i];
                if (candidate.transform.IsChildOf(playerRoot) || candidate.distance >= best) continue;
                closest = candidate; best = candidate.distance;
            }
            return best < float.PositiveInfinity;
        }

        static bool IsTarget(Transform hit, Transform target) => hit == target || hit.IsChildOf(target);
    }
}
