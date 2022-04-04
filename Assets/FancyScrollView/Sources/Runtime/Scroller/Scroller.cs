/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using EasingCore;

namespace FancyScrollView
{
    /// <summary>
    /// A component that controls the scroll position.
    /// </summary>
    public class Scroller : UIBehaviour, IPointerUpHandler, IPointerDownHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IScrollHandler
    {
        [SerializeField] RectTransform viewport = default;

        /// <summary>
        /// Viewport size.
        /// </summary>
        public float ViewportSize => scrollDirection == ScrollDirection.Horizontal
            ? viewport.rect.size.x
            : viewport.rect.size.y;

        [SerializeField] ScrollDirection scrollDirection = ScrollDirection.Vertical;

        /// <summary>
        /// Scroll direction.
        /// </summary>
        public ScrollDirection ScrollDirection => scrollDirection;

        [SerializeField] MovementType movementType = MovementType.Elastic;

        /// <summary>
        /// Behavior to use when the content moves beyond the scroll range.
        /// </summary>
        public MovementType MovementType
        {
            get => movementType;
            set => movementType = value;
        }

        [SerializeField] float elasticity = 0.1f;

        /// <summary>
        /// Amount of elasticity to use when the content moves beyond the scroll range.
        /// </summary>
        public float Elasticity
        {
            get => elasticity;
            set => elasticity = value;
        }

        [SerializeField] float scrollSensitivity = 1f;

        /// <summary>
        /// <see cref="ViewportSize"/> The amount of change in the scroll position when dragging from end to end Drag したときのスクロール位置の変化量.
        /// </summary>
        public float ScrollSensitivity
        {
            get => scrollSensitivity;
            set => scrollSensitivity = value;
        }

        [SerializeField] bool inertia = true;

        /// <summary>
        /// Whether to use inertia. Specifying <c>true</c> enables inertia, <c>false</c> disables inertia.
        /// </summary>
        public bool Inertia
        {
            get => inertia;
            set => inertia = value;
        }

        [SerializeField] float decelerationRate = 0.03f;

        /// <summary>
        /// Scroll deceleration rate. Only valid if <see cref="Inertia"/> is <c>true</c>
        /// </summary>
        public float DecelerationRate
        {
            get => decelerationRate;
            set => decelerationRate = value;
        }

        [SerializeField] Snap snap = new Snap {
            Enable = true,
            VelocityThreshold = 0.5f,
            Duration = 0.3f,
            Easing = Ease.InOutCubic
        };

        /// <summary>
        /// <c>true</c> will snap, <c>false</c> will not.
        /// </summary>
        /// <remarks>
        /// If snap is enabled, it will move to the nearest cell just before scrolling stops due to inertia.
        /// </remarks>
        public bool SnapEnabled
        {
            get => snap.Enable;
            set => snap.Enable = value;
        }

        [SerializeField] bool draggable = true;

        /// <summary>
        /// Drag Whether to accept Drag input.
        /// </summary>
        public bool Draggable
        {
            get => draggable;
            set => draggable = value;
        }

        [SerializeField] Scrollbar scrollbar = default;

        /// <summary>
        /// Scrollbar object.
        /// </summary>
        public Scrollbar Scrollbar => scrollbar;

        /// <summary>
        /// Current scroll position.
        /// </summary>
        /// <value></value>
        public float Position
        {
            get => currentPosition;
            set
            {
                autoScrollState.Reset();
                velocity = 0f;
                dragging = false;

                UpdatePosition(value);
            }
        }

        readonly AutoScrollState autoScrollState = new AutoScrollState();

        Action<float> onValueChanged;
        Action<int> onSelectionChanged;

        Vector2 beginDragPointerPosition;
        float scrollStartPosition;
        float prevPosition;
        float currentPosition;

        int totalCount;

        bool hold;
        bool scrolling;
        bool dragging;
        float velocity;

        [Serializable]
        class Snap
        {
            public bool Enable;
            public float VelocityThreshold;
            public float Duration;
            public Ease Easing;
        }

