using System.Collections;
using UnityEngine;
using WinterCrestal.Utils;
using Random = UnityEngine.Random;

namespace WinterCrestal.SpriteCutter
{
    public class DemoScene0 : SpriteCutterBase
    {
        public enum TestCutType
        {
            CUSTOM, RANDOM,
            HORIZONTAL_SLANTED_POSITIVE_SLOPE, HORIZONTAL_SLANTED_NEGATIVE_SLOPE, HORIZONTAL,
            VERTICAL_SLANTED_POSITIVE_SLOPE, VERTICAL_SLANTED_NEGATIVE_SLOPE, VERTICAL,
            CORNER_BOTTOM_LEFT, CORNER_BOTTOM_RIGHT, CORNER_TOP_LEFT, CORNER_TOP_RIGHT
        }

        [Space, Header("Debug")]
        [SerializeField] private SpriteRenderer _testSpriteRenderer;
        [SerializeField] private bool allowInput = true;
        private TestCutType _cutType;
        private Vector2Int p1;
        private Vector2Int p2;

        private LineRenderer _lineRenderer;
        private SpriteRenderer _lineEndPoint0, _lineEndPoint1;

        private Camera _camera;

        private void Awake()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.enabled = false;

            _lineEndPoint0 = transform.GetChild(0).GetComponent<SpriteRenderer>();
            _lineEndPoint1 = transform.GetChild(1).GetComponent<SpriteRenderer>();

            _lineEndPoint0.enabled = false;
            _lineEndPoint1.enabled = false;

            _spriteCutterInputManager.SpriteRenderersToCut = new[] { _testSpriteRenderer };
            _camera = Camera.main;
            GenerateCollidersAcrossScreen();
        }

        void Update()
        {
            var pos = _testSpriteRenderer.transform.position;
            pos.x = Mathf.Sin(Time.time);
            _testSpriteRenderer.transform.position = pos;
            _testSpriteRenderer.transform.Rotate(0, 0, Time.deltaTime * 30f);

            if (Input.GetKeyDown(KeyCode.S))
            {
                ChooseCutType();
            }
        }

        protected override void OnInputPointerDown(Vector3 position)
        {
            if(!allowInput) return;
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(position);
            _lineEndPoint0.transform.position = worldPos;
            _lineEndPoint1.transform.position = worldPos;
            _lineEndPoint0.enabled = true;
            _lineEndPoint1.enabled = true;

            _lineRenderer.SetPosition(0, worldPos);
            _lineRenderer.SetPosition(1, worldPos);
            _lineRenderer.enabled = true;

            if (_splitSprite0.SpriteRenderer != null) { Destroy(_splitSprite0.SpriteRenderer.gameObject); _splitSprite0.SpriteRenderer = null; _splitSprite0.Rect = default; }
            if (_splitSprite1.SpriteRenderer != null) { Destroy(_splitSprite1.SpriteRenderer.gameObject); _splitSprite1.SpriteRenderer = null; _splitSprite1.Rect = default; }
        }
        protected override void OnInputPointerUp(Vector3 position)
        {
            _lineEndPoint0.enabled = false;
            _lineEndPoint1.enabled = false;
            _lineRenderer.enabled = false;
        }
        protected override void OnInputPointer(Vector3 position)
        {
            if (!allowInput) return;
            Vector2 worldPos = Camera.main.ScreenToWorldPoint(position);
            _lineEndPoint1.transform.position = worldPos;
            _lineRenderer.SetPosition(1, worldPos);
        }
        protected override void OnSpriteRendererCut(SpriteRenderer original, SpriteCutterInputManager.SplitSprite s0, SpriteCutterInputManager.SplitSprite s1)
        {  
            base.OnSpriteRendererCut(original, s0, s1);

            _testSpriteRenderer.gameObject.SetActive(false);
            StartCoroutine(ShowDelayed());

            AddPhysics(s0.SpriteRenderer);
            AddPhysics(s1.SpriteRenderer);
        }

        private void AddPhysics(SpriteRenderer renderer)
        {
            renderer.gameObject.AddComponent<Rigidbody2D>();
            var poly = renderer.gameObject.AddComponent<PolygonCollider2D>();
            // Optimize the polygon collider 2D
        }

