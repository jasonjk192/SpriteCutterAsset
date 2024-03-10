using System.Collections.Generic;
using UnityEngine;

namespace WinterCrestal.SpriteCutter
{
    public class GeneralSpriteCutter : SpriteCutterBase
    {
        Vector2 p0, p1;

        private List<SpriteRenderer> _createdSpriteRenderersList = new();

        protected override void OnInputPointerDown(Vector3 position)
        {
            _spriteCutterInputManager.SpriteRenderersToCut = null;

            p0 = Camera.main.ScreenToWorldPoint(position);
        }

        protected override void OnInputPointerUp(Vector3 position)
        {
            p1 = Camera.main.ScreenToWorldPoint(position);

            var hitArray = Physics2D.LinecastAll(p0, p1);
            List<SpriteRenderer> spriteRenderers = new();

            for(int i = 0; i < hitArray.Length; i++)
            {
                if(hitArray[i].collider.TryGetComponent<SpriteRenderer>(out var renderer))
                {
                    spriteRenderers.Add(renderer);
                }
            }

            _spriteCutterInputManager.SpriteRenderersToCut = spriteRenderers.ToArray();
        }

        protected override void OnSpriteRendererCut(SpriteRenderer original, SplitSprite s0, SplitSprite s1)
        {
            _createdSpriteRenderersList.Remove(original);

            base.OnSpriteRendererCut(original, s0, s1);
            _createdSpriteRenderersList.Add(s0.SpriteRenderer);
            _createdSpriteRenderersList.Add(s1.SpriteRenderer);

            Destroy(original.gameObject);

            AddPhysics(s0.SpriteRenderer);
            AddPhysics(s1.SpriteRenderer);
        }

        private void AddPhysics(SpriteRenderer renderer)
        {
            renderer.gameObject.AddComponent<Rigidbody2D>();
            var poly = renderer.gameObject.AddComponent<PolygonCollider2D>();
            // Optimize the polygon collider 2D
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            foreach(var renderer in  _createdSpriteRenderersList)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(renderer.transform.position, .1f);
                Gizmos.color = Color.yellow;
                DrawBounds(renderer.localBounds, renderer.transform.position, renderer.transform.rotation);
                Gizmos.color = Color.red;
                DrawBounds(renderer.bounds, Vector3.zero, Quaternion.identity);
            }
        }

        private void DrawBounds(Bounds bounds, Vector3 offset, Quaternion rotation)
        {
            Vector3 ld = new(bounds.center.x - bounds.extents.x, bounds.center.y - bounds.extents.y);
            Vector3 lt = new(bounds.center.x - bounds.extents.x, bounds.center.y + bounds.extents.y);
            Vector3 rd = new(bounds.center.x + bounds.extents.x, bounds.center.y - bounds.extents.y);
            Vector3 rt = new(bounds.center.x + bounds.extents.x, bounds.center.y + bounds.extents.y);

            ld = bounds.center + rotation * (ld - bounds.center) + offset;
            lt = bounds.center + rotation * (lt - bounds.center) + offset;
            rd = bounds.center + rotation * (rd - bounds.center) + offset;
            rt = bounds.center + rotation * (rt - bounds.center) + offset;

            Gizmos.DrawLine(ld, lt);
            Gizmos.DrawLine(lt, rt);
            Gizmos.DrawLine(rt, rd);
            Gizmos.DrawLine(rd, ld);
        }
#endif
    }

}

