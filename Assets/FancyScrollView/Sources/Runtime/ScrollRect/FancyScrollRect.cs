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
    /// ScrollRect is an abstract base class for implementing a scrolling view.
    /// <see cref="FancyScrollView{TItemData, TContext}.Context"/> が不要な場合は
    /// 代わりに <see cref="FancyScrollRect{TItemData}"/> を使用します.
    /// </summary>
    /// <typeparam name="TItemData">Item data type.</typeparam>
    /// <typeparam name="TContext"><see cref="FancyScrollView{TItemData, TContext}.Context"/> の型.</typeparam>
    [RequireComponent(typeof(Scroller))]
    public abstract class FancyScrollRect<TItemData, TContext> : FancyScrollView<TItemData, TContext>
        where TContext : class, IFancyScrollRectContext, new()
    {
        /// <summary>
        /// Number of cells in the margin before the cells are reused while scrolling.
        /// </summary>
        /// <remarks>
        /// If <c> 0 </c> is specified, the cell will be reused immediately after it is completely hidden.
        /// <c> 1 </c> If you specify more than that, it will be reused after scrolling extra by the number of cells.
        /// </remarks>
        [SerializeField] protected float reuseCellMarginCount = 0f;
        [SerializeField] protected float paddingHead = 0f;
        [SerializeField] protected float paddingTail = 0f;

        /// <summary>
        /// Margins between cells in the scroll axis direction.
        /// </summary>
        [SerializeField] protected float spacing = 0f;

        protected abstract float CellSize { get; }

        /// <remarks>
        /// <c> false </c> if the number of items is small enough and all cells fit in the viewport, otherwise <c> true </c>.
        /// </remarks>
        protected virtual bool Scrollable => MaxScrollPosition > 0f;

        private Scroller cachedScroller;

        /// <summary>
        /// Instance of <see cref="FancyScrollView.Scroller"/> that controls scroll position.
        /// </summary>
        /// <remarks>
        /// <see cref="Scroller"/> When changing the scroll position, be sure to use the convert position using <see cref="ToScrollerPosition(float)"/>.
        /// </remarks>
        protected Scroller Scroller => cachedScroller == null ? cachedScroller = GetComponent<Scroller>() : cachedScroller;

        private float ScrollLength => 1f / Mathf.Max(cellInterval, 1e-2f) - 1f;
        private float ViewportLength => ScrollLength - reuseCellMarginCount * 2f;
        private float PaddingHeadLength => (paddingHead - spacing * 0.5f) / (CellSize + spacing);
        private float MaxScrollPosition => ItemsSource.Count
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
        /// <see cref="Scroller"/> Processing when the scroll position of is changed.
        /// </summary>
        /// <param name="p"><see cref="Scroller"/> Scroll position.</param>
        private void OnScrollerValueChanged(float p)
        {
            base.UpdatePosition(ToFancyScrollViewPosition(Scrollable ? p : 0f));

            if (!Scroller.Scrollbar) return;

            if (p > ItemsSource.Count - 1)
                ShrinkScrollbar(p - (ItemsSource.Count - 1));
            else if (p < 0f)
                ShrinkScrollbar(-p);
        }

        /// <summary>
        /// Reduces the size of the scrollbar based on the amount scrolled beyond the scroll range.
        /// </summary>
        /// <param name="offset">The amount scrolled beyond the scroll range.</param>
        private void ShrinkScrollbar(float offset)
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
        /// <see cref="Scroller"/> Update various states of the scroller.
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
        /// <param name="position">Scroll position</param>
        protected new void UpdatePosition(float position)
        {
            Scroller.Position = ToScrollerPosition(position, 0.5f);
        }

        /// <summary>
        /// Jumps to the position of the specified item immediately.
        /// </summary>
        /// <param name="itemIndex">Item index.</param>
        /// <param name="alignment">Criteria for cell position in the viewport. 0f (top) ~ 1f (end).</param>
        protected virtual void JumpTo(int itemIndex, float alignment = 0.5f)
        {
            Scroller.Position = ToScrollerPosition(itemIndex, alignment);
        }

        /// <summary>
        /// Moves to the position of the specified item.
        /// </summary>
        /// <param name="index">Item index.</param>
        /// <param name="duration">Scroll duration in seconds.</param>
        /// <param name="alignment">Criteria for cell position in the viewport. 0f (top) ~ 1f (end).</param>
        /// <param name="onComplete">Callback called when the move is complete.</param>
        protected virtual void ScrollTo(int index, float duration, float alignment = 0.5f, Action onComplete = null)
        {
            Scroller.ScrollTo(ToScrollerPosition(index, alignment), duration, onComplete);
        }

        /// <summary>
        /// Moves to the position of the specified item.
        /// </summary>
        /// <param name="index">Item index.</param>
        /// <param name="duration">Scroll duration in seconds.</param>
        /// <param name="easing">Easing used for movement.</param>
        /// <param name="alignment">Criteria for cell position in the viewport. 0f (top) ~ 1f (end).</param>
        /// <param name="onComplete">Callback called when the move is complete.</param>
        protected virtual void ScrollTo(int index, float duration, Ease easing, float alignment = 0.5f, Action onComplete = null)
        {
            Scroller.ScrollTo(ToScrollerPosition(index, alignment), duration, easing, onComplete);
        }

        /// <summary>
        /// ビューポートとコンテンツの長さに基づいてスクロールバーのサイズを更新します.
        /// </summary>
        /// <param name="viewportLength">ビューポートのサイズ.</param>
        protected void UpdateScrollbarSize(float viewportLength)
        {
            var contentLength = Mathf.Max(ItemsSource.Count + (paddingHead + paddingTail - spacing) / (CellSize + spacing), 1);
            Scroller.Scrollbar.size = Scrollable ? Mathf.Clamp01(viewportLength / contentLength) : 1f;
        }

        /// <summary>
        /// <see cref="Scroller"/> が扱うスクロール位置を <see cref="FancyScrollRect{TItemData, TContext}"/> が扱うスクロール位置に変換します.
        /// </summary>
        /// <param name="position"><see cref="Scroller"/> が扱うスクロール位置.</param>
        /// <returns><see cref="FancyScrollRect{TItemData, TContext}"/> が扱うスクロール位置.</returns>
        protected float ToFancyScrollViewPosition(float position)
        {
            return position / Mathf.Max(ItemsSource.Count - 1, 1) * MaxScrollPosition - PaddingHeadLength;
        }

        /// <summary>
        /// <see cref="FancyScrollRect{TItemData, TContext}"/> が扱うスクロール位置を <see cref="Scroller"/> が扱うスクロール位置に変換します.
        /// </summary>
        /// <param name="position"><see cref="FancyScrollRect{TItemData, TContext}"/> が扱うスクロール位置.</param>
        /// <returns><see cref="Scroller"/> が扱うスクロール位置.</returns>
        protected float ToScrollerPosition(float position)
        {
            return (position + PaddingHeadLength) / MaxScrollPosition * Mathf.Max(ItemsSource.Count - 1, 1);
        }

        /// <summary>
        /// <see cref="FancyScrollRect{TItemData, TContext}"/> が扱うスクロール位置を <see cref="Scroller"/> が扱うスクロール位置に変換します.
        /// </summary>
        /// <param name="position"><see cref="FancyScrollRect{TItemData, TContext}"/> が扱うスクロール位置.</param>
        /// <param name="alignment">ビューポート内におけるセル位置の基準. 0f(先頭) ~ 1f(末尾).</param>
        /// <returns><see cref="Scroller"/> が扱うスクロール位置.</returns>
        protected float ToScrollerPosition(float position, float alignment = 0.5f)
        {
            var offset = alignment * (ScrollLength - (1f + reuseCellMarginCount * 2f))
                + (1f - alignment - 0.5f) * spacing / (CellSize + spacing);
            return ToScrollerPosition(Mathf.Clamp(position - offset, 0f, MaxScrollPosition));
        }

        /// <summary>
        /// 指定された設定を実現するための
        /// <see cref="FancyScrollView{TItemData,TContext}.cellInterval"/> と
        /// <see cref="FancyScrollView{TItemData,TContext}.scrollOffset"/> を計算して適用します.
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
        }
    }

    /// <summary>
    /// ScrollRect is an abstract base class for implementing a scrolling view.
    /// </summary>
    /// <typeparam name="TItemData">Item data type.</typeparam>
    /// <seealso cref="FancyScrollRect{TItemData, TContext}"/>
    public abstract class FancyScrollRect<TItemData> : FancyScrollRect<TItemData, FancyScrollRectContext> { }
}