        static readonly EasingFunction DefaultEasingFunction = Easing.Get(Ease.OutCubic);

        class AutoScrollState
        {
            public bool Enable;
            public bool Elastic;
            public float Duration;
            public EasingFunction EasingFunction;
            public float StartTime;
            public float EndPosition;

            public Action OnComplete;

            public void Reset()
            {
                Enable = false;
                Elastic = false;
                Duration = 0f;
                StartTime = 0f;
                EasingFunction = DefaultEasingFunction;
                EndPosition = 0f;
                OnComplete = null;
            }

            public void Complete()
            {
                OnComplete?.Invoke();
                Reset();
            }
        }

        protected override void Start()
        {
            base.Start();

            if (scrollbar)
            {
                scrollbar.onValueChanged.AddListener(x => UpdatePosition(x * (totalCount - 1f), false));
            }
        }

        /// <summary>
        /// Set the callback when the scroll position changes.
        /// </summary>
        /// <param name="callback">Callback when the scroll position changes.</param>
        public void OnValueChanged(Action<float> callback) => onValueChanged = callback;

        /// <summary>
        /// Set the callback when the selected position changes.
        /// </summary>
        /// <param name="callback">Callback when the selected position changes.</param>
        public void OnSelectionChanged(Action<int> callback) => onSelectionChanged = callback;

        /// <summary>
        /// Set the total number of items.
        /// </summary>
        /// <remarks>
        /// <paramref name="totalCount"/> Calculate the maximum scroll position.
        /// </remarks>
        /// <param name="totalCount">Total number of items.</param>
        public void SetTotalCount(int totalCount) => this.totalCount = totalCount;

        /// <summary>
        /// Move to the specified position.
        /// </summary>
        /// <param name="position">Scroll position. <c>0f</c> ~ <c>totalCount - 1f</c> range.</param>
        /// <param name="duration">Seconds to move.</param>
        /// <param name="onComplete">Callback called when the move is completed.</param>
        public void ScrollTo(float position, float duration, Action onComplete = null) => ScrollTo(position, duration, Ease.OutCubic, onComplete);

        /// <summary>
        /// Move to the specified position.
        /// </summary>
        /// <param name="position">Scroll position. <c>0f</c> ~ <c>totalCount - 1f</c> range.</param>
        /// <param name="duration">Seconds to move.</param>
        /// <param name="easing">Easing used for movement.</param>
        /// <param name="onComplete">Callback called when the move is completed.</param>
        public void ScrollTo(float position, float duration, Ease easing, Action onComplete = null) => ScrollTo(position, duration, Easing.Get(easing), onComplete);

        /// <summary>
        /// Move to the specified position.
        /// </summary>
        /// <param name="position">Scroll position. <c>0f</c> ~ <c>totalCount - 1f</c> range.</param>
        /// <param name="duration">Seconds to move.</param>
        /// <param name="easingFunction">Easing function used for movement.</param>
        /// <param name="onComplete">Callback called when the move is completed.</param>
        public void ScrollTo(float position, float duration, EasingFunction easingFunction, Action onComplete = null)
        {
            if (duration <= 0f)
            {
                Position = CircularPosition(position, totalCount);
                onComplete?.Invoke();
                return;
            }

            autoScrollState.Reset();
            autoScrollState.Enable = true;
            autoScrollState.Duration = duration;
            autoScrollState.EasingFunction = easingFunction ?? DefaultEasingFunction;
            autoScrollState.StartTime = Time.unscaledTime;
            autoScrollState.EndPosition = currentPosition + CalculateMovementAmount(currentPosition, position);
            autoScrollState.OnComplete = onComplete;

            velocity = 0f;
            scrollStartPosition = currentPosition;

            UpdateSelection(Mathf.RoundToInt(CircularPosition(autoScrollState.EndPosition, totalCount)));
        }

        /// <summary>
        /// Jump to the position of the specified index.
        /// </summary>
        /// <param name="index">Index of items.</param>
        public void JumpTo(int index)
        {
            if (index < 0 || index > totalCount - 1)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            UpdateSelection(index);
            Position = index;
        }

