/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

using System;
using System.Collections.Generic;
using UnityEngine;
using EasingCore;

namespace FancyScrollView
{
    /// <summary>
    /// An abstract base class for implementing a ScrollRect-style scroll view.
    /// Infinite scrolling and snapping are not supported.
    /// If you don't need <see cref="FancyScrollView{TItemData, TContext}.Context"/>
    /// Use <see cref="FancyScrollRect{TItemData}"/> instead.
    /// </summary>
    /// <typeparam name="TItemData">Item data type.</typeparam>
    /// <typeparam name="TContext"><see cref="FancyScrollView{TItemData, TContext}.Context"/>type.</typeparam>
    [RequireComponent(typeof(Scroller))]
    public abstract class FancyScrollRect<TItemData, TContext> : FancyScrollView<TItemData, TContext>
        where TContext : class, IFancyScrollRectContext, new()
    {
        /// <summary>
        /// Number of cells in the margin before the cells are reused while scrolling.
        /// </summary>
        /// <remarks>
        /// If  <c>0</c> is specified, the cell will be reused immediately after it is completely hidden.
        /// <c>1</c> If you specify the above, it will be reused after scrolling extra by the number of cells.
        /// </remarks>
        [SerializeField] protected float reuseCellMarginCount = 0f;

        /// <summary>
        /// Margin at the beginning of the content..
        /// </summary>
        [SerializeField] protected float paddingHead = 0f;

        /// <summary>
        /// Margins at the end of the content.
        /// </summary>
        [SerializeField] protected float paddingTail = 0f;

        /// <summary>
        /// Margins between cells in the scroll axis direction.
        /// </summary>
        [SerializeField] protected float spacing = 0f;

        /// <summary>
        /// Cell size.
        /// </summary>
        protected abstract float CellSize { get; }

        /// <summary>
        /// Whether scrolling is possible.
        /// </summary>
        /// <remarks>
        /// If the number of items is small enough and all cells fit in the viewport, it will be <c>false</c>, otherwise it will be <c>true</c>.
        /// </remarks>
        protected virtual bool Scrollable => MaxScrollPosition > 0f;

        Scroller cachedScroller;

        /// <summary>
        /// An instance of <see cref="FancyScrollView.Scroller"/> that controls the scroll position.
        /// </summary>
        /// <remarks>
        /// When changing the scroll position of <see cref="Scroller"/>, be sure to use the position converted using <see cref="ToScrollerPosition(float)"/>.
        /// </remarks>
        protected Scroller Scroller => cachedScroller ?? (cachedScroller = GetComponent<Scroller>());

        float ScrollLength => 1f / Mathf.Max(cellInterval, 1e-2f) - 1f;

        float ViewportLength => ScrollLength - reuseCellMarginCount * 2f;

        float PaddingHeadLength => (paddingHead - spacing * 0.5f) / (CellSize + spacing);

        float MaxScrollPosition => ItemsSource.Count
            - ScrollLength
            + reuseCellMarginCount * 2f
            + (paddingHead + paddingTail - spacing) / (CellSize + spacing);

        /// <inheritdoc/>
        protected override void Initialize()
        {
            base.Initialize();

            Context.ScrollDirection = Scroller.ScrollDirection;
            Context.CalculateScrollSize = () =>
            {
                var interval = CellSize + spacing;
                var reuseMargin = interval * reuseCellMarginCount;
                var scrollSize = Scroller.ViewportSize + interval + reuseMargin * 2f;
                return (scrollSize, reuseMargin);
            };

            AdjustCellIntervalAndScrollOffset();
            Scroller.OnValueChanged(OnScrollerValueChanged);
        }

        /// <summary>
        /// <see cref="Scroller"/> Processing when the scroll position is changed..
        /// </summary>
        /// <param name="p"><see cref="Scroller"/> scroll position.</param>
        void OnScrollerValueChanged(float p)
        {
            base.UpdatePosition(ToFancyScrollViewPosition(Scrollable ? p : 0f));

            if (Scroller.Scrollbar)
            {
                if (p > ItemsSource.Count - 1)
                {
                    ShrinkScrollbar(p - (ItemsSource.Count - 1));
                }
                else if (p < 0f)
                {
                    ShrinkScrollbar(-p);
                }
            }
        }

        /// <summary>
        /// Reduce the size of the scrollbar based on the amount scrolled beyond the scroll range.
        /// </summary>
        /// <param name="offset">Amount scrolled beyond the scroll range.</param>
        void ShrinkScrollbar(float offset)
        {
            var scale = 1f - ToFancyScrollViewPosition(offset) / (ViewportLength - PaddingHeadLength);
            UpdateScrollbarSize((ViewportLength - PaddingHeadLength) * scale);
        }

        /// <inheritdoc/>
        protected override void Refresh()
        {
            AdjustCellIntervalAndScrollOffset();
            RefreshScroller();
            base.Refresh();
        }

        /// <inheritdoc/>
        protected override void Relayout()
        {
            AdjustCellIntervalAndScrollOffset();
            RefreshScroller();
            base.Relayout();
        }

        /// <summary>
        /// <see cref="Scroller"/> Update various states.
        /// </summary>
        protected void RefreshScroller()
        {
            Scroller.Draggable = Scrollable;
            Scroller.ScrollSensitivity = ToScrollerPosition(ViewportLength - PaddingHeadLength);
            Scroller.Position = ToScrollerPosition(currentPosition);

            if (Scroller.Scrollbar)
            {
                Scroller.Scrollbar.gameObject.SetActive(Scrollable);
                UpdateScrollbarSize(ViewportLength);
            }
        }

        /// <inheritdoc/>
        protected override void UpdateContents(IList<TItemData> items)
        {
            AdjustCellIntervalAndScrollOffset();
            base.UpdateContents(items);

            Scroller.SetTotalCount(items.Count);
            RefreshScroller();
        }

        /// <summary>
        /// Update the scroll position.
        /// </summary>
        /// <param name="position">scroll position.</param>
        protected new void UpdatePosition(float position)
        {
            Scroller.Position = ToScrollerPosition(position, 0.5f);
        }

        /// <summary>
        /// Jumps to the position of the specified item.
        /// </summary>
        /// <param name="itemIndex">Item index.</param>
        /// <param name="alignment">Criteria for cell position in viewport. 0f(top) ~ 1f(end).</param>
        protected virtual void JumpTo(int itemIndex, float alignment = 0.5f)
        {
            Scroller.Position = ToScrollerPosition(itemIndex, alignment);
        }

        /// <summary>
        /// Moves to the position of the specified item.
        /// </summary>
        /// <param name="index">Index of items.</param>
        /// <param name="duration">Seconds to move.</param>
        /// <param name="alignment">Criteria for cell position in viewport. 0f(top) ~ 1f(end).</param>
        /// <param name="onComplete">Callback called when the move is completed.</param>
        protected virtual void ScrollTo(int index, float duration, float alignment = 0.5f, Action onComplete = null)
        {
            Scroller.ScrollTo(ToScrollerPosition(index, alignment), duration, onComplete);
        }

        /// <summary>
        /// Moves to the position of the specified item.
        /// </summary>
        /// <param name="index">Index of items.</param>
        /// <param name="duration">Seconds to move.</param>
        /// <param name="easing">Easing used for movement.</param>
        /// <param name="alignment">Criteria for cell position in viewport. 0f(top) ~ 1f(end).</param>
        /// <param name="onComplete">Callback called when the move is completed.</param>
        protected virtual void ScrollTo(int index, float duration, Ease easing, float alignment = 0.5f, Action onComplete = null)
        {
            Scroller.ScrollTo(ToScrollerPosition(index, alignment), duration, easing, onComplete);
        }

        /// <summary>
        /// Update the scrollbar size based on the viewport and content length.
        /// </summary>
        /// <param name="viewportLength">Viewport size.</param>
        protected void UpdateScrollbarSize(float viewportLength)
        {
            var contentLength = Mathf.Max(ItemsSource.Count + (paddingHead + paddingTail - spacing) / (CellSize + spacing), 1);
            Scroller.Scrollbar.size = Scrollable ? Mathf.Clamp01(viewportLength / contentLength) : 1f;
        }

        /// <summary>
        /// <see cref="Scroller"/> Converts the scroll position handled by <see cref="FancyScrollRect{TItemData, TContext}"/> to the scroll position handled.
        /// </summary>
        /// <param name="position"><see cref="Scroller"/> handles scroll positions.</param>
        /// <returns>Scroll position handled by <see cref="FancyScrollRect{TItemData, TContext}"/></returns>
        protected float ToFancyScrollViewPosition(float position)
        {
            return position / Mathf.Max(ItemsSource.Count - 1, 1) * MaxScrollPosition - PaddingHeadLength;
        }

        /// <summary>
        /// <see cref="FancyScrollRect{TItemData, TContext}"/> Converts the scroll position handled by <see cref="Scroller"/> to the scroll position handled.
        /// </summary>
        /// <param name="position">Scroll position handled by <see cref="FancyScrollRect{TItemData, TContext}"/></param>
        /// <returns><see cref="Scroller"/> handles scroll positions.</returns>
        protected float ToScrollerPosition(float position)
        {
            return (position + PaddingHeadLength) / MaxScrollPosition * Mathf.Max(ItemsSource.Count - 1, 1);
        }

        /// <summary>
        /// <see cref="FancyScrollRect{TItemData, TContext}"/> Converts the scroll position handled by <see cref="Scroller"/> to the scroll position handled.
        /// </summary>
        /// <param name="position">Scroll position handled by <see cref="FancyScrollRect{TItemData, TContext}"/></param>
        /// <param name="alignment">Criteria for cell position in viewport. 0f(top) ~ 1f(end).</param>
        /// <returns><see cref="Scroller"/> handles scroll positions.</returns>
        protected float ToScrollerPosition(float position, float alignment = 0.5f)
        {
            var offset = alignment * (ScrollLength - (1f + reuseCellMarginCount * 2f))
                + (1f - alignment - 0.5f) * spacing / (CellSize + spacing);
            return ToScrollerPosition(Mathf.Clamp(position - offset, 0f, MaxScrollPosition));
        }

        /// <summary>
        /// To achieve the specified settings
        /// <see cref="FancyScrollView{TItemData,TContext}.cellInterval"/> When
        /// <see cref="FancyScrollView{TItemData,TContext}.scrollOffset"/> is calculated and applied.
        /// </summary>
        protected void AdjustCellIntervalAndScrollOffset()
        {
            var totalSize = Scroller.ViewportSize + (CellSize + spacing) * (1f + reuseCellMarginCount * 2f);
            cellInterval = (CellSize + spacing) / totalSize;
            scrollOffset = cellInterval * (1f + reuseCellMarginCount);
        }

        protected virtual void OnValidate()
        {
            AdjustCellIntervalAndScrollOffset();

            if (loop)
            {
                loop = false;
                Debug.LogError("Loop is currently not supported in FancyScrollRect.");
            }

            if (Scroller.SnapEnabled)
            {
                Scroller.SnapEnabled = false;
                Debug.LogError("Snap is currently not supported in FancyScrollRect.");
            }

            if (Scroller.MovementType == MovementType.Unrestricted)
            {
                Scroller.MovementType = MovementType.Elastic;
                Debug.LogError("MovementType.Unrestricted is currently not supported in FancyScrollRect.");
            }
        }
    }

    /// <summary>
    /// An abstract base class for implementing a ScrollRect-style scroll view.
    /// Infinite scrolling and snapping are not supported.
    /// </summary>
    /// <typeparam name="TItemData">アイテムのデータ型.</typeparam>
    /// <seealso cref="FancyScrollRect{TItemData, TContext}"/>
    public abstract class FancyScrollRect<TItemData> : FancyScrollRect<TItemData, FancyScrollRectContext> { }
}
