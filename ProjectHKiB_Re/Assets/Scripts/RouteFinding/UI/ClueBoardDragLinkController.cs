using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RouteFinding.UI
{
    // C03 고정 관계도 전용 드래그 연결 입력. 기존 NoteRouteGraphView의 자유 배치/맵 GUID 혼합 링크와
    // 공유하지 않는다. 화면은 이 컨트롤러의 OnConnectionRequested에서 C02 TryConnect를 호출하고,
    // 결과에 맞춰 영구 선·코멘트·실패 표시를 갱신한다.
    public sealed class ClueBoardDragLinkController : MonoBehaviour
    {
        [SerializeField] private RectTransform _edgeLayer;
        [SerializeField] private Color _temporaryLineColor = new Color(0.92f, 0.88f, 0.38f, 0.9f);
        [SerializeField] private float _temporaryLineThickness = 3f;

        private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();
        private ClueBoardDragNode _source;
        private Image _temporaryLine;
        private Canvas _canvas;

        public event Action<string, string> OnConnectionRequested;
        public bool IsDragging => _source != null;
        internal bool IsDraggingFrom(ClueBoardDragNode node) => _source == node;

        private void Awake()
        {
            if (_edgeLayer == null) _edgeLayer = transform as RectTransform;
            if (_edgeLayer == null)
            {
                Debug.LogError("[ClueBoardDragLinkController] Edge layer needs a RectTransform.", this);
                enabled = false;
                return;
            }

            _canvas = GetComponentInParent<Canvas>();
            EnsureTemporaryLine();
        }

        private void OnDisable() => CancelDrag();

        internal bool BeginDrag(ClueBoardDragNode source, PointerEventData eventData)
        {
            if (source == null || !source.CanStartConnection || _source != null) return false;
            _source = source;
            UpdateTemporaryLine(eventData);
            _temporaryLine.gameObject.SetActive(true);
            return true;
        }

        internal void UpdateDrag(PointerEventData eventData)
        {
            if (_source == null) return;
            UpdateTemporaryLine(eventData);
        }

        internal void EndDrag(PointerEventData eventData)
        {
            if (_source == null) return;

            ClueBoardDragNode source = _source;
            ClueBoardDragNode target = FindDropTarget(eventData);
            CancelDrag();

            // 빈 곳·자기 자신·잠김/실루엣/환각 노드에 놓으면 조용히 취소한다. C02는 유효한 두 노드의
            // 관계/보드/진행 상태를 최종 판정하므로 여기서 관계 후보를 미리 드러내지 않는다.
            if (target == null || target == source || !target.CanReceiveConnection) return;
            OnConnectionRequested?.Invoke(source.NodeId, target.NodeId);
        }

        internal void CancelDrag()
        {
            _source = null;
            if (_temporaryLine != null) _temporaryLine.gameObject.SetActive(false);
        }

        private ClueBoardDragNode FindDropTarget(PointerEventData eventData)
        {
            if (EventSystem.current == null) return null;
            _raycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, _raycastResults);
            foreach (RaycastResult result in _raycastResults)
            {
                if (result.gameObject == null) continue;
                var node = result.gameObject.GetComponentInParent<ClueBoardDragNode>();
                if (node != null) return node;
            }
            return null;
        }

        private void EnsureTemporaryLine()
        {
            if (_temporaryLine != null) return;
            var lineObject = new GameObject("TemporaryConnectionLine", typeof(RectTransform), typeof(Image));
            lineObject.transform.SetParent(_edgeLayer, false);
            var lineTransform = (RectTransform)lineObject.transform;
            // ScreenPointToLocalPointInRectangle의 결과는 부모 pivot 기준 좌표다. 이전처럼 선을
            // 좌상단 anchor에 두고 그 값을 그대로 anchoredPosition에 넣으면 부모 크기만큼 어긋나
            // 실제 노드가 아니라 화면 좌상단에서 선이 시작한다. 부모 pivot과 같은 anchor를 사용하면
            // 반환된 local 좌표와 anchoredPosition의 원점이 정확히 일치한다.
            lineTransform.anchorMin = lineTransform.anchorMax = _edgeLayer.pivot;
            lineTransform.pivot = new Vector2(0.5f, 0.5f);
            _temporaryLine = lineObject.GetComponent<Image>();
            _temporaryLine.color = _temporaryLineColor;
            _temporaryLine.raycastTarget = false;
            lineObject.SetActive(false);
        }

        private void UpdateTemporaryLine(PointerEventData eventData)
        {
            if (_source == null || _temporaryLine == null || eventData == null) return;

            // Note/Map 입력과 동일하게 현재 포인터 이벤트가 실제로 사용한 카메라를 우선한다.
            // Screen Space-Camera Canvas에서 canvas.worldCamera를 고정 캐시하면 GraphicRaycaster의
            // eventCamera와 달라질 수 있다.
            Camera eventCamera = eventData.pressEventCamera ?? eventData.enterEventCamera;
            if (eventCamera == null && _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                eventCamera = _canvas.worldCamera;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _edgeLayer, eventData.position, eventCamera, out Vector2 pointer)) return;

            // 노드 중심은 이미 같은 Canvas의 월드 좌표이므로 화면 좌표로 왕복하지 않는다.
            Vector3 sourceWorldCenter = _source.RectTransform.TransformPoint(_source.RectTransform.rect.center);
            Vector2 source = _edgeLayer.InverseTransformPoint(sourceWorldCenter);

            Vector2 direction = pointer - source;
            var lineTransform = (RectTransform)_temporaryLine.transform;
            lineTransform.anchoredPosition = source + direction * 0.5f;
            lineTransform.sizeDelta = new Vector2(Mathf.Max(0.01f, direction.magnitude), _temporaryLineThickness);
            lineTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }
    }

    // NodeView가 생성될 때 연결한다. 고정 위치는 이 컴포넌트가 바꾸지 않으며, 드래그의 의미는 선 연결뿐이다.
    public sealed class ClueBoardDragNode : MonoBehaviour,
        IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private string _nodeId;
        [SerializeField] private bool _canStartConnection = true;
        [SerializeField] private bool _canReceiveConnection = true;

        public string NodeId => _nodeId;
        public bool CanStartConnection => _canStartConnection;
        public bool CanReceiveConnection => _canReceiveConnection;
        public RectTransform RectTransform => transform as RectTransform;
        public ClueBoardDragLinkController Controller { get; private set; }

        public void Bind(
            ClueBoardDragLinkController controller,
            string nodeId,
            bool canStartConnection,
            bool canReceiveConnection)
        {
            Controller = controller;
            _nodeId = nodeId ?? "";
            _canStartConnection = canStartConnection;
            _canReceiveConnection = canReceiveConnection;
        }

        public void OnPointerDown(PointerEventData eventData) { }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (string.IsNullOrWhiteSpace(_nodeId)) return;
            Controller?.BeginDrag(this, eventData);
        }

        public void OnDrag(PointerEventData eventData) => Controller?.UpdateDrag(eventData);

        public void OnEndDrag(PointerEventData eventData) => Controller?.EndDrag(eventData);

        private void OnDisable()
        {
            if (Controller != null && Controller.IsDraggingFrom(this)) Controller.CancelDrag();
        }
    }
}
