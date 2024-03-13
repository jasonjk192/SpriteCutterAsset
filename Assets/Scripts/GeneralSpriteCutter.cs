using System.Collections.Generic;
using UnityEngine;

namespace WinterCrestal.SpriteCutter
{
    public class GeneralSpriteCutter : SpriteCutterBase
    {
        Vector2 p0, p1;

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
            base.OnSpriteRendererCut(original, s0, s1);

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
    }

}

