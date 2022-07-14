﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Nodify
{
    /// <summary>
    /// Delegate used to notify when an <see cref="ItemContainer"/> is previewing a new location.
    /// </summary>
    /// <param name="newLocation">The new location.</param>
    public delegate void PreviewLocationChanged(Point newLocation);

    /// <summary>
    /// The container for all the items generated by the <see cref="ItemsControl.ItemsSource"/> of the <see cref="NodifyEditor"/>.
    /// </summary>
    public class ItemContainer : ContentControl, INodifyCanvasItem
    {
        #region Dependency Properties

        public static readonly DependencyProperty HighlightBrushProperty = DependencyProperty.Register(nameof(HighlightBrush), typeof(Brush), typeof(ItemContainer));
        public static readonly DependencyProperty SelectedBrushProperty = DependencyProperty.Register(nameof(SelectedBrush), typeof(Brush), typeof(ItemContainer));
        public static readonly DependencyProperty IsSelectableProperty = DependencyProperty.Register(nameof(IsSelectable), typeof(bool), typeof(ItemContainer), new FrameworkPropertyMetadata(BoxValue.True));
        public static readonly DependencyProperty IsSelectedProperty = Selector.IsSelectedProperty.AddOwner(typeof(ItemContainer), new FrameworkPropertyMetadata(BoxValue.False, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsSelectedChanged));
        public static readonly DependencyPropertyKey IsPreviewingSelectionPropertyKey = DependencyProperty.RegisterReadOnly(nameof(IsPreviewingSelection), typeof(bool?), typeof(ItemContainer), new FrameworkPropertyMetadata(null));
        public static readonly DependencyProperty IsPreviewingSelectionProperty = IsPreviewingSelectionPropertyKey.DependencyProperty;
        public static readonly DependencyProperty LocationProperty = DependencyProperty.Register(nameof(Location), typeof(Point), typeof(ItemContainer), new FrameworkPropertyMetadata(BoxValue.Point, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnLocationChanged));
        public static readonly DependencyProperty ActualSizeProperty = DependencyProperty.Register(nameof(ActualSize), typeof(Size), typeof(ItemContainer), new FrameworkPropertyMetadata(BoxValue.Size));
        public static readonly DependencyProperty DesiredSizeForSelectionProperty = DependencyProperty.Register(nameof(DesiredSizeForSelection), typeof(Size?), typeof(ItemContainer), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.NotDataBindable));
        public static readonly DependencyPropertyKey IsPreviewingLocationPropertyKey = DependencyProperty.RegisterReadOnly(nameof(IsPreviewingLocation), typeof(bool), typeof(ItemContainer), new FrameworkPropertyMetadata(BoxValue.False));
        public static readonly DependencyProperty IsPreviewingLocationProperty = IsPreviewingLocationPropertyKey.DependencyProperty;
        public static readonly DependencyProperty IsDraggableProperty = DependencyProperty.Register(nameof(IsDraggable), typeof(bool), typeof(ItemContainer), new FrameworkPropertyMetadata(BoxValue.True));

        /// <summary>
        /// Gets or sets the brush used when the <see cref="PendingConnection.IsOverElementProperty"/> attached property is true for this <see cref="ItemContainer"/>.
        /// </summary>
        public Brush HighlightBrush
        {
            get => (Brush)GetValue(HighlightBrushProperty);
            set => SetValue(HighlightBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used when <see cref="IsSelected"/> or <see cref="IsPreviewingSelection"/> is true.
        /// </summary>
        public Brush SelectedBrush
        {
            get => (Brush)GetValue(SelectedBrushProperty);
            set => SetValue(SelectedBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the location of this <see cref="ItemContainer"/> inside the <see cref="NodifyEditor.ItemsHost"/>.
        /// </summary>
        public Point Location
        {
            get => (Point)GetValue(LocationProperty);
            set => SetValue(LocationProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether this <see cref="ItemContainer"/> is selected.
        /// Can only be set if <see cref="IsSelectable"/> is true.
        /// </summary>
        public bool IsSelected
        {
            get => (bool)GetValue(IsSelectedProperty);
            set => SetValue(IsSelectedProperty, value);
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="ItemContainer"/> is about to change its <see cref="IsSelected"/> state.
        /// </summary>
        public bool? IsPreviewingSelection
        {
            get => (bool?)GetValue(IsPreviewingSelectionProperty);
            internal set => SetValue(IsPreviewingSelectionPropertyKey, value);
        }

        /// <summary>
        /// Gets or sets whether this <see cref="ItemContainer"/> can be selected.
        /// </summary>
        public bool IsSelectable
        {
            get => (bool)GetValue(IsSelectableProperty);
            set => SetValue(IsSelectableProperty, value);
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="ItemContainer"/> is previewing a new location but didn't logically move there.
        /// </summary>
        public bool IsPreviewingLocation
        {
            get => (bool)GetValue(IsPreviewingLocationProperty);
            private set => SetValue(IsPreviewingLocationPropertyKey, value);
        }

        /// <summary>
        /// Gets the actual size of this <see cref="ItemContainer"/>.
        /// </summary>
        public Size ActualSize
        {
            get => (Size)GetValue(ActualSizeProperty);
            set => SetValue(ActualSizeProperty, value);
        }

        /// <summary>
        /// Overrides the size to check against when calculating if this <see cref="ItemContainer"/> can be part of the current <see cref="NodifyEditor.SelectedArea"/>.
        /// Defaults to <see cref="UIElement.RenderSize"/>.
        /// </summary>
        public Size? DesiredSizeForSelection
        {
            get => (Size?)GetValue(DesiredSizeForSelectionProperty);
            set => SetValue(DesiredSizeForSelectionProperty, value);
        }

        /// <summary>
        /// Gets or sets whether this <see cref="ItemContainer"/> can be dragged.
        /// </summary>
        public bool IsDraggable
        {
            get => (bool)GetValue(IsDraggableProperty);
            set => SetValue(IsDraggableProperty, value);
        }

        private static void OnLocationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var item = (ItemContainer)d;
            item.OnLocationChanged();

            if (!item.Editor.IsBulkUpdatingItems)
            {
                item.Editor.ItemsHost.InvalidateArrange();
            }
        }

        #endregion

        #region Routed Events

        public static readonly RoutedEvent DragStartedEvent = EventManager.RegisterRoutedEvent(nameof(DragStarted), RoutingStrategy.Bubble, typeof(DragStartedEventHandler), typeof(ItemContainer));
        public static readonly RoutedEvent DragCompletedEvent = EventManager.RegisterRoutedEvent(nameof(DragCompleted), RoutingStrategy.Bubble, typeof(DragCompletedEventHandler), typeof(ItemContainer));
        public static readonly RoutedEvent DragDeltaEvent = EventManager.RegisterRoutedEvent(nameof(DragDelta), RoutingStrategy.Bubble, typeof(DragDeltaEventHandler), typeof(ItemContainer));
        public static readonly RoutedEvent SelectedEvent = Selector.SelectedEvent.AddOwner(typeof(ItemContainer));
        public static readonly RoutedEvent UnselectedEvent = Selector.UnselectedEvent.AddOwner(typeof(ItemContainer));
        public static readonly RoutedEvent LocationChangedEvent = EventManager.RegisterRoutedEvent(nameof(LocationChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ItemContainer));

        /// <summary>
        /// Occurs when the <see cref="Location"/> of this <see cref="ItemContainer"/> is changed.
        /// </summary>
        public event RoutedEventHandler LocationChanged
        {
            add => AddHandler(LocationChangedEvent, value);
            remove => RemoveHandler(LocationChangedEvent, value);
        }

        /// <summary>
        /// Occurs when this <see cref="ItemContainer"/> is the instigator of a drag operation.
        /// </summary>
        public event DragStartedEventHandler DragStarted
        {
            add => AddHandler(DragStartedEvent, value);
            remove => RemoveHandler(DragStartedEvent, value);
        }

        /// <summary>
        /// Occurs when this <see cref="ItemContainer"/> is being dragged.
        /// </summary>
        public event DragDeltaEventHandler DragDelta
        {
            add => AddHandler(DragDeltaEvent, value);
            remove => RemoveHandler(DragDeltaEvent, value);
        }

        /// <summary>
        /// Occurs when this <see cref="ItemContainer"/> completed the drag operation.
        /// </summary>
        public event DragCompletedEventHandler DragCompleted
        {
            add => AddHandler(DragCompletedEvent, value);
            remove => RemoveHandler(DragCompletedEvent, value);
        }

        /// <summary>
        /// Occurs when this <see cref="ItemContainer"/> is selected.
        /// </summary>
        public event RoutedEventHandler Selected
        {
            add => AddHandler(SelectedEvent, value);
            remove => RemoveHandler(SelectedEvent, value);
        }

        /// <summary>
        /// Occurs when this <see cref="ItemContainer"/> is unselected.
        /// </summary>
        public event RoutedEventHandler Unselected
        {
            add => AddHandler(UnselectedEvent, value);
            remove => RemoveHandler(UnselectedEvent, value);
        }

        /// <summary>
        /// Raises the <see cref="LocationChangedEvent"/> and sets <see cref="IsPreviewingLocation"/> to false.
        /// </summary>
        protected void OnLocationChanged()
        {
            IsPreviewingLocation = false;
            RaiseEvent(new RoutedEventArgs(LocationChangedEvent, this));
        }

        /// <summary>
        /// Raises the <see cref="SelectedEvent"/> or <see cref="UnselectedEvent"/> based on <paramref name="newValue"/>.
        /// Called when the <see cref="IsSelected"/> value is changed.
        /// </summary>
        /// <param name="newValue">True if selected, false otherwise.</param>
        protected void OnSelectedChanged(bool newValue)
        {
            // Raise event after the selection operation ended
            if (!Editor.IsSelecting)
            {
                RaiseEvent(new RoutedEventArgs(newValue ? SelectedEvent : UnselectedEvent, this));
            }
        }

        private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var elem = (ItemContainer)d;

            bool result = elem.IsSelectable && (bool)e.NewValue;
            elem.OnSelectedChanged(result);
            elem.IsSelected = result;
        }

        #endregion

        #region Fields

        /// <summary>
        /// Gets or sets whether cancelling a dragging operation is allowed.
        /// </summary>
        public static bool AllowDraggingCancellation { get; set; } = true;

        /// <summary>
        /// The <see cref="NodifyEditor"/> that owns this <see cref="ItemContainer"/>.
        /// </summary>
        public NodifyEditor Editor { get; }

        /// <summary>
        /// Gets or sets the ancestor of this container used to calculate the relative position when dragging.
        /// Prioritizes the NodifyEditor and defaults to this ItemContainer if not found.
        /// </summary>
        protected UIElement DraggableHost { get; set; }

        private Point _previousDragPosition;
        private Point _initialDragPosition;

        #endregion

        /// <summary>
        /// Occurs when the <see cref="ItemContainer"/> is previewing a new location.
        /// </summary>
        public event PreviewLocationChanged? PreviewLocationChanged;

        /// <summary>
        /// Raises the <see cref="PreviewLocationChanged"/> event and sets the <see cref="IsPreviewingLocation"/> property to true.
        /// </summary>
        /// <param name="newLocation">The new location.</param>
        protected internal void OnPreviewLocationChanged(Point newLocation)
        {
            IsPreviewingLocation = true;
            PreviewLocationChanged?.Invoke(newLocation);
        }

        static ItemContainer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ItemContainer), new FrameworkPropertyMetadata(typeof(ItemContainer)));
            FocusableProperty.OverrideMetadata(typeof(ItemContainer), new FrameworkPropertyMetadata(BoxValue.True));
        }

        /// <summary>
        /// Constructs an instance of an <see cref="ItemContainer"/> in the specified <see cref="NodifyEditor"/>.
        /// </summary>
        /// <param name="editor"></param>
        public ItemContainer(NodifyEditor editor)
        {
            Editor = editor;
            DraggableHost = editor.ItemsHost;
        }

        /// <inheritdoc />
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            ActualSize = sizeInfo.NewSize;
        }

        /// <summary>
        /// Checks if <paramref name="position"/> is selectable.
        /// </summary>
        /// <param name="position">A position relative to this <see cref="ItemContainer"/>.</param>
        /// <returns>True if <paramref name="position"/> is selectable.</returns>
        protected virtual bool IsSelectableLocation(Point position)
        {
            Size size = DesiredSizeForSelection ?? RenderSize;
            return position.X >= 0 && position.Y >= 0 && position.X <= size.Width && position.Y <= size.Height;
        }

        /// <summary>
        /// Checks if <paramref name="area"/> contains or intersects with this <see cref="ItemContainer"/> taking into consideration the <see cref="DesiredSizeForSelection"/>.
        /// </summary>
        /// <param name="area">The area to check if contains or intersects this <see cref="ItemContainer"/>.</param>
        /// <param name="isContained">If true will check if <paramref name="area"/> contains this, otherwise will check if <paramref name="area"/> intersects with this.</param>
        /// <returns>True if <paramref name="area"/> contains or intersects this <see cref="ItemContainer"/>.</returns>
        public virtual bool IsSelectableInArea(Rect area, bool isContained)
        {
            var bounds = new Rect(Location, DesiredSizeForSelection ?? RenderSize);
            return isContained ? area.Contains(bounds) : area.IntersectsWith(bounds);
        }

        /// <inheritdoc />
        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            // Handle right click if is previewing location so context menus don't open
            if (IsPreviewingLocation)
            {
                e.Handled = true;
            }
            // Select with right click
            else if (!IsSelected && IsSelectableLocation(e.GetPosition(this)))
            {
                Editor.UnselectAll();
                IsSelected = true;
                Focus();
            }

            // Cancel dragging
            if (AllowDraggingCancellation && IsPreviewingLocation)
            {
                IsPreviewingLocation = false;
                Vector position = e.GetPosition(DraggableHost) - _initialDragPosition;

                RaiseEvent(new DragCompletedEventArgs(position.X, position.Y, true)
                {
                    RoutedEvent = DragCompletedEvent
                });

                if (IsMouseCaptured)
                {
                    ReleaseMouseCapture();
                }
            }
        }

        /// <inheritdoc />
        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            if (IsSelectableLocation(e.GetPosition(this)))
            {
                Focus();
                e.Handled = true;
            }
        }

        /// <inheritdoc />
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (IsSelectableLocation(e.GetPosition(this)))
            {
                // Prepare for dragging
                _initialDragPosition = e.GetPosition(DraggableHost);
                _previousDragPosition = _initialDragPosition;

                Focus();
                CaptureMouse();
                e.Handled = true;
            }
        }

        /// <inheritdoc />
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            // If it's previewing location, then we're dragging the container
            if (IsPreviewingLocation)
            {
                // Do not rely on OnLocationChanged setting this to false
                IsPreviewingLocation = false;
                Vector position = e.GetPosition(DraggableHost) - _initialDragPosition;

                RaiseEvent(new DragCompletedEventArgs(position.X, position.Y, false)
                {
                    RoutedEvent = DragCompletedEvent
                });

                e.Handled = true;
            }
            else if (IsSelectableLocation(e.GetPosition(this)))
            {
                switch (Keyboard.Modifiers)
                {
                    case ModifierKeys.Control:
                        IsSelected = !IsSelected;
                        break;
                    case ModifierKeys.Shift:
                        IsSelected = true;
                        break;
                    default:
                        Editor.UnselectAll();
                        IsSelected = true;
                        break;
                }

                Focus();
                e.Handled = true;
            }

            if (IsMouseCaptured)
            {
                ReleaseMouseCapture();
            }
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && IsMouseCaptured && IsDraggable)
            {
                // Needs host for snapping and to apply transform 
                Point position = e.GetPosition(DraggableHost);

                if (_previousDragPosition != position)
                {
                    // Start dragging
                    if (!IsPreviewingLocation)
                    {
                        RaiseEvent(new DragStartedEventArgs(_initialDragPosition.X, _initialDragPosition.Y)
                        {
                            RoutedEvent = DragStartedEvent
                        });

                        IsPreviewingLocation = true;
                    }
                    else
                    {
                        Vector delta = position - _previousDragPosition;
                        _previousDragPosition = position;

                        RaiseEvent(new DragDeltaEventArgs(delta.X, delta.Y)
                        {
                            RoutedEvent = DragDeltaEvent
                        });

                        e.Handled = true;
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            if (IsPreviewingLocation)
            {
                IsPreviewingLocation = false;
                Vector position = e.GetPosition(DraggableHost) - _initialDragPosition;

                RaiseEvent(new DragCompletedEventArgs(position.X, position.Y, AllowDraggingCancellation)
                {
                    RoutedEvent = DragCompletedEvent
                });
            }
        }
    }
}
