/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

using System.Collections.Generic;
using UnityEngine;

namespace FancyScrollView
{
    /// <summary>
    /// An abstract base class for implementing scroll views.
    /// Supports infinite scrolling and snapping.
    /// If you don't need <see cref="FancyScrollView{TItemData, TContext}.Context"/>
    /// Use  <see cref="FancyScrollView{TItemData}"/> instead.
    /// </summary>
    /// <typeparam name="TItemData">Item data type.</typeparam>
    /// <typeparam name="TContext"><see cref="Context"/>type.</typeparam>
    public abstract class FancyScrollView<TItemData, TContext> : MonoBehaviour where TContext : class, new()
    {
        /// <summary>
        /// Spacing between cells.
        /// </summary>
        [SerializeField, Range(1e-2f, 1f)] protected float cellInterval = 0.2f;

        /// <summary>
        /// Criteria for scroll position.
        /// </summary>
        /// <remarks>
        /// For example, if <c>0.5</c> is specified and the scroll position is <c>0</c>, the first cell will be placed in the center.
        /// </remarks>
        [SerializeField, Range(0f, 1f)] protected float scrollOffset = 0.5f;

        /// <summary>
        /// Do you want to cycle the cells?
        /// </summary>
        /// <remarks>
        /// <c>true</c> will put the first cell after the last cell and the last cell before the first cell.
        /// Specify <c>true</c> to implement infinite scrolling.
        /// </remarks>
        [SerializeField] protected bool loop = false;

        /// <summary>
        /// The parent element of the cell <c>Transform</c>.
        /// </summary>
        [SerializeField] protected Transform cellContainer = default;

        readonly IList<FancyCell<TItemData, TContext>> pool = new List<FancyCell<TItemData, TContext>>();

        /// <summary>
        /// Whether it has been initialized.
        /// </summary>
        protected bool initialized;

        /// <summary>
        /// Current scroll position.
        /// </summary>
        protected float currentPosition;

        /// <summary>
        /// Cell Prefab.
        /// </summary>
        protected abstract GameObject CellPrefab { get; }

        /// <summary>
        /// Item list data.
        /// </summary>
        protected IList<TItemData> ItemsSource { get; set; } = new List<TItemData>();

        /// <summary>
        /// <typeparamref name="TContext"/> instance.
        /// The same instance is shared between the cell and the scroll view. Used for passing information and preserving state.
        /// </summary>
        protected TContext Context { get; } = new TContext();

        /// <summary>
        /// Initialize.
        /// </summary>
        /// <remarks>
        /// Called just before the first cell is created.
        /// </remarks>
        protected virtual void Initialize() { }

        /// <summary>
        /// Update the display contents based on the passed item list.
        /// </summary>
        /// <param name="itemsSource">Item list.</param>
        protected virtual void UpdateContents(IList<TItemData> itemsSource)
        {
            ItemsSource = itemsSource;
            Refresh();
        }

        /// <summary>
        /// Force the cell layout to be updated.
        /// </summary>
        protected virtual void Relayout() => UpdatePosition(currentPosition, false);

        /// <summary>
        /// Forcibly update the cell layout and display contents.
        /// </summary>
        protected virtual void Refresh() => UpdatePosition(currentPosition, true);

        /// <summary>
        /// Update the scroll position.
        /// </summary>
        /// <param name="position">scroll position.</param>
        protected virtual void UpdatePosition(float position) => UpdatePosition(position, false);

        void UpdatePosition(float position, bool forceRefresh)
        {
            if (!initialized)
            {
                Initialize();
                initialized = true;
            }

            currentPosition = position;

            var p = position - scrollOffset / cellInterval;
            var firstIndex = Mathf.CeilToInt(p);
            var firstPosition = (Mathf.Ceil(p) - p) * cellInterval;

            if (firstPosition + pool.Count * cellInterval < 1f)
            {
                ResizePool(firstPosition);
            }

            UpdateCells(firstPosition, firstIndex, forceRefresh);
        }

        void ResizePool(float firstPosition)
        {
            Debug.Assert(CellPrefab != null);
            Debug.Assert(cellContainer != null);

            var addCount = Mathf.CeilToInt((1f - firstPosition) / cellInterval) - pool.Count;
            for (var i = 0; i < addCount; i++)
            {
                var cell = Instantiate(CellPrefab, cellContainer).GetComponent<FancyCell<TItemData, TContext>>();
                if (cell == null)
                {
                    throw new MissingComponentException(string.Format(
                        "FancyCell<{0}, {1}> component not found in {2}.",
                        typeof(TItemData).FullName, typeof(TContext).FullName, CellPrefab.name));
                }

                cell.SetContext(Context);
                cell.Initialize();
                cell.SetVisible(false);
                pool.Add(cell);
            }
        }

        void UpdateCells(float firstPosition, int firstIndex, bool forceRefresh)
        {
            for (var i = 0; i < pool.Count; i++)
            {
                var index = firstIndex + i;
                var position = firstPosition + i * cellInterval;
                var cell = pool[CircularIndex(index, pool.Count)];

                if (loop)
                {
                    index = CircularIndex(index, ItemsSource.Count);
                }

                if (index < 0 || index >= ItemsSource.Count || position > 1f)
                {
                    cell.SetVisible(false);
                    continue;
                }

                if (forceRefresh || cell.Index != index || !cell.IsVisible)
                {
                    cell.Index = index;
                    cell.SetVisible(true);
                    cell.UpdateContent(ItemsSource[index]);
                }

                cell.UpdatePosition(position);
            }
        }

        int CircularIndex(int i, int size) => size < 1 ? 0 : i < 0 ? size - 1 + (i + 1) % size : i % size;

#if UNITY_EDITOR
        bool cachedLoop;
        float cachedCellInterval, cachedScrollOffset;

        void LateUpdate()
        {
            if (cachedLoop != loop ||
                cachedCellInterval != cellInterval ||
                cachedScrollOffset != scrollOffset)
            {
                cachedLoop = loop;
                cachedCellInterval = cellInterval;
                cachedScrollOffset = scrollOffset;

                UpdatePosition(currentPosition);
            }
        }
#endif
    }

    /// <summary>
    /// <see cref="FancyScrollView{TItemData}"/> context class.
    /// </summary>
    public sealed class NullContext { }

    /// <summary>
    /// An abstract base class for implementing scroll views.
    /// Supports infinite scrolling and snapping.
    /// </summary>
    /// <typeparam name="TItemData"></typeparam>
    /// <seealso cref="FancyScrollView{TItemData, TContext}"/>
    public abstract class FancyScrollView<TItemData> : FancyScrollView<TItemData, NullContext> { }
}
