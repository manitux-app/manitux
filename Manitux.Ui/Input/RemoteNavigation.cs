using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Manitux.Ui.Input;

public interface IRemoteDirectionalNavigation
{
    bool TryMoveFocus(Control current, NavigationDirection direction);
}

public sealed class RemoteNavigation
{
    private const double DirectionTolerance = 6;
    private const double MinimumCrossAxisOverlap = 8;

    private RemoteNavigation()
    {
    }

    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<RemoteNavigation, InputElement, bool>("IsEnabled");

    public static bool GetIsEnabled(InputElement element)
    {
        return element.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(InputElement element, bool value)
    {
        element.SetValue(IsEnabledProperty, value);
    }

    static RemoteNavigation()
    {
        IsEnabledProperty.Changed.AddClassHandler<InputElement>(OnIsEnabledChanged);
    }

    private static void OnIsEnabledChanged(InputElement element, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            element.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
            element.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        }
        else
        {
            element.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
            element.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        }
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Control source)
        {
            return;
        }

        var focusTarget = FindFocusableOwner(source);
        if (focusTarget is null or TextBox)
        {
            return;
        }

        focusTarget.Focus(NavigationMethod.Pointer);
    }

    private static void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || sender is not InputElement scope)
        {
            return;
        }

        if (TryActivate(e.Key, scope))
        {
            e.Handled = true;
            return;
        }

        var direction = e.Key switch
        {
            Key.Left => NavigationDirection.Left,
            Key.Right => NavigationDirection.Right,
            Key.Up => NavigationDirection.Up,
            Key.Down => NavigationDirection.Down,
            _ => (NavigationDirection?)null
        };

        if (direction is null)
        {
            return;
        }

        var rawFocused = TopLevel.GetTopLevel(scope)?.FocusManager?.GetFocusedElement() as Control;
        if (rawFocused is TextBox)
        {
            return;
        }

        var focused = rawFocused is ListBoxItem or ListBox
            ? FindFocusableDescendant(rawFocused) ?? FindFocusableOwner(rawFocused)
            : FindFocusableOwner(rawFocused) ?? FindFocusableDescendant(rawFocused);
        focused ??= FindFocusableOwner(scope as Control)
                    ?? FindFocusableDescendant(scope as Control);

        if (focused is null)
        {
            return;
        }

        var directionalScope = focused.GetVisualAncestors()
            .OfType<IRemoteDirectionalNavigation>()
            .FirstOrDefault();
        if (directionalScope?.TryMoveFocus(focused, direction.Value) == true)
        {
            e.Handled = true;
            return;
        }

        if (TryAdjustRangeValue(focused, direction.Value))
        {
            e.Handled = true;
            return;
        }

        if (TryMoveFocus(scope, focused, direction.Value))
        {
            e.Handled = true;
        }
    }

    private static bool TryAdjustRangeValue(Control focused, NavigationDirection direction)
    {
        if (focused is not RangeBase range || !range.Classes.Contains("seek-active"))
        {
            return false;
        }

        var delta = direction switch
        {
            NavigationDirection.Left => -5,
            NavigationDirection.Right => 5,
            _ => 0
        };

        if (delta == 0)
        {
            return false;
        }

        range.Value = Math.Clamp(range.Value + delta, range.Minimum, range.Maximum);
        return true;
    }

    private static bool TryActivate(Key key, InputElement scope)
    {
        if (key is not (Key.Enter or Key.Space or Key.Select))
        {
            return false;
        }

        var focused = TopLevel.GetTopLevel(scope)?.FocusManager?.GetFocusedElement();
        if (focused is ToggleButton toggleButton && toggleButton.IsEnabled)
        {
            toggleButton.IsChecked = toggleButton.IsChecked != true;
            toggleButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, toggleButton));
            return true;
        }

        if (focused is Button button && button.IsEnabled)
        {
            var parameter = button.CommandParameter;
            if (button.Command?.CanExecute(parameter) == true)
            {
                button.Command.Execute(parameter);
            }

            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button));
            return true;
        }

        if (focused is MenuItem menuItem && menuItem.IsEnabled)
        {
            menuItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, menuItem));
            return true;
        }

        return false;
    }

    private static bool TryMoveFocus(InputElement scope, Control current, NavigationDirection direction)
    {
        var root = scope as Visual ?? scope.GetVisualRoot() as Visual;
        if (root is null)
        {
            return false;
        }

        current = FindFocusableOwner(current) ?? current;

        var currentBounds = GetBounds(current, root);
        if (currentBounds is null)
        {
            return false;
        }

        var target = root.GetVisualDescendants()
            .OfType<Control>()
            .Where(candidate => candidate != current)
            .Where(IsFocusableCandidate)
            .Where(candidate => !IsInside(candidate, current))
            .Select(candidate => new
            {
                Control = candidate,
                Bounds = GetBounds(candidate, root)
            })
            .Where(item => item.Bounds is not null)
            .Select(item => new
            {
                item.Control,
                Score = Score(currentBounds.Value, item.Bounds!.Value, direction)
            })
            .Where(item => item.Score is not null)
            .OrderBy(item => item.Score!.Value.Bucket)
            .ThenBy(item => item.Score!.Value.Primary)
            .ThenByDescending(item => item.Score!.Value.CrossOverlap)
            .ThenBy(item => item.Score!.Value.Secondary)
            .FirstOrDefault();

        if (target?.Control.Focus(NavigationMethod.Directional) != true)
        {
            return false;
        }

        target.Control.BringIntoView();
        return true;
    }

    private static bool IsFocusableCandidate(Control candidate)
    {
        return candidate.Focusable
               && candidate.IsEnabled
               && candidate.IsEffectivelyVisible
               && candidate.Bounds.Width > 0
               && candidate.Bounds.Height > 0
               && IsInteractiveCandidate(candidate);
    }

    private static bool IsInteractiveCandidate(Control candidate)
    {
        return candidate is Button
               or TextBox
               or MenuItem
               or ListBoxItem
               || candidate is RangeBase range && range.Classes.Contains("seek-active");
    }

    private static Rect? GetBounds(Control control, Visual root)
    {
        var origin = control.TranslatePoint(new Point(0, 0), root);
        return origin is null
            ? null
            : new Rect(origin.Value, control.Bounds.Size);
    }

    private static NavigationScore? Score(Rect current, Rect candidate, NavigationDirection direction)
    {
        var axis = GetAxisDistances(current, candidate, direction);
        if (axis is null)
        {
            return null;
        }

        var bucket = axis.Value.CrossOverlap >= MinimumCrossAxisOverlap
            ? 0
            : axis.Value.IsEdgeSeparated
                ? 1
                : 2;

        if (bucket > 0 && !IsInsideDirectionalCone(axis.Value))
        {
            return null;
        }

        return new NavigationScore(
            bucket,
            Math.Max(0, axis.Value.Primary),
            axis.Value.Secondary,
            axis.Value.CrossOverlap);
    }

    private static AxisDistances? GetAxisDistances(Rect current, Rect candidate, NavigationDirection direction)
    {
        var currentCenter = current.Center;
        var candidateCenter = candidate.Center;

        return direction switch
        {
            NavigationDirection.Left when candidate.Right <= current.Left + DirectionTolerance =>
                new AxisDistances(
                    current.Left - candidate.Right,
                    Math.Abs(candidateCenter.Y - currentCenter.Y),
                    GetOverlap(current.Top, current.Bottom, candidate.Top, candidate.Bottom),
                    IsEdgeSeparated: true),

            NavigationDirection.Left when candidateCenter.X < currentCenter.X - DirectionTolerance =>
                new AxisDistances(
                    currentCenter.X - candidateCenter.X,
                    Math.Abs(candidateCenter.Y - currentCenter.Y),
                    GetOverlap(current.Top, current.Bottom, candidate.Top, candidate.Bottom),
                    IsEdgeSeparated: false),

            NavigationDirection.Right when candidate.Left >= current.Right - DirectionTolerance =>
                new AxisDistances(
                    candidate.Left - current.Right,
                    Math.Abs(candidateCenter.Y - currentCenter.Y),
                    GetOverlap(current.Top, current.Bottom, candidate.Top, candidate.Bottom),
                    IsEdgeSeparated: true),

            NavigationDirection.Right when candidateCenter.X > currentCenter.X + DirectionTolerance =>
                new AxisDistances(
                    candidateCenter.X - currentCenter.X,
                    Math.Abs(candidateCenter.Y - currentCenter.Y),
                    GetOverlap(current.Top, current.Bottom, candidate.Top, candidate.Bottom),
                    IsEdgeSeparated: false),

            NavigationDirection.Up when candidate.Bottom <= current.Top + DirectionTolerance =>
                new AxisDistances(
                    current.Top - candidate.Bottom,
                    Math.Abs(candidateCenter.X - currentCenter.X),
                    GetOverlap(current.Left, current.Right, candidate.Left, candidate.Right),
                    IsEdgeSeparated: true),

            NavigationDirection.Up when candidateCenter.Y < currentCenter.Y - DirectionTolerance =>
                new AxisDistances(
                    currentCenter.Y - candidateCenter.Y,
                    Math.Abs(candidateCenter.X - currentCenter.X),
                    GetOverlap(current.Left, current.Right, candidate.Left, candidate.Right),
                    IsEdgeSeparated: false),

            NavigationDirection.Down when candidate.Top >= current.Bottom - DirectionTolerance =>
                new AxisDistances(
                    candidate.Top - current.Bottom,
                    Math.Abs(candidateCenter.X - currentCenter.X),
                    GetOverlap(current.Left, current.Right, candidate.Left, candidate.Right),
                    IsEdgeSeparated: true),

            NavigationDirection.Down when candidateCenter.Y > currentCenter.Y + DirectionTolerance =>
                new AxisDistances(
                    candidateCenter.Y - currentCenter.Y,
                    Math.Abs(candidateCenter.X - currentCenter.X),
                    GetOverlap(current.Left, current.Right, candidate.Left, candidate.Right),
                    IsEdgeSeparated: false),

            _ => null
        };
    }

    private static bool IsInsideDirectionalCone(AxisDistances axis)
    {
        var primary = Math.Max(axis.Primary, DirectionTolerance);
        return axis.Secondary <= primary * 0.55 + 36;
    }

    private static double GetOverlap(double firstStart, double firstEnd, double secondStart, double secondEnd)
    {
        return Math.Max(0, Math.Min(firstEnd, secondEnd) - Math.Max(firstStart, secondStart));
    }

    private static Control? FindFocusableOwner(Control? control)
    {
        while (control is not null)
        {
            if (IsFocusableCandidate(control))
            {
                return control;
            }

            control = control.GetVisualParent() as Control;
        }

        return null;
    }

    private static Control? FindFocusableDescendant(Control? control)
    {
        if (control is null)
        {
            return null;
        }

        // Sanallaştırılmış ListBox bazen odağı öğe konteynerinde tutar.
        // CloudStream'in RecyclerView yönlendirmesi gibi gerçek içerik
        // kontrolüne dönerek yön bilgisinin liste dışına kaçmasını önler.
        return control.GetVisualDescendants()
            .OfType<Control>()
            .Where(IsFocusableCandidate)
            .OrderByDescending(candidate => candidate is Button button && button.Classes.Contains("poster"))
            .ThenBy(candidate => candidate.Bounds.X)
            .FirstOrDefault();
    }

    private static bool IsInside(Control candidate, Control current)
    {
        var parent = candidate.GetVisualParent();
        while (parent is not null)
        {
            if (ReferenceEquals(parent, current))
            {
                return true;
            }

            parent = parent.GetVisualParent();
        }

        return false;
    }

    private readonly record struct AxisDistances(
        double Primary,
        double Secondary,
        double CrossOverlap,
        bool IsEdgeSeparated);

    private readonly record struct NavigationScore(int Bucket, double Primary, double Secondary, double CrossOverlap);
}