        private IEnumerator ShowDelayed()
        {
            yield return new WaitForSeconds(1f);
            _testSpriteRenderer.gameObject.SetActive(true);
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

        private void ChooseCutType()
        {
            int w = (int)_testSpriteRenderer.sprite.rect.width;
            int h = (int)_testSpriteRenderer.sprite.rect.height;

            int w14 = (int)(w * .25f);
            int w24 = (int)(w * .5f);
            int w34 = (int)(w * .75f);

            int h14 = (int)(h * .25f);
            int h24 = (int)(h * .5f);
            int h34 = (int)(h * .75f);

            switch (_cutType)
            {
                case TestCutType.RANDOM:
                    int rx1 = 0, rx2 = 0, ry1 = 0, ry2 = 0;
                    int r = Random.Range(0, 4);
                    if (r == 0)
                    {
                        rx1 = (int)Random.Range(0f, w);
                        rx2 = (int)Random.Range(0f, w);
                        int t = Random.Range(0, 2);
                        if (t == 0) { ry1 = 0; ry2 = h; }
                        else if (t == 1) { ry2 = 0; ry1 = h; }
                        else if (t == 2) { ry1 = 0; ry2 = h; }
                        else { ry1 = h; ry2 = (int)Random.Range(0f, h); }
                    }
                    else if (r == 1)
                    {
                        ry1 = (int)Random.Range(0f, h);
                        ry2 = (int)Random.Range(0f, h);
                        int t = Random.Range(0, 2);
                        if (t == 0) { rx1 = 0; rx2 = w; }
                        else if (t == 1) { rx2 = 0; rx1 = w; }
                        else if (t == 2) { rx1 = 0; rx2 = (int)Random.Range(0f, w); }
                        else { rx1 = w; rx2 = (int)Random.Range(0f, w); }
                    }
                    else if (r == 2)
                    {
                        int t = Random.Range(0, 2);
                        if (t == 0)
                        {
                            rx1 = 0;
                            ry1 = (int)Random.Range(0f, _testSpriteRenderer.sprite.rect.height);
                            rx2 = (int)Random.Range(0f, w);
                            if (Random.Range(0, 2) == 0) ry2 = 0;
                            else ry2 = (int)_testSpriteRenderer.sprite.rect.height;
                        }
                        else
                        {
                            rx1 = w;
                            ry1 = (int)Random.Range(0f, _testSpriteRenderer.sprite.rect.height);
                            rx2 = (int)Random.Range(0f, w);
                            if (Random.Range(0, 2) == 0) ry2 = 0;
                            else ry2 = (int)_testSpriteRenderer.sprite.rect.height;
                        }

                    }
                    else
                    {
                        int t = Random.Range(0, 2);
                        if (t == 0)
                        {
                            rx1 = (int)Random.Range(0f, w);
                            ry1 = 0;
                            ry2 = (int)Random.Range(0f, _testSpriteRenderer.sprite.rect.height);
                            if (Random.Range(0, 2) == 0) rx2 = 0;
                            else rx2 = w;
                        }
                        else
                        {
                            rx1 = (int)Random.Range(0f, w);
                            ry1 = (int)_testSpriteRenderer.sprite.rect.height;
                            ry2 = (int)Random.Range(0f, _testSpriteRenderer.sprite.rect.height);
                            if (Random.Range(0, 2) == 0) rx2 = 0;
                            else rx2 = w;
                        }
                    }

                    p1 = new(rx1, ry1);
                    p2 = new(rx2, ry2);
                    break;

                case TestCutType.HORIZONTAL_SLANTED_POSITIVE_SLOPE:
                    p1 = new(0, h14);
                    p2 = new(w, h34);
                    break;

                case TestCutType.HORIZONTAL_SLANTED_NEGATIVE_SLOPE:
                    p1 = new(0, h34);
                    p2 = new(w, h14);
                    break;

                case TestCutType.HORIZONTAL:
                    p1 = new(0, h24);
                    p2 = new(w, h24);
                    break;

                case TestCutType.VERTICAL_SLANTED_POSITIVE_SLOPE:
                    p1 = new(w14, 0);
                    p2 = new(w34, h);
                    break;

                case TestCutType.VERTICAL_SLANTED_NEGATIVE_SLOPE:
                    p1 = new(w34, 0);
                    p2 = new(w14, h);
                    break;

                case TestCutType.VERTICAL:
                    p1 = new(w24, 0);
                    p2 = new(w24, h);
                    break;

                case TestCutType.CORNER_BOTTOM_LEFT:
                    p1 = new(0, h24);
                    p2 = new(w24, 0);
                    break;

                case TestCutType.CORNER_BOTTOM_RIGHT:
                    p1 = new(w24, 0);
                    p2 = new(w, h24);
                    break;

                case TestCutType.CORNER_TOP_LEFT:
                    p1 = new(0, h24);
                    p2 = new(w24, h);
                    break;

                case TestCutType.CORNER_TOP_RIGHT:
                    p1 = new(w24, h);
                    p2 = new(w, h24);
                    break;

                default: break;
            }

            if (_splitSprite0.SpriteRenderer != null) { Destroy(_splitSprite0.SpriteRenderer.gameObject); _splitSprite0.SpriteRenderer = null; _splitSprite0.Rect = default; }
            if (_splitSprite1.SpriteRenderer != null) { Destroy(_splitSprite1.SpriteRenderer.gameObject); _splitSprite1.SpriteRenderer = null; _splitSprite1.Rect = default; }


            if (_testSpriteRenderer.sprite.CutSprite(p1, p2, out _splitSprite0.SpriteRenderer, out _splitSprite1.SpriteRenderer, out _splitSprite0.Rect, out _splitSprite1.Rect))
            {
                OnSpriteRendererCut(_testSpriteRenderer, _splitSprite0, _splitSprite1);
            }
        }

#if UNITY_EDITOR
        private bool showDropdown = false;
        private void OnGUI()
        {
            GUILayout.BeginVertical();
            showDropdown = ExtendedGUIUtils.Dropdown(_cutType.ToString(), showDropdown, ref _cutType, out bool hasChosen);
            if(hasChosen)
            {
                ChooseCutType();
            }

            if(!showDropdown)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Point 0: ");
                GUILayout.TextField(p1.x.ToString());
                GUILayout.Label("x");
                GUILayout.TextField(p1.y.ToString());
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Point 1: ");
                GUILayout.TextField(p2.x.ToString());
                GUILayout.Label("x");
                GUILayout.TextField(p2.y.ToString());
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();
        }


        private void OnDrawGizmos()
        {
            if (_testSpriteRenderer != null)
            {
                Gizmos.color = Color.yellow;
                DrawBounds(_testSpriteRenderer.localBounds, _testSpriteRenderer.transform.position, _testSpriteRenderer.transform.rotation);
                Gizmos.color = Color.red;
                DrawBounds(_testSpriteRenderer.bounds, Vector3.zero, Quaternion.identity);
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(_testSpriteRenderer.transform.position, .1f);
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

