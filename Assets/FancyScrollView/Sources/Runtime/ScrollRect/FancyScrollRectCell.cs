/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

using UnityEngine;

namespace FancyScrollView
{
    /// <summary>
    /// <see cref="FancyScrollRect{TItemData, TContext}"/> Abstract base class for implementing cells.
    /// If you don't need <see cref="FancyCell{TItemData, TContext}.Context"/>
    /// Use <see cref="FancyScrollRectCell{TItemData}"/> instead.
    /// </summary>
    /// <typeparam name="TItemData">Item data type.</typeparam>
    /// <typeparam name="TContext"><see cref="FancyCell{TItemData, TContext}.Context"/>type.</typeparam>
    public abstract class FancyScrollRectCell<TItemData, TContext> : FancyCell<TItemData, TContext>
        where TContext : class, IFancyScrollRectContext, new()
    {
        /// <inheritdoc/>
        public override void UpdatePosition(float position)
        {
            var (scrollSize, reuseMargin) = Context.CalculateScrollSize();

            var normalizedPosition = (Mathf.Lerp(0f, scrollSize, position) - reuseMargin) / (scrollSize - reuseMargin * 2f);

            var start = 0.5f * scrollSize;
            var end = -start;

            UpdatePosition(normalizedPosition, Mathf.Lerp(start, end, position));
        }

        /// <summary>
        /// Update the position of this cell.
        /// </summary>
        /// <param name="normalizedPosition">
        /// Normalized scroll position in the viewport range.
        /// based on the value of <see cref="FancyScrollRect{TItemData, TContext}.reuseCellMarginCount"/>
        /// A value outside the range of <c>0.0</c> ~ <c>1.0</c> may be passed.
        /// </param>
        /// <param name="localPosition">local location.</param>
        protected virtual void UpdatePosition(float normalizedPosition, float localPosition)
        {
            transform.localPosition = Context.ScrollDirection == ScrollDirection.Horizontal
                ? new Vector2(-localPosition, 0)
                : new Vector2(0, localPosition);
        }
    }

    /// <summary>
    /// <see cref="FancyScrollRect{TItemData}"/> Abstract base class for implementing cells.
    /// </summary>
    /// <typeparam name="TItemData">Item data type.</typeparam>
    /// <seealso cref="FancyScrollRectCell{TItemData, TContext}"/>
    public abstract class FancyScrollRectCell<TItemData> : FancyScrollRectCell<TItemData, FancyScrollRectContext>
    {
        /// <inheritdoc/>
        public sealed override void SetContext(FancyScrollRectContext context) => base.SetContext(context);
    }
}
