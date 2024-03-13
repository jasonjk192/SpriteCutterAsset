using System.Collections.Generic;
using UnityEngine;

namespace WinterCrestal.SpriteCutter
{
    public abstract class SpriteCutterBase : MonoBehaviour
    {
        [SerializeField] protected SpriteCutterInputManager _spriteCutterInputManager;
        [SerializeField] protected bool _manageCreatedTextures;

        protected List<Texture2D> _createdTexturesList;

        protected SplitSprite _splitSprite0, _splitSprite1;

        protected virtual void Awake()
        {
            if(_manageCreatedTextures)
                _createdTexturesList = new List<Texture2D>();
        }

        protected virtual void OnDestroy()
        {
            if( _manageCreatedTextures)
            {
                for(int i = _createdTexturesList.Count - 1; i >= 0; i--)
                    DestroyImmediate(_createdTexturesList[i]);
                _createdTexturesList.Clear();
            }
        }

        protected virtual void OnEnable()
        {
            _spriteCutterInputManager.onInputPointerDown += OnInputPointerDown;
            _spriteCutterInputManager.onInputPointerUp += OnInputPointerUp;
            _spriteCutterInputManager.onInputPointer += OnInputPointer;
            _spriteCutterInputManager.onSpriteRendererCut += OnSpriteRendererCut;
        }

        protected virtual void OnDisable()
        {
            _spriteCutterInputManager.onInputPointerDown -= OnInputPointerDown;
            _spriteCutterInputManager.onInputPointerUp -= OnInputPointerUp;
            _spriteCutterInputManager.onInputPointer -= OnInputPointer;
            _spriteCutterInputManager.onSpriteRendererCut -= OnSpriteRendererCut;
        }

        protected virtual void OnInputPointerDown(Vector3 position)
        {
        }
        protected virtual void OnInputPointerUp(Vector3 position)
        {

        }
        protected virtual void OnInputPointer(Vector3 position)
        {
        }
        protected virtual void OnSpriteRendererCut(SpriteRenderer original, SplitSprite s0, SplitSprite s1)
        {
            _splitSprite0 = s0;
            _splitSprite1 = s1;

            if(_manageCreatedTextures)
            {
                _createdTexturesList.Add(s0.SpriteRenderer.sprite.texture);
                _createdTexturesList.Add(s1.SpriteRenderer.sprite.texture);
            }

            _splitSprite0.SpriteRenderer.transform.localScale = original.transform.localScale;
            _splitSprite1.SpriteRenderer.transform.localScale = original.transform.localScale;
            
            _splitSprite0.SpriteRenderer.transform.position = original.SpriteLocalToWorld(_splitSprite0.Rect.center);
            _splitSprite1.SpriteRenderer.transform.position = original.SpriteLocalToWorld(_splitSprite1.Rect.center);

            var rot = _splitSprite0.SpriteRenderer.transform.rotation;
            rot = Quaternion.Euler(rot.x, rot.y, original.transform.rotation.eulerAngles.z);
            _splitSprite0.SpriteRenderer.transform.rotation = rot;

            rot = _splitSprite1.SpriteRenderer.transform.rotation;
            rot = Quaternion.Euler(rot.x, rot.y, original.transform.rotation.eulerAngles.z);
            _splitSprite1.SpriteRenderer.transform.rotation = rot;
        }

    }
}