        /// <summary>
        /// Returns the direction of movement when moving from <paramref name="sourceIndex"/> to <paramref name="destIndex"/>.
        /// If the scroll range is set to unlimited, the shortest travel direction is returned.
        /// </summary>
        /// <param name="sourceIndex">Source index.</param>
        /// <param name="destIndex">Destination index.</param>
        /// <returns></returns>
        public MovementDirection GetMovementDirection(int sourceIndex, int destIndex)
        {
            var movementAmount = CalculateMovementAmount(sourceIndex, destIndex);
            return scrollDirection == ScrollDirection.Horizontal
                ? movementAmount > 0
                    ? MovementDirection.Left
                    : MovementDirection.Right
                : movementAmount > 0
                    ? MovementDirection.Up
                    : MovementDirection.Down;
        }

        /// <inheritdoc/>
        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            if (!draggable || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            hold = true;
            velocity = 0f;
            autoScrollState.Reset();
        }

        /// <inheritdoc/>
        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            if (!draggable || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (hold && snap.Enable)
            {
                UpdateSelection(Mathf.Clamp(Mathf.RoundToInt(currentPosition), 0, totalCount - 1));
                ScrollTo(Mathf.RoundToInt(currentPosition), snap.Duration, snap.Easing);
            }

            hold = false;
        }

        /// <inheritdoc/>
        void IScrollHandler.OnScroll(PointerEventData eventData)
        {
            if (!draggable)
            {
                return;
            }

            var delta = eventData.scrollDelta;

            // Down is positive for scroll events, while in UI system up is positive.
            delta.y *= -1;
            var scrollDelta = scrollDirection == ScrollDirection.Horizontal
                ? Mathf.Abs(delta.y) > Mathf.Abs(delta.x)
                        ? delta.y
                        : delta.x
                : Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                        ? delta.x
                        : delta.y;

            if (eventData.IsScrolling())
            {
                scrolling = true;
            }

            var position = currentPosition + scrollDelta / ViewportSize * scrollSensitivity;
            if (movementType == MovementType.Clamped)
            {
                position += CalculateOffset(position);
            }

            if (autoScrollState.Enable)
            {
                autoScrollState.Reset();
            }

            UpdatePosition(position);
        }

        /// <inheritdoc/>
        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            if (!draggable || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            hold = false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                viewport,
                eventData.position,
                eventData.pressEventCamera,
                out beginDragPointerPosition);

            scrollStartPosition = currentPosition;
            dragging = true;
            autoScrollState.Reset();
        }

