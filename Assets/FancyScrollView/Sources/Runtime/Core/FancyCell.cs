/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

using UnityEngine;

namespace FancyScrollView
{
    /// <summary>
    /// <see cref="FancyScrollView{TItemData, TContext}"/> Abstract base class for implementing cells.
    /// <see cref="FancyCell{TItemData, TContext}"/> If you don't need <see cref="Context"/>
    /// Use  <see cref="FancyCell{TItemData}"/> instead.
    /// </summary>
    /// <typeparam name="TItemData">Item data type.</typeparam>
    /// <typeparam name="TContext"><see cref="Context"/>type.</typeparam>
    public abstract class FancyCell<TItemData, TContext> : MonoBehaviour where TContext : class, new()
    {
        /// <summary>
        /// Index of the data displayed in this cell.
        /// </summary>
        public int Index { get; set; } = -1;

        /// <summary>
        /// Visible state of this cell.
        /// </summary>
        public virtual bool IsVisible => gameObject.activeSelf;

        /// <summary>
        /// Refer to <see cref="FancyScrollView{TItemData, TContext}.Context"/> 
        /// The same instance is shared between the cell and the scroll view. Used for passing information and preserving state.
        /// </summary>
        protected TContext Context { get; private set; }

        /// <summary>
        /// <see cref="Context"/> is set.
        /// </summary>
        /// <param name="context">context.</param>
        public virtual void SetContext(TContext context) => Context = context;

        /// <summary>
        /// Initialize.
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Set the visibility of this cell.
        /// </summary>
        /// <param name="visible"><c>true</c> in the visible state, <c>false</c> in the invisible state.</param>
        public virtual void SetVisible(bool visible) => gameObject.SetActive(visible);

        /// <summary>
        /// Update the display contents of this cell based on the item data.
        /// </summary>
        /// <param name="itemData">Item data.</param>
        public abstract void UpdateContent(TItemData itemData);

        /// <summary>
        /// Updates the scroll position of this cell based on the value of <c>0.0f</c> ~ <c>1.0f</c>
        /// </summary>
        /// <param name="position">Normalized scroll position in viewport range.</param>
        public abstract void UpdatePosition(float position);
    }

    /// <summary>
    /// <see cref="FancyScrollView{TItemData}"/> Abstract base class for implementing cells.
    /// </summary>
    /// <typeparam name="TItemData">Item data type.</typeparam>
    /// <seealso cref="FancyCell{TItemData, TContext}"/>
    public abstract class FancyCell<TItemData> : FancyCell<TItemData, NullContext>
    {
        /// <inheritdoc/>
        public sealed override void SetContext(NullContext context) => base.SetContext(context);
    }
}
