/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using EasingCore;

namespace FancyScrollView
{
    /// <summary>
    /// An abstract base class for implementing a scroll view of a grid layout.
    /// Infinite scrolling and snapping are not supported.
    /// If you don't need <see cref="FancyScrollView{TItemData, TContext}.Context"/>
    /// Use <see cref="FancyGridView{TItemData}"/> instead.
    /// </summary>
    /// <typeparam name="TItemData">Item data type.</typeparam>
    /// <typeparam name="TContext"><see cref="FancyScrollView{TItemData, TContext}.Context"/>type.</typeparam>
    public abstract class FancyGridView<TItemData, TContext> : FancyScrollRect<TItemData[], TContext>
        where TContext : class, IFancyGridViewContext, new()
    {
        /// <summary>
        /// Default cell group class.
        /// </summary>
        protected abstract class DefaultCellGroup : FancyCellGroup<TItemData, TContext> { }

        /// <summary>
        /// Margins between cells in the axial direction where the cells are placed first.
        /// </summary>
        [SerializeField] protected float startAxisSpacing = 0f;

        /// <summary>
        /// Number of cells in the axial direction to place cells first.
        /// </summary>
        [SerializeField] protected int startAxisCellCount = 4;

        /// <summary>
        /// Cell size.
        /// </summary>
        [SerializeField] protected Vector2 cellSize = new Vector2(100f, 100f);

        /// <summary>
        /// Cell group Prefab.
        /// </summary>
        /// <remarks>
        /// <see cref="FancyGridView{TItemData, TContext}"/> then,
        /// <see cref="FancyScrollView{TItemData, TContext}.CellPrefab"/> is used as the axial cell container to place the cell first.
        /// </remarks>
        protected sealed override GameObject CellPrefab => cellGroupTemplate;

        /// <inheritdoc/>
        protected override float CellSize => Scroller.ScrollDirection == ScrollDirection.Horizontal
            ? cellSize.x
            : cellSize.y;

        /// <summary>
        /// Total number of items.
        /// </summary>
        public int DataCount { get; private set; }

        GameObject cellGroupTemplate;

        /// <inheritdoc/>
        protected override void Initialize()
        {
            base.Initialize();

            Debug.Assert(startAxisCellCount > 0);

            Context.ScrollDirection = Scroller.ScrollDirection;
            Context.GetGroupCount = () => startAxisCellCount;
            Context.GetStartAxisSpacing = () => startAxisSpacing;
            Context.GetCellSize = () => Scroller.ScrollDirection == ScrollDirection.Horizontal
                ? cellSize.y
                : cellSize.x;

            SetupCellTemplate();
        }

        /// <summary>
        /// Called just before the first cell is created.
        /// <see cref="Setup{TGroup}(FancyCell{TItemData, TContext})"/> Use the method to set up the cell template.
        /// </summary>
        /// <example>
        /// <code><![CDATA[
        /// using UnityEngine;
        /// using FancyScrollView;
        /// 
        /// public class MyGridView : FancyGridView<ItemData, Context>
        /// {
        ///     class CellGroup : DefaultCellGroup { }
        /// 
        ///     [SerializeField] Cell cellPrefab = default;
        /// 
        ///     protected override void SetupCellTemplate() => Setup<CellGroup>(cellPrefab);
        /// }
        /// ]]></code>
        /// </example>
        protected abstract void SetupCellTemplate();

        /// <summary>
        /// Set up the cell template.
        /// </summary>
        /// <param name="cellTemplate">Cell template.</param>
        /// <typeparam name="TGroup">Cell group type.</typeparam>
        protected virtual void Setup<TGroup>(FancyCell<TItemData, TContext> cellTemplate)
            where TGroup : FancyCell<TItemData[], TContext>
        {
            Context.CellTemplate = cellTemplate.gameObject;

            cellGroupTemplate = new GameObject("Group").AddComponent<TGroup>().gameObject;
            cellGroupTemplate.transform.SetParent(cellContainer, false);
            cellGroupTemplate.SetActive(false);
        }

        /// <summary>
        /// Update the display contents based on the passed item list.
        /// </summary>
        /// <param name="items">Item list.</param>
        public virtual void UpdateContents(IList<TItemData> items)
        {
            DataCount = items.Count;

            var itemGroups = items
                .Select((item, index) => (item, index))
                .GroupBy(
                    x => x.index / startAxisCellCount,
                    x => x.item)
                .Select(group => group.ToArray())
                .ToArray();

            UpdateContents(itemGroups);
        }

        /// <summary>
        /// Jumps to the position of the specified item.
        /// </summary>
        /// <param name="itemIndex">Item index.</param>
        /// <param name="alignment">Criteria for cell position in viewport. 0f(top) ~ 1f(end).</param>
        protected override void JumpTo(int itemIndex, float alignment = 0.5f)
        {
            var groupIndex = itemIndex / startAxisCellCount;
            base.JumpTo(groupIndex, alignment);
        }

        /// <summary>
        /// Moves to the position of the specified item.
        /// </summary>
        /// <param name="itemIndex">Item index.</param>
        /// <param name="duration">Seconds to move.</param>
        /// <param name="alignment">Criteria for cell position in viewport. 0f(top) ~ 1f(end).</param>
        /// <param name="onComplete">Callback called when the move is completed.</param>
        protected override void ScrollTo(int itemIndex, float duration, float alignment = 0.5f, Action onComplete = null)
        {
            var groupIndex = itemIndex / startAxisCellCount;
            base.ScrollTo(groupIndex, duration, alignment, onComplete);
        }

        /// <summary>
        /// Moves to the position of the specified item.
        /// </summary>
        /// <param name="itemIndex">Item index.</param>
        /// <param name="duration">Seconds to move.</param>
        /// <param name="easing">Easing used for movement.</param>
        /// <param name="alignment">Criteria for cell position in viewport. 0f(top) ~ 1f(end).</param>
        /// <param name="onComplete">Callback called when the move is completed.</param>
        protected override void ScrollTo(int itemIndex, float duration, Ease easing, float alignment = 0.5f, Action onComplete = null)
        {
            var groupIndex = itemIndex / startAxisCellCount;
            base.ScrollTo(groupIndex, duration, easing, alignment, onComplete);
        }
    }

    /// <summary>
    /// An abstract base class for implementing a scroll view of a grid layout.
    /// Infinite scrolling and snapping are not supported.
    /// </summary>
    /// <typeparam name="TItemData">Item data type.</typeparam>
    /// <seealso cref="FancyGridView{TItemData, TContext}"/>
    public abstract class FancyGridView<TItemData> : FancyGridView<TItemData, FancyGridViewContext> { }
}