        /// <inheritdoc/>
        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            if (!draggable || eventData.button != PointerEventData.InputButton.Left || !dragging)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                viewport,
                eventData.position,
                eventData.pressEventCamera,
                out var dragPointerPosition))
            {
                return;
            }

            var pointerDelta = dragPointerPosition - beginDragPointerPosition;
            var position = (scrollDirection == ScrollDirection.Horizontal ? -pointerDelta.x : pointerDelta.y)
                           / ViewportSize
                           * scrollSensitivity
                           + scrollStartPosition;

            var offset = CalculateOffset(position);
            position += offset;

            if (movementType == MovementType.Elastic)
            {
                if (offset != 0f)
                {
                    position -= RubberDelta(offset, scrollSensitivity);
                }
            }

            UpdatePosition(position);
        }

        /// <inheritdoc/>
        void IEndDragHandler.OnEndDrag(PointerEventData eventData)
        {
            if (!draggable || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            dragging = false;
        }

        float CalculateOffset(float position)
        {
            if (movementType == MovementType.Unrestricted)
            {
                return 0f;
            }

            if (position < 0f)
            {
                return -position;
            }

            if (position > totalCount - 1)
            {
                return totalCount - 1 - position;
            }

            return 0f;
        }

        void UpdatePosition(float position, bool updateScrollbar = true)
        {
            onValueChanged?.Invoke(currentPosition = position);

            if (scrollbar && updateScrollbar)
            {
                scrollbar.value = Mathf.Clamp01(position / Mathf.Max(totalCount - 1f, 1e-4f));
            }
        }

        void UpdateSelection(int index) => onSelectionChanged?.Invoke(index);

        float RubberDelta(float overStretching, float viewSize) =>
            (1 - 1 / (Mathf.Abs(overStretching) * 0.55f / viewSize + 1)) * viewSize * Mathf.Sign(overStretching);

        void Update()
        {
            var deltaTime = Time.unscaledDeltaTime;
            var offset = CalculateOffset(currentPosition);

            if (autoScrollState.Enable)
            {
                var position = 0f;

                if (autoScrollState.Elastic)
                {
                    position = Mathf.SmoothDamp(currentPosition, currentPosition + offset, ref velocity,
                        elasticity, Mathf.Infinity, deltaTime);

                    if (Mathf.Abs(velocity) < 0.01f)
                    {
                        position = Mathf.Clamp(Mathf.RoundToInt(position), 0, totalCount - 1);
                        velocity = 0f;
                        autoScrollState.Complete();
                    }
                }
                else
                {
                    var alpha = Mathf.Clamp01((Time.unscaledTime - autoScrollState.StartTime) /
                                               Mathf.Max(autoScrollState.Duration, float.Epsilon));
                    position = Mathf.LerpUnclamped(scrollStartPosition, autoScrollState.EndPosition,
                        autoScrollState.EasingFunction(alpha));

                    if (Mathf.Approximately(alpha, 1f))
                    {
                        autoScrollState.Complete();
                    }
                }

                UpdatePosition(position);
            }
            else if (!(dragging || scrolling) && (!Mathf.Approximately(offset, 0f) || !Mathf.Approximately(velocity, 0f)))
            {
                var position = currentPosition;

                if (movementType == MovementType.Elastic && !Mathf.Approximately(offset, 0f))
                {
                    autoScrollState.Reset();
                    autoScrollState.Enable = true;
                    autoScrollState.Elastic = true;

                    UpdateSelection(Mathf.Clamp(Mathf.RoundToInt(position), 0, totalCount - 1));
                }
                else if (inertia)
                {
                    velocity *= Mathf.Pow(decelerationRate, deltaTime);

                    if (Mathf.Abs(velocity) < 0.001f)
                    {
                        velocity = 0f;
                    }

                    position += velocity * deltaTime;

                    if (snap.Enable && Mathf.Abs(velocity) < snap.VelocityThreshold)
                    {
                        ScrollTo(Mathf.RoundToInt(currentPosition), snap.Duration, snap.Easing);
                    }
                }
                else
                {
                    velocity = 0f;
                }

                if (!Mathf.Approximately(velocity, 0f))
                {
                    if (movementType == MovementType.Clamped)
                    {
                        offset = CalculateOffset(position);
                        position += offset;

                        if (Mathf.Approximately(position, 0f) || Mathf.Approximately(position, totalCount - 1f))
                        {
                            velocity = 0f;
                            UpdateSelection(Mathf.RoundToInt(position));
                        }
                    }

                    UpdatePosition(position);
                }
            }

            if (!autoScrollState.Enable && (dragging || scrolling) && inertia)
            {
                var newVelocity = (currentPosition - prevPosition) / deltaTime;
                velocity = Mathf.Lerp(velocity, newVelocity, deltaTime * 10f);
            }

            prevPosition = currentPosition;
            scrolling = false;
        }

        float CalculateMovementAmount(float sourcePosition, float destPosition)
        {
            if (movementType != MovementType.Unrestricted)
            {
                return Mathf.Clamp(destPosition, 0, totalCount - 1) - sourcePosition;
            }

            var amount = CircularPosition(destPosition, totalCount) - CircularPosition(sourcePosition, totalCount);

            if (Mathf.Abs(amount) > totalCount * 0.5f)
            {
                amount = Mathf.Sign(-amount) * (totalCount - Mathf.Abs(amount));
            }

            return amount;
        }

        float CircularPosition(float p, int size) => size < 1 ? 0 : p < 0 ? size - 1 + (p + 1) % size : p % size;
    }
}
