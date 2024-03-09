using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using ISTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif

namespace WinterCrestal.SpriteCutter
{
    public struct SplitSprite
    {
        public SpriteRenderer SpriteRenderer;
        public Rect Rect;
        public SplitSprite(SpriteRenderer spriteRenderer, Rect rect)
        { SpriteRenderer = spriteRenderer; Rect = rect; }
    }

    public class SpriteCutterInputManager : MonoBehaviour
    {
        [HideInInspector] public SpriteRenderer[] SpriteRenderersToCut;
        [HideInInspector] public Camera Camera;
        private float _spriteCuttingTolerance = .2f;
        public float SpriteCuttingTolerance { get { return _spriteCuttingTolerance; } set { _spriteCuttingTolerance = Mathf.Clamp(value, 0f, 1f); } }

        private Vector2 _p0, _p1;

        public UnityAction<Vector3> onInputPointerDown;
        public UnityAction<Vector3> onInputPointerUp;
        public UnityAction<Vector3> onInputPointer;
        public UnityAction<SpriteRenderer, SplitSprite, SplitSprite> onSpriteRendererCut;

        private void OnEnable()
        {
#if (UNITY_ANDROID || UNITY_IOS) && ENABLE_INPUT_SYSTEM
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
#endif
        }

        private void OnDisable()
        {
#if (UNITY_ANDROID || UNITY_IOS) && ENABLE_INPUT_SYSTEM
            UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Disable();
#endif
        }

        private void Awake()
        {
            if (Camera == null) Camera = Camera.main;
        }

        private void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER && false
            if (Input.touchSupported)
            {
                if (Input.touchCount > 0)
                {
                    var touch = Input.GetTouch(0);
                    if (touch.phase == UnityEngine.TouchPhase.Began)
                        OnInputPointerDown(touch.position);
                    else if (touch.phase == UnityEngine.TouchPhase.Ended)
                        OnInputPointerUp(touch.position);
                    else
                        OnInputPointer(touch.position);
                }
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                    OnInputPointerDown(Input.mousePosition);
                else if (Input.GetMouseButtonUp(0))
                    OnInputPointerUp(Input.mousePosition);
                else if (Input.GetMouseButton(0))
                    OnInputPointer(Input.mousePosition);
            }
#elif ENABLE_INPUT_SYSTEM
        if(ISTouch.activeTouches.Count > 0)
        {
            var touch = ISTouch.activeTouches[0];
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
                OnInputPointerDown(touch.screenPosition);
            else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended)
                OnInputPointerUp(touch.screenPosition);
            else
                OnInputPointer(touch.screenPosition);
        }
        else
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
                OnInputPointerDown(Mouse.current.position.ReadValue());
            else if (Mouse.current.leftButton.wasReleasedThisFrame)
                OnInputPointerUp(Mouse.current.position.ReadValue());
            else if (Mouse.current.leftButton.isPressed)
                OnInputPointer(Mouse.current.position.ReadValue());
        }
#endif
        }

        private void OnInputPointerDown(Vector3 inputPosition)
        {
            onInputPointerDown?.Invoke(inputPosition);

            _p0 = Camera.ScreenToWorldPoint(inputPosition);
            _p1 = _p0;
        }

        private void OnInputPointerUp(Vector3 inputPosition)
        {
            onInputPointerUp?.Invoke(inputPosition);

            if (SpriteRenderersToCut == null || SpriteRenderersToCut.Length == 0) return;

            foreach(var renderer in SpriteRenderersToCut)
            {
                var hitCount = renderer.IntersectLine(_p0, _p1, out var hit0, out var hit1);
                if (hitCount == 2)
                {
                    var cut0 = renderer.WorldToSpriteLocal(hit0);
                    var cut1 = renderer.WorldToSpriteLocal(hit1);

                    cut0 = CheckCutTolerance(renderer, cut0, _spriteCuttingTolerance);
                    cut1 = CheckCutTolerance(renderer, cut1, _spriteCuttingTolerance);

                    if (renderer.CutSprite(cut0, cut1, out var _cutSpriteRenderer0, out var _cutSpriteRenderer1))
                    {
                        float xMin = Mathf.Min(cut0.x, cut1.x);
                        float yMin = Mathf.Min(cut0.y, cut1.y);

                        onSpriteRendererCut?.Invoke(renderer, 
                            new(_cutSpriteRenderer0, new Rect(0, 0, _cutSpriteRenderer0.sprite.texture.width, _cutSpriteRenderer0.sprite.texture.height)), 
                            new(_cutSpriteRenderer1, new Rect(xMin, yMin, _cutSpriteRenderer1.sprite.texture.width, _cutSpriteRenderer1.sprite.texture.height)));
                    }
                }
            } 
        }

        private void OnInputPointer(Vector3 inputPosition)
        {
            onInputPointer?.Invoke(inputPosition);

            _p1 = Camera.ScreenToWorldPoint(inputPosition);
        }

        private Vector2Int CheckCutTolerance(SpriteRenderer renderer, Vector2Int cut, float tolerance)
        {
            int px = (int)(renderer.sprite.rect.width * tolerance);
            int py = (int)(renderer.sprite.rect.height * tolerance);
            Vector2Int size = new((int)renderer.sprite.rect.width, (int)renderer.sprite.rect.height);
            var diff = cut - size;
            diff.x = Mathf.Abs(diff.x);
            diff.y = Mathf.Abs(diff.y);
            if (diff.x < px) cut.x = size.x;
            else if (cut.x < px) cut.x = 0;
            if (diff.y < py) cut.y = size.y;
            else if (cut.y < py) cut.y = 0;
            return cut;
        }

    }

}
