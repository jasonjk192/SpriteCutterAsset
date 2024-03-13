using UnityEngine;

namespace WinterCrestal.SpriteCutter
{
    public class DemoScene1 : MonoBehaviour
    {
        private Camera _camera;
        private LineRenderer _lineRenderer;

        [SerializeField] private SpriteCutterInputManager _spriteCutterInputManager;

        [Space, Header("Saving")]
        public SpriteRenderer SpriteToSave;
        public string SpriteToSaveName;

        private void Awake()
        {
            _camera = Camera.main;
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.enabled = false;

            GenerateCollidersAcrossScreen();
        }

        private void OnEnable()
        {
            _spriteCutterInputManager.onInputPointerDown += OnInputPointerDown;
            _spriteCutterInputManager.onInputPointer += OnInputPointer;
            _spriteCutterInputManager.onInputPointerUp += OnInputPointerUp;
        }

        private void OnDisable()
        {
            _spriteCutterInputManager.onInputPointerDown -= OnInputPointerDown;
            _spriteCutterInputManager.onInputPointer -= OnInputPointer;
            _spriteCutterInputManager.onInputPointerUp -= OnInputPointerUp;
        }

        private void GenerateCollidersAcrossScreen()
        {
            GameObject wallParent = new GameObject("Walls");
            Vector2 lDCorner = _camera.ViewportToWorldPoint(new Vector3(0, 0f, _camera.nearClipPlane));
            Vector2 rUCorner = _camera.ViewportToWorldPoint(new Vector3(1f, 1f, _camera.nearClipPlane));
            Vector2[] colliderpoints;

            EdgeCollider2D upperEdge = new GameObject("upperEdge").AddComponent<EdgeCollider2D>();
            colliderpoints = upperEdge.points;
            colliderpoints[0] = new Vector2(lDCorner.x, rUCorner.y);
            colliderpoints[1] = new Vector2(rUCorner.x, rUCorner.y);
            upperEdge.points = colliderpoints;
            upperEdge.transform.SetParent(wallParent.transform);

            EdgeCollider2D lowerEdge = new GameObject("lowerEdge").AddComponent<EdgeCollider2D>();
            colliderpoints = lowerEdge.points;
            colliderpoints[0] = new Vector2(lDCorner.x, lDCorner.y);
            colliderpoints[1] = new Vector2(rUCorner.x, lDCorner.y);
            lowerEdge.points = colliderpoints;
            lowerEdge.transform.SetParent(wallParent.transform);

            EdgeCollider2D leftEdge = new GameObject("leftEdge").AddComponent<EdgeCollider2D>();
            colliderpoints = leftEdge.points;
            colliderpoints[0] = new Vector2(lDCorner.x, lDCorner.y);
            colliderpoints[1] = new Vector2(lDCorner.x, rUCorner.y);
            leftEdge.points = colliderpoints;
            leftEdge.transform.SetParent(wallParent.transform);

            EdgeCollider2D rightEdge = new GameObject("rightEdge").AddComponent<EdgeCollider2D>();

            colliderpoints = rightEdge.points;
            colliderpoints[0] = new Vector2(rUCorner.x, rUCorner.y);
            colliderpoints[1] = new Vector2(rUCorner.x, lDCorner.y);
            rightEdge.points = colliderpoints;
            rightEdge.transform.SetParent(wallParent.transform);
        }

        private void OnInputPointerDown(Vector3 position)
        {
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(position);
            _lineRenderer.SetPosition(0, worldPos);
            _lineRenderer.SetPosition(1, worldPos);
            _lineRenderer.enabled = true;
        }
        private void OnInputPointerUp(Vector3 position)
        {
            _lineRenderer.enabled = false;
        }
        private void OnInputPointer(Vector3 position)
        {
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(position);
            _lineRenderer.SetPosition(1, worldPos);
        }

        private void SaveSprite()
        {
            if (SpriteToSave == null) return;

            SpriteToSave.sprite.texture.SaveToFIle("Saved/" + SpriteToSaveName + ".png");
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();
            if (GUILayout.Button("Save")) SaveSprite();
            GUILayout.EndVertical();
        }

    }

}
