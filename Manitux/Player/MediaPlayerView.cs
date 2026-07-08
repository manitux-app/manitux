using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Labs.Input;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using LibMPVSharp;
using LibMPVSharp.Extensions;
using Manitux.Core.Application;
using Manitux.Core.Models;
using Manitux.ViewModels;

namespace Manitux.Player
{
    public class MediaPlayerView : TemplatedControl
    {
        public static readonly StyledProperty<MPVMediaPlayer?> MediaPlayerProperty = AvaloniaProperty.Register<MediaPlayerView, MPVMediaPlayer?>(nameof(MediaPlayer));
        public MPVMediaPlayer? MediaPlayer
        {
            get => GetValue(MediaPlayerProperty);
            set => SetValue(MediaPlayerProperty, value);
        }

        public static readonly StyledProperty<AppStrings?> LocalizeProperty =
            AvaloniaProperty.Register<MediaPlayerView, AppStrings?>(nameof(Localize));

        public AppStrings? Localize
        {
            get => GetValue(LocalizeProperty);
            set => SetValue(LocalizeProperty, value);
        }

        public static readonly StyledProperty<TimeSpan> DurationProperty = AvaloniaProperty.Register<MediaPlayerView, TimeSpan>(nameof(Duration));
        public TimeSpan Duration
        {
            get => GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public static readonly StyledProperty<TimeSpan> TimeProperty = AvaloniaProperty.Register<MediaPlayerView, TimeSpan>(nameof(Time));
        public TimeSpan Time
        {
            get => GetValue(TimeProperty);
            set => SetValue(TimeProperty, value);
        }

        public static readonly StyledProperty<TimeSpan> RemainingTimeProperty = AvaloniaProperty.Register<MediaPlayerView, TimeSpan>(nameof(RemainingTime));
        public TimeSpan RemainingTime
        {
            get => GetValue(RemainingTimeProperty);
            set => SetValue(RemainingTimeProperty, value);
        }

        public static readonly StyledProperty<long> VolumeProperty = AvaloniaProperty.Register<MediaPlayerView, long>(nameof(Volume));
        public long Volume
        {
            get => GetValue(VolumeProperty);
            set => SetValue(VolumeProperty, value);
        }

        public static readonly StyledProperty<int> VolumePercentProperty = AvaloniaProperty.Register<MediaPlayerView, int>(nameof(VolumePercent));
        public int VolumePercent
        {
            get => GetValue(VolumePercentProperty);
            set => SetValue(VolumePercentProperty, value);
        }

        public static readonly StyledProperty<long> MaxVolumeProperty = AvaloniaProperty.Register<MediaPlayerView, long>(nameof(MaxVolume), 1000L);
        public long MaxVolume
        {
            get => GetValue(MaxVolumeProperty);
            set => SetValue(MaxVolumeProperty, value);
        }

        public static readonly StyledProperty<double> SpeedProperty = AvaloniaProperty.Register<MediaPlayerView, double>(nameof(Speed), 1d);
        public double Speed
        {
            get => GetValue(SpeedProperty);
            set => SetValue(SpeedProperty, value);
        }

        public static readonly StyledProperty<bool> PlayingProperty = AvaloniaProperty.Register<MediaPlayerView, bool>(nameof(Playing), false);
        public bool Playing
        {
            get => (bool)GetValue(PlayingProperty);
            set => SetValue(PlayingProperty, value);
        }

        public static readonly StyledProperty<bool> IsFullScreenProperty = AvaloniaProperty.Register<MediaPlayerView, bool>(nameof(IsFullScreen), false);
        public bool IsFullScreen
        {
            get => GetValue(IsFullScreenProperty);
            set => SetValue(IsFullScreenProperty, value);
        }

        public static readonly StyledProperty<bool> AreControlsVisibleProperty = AvaloniaProperty.Register<MediaPlayerView, bool>(nameof(AreControlsVisible), true);
        public bool AreControlsVisible
        {
            get => GetValue(AreControlsVisibleProperty);
            set => SetValue(AreControlsVisibleProperty, value);
        }

        public static readonly StyledProperty<bool> IsVolumePanelVisibleProperty = AvaloniaProperty.Register<MediaPlayerView, bool>(nameof(IsVolumePanelVisible));
        public bool IsVolumePanelVisible
        {
            get => GetValue(IsVolumePanelVisibleProperty);
            set => SetValue(IsVolumePanelVisibleProperty, value);
        }

        public static readonly StyledProperty<bool> IsAudioPanelVisibleProperty = AvaloniaProperty.Register<MediaPlayerView, bool>(nameof(IsAudioPanelVisible));
        public bool IsAudioPanelVisible
        {
            get => GetValue(IsAudioPanelVisibleProperty);
            set => SetValue(IsAudioPanelVisibleProperty, value);
        }

        public static readonly StyledProperty<string?> TitleProperty = AvaloniaProperty.Register<MediaPlayerView, string?>(nameof(Title), "");
        public string? Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly StyledProperty<string> AspectRatioProperty = AvaloniaProperty.Register<MediaPlayerView, string>(nameof(AspectRatio), "no");
        public string AspectRatio
        {
            get => GetValue(AspectRatioProperty);
            set => SetValue(AspectRatioProperty, value);
        }

        public static readonly StyledProperty<string> VideoParamsProperty = AvaloniaProperty.Register<MediaPlayerView, string>(nameof(VideoParams), "");
        public string VideoParams
        {
            get => GetValue(VideoParamsProperty);
            set => SetValue(VideoParamsProperty, value);
        }

        public static readonly StyledProperty<AvaloniaList<SubtitleModel>> SubTitlesProperty =
            AvaloniaProperty.Register<MediaPlayerView, AvaloniaList<SubtitleModel>>(
                nameof(SubTitles),
                defaultValue: new AvaloniaList<SubtitleModel>());

        public AvaloniaList<SubtitleModel> SubTitles
        {
            get => GetValue(SubTitlesProperty);
            set => SetValue(SubTitlesProperty, value);
        }

        public static readonly StyledProperty<SubtitleModel?> SelectedSubTitleProperty =
            AvaloniaProperty.Register<MediaPlayerView, SubtitleModel?>(nameof(SelectedSubTitle));

        public SubtitleModel? SelectedSubTitle
        {
            get => GetValue(SelectedSubTitleProperty);
            set => SetValue(SelectedSubTitleProperty, value);
        }

        public static readonly StyledProperty<bool> HasSubTitlesProperty =
            AvaloniaProperty.Register<MediaPlayerView, bool>(nameof(HasSubTitles));

        public bool HasSubTitles
        {
            get => GetValue(HasSubTitlesProperty);
            set => SetValue(HasSubTitlesProperty, value);
        }

        public static readonly StyledProperty<AvaloniaList<AudioTrackModel>> AudioTracksProperty =
            AvaloniaProperty.Register<MediaPlayerView, AvaloniaList<AudioTrackModel>>(
                nameof(AudioTracks),
                defaultValue: new AvaloniaList<AudioTrackModel>());

        public AvaloniaList<AudioTrackModel> AudioTracks
        {
            get => GetValue(AudioTracksProperty);
            set => SetValue(AudioTracksProperty, value);
        }

        public static readonly StyledProperty<AudioTrackModel?> SelectedAudioTrackProperty =
            AvaloniaProperty.Register<MediaPlayerView, AudioTrackModel?>(nameof(SelectedAudioTrack));

        public AudioTrackModel? SelectedAudioTrack
        {
            get => GetValue(SelectedAudioTrackProperty);
            set => SetValue(SelectedAudioTrackProperty, value);
        }

        public static readonly StyledProperty<bool> HasAudioTracksProperty =
            AvaloniaProperty.Register<MediaPlayerView, bool>(nameof(HasAudioTracks));

        public bool HasAudioTracks
        {
            get => GetValue(HasAudioTracksProperty);
            set => SetValue(HasAudioTracksProperty, value);
        }

        public static readonly RoutedCommand PlayPauseCmd = new RoutedCommand(nameof(PlayPauseCmd));
        public static readonly RoutedCommand OpenFileCmd = new RoutedCommand(nameof(OpenFileCmd));
        public static readonly RoutedCommand SpeedCmd = new RoutedCommand(nameof(SpeedCmd));
        public static readonly RoutedCommand AspectRatioCmd = new RoutedCommand(nameof(AspectRatioCmd));
        public static readonly RoutedCommand SubTitleCmd = new RoutedCommand(nameof(SubTitleCmd));
        public static readonly RoutedCommand AudioTrackCmd = new RoutedCommand(nameof(AudioTrackCmd));
        public static readonly RoutedCommand FullScreenCmd = new RoutedCommand(nameof(FullScreenCmd));
        public static readonly RoutedCommand StopCmd = new RoutedCommand(nameof(StopCmd));

        private const double KeyboardSeekStepSeconds = 5d;
        private static Queue<string> _aspectRatio = new Queue<string>();
        private Slider? _timeSlider;
        private Button? _playPauseButton;
        private Button? _volumeButton;
        private Button? _subtitleButton;
        private Button? _audioButton;
        private Button? _lastPlaybackFlyoutButton;
        private Control? _audioPanel;
        private Control? _controlBar;
        private DispatcherTimer? _seekDebounceTimer;
        private TimeSpan _pendingSeekTime;
        private bool _isTimeSliderRemoteActive;
        private bool _isScrubbing;
        private bool _isUpdatingTimeFromPlayer;
        private bool _suppressAudioPanelOnFocus;
        private WindowState _restoreWindowState = WindowState.Normal;
        private readonly DispatcherTimer _controlsIdleTimer;

        static MediaPlayerView()
        {
            MediaPlayerProperty.Changed.AddClassHandler<MediaPlayerView>((s, e) => s.OnPropertyChanged(e));
            TimeProperty.Changed.AddClassHandler<MediaPlayerView>((s, e) => s.OnPropertyChanged(e));
            DurationProperty.Changed.AddClassHandler<MediaPlayerView>((s, e) => s.OnPropertyChanged(e));
            VolumeProperty.Changed.AddClassHandler<MediaPlayerView>((s, e) => s.OnPropertyChanged(e));
            MaxVolumeProperty.Changed.AddClassHandler<MediaPlayerView>((s, e) => s.OnPropertyChanged(e));
            AspectRatioProperty.Changed.AddClassHandler<MediaPlayerView>((s, e) => s.OnPropertyChanged(e));

            _aspectRatio.Enqueue("no");
            _aspectRatio.Enqueue("16:9");
            _aspectRatio.Enqueue("4:3");
        }

        protected override Type StyleKeyOverride => typeof(MediaPlayerView);

        public MediaPlayerView()
        {
            Focusable = true;

            _controlsIdleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _controlsIdleTimer.Tick += (_, _) => HideTransientControls();

            AddHandler(PointerMovedEvent, OnPointerActivity, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);
            AddHandler(PointerPressedEvent, OnPointerActivity, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);

            var binds = new[]
            {
                new CommandBinding(PlayPauseCmd, (s,e) => TryPlayPause()),
                new CommandBinding(OpenFileCmd, async (s,e) => await TryOpenFile()),
                new CommandBinding(SpeedCmd, (s,e) => TrySwitchSpeed()),
                new CommandBinding(AspectRatioCmd, (s, e) => TrySwitchAspectRatio()),
                new CommandBinding(SubTitleCmd, (s, e) => TrySwitchSubTitle(e.Parameter)),
                new CommandBinding(AudioTrackCmd, (s, e) => TrySwitchAudioTrack(e.Parameter)),
                new CommandBinding(FullScreenCmd, (s, e) => TryToggleFullScreen()),
                new CommandBinding(StopCmd, (s, e) => TryStop())
            };
            CommandManager.SetCommandBindings(this, binds);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            Focus(NavigationMethod.Tab);
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            ShowTransientControls();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            ShowTransientControls();

            if (IsPlaybackChromeSource(e.Source))
            {
                return;
            }

            Focus(NavigationMethod.Pointer);

            if (MediaPlayer == null)
            {
                return;
            }

            var point = e.GetCurrentPoint(this);
            if (point.Properties.IsLeftButtonPressed)
            {
                TryPlayPause();
                e.Handled = true;
            }
        }

        private void OnPointerActivity(object? sender, PointerEventArgs e)
        {
            ShowTransientControls();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _controlsIdleTimer.Stop();
            Cursor = null;
            base.OnDetachedFromVisualTree(e);
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            if (_timeSlider is not null)
            {
                _timeSlider.PointerPressed -= TimeSliderPointerPressed;
                _timeSlider.PointerReleased -= TimeSliderPointerReleased;
                _timeSlider.PointerCaptureLost -= TimeSliderPointerCaptureLost;
            }

            _controlBar = e.NameScope.Find<Control>("PART_ControlBar");
            _audioPanel = e.NameScope.Find<Control>("PART_AudioPanel");
            _timeSlider = e.NameScope.Find<Slider>("PART_TimeBar");
            if (_timeSlider is not null)
            {
                _timeSlider.PointerPressed += TimeSliderPointerPressed;
                _timeSlider.PointerReleased += TimeSliderPointerReleased;
                _timeSlider.PointerCaptureLost += TimeSliderPointerCaptureLost;
            }

            _playPauseButton = e.NameScope.Find<Button>("playpause_button");
            _volumeButton = e.NameScope.Find<Button>("volume_button");
            _subtitleButton = e.NameScope.Find<Button>("subtitle_button");
            _audioButton = e.NameScope.Find<Button>("audio_button");
            if (_volumeButton is not null)
            {
                _volumeButton.GotFocus -= VolumeButtonGotFocus;
                _volumeButton.GotFocus += VolumeButtonGotFocus;
            }

            if (_subtitleButton is not null)
            {
                _subtitleButton.GotFocus -= PlaybackFlyoutButtonGotFocus;
                _subtitleButton.GotFocus += PlaybackFlyoutButtonGotFocus;
            }

            if (_audioButton is not null)
            {
                _audioButton.GotFocus -= AudioButtonGotFocus;
                _audioButton.GotFocus += AudioButtonGotFocus;
            }

            ShowTransientControls(focusDefaultControl: true);
        }

        public void FocusForRemote()
        {
            ShowTransientControls(focusDefaultControl: true);
        }

        public void HandleRemoteKeyDown(KeyEventArgs e)
        {
            if (e.Handled || MediaPlayer == null || IsTextInputSource(e.Source))
            {
                return;
            }

            if (IsAudioPanelSource(e.Source) || IsFocusInAudioPanel())
            {
                HandleAudioPanelKey(e);
                return;
            }

            if (IsPlaybackMenuSource(e.Source) || IsFocusInPlaybackMenu())
            {
                if (e.Key is Key.Escape or Key.Back)
                {
                    HidePlaybackPopups(focusLastFlyoutButton: true);
                    ShowTransientControls();
                    e.Handled = true;
                }

                return;
            }

            var focusIsInChrome = IsFocusInPlaybackChrome();
            var controlsWereVisible = AreControlsVisible;

            switch (e.Key)
            {
                case Key.Up:
                case Key.Down:
                    if (TryHandleVolumeDirection(e.Key))
                    {
                        ShowTransientControls();
                        e.Handled = true;
                        return;
                    }

                    if (!controlsWereVisible)
                    {
                        ShowTransientControls(focusDefaultControl: true);
                        e.Handled = true;
                        return;
                    }

                    if (focusIsInChrome && TryMoveChromeFocus(e.Key))
                    {
                        ShowTransientControls();
                        e.Handled = true;
                        return;
                    }

                    ShowTransientControls(focusDefaultControl: true);
                    e.Handled = true;
                    break;
                case Key.Left:
                case Key.Right:
                    if (!controlsWereVisible)
                    {
                        TrySeekRelative(e.Key == Key.Left ? -KeyboardSeekStepSeconds : KeyboardSeekStepSeconds);
                        ShowTransientControls(focusDefaultControl: true);
                        e.Handled = true;
                        return;
                    }

                    if (focusIsInChrome)
                    {
                        if (TryHandleFocusedSliderDirection(e.Key) || TryMoveChromeFocus(e.Key))
                        {
                            ShowTransientControls();
                            e.Handled = true;
                        }

                        return;
                    }

                    TrySeekRelative(e.Key == Key.Left ? -KeyboardSeekStepSeconds : KeyboardSeekStepSeconds);
                    ShowTransientControls();
                    e.Handled = true;
                    break;
                case Key.Enter:
                case Key.Select:
                    if (focusIsInChrome)
                    {
                        ActivateFocusedChromeControl();
                        ShowTransientControls();
                        e.Handled = true;
                        return;
                    }

                    TryPlayPause();
                    ShowTransientControls(focusDefaultControl: true);
                    e.Handled = true;
                    break;
                case Key.Space:
                    if (focusIsInChrome)
                    {
                        ActivateFocusedChromeControl();
                        ShowTransientControls();
                        e.Handled = true;
                        return;
                    }

                    TryPlayPause();
                    ShowTransientControls();
                    e.Handled = true;
                    break;
                case Key.Escape:
                case Key.Back:
                    if (IsAudioPanelVisible)
                    {
                        HideAudioPanel(focusAudioButton: true);
                        ShowTransientControls();
                        e.Handled = true;
                        return;
                    }

                    if (IsVolumePanelVisible)
                    {
                        HideVolumePanel(focusVolumeButton: true);
                        ShowTransientControls();
                        e.Handled = true;
                        return;
                    }

                    if (_isTimeSliderRemoteActive)
                    {
                        SetTimeSliderRemoteActive(false);
                        ShowTransientControls();
                        e.Handled = true;
                        return;
                    }

                    if (controlsWereVisible)
                    {
                        HideTransientControls(force: true);
                        e.Handled = true;
                    }

                    break;
            }
        }

        private bool IsPlaybackChromeSource(object? source)
        {
            for (var visual = source as Visual; visual is not null; visual = visual.GetVisualParent())
            {
                if (ReferenceEquals(visual, _controlBar)
                    || visual is Button
                        or Slider
                        or ComboBox
                        or ComboBoxItem
                        or ListBoxItem
                        or MenuItem
                        or TextBox)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsFocusInPlaybackChrome()
        {
            var focused = GetFocusedVisual();
            for (var visual = focused; visual is not null; visual = visual.GetVisualParent())
            {
                if (ReferenceEquals(visual, _controlBar))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTextInputSource(object? source)
        {
            for (var visual = source as Visual; visual is not null; visual = visual.GetVisualParent())
            {
                if (visual is TextBox)
                {
                    return true;
                }
            }

            return false;
        }

        private Visual? GetFocusedVisual()
        {
            return TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Visual;
        }

        private void FocusDefaultControl()
        {
            SetTimeSliderRemoteActive(false);
            HidePlaybackPopups();

            if (_playPauseButton is { IsEffectivelyVisible: true, IsEnabled: true }
                && _playPauseButton.Focus(NavigationMethod.Directional))
            {
                return;
            }

            Focus(NavigationMethod.Directional);
        }

        private bool TryMoveChromeFocus(Key key)
        {
            var controls = GetChromeFocusTargets();
            if (controls.Count == 0)
            {
                return false;
            }

            var focused = GetFocusedVisual() as Control;
            var current = focused is null
                ? -1
                : controls.FindIndex(control => ReferenceEquals(control, focused)
                                                || IsVisualAncestorOf(control, focused));

            if (key == Key.Up || key == Key.Down)
            {
                return FocusChromeTarget(current < 0 ? 0 : current);
            }

            var next = key == Key.Left
                ? current <= 0 ? controls.Count - 1 : current - 1
                : current < 0 || current >= controls.Count - 1 ? 0 : current + 1;

            return FocusChromeTarget(next);
        }

        private bool TryHandleFocusedSliderDirection(Key key)
        {
            if (GetFocusedVisual() is not Slider slider)
            {
                return false;
            }

            if (!_isTimeSliderRemoteActive || !ReferenceEquals(slider, _timeSlider))
            {
                return false;
            }

            TrySeekRelative(key == Key.Left ? -KeyboardSeekStepSeconds : KeyboardSeekStepSeconds);
            return true;
        }

        private void ActivateFocusedChromeControl()
        {
            if (GetFocusedVisual() is not Control focused)
            {
                TryPlayPause();
                return;
            }

            if (focused is Button button && button.IsEnabled)
            {
                if (ReferenceEquals(button, _volumeButton))
                {
                    ShowVolumePanel();
                    return;
                }

                if (ReferenceEquals(button, _audioButton))
                {
                    ShowAudioPanel(focusFirstItem: true);
                    return;
                }

                if (button.Flyout is not null)
                {
                    ShowButtonFlyout(button, focusFirstItem: true);
                    return;
                }

                var parameter = button.CommandParameter;
                if (button.Command?.CanExecute(parameter) == true)
                {
                    button.Command.Execute(parameter);
                }

                button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, button));
                return;
            }

            if (focused is Slider)
            {
                SetTimeSliderRemoteActive(!_isTimeSliderRemoteActive);
                return;
            }

            TryPlayPause();
        }

        private List<Control> GetChromeFocusTargets()
        {
            if (_controlBar is null)
            {
                return [];
            }

            return _controlBar
                .GetVisualDescendants()
                .OfType<Control>()
                .Where(control => control.Focusable
                                  && control.IsEnabled
                                  && control.IsEffectivelyVisible
                                  && control.Bounds.Width > 0
                                  && control.Bounds.Height > 0
                                  && (control is Button || control is Slider))
                .OrderBy(control => GetBoundsLeft(control, _controlBar))
                .ToList();
        }

        private bool FocusChromeTarget(int index)
        {
            var controls = GetChromeFocusTargets();
            if (index < 0 || index >= controls.Count)
            {
                return false;
            }

            var target = controls[index];
            if (!ReferenceEquals(target, _timeSlider))
            {
                SetTimeSliderRemoteActive(false);
            }

            if (ReferenceEquals(target, _volumeButton))
            {
                ShowVolumePanel();
            }
            else
            {
                HideVolumePanel();
            }

            if (ReferenceEquals(target, _audioButton))
            {
                ShowAudioPanel(focusFirstItem: true);
            }
            else if (_audioPanel is null || !IsVisualAncestorOf(_audioPanel, target))
            {
                HideAudioPanel();
            }

            return target.Focus(NavigationMethod.Directional);
        }

        private void HidePlaybackPopups(bool focusLastFlyoutButton = false)
        {
            HideVolumePanel();
            HideAudioPanel();

            if (_controlBar is null)
            {
                return;
            }

            foreach (var button in _controlBar.GetVisualDescendants().OfType<Button>())
            {
                button.Flyout?.Hide();
            }

            if (focusLastFlyoutButton && _lastPlaybackFlyoutButton is { IsEffectivelyVisible: true, IsEnabled: true })
            {
                _lastPlaybackFlyoutButton.Focus(NavigationMethod.Directional);
            }
        }

        private void PlaybackFlyoutButtonGotFocus(object? sender, GotFocusEventArgs e)
        {
            if (sender is Button button)
            {
                ShowButtonFlyout(button, focusFirstItem: true);
            }
        }

        private void AudioButtonGotFocus(object? sender, GotFocusEventArgs e)
        {
            if (_suppressAudioPanelOnFocus)
            {
                _suppressAudioPanelOnFocus = false;
                return;
            }

            ShowAudioPanel(focusFirstItem: true);
        }

        private void ShowAudioPanel(bool focusFirstItem)
        {
            HideVolumePanel();
            SetCurrentValue(IsAudioPanelVisibleProperty, true);

            if (focusFirstItem)
            {
                Dispatcher.UIThread.Post(FocusSelectedOrFirstAudioPanelItem, DispatcherPriority.Background);
            }
        }

        private void HideAudioPanel(bool focusAudioButton = false)
        {
            SetCurrentValue(IsAudioPanelVisibleProperty, false);

            if (focusAudioButton && _audioButton is { IsEffectivelyVisible: true, IsEnabled: true })
            {
                _suppressAudioPanelOnFocus = true;
                _audioButton.Focus(NavigationMethod.Directional);
            }
        }

        private void FocusSelectedOrFirstAudioPanelItem()
        {
            var items = GetAudioPanelButtons();
            if (items.Count == 0)
            {
                _audioButton?.Focus(NavigationMethod.Directional);
                return;
            }

            var selected = SelectedAudioTrack;
            var target = selected is null
                ? items[0]
                : items.FirstOrDefault(button => Equals(button.CommandParameter, selected.Id)) ?? items[0];

            target.Focus(NavigationMethod.Directional);
        }

        private void HandleAudioPanelKey(KeyEventArgs e)
        {
            if (!IsAudioPanelVisible)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Up:
                case Key.Down:
                    MoveAudioPanelFocus(e.Key == Key.Up ? -1 : 1);
                    ShowTransientControls();
                    e.Handled = true;
                    break;
                case Key.Enter:
                case Key.Select:
                case Key.Space:
                    ActivateFocusedAudioPanelItem();
                    HideAudioPanel(focusAudioButton: true);
                    ShowTransientControls();
                    e.Handled = true;
                    break;
                case Key.Escape:
                case Key.Back:
                    HideAudioPanel(focusAudioButton: true);
                    ShowTransientControls();
                    e.Handled = true;
                    break;
                case Key.Left:
                case Key.Right:
                    HideAudioPanel(focusAudioButton: true);
                    ShowTransientControls();
                    e.Handled = true;
                    break;
            }
        }

        private void MoveAudioPanelFocus(int direction)
        {
            var items = GetAudioPanelButtons();
            if (items.Count == 0)
            {
                return;
            }

            var focused = GetFocusedVisual() as Control;
            var current = focused is null
                ? -1
                : items.FindIndex(button => ReferenceEquals(button, focused) || IsVisualAncestorOf(button, focused));

            var next = current < 0
                ? 0
                : (current + direction + items.Count) % items.Count;

            items[next].Focus(NavigationMethod.Directional);
        }

        private void ActivateFocusedAudioPanelItem()
        {
            if (GetFocusedVisual() is not Button button || !IsAudioPanelButton(button))
            {
                return;
            }

            var parameter = button.CommandParameter;
            if (button.Command?.CanExecute(parameter) == true)
            {
                button.Command.Execute(parameter);
            }
        }

        private List<Button> GetAudioPanelButtons()
        {
            if (_audioPanel is null || !IsAudioPanelVisible)
            {
                return [];
            }

            return _audioPanel
                .GetVisualDescendants()
                .OfType<Button>()
                .Where(IsAudioPanelButton)
                .OrderBy(button => GetBoundsTop(button, _audioPanel))
                .ToList();
        }

        private bool IsAudioPanelSource(object? source)
        {
            for (var visual = source as Visual; visual is not null; visual = visual.GetVisualParent())
            {
                if (ReferenceEquals(visual, _audioPanel)
                    || visual is Button button && IsAudioPanelButton(button))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsFocusInAudioPanel()
        {
            var focused = GetFocusedVisual();
            if (focused is null || _audioPanel is null)
            {
                return false;
            }

            return ReferenceEquals(focused, _audioPanel)
                   || IsVisualAncestorOf(_audioPanel, focused)
                   || focused is Button button && IsAudioPanelButton(button);
        }

        private static bool IsAudioPanelButton(Button button)
        {
            return button.Classes.Contains("audio-panel-item")
                   && button.IsEffectivelyVisible
                   && button.IsEnabled;
        }

        private void ShowButtonFlyout(Button button, bool focusFirstItem)
        {
            if (button.Flyout is null)
            {
                return;
            }

            HidePlaybackPopups();
            _lastPlaybackFlyoutButton = button;
            button.Flyout.ShowAt(button);

            if (focusFirstItem)
            {
                Dispatcher.UIThread.Post(FocusFirstPlaybackMenuItem, DispatcherPriority.Background);
            }
        }

        private void FocusFirstPlaybackMenuItem()
        {
            var firstItem = TopLevel.GetTopLevel(this)?
                .GetVisualDescendants()
                .OfType<Button>()
                .FirstOrDefault(IsPlaybackMenuButton);

            firstItem?.Focus(NavigationMethod.Directional);
        }

        private static bool IsPlaybackMenuSource(object? source)
        {
            for (var visual = source as Visual; visual is not null; visual = visual.GetVisualParent())
            {
                if (visual is Button button && IsPlaybackMenuButton(button))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsFocusInPlaybackMenu()
        {
            return GetFocusedVisual() is Button button && IsPlaybackMenuButton(button);
        }

        private static bool IsPlaybackMenuButton(Button button)
        {
            return button.Classes.Contains("player-menu-item")
                   && button.IsEffectivelyVisible
                   && button.IsEnabled;
        }

        private static bool IsVisualAncestorOf(Visual ancestor, Visual visual)
        {
            for (var current = visual; current is not null; current = current.GetVisualParent())
            {
                if (ReferenceEquals(current, ancestor))
                {
                    return true;
                }
            }

            return false;
        }

        private static double GetBoundsLeft(Control control, Visual root)
        {
            return control.TranslatePoint(new Point(0, 0), root)?.X ?? double.MaxValue;
        }

        private static double GetBoundsTop(Control control, Visual root)
        {
            return control.TranslatePoint(new Point(0, 0), root)?.Y ?? double.MaxValue;
        }

        private void SetTimeSliderRemoteActive(bool isActive)
        {
            _isTimeSliderRemoteActive = isActive && _timeSlider is not null;

            if (_timeSlider is null)
            {
                return;
            }

            if (_isTimeSliderRemoteActive)
            {
                if (!_timeSlider.Classes.Contains("remote-active"))
                {
                    _timeSlider.Classes.Add("remote-active");
                }
            }
            else
            {
                _timeSlider.Classes.Remove("remote-active");
            }
        }

        private bool TryHandleVolumeDirection(Key key)
        {
            if (GetFocusedVisual() is not Control focused
                || _volumeButton is null
                || (!ReferenceEquals(focused, _volumeButton) && !IsVisualAncestorOf(_volumeButton, focused)))
            {
                return false;
            }

            ShowVolumePanel();
            var delta = key == Key.Up ? 5 : -5;
            Volume = Math.Clamp(Volume + delta, 0, MaxVolume);
            return true;
        }

        private void VolumeButtonGotFocus(object? sender, GotFocusEventArgs e)
        {
            ShowVolumePanel();
        }

        private void ShowVolumePanel()
        {
            SetCurrentValue(IsVolumePanelVisibleProperty, true);
        }

        private void HideVolumePanel(bool focusVolumeButton = false)
        {
            SetCurrentValue(IsVolumePanelVisibleProperty, false);

            if (focusVolumeButton && _volumeButton is { IsEffectivelyVisible: true, IsEnabled: true })
            {
                _volumeButton.Focus(NavigationMethod.Directional);
            }
        }

        private void UpdateRemainingTime()
        {
            var remaining = Duration - Time;
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }

            SetCurrentValue(RemainingTimeProperty, remaining);
        }

        private void UpdateVolumePercent()
        {
            var maxVolume = MaxVolume;
            var percent = maxVolume <= 0
                ? 0
                : (int)Math.Round(Volume * 100d / maxVolume);

            SetCurrentValue(VolumePercentProperty, Math.Clamp(percent, 0, 100));
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == MediaPlayerProperty)
            {
                var oldNew = change.GetOldAndNewValue<MPVMediaPlayer>();

                if (oldNew.oldValue != null)
                {
                    oldNew.oldValue.MpvEvent -= MpvEvent;
                }

                if (oldNew.newValue != null)
                {
                    var player = oldNew.newValue;

                    player.ObservableProperty(MPVMediaPlayer.PlaybackControlOpts.Pause, MpvFormat.MPV_FORMAT_FLAG);
                    player.ObservableProperty(MPVMediaPlayer.Properties.Duration, MpvFormat.MPV_FORMAT_DOUBLE);
                    player.ObservableProperty(MPVMediaPlayer.Properties.TimePos, MpvFormat.MPV_FORMAT_DOUBLE);
                    player.ObservableProperty(MPVMediaPlayer.AudioOpts.Volume, MpvFormat.MPV_FORMAT_INT64);
                    player.ObservableProperty(MPVMediaPlayer.AudioOpts.Mute, MpvFormat.MPV_FORMAT_STRING);
                    player.ObservableProperty(MPVMediaPlayer.PlaybackControlOpts.Speed, MpvFormat.MPV_FORMAT_DOUBLE);

                    player.MpvEvent += MpvEvent;

                    SetCurrentValue(SpeedProperty, player.GetPropertyDouble(MPVMediaPlayer.PlaybackControlOpts.Speed));
                    SetCurrentValue(VolumeProperty, player.GetPropertyLong(MPVMediaPlayer.AudioOpts.Volume));
                    SetCurrentValue(MaxVolumeProperty, player.GetPropertyLong(MPVMediaPlayer.AudioOpts.VolumeMax));
                    UpdateVolumePercent();

                }
            }
            else if (change.Property == TimeProperty)
            {
                UpdateRemainingTime();
                if (MediaPlayer == null || _isUpdatingTimeFromPlayer) return;
                var oldNew = change.GetOldAndNewValue<TimeSpan>();
                if (Math.Abs(oldNew.newValue.TotalSeconds - oldNew.oldValue.TotalSeconds) > 0.25)
                {
                    RequestSeek(oldNew.newValue);
                }
            }
            else if (change.Property == DurationProperty)
            {
                UpdateRemainingTime();
            }
            else if (change.Property == VolumeProperty)
            {
                UpdateVolumePercent();
                if (MediaPlayer == null) return;
                var value = change.GetNewValue<long>();
                if (value != MediaPlayer.GetPropertyLong(MPVMediaPlayer.AudioOpts.Volume))
                {
                    MediaPlayer.SetProperty(MPVMediaPlayer.AudioOpts.Volume, value);
                }
            }
            else if (change.Property == MaxVolumeProperty)
            {
                UpdateVolumePercent();
            }
            else if (change.Property == AspectRatioProperty)
            {
                if (MediaPlayer == null) return;
                MediaPlayer.SetProperty(MPVMediaPlayer.VideoOpts.VideoAspectOverride, change.GetNewValue<string>());
            }
        }

        private void MpvEvent(object? sender, MpvEvent mpvEvent)
        {
            switch (mpvEvent.event_id)
            {
                case MpvEventId.MPV_EVENT_NONE:
                    break;
                case MpvEventId.MPV_EVENT_SHUTDOWN:
                    break;
                case MpvEventId.MPV_EVENT_LOG_MESSAGE:
                    break;
                case MpvEventId.MPV_EVENT_GET_PROPERTY_REPLY:
                    break;
                case MpvEventId.MPV_EVENT_SET_PROPERTY_REPLY:
                    break;
                case MpvEventId.MPV_EVENT_COMMAND_REPLY:
                    break;
                case MpvEventId.MPV_EVENT_START_FILE:
                    break;
                case MpvEventId.MPV_EVENT_END_FILE:
                    break;
                case MpvEventId.MPV_EVENT_FILE_LOADED:
                    MpvFiledLoaded(sender);
                    break;
                case MpvEventId.MPV_EVENT_IDLE:
                    break;
                case MpvEventId.MPV_EVENT_TICK:
                    break;
                case MpvEventId.MPV_EVENT_CLIENT_MESSAGE:
                    break;
                case MpvEventId.MPV_EVENT_VIDEO_RECONFIG:
                    break;
                case MpvEventId.MPV_EVENT_AUDIO_RECONFIG:
                    break;
                case MpvEventId.MPV_EVENT_SEEK:
                    break;
                case MpvEventId.MPV_EVENT_PLAYBACK_RESTART:
                    break;
                case MpvEventId.MPV_EVENT_PROPERTY_CHANGE:
                    var property = mpvEvent.ReadData<MpvEventProperty>();
                    MpvPropertyChanged(sender, property);
                    break;
                case MpvEventId.MPV_EVENT_QUEUE_OVERFLOW:
                    break;
                case MpvEventId.MPV_EVENT_HOOK:
                    break;
                default:
                    break;
            }
        }

        private void MpvPropertyChanged(object? sender, MpvEventProperty property)
        {
            try
            {
                if (property.name == MPVMediaPlayer.Properties.Duration)
                {
                    if (property.format != MpvFormat.MPV_FORMAT_DOUBLE)
                    {
                        return;
                    }
                    DispatchSetCurrentValue(DurationProperty, TimeSpan.FromSeconds(property.ReadDoubleValue()));
                }
                else if (property.name == "time-pos")
                {
                    if (property.format != MpvFormat.MPV_FORMAT_DOUBLE)
                    {
                        return;
                    }
                    DispatchSetTimeFromPlayer(TimeSpan.FromSeconds(property.ReadDoubleValue()));
                }
                else if (property.name == "pause")
                {
                    if (property.format != MpvFormat.MPV_FORMAT_FLAG)
                    {
                        return;
                    }
                    var isPlaying = !property.ReadBoolValue();
                    DispatchSetCurrentValue(PlayingProperty, isPlaying);
                    Dispatcher.UIThread.InvokeAsync(() => ShowTransientControls());
                }
                else if (property.name == "volume")
                {
                    if (!TryReadNumericProperty(property, out var volume))
                    {
                        return;
                    }

                    DispatchSetCurrentValue(VolumeProperty, (long)Math.Round(volume));
                }
                else if (property.name == "speed")
                {
                    if (!TryReadNumericProperty(property, out var speed))
                    {
                        return;
                    }

                    DispatchSetCurrentValue(SpeedProperty, speed);
                }
            }
            catch (FormatException)
            {
            }
        }

        private static bool TryReadNumericProperty(MpvEventProperty property, out double value)
        {
            value = 0;

            if (property.format == MpvFormat.MPV_FORMAT_DOUBLE)
            {
                value = property.ReadDoubleValue();
                return true;
            }

            if (property.format == MpvFormat.MPV_FORMAT_INT64)
            {
                value = property.ReadLongValue();
                return true;
            }

            return false;
        }

        private void MpvFiledLoaded(object? sender)
        {
            Debug.WriteLine("[MediaPlayerView] file-loaded received.");
            Dispatcher.UIThread.InvokeAsync(TryGetVideoParams);
            Dispatcher.UIThread.InvokeAsync(TryGetAudioTracks);
            Dispatcher.UIThread.InvokeAsync(TryGetSubtitleTracks);
        }

        private void DispatchSetCurrentValue(AvaloniaProperty property, object value)
        {
            Dispatcher.UIThread.InvokeAsync(() => SetCurrentValue(property, value));
        }

        private void DispatchSetTimeFromPlayer(TimeSpan value)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (_isScrubbing)
                {
                    return;
                }

                _isUpdatingTimeFromPlayer = true;
                try
                {
                    SetCurrentValue(TimeProperty, value);
                }
                finally
                {
                    _isUpdatingTimeFromPlayer = false;
                }
            });
        }

        private void TimeSliderPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            _isScrubbing = true;
        }

        private void TimeSliderPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            CompleteScrub();
        }

        private void TimeSliderPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            CompleteScrub();
        }

        private void CompleteScrub()
        {
            if (!_isScrubbing)
            {
                return;
            }

            _isScrubbing = false;
            RequestSeek(Time);
        }

        private void RequestSeek(TimeSpan time)
        {
            if (MediaPlayer == null)
            {
                return;
            }

            _pendingSeekTime = time;

            if (_seekDebounceTimer is null)
            {
                _seekDebounceTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(120)
                };

                _seekDebounceTimer.Tick += (_, _) =>
                {
                    _seekDebounceTimer?.Stop();

                    if (MediaPlayer == null)
                    {
                        return;
                    }

                    MediaPlayer.SetProperty(MPVMediaPlayer.Properties.TimePos, _pendingSeekTime.TotalSeconds);
                };
            }

            _seekDebounceTimer.Stop();
            _seekDebounceTimer.Start();
        }

        private void ShowTransientControls(bool focusDefaultControl = false)
        {
            SetCurrentValue(AreControlsVisibleProperty, true);
            Cursor = null;

            _controlsIdleTimer.Stop();
            if (Playing)
            {
                _controlsIdleTimer.Start();
            }

            if (focusDefaultControl)
            {
                Dispatcher.UIThread.Post(FocusDefaultControl, DispatcherPriority.Background);
            }
        }

        private void HideTransientControls(bool force = false)
        {
            _controlsIdleTimer.Stop();

            if (!Playing)
            {
                ShowTransientControls();
                return;
            }

            if (!force && _isScrubbing)
            {
                ShowTransientControls();
                return;
            }

            HidePlaybackPopups();
            SetTimeSliderRemoteActive(false);

            if (IsFocusInPlaybackChrome())
            {
                Focus(NavigationMethod.Unspecified);
            }

            SetCurrentValue(AreControlsVisibleProperty, false);
            Cursor = new Cursor(StandardCursorType.None);
        }

        private async Task TryOpenFile()
        {
            if (MediaPlayer == null) return;

            var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storageProvider == null) return;
            var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = Localize?.MediaSelector ?? "Media selector",
                FileTypeFilter =
                [
                    new FilePickerFileType("mp4")
                    {
                        Patterns = ["*.mp4"],
                        AppleUniformTypeIdentifiers = ["public.mpeg-4"],
                        MimeTypes = ["video/mp4"]
                    }
                ],
                AllowMultiple = false
            });

            if (files.Count > 0)
            {
                var file = files[0];
                var path = file.Path.LocalPath; //App.Instance?.UriResolver?.GetRealPath(file.Path);
                MediaPlayer.EnsureRenderContextCreated();
                await MediaPlayer.ExecuteCommandAsync([MPVMediaPlayer.PlaylistManipulationCommands.Loadfile, path!]);
                SetCurrentValue(PlayingProperty, true);
            }
        }

        private void TryPlayPause()
        {
            if (MediaPlayer == null) return;
            var pause = MediaPlayer.GetPropertyBoolean(MPVMediaPlayer.PlaybackControlOpts.Pause);
            MediaPlayer.SetProperty(MPVMediaPlayer.PlaybackControlOpts.Pause, !pause);
        }

        private void TrySeekRelative(double seconds)
        {
            if (MediaPlayer == null) return;

            var target = Time + TimeSpan.FromSeconds(seconds);

            if (target < TimeSpan.Zero)
            {
                target = TimeSpan.Zero;
            }
            else if (Duration > TimeSpan.Zero && target > Duration)
            {
                target = Duration;
            }

            _isUpdatingTimeFromPlayer = true;
            try
            {
                SetCurrentValue(TimeProperty, target);
            }
            finally
            {
                _isUpdatingTimeFromPlayer = false;
            }

            RequestSeek(target);
        }

        private void TrySwitchSpeed()
        {
            if (MediaPlayer == null) return;

            var speed = MediaPlayer.GetPropertyDouble(MPVMediaPlayer.PlaybackControlOpts.Speed);
            speed++;
            if (speed > 2)
            {
                MediaPlayer.SetProperty(MPVMediaPlayer.PlaybackControlOpts.Speed, 1d);
            }
            else
            {
                MediaPlayer.SetProperty(MPVMediaPlayer.PlaybackControlOpts.Speed, speed);
            }
        }

        private void TrySwitchAspectRatio()
        {
            if (MediaPlayer == null) return;
            var ratio = _aspectRatio.Dequeue();
            _aspectRatio.Enqueue(ratio);
            AspectRatio = ratio;
        }

        private void TryGetVideoParams()
        {
            if (MediaPlayer == null) return;

            MpvNodeWrap? node = null;

            try
            {
                node = MediaPlayer.GetPropertyNode(MPVMediaPlayer.Properties.VideoParams);
                using var sw = new StringWriter();
                using var writer = new IndentedTextWriter(sw);
                node.Node.ReadToWriter(writer);
                writer.Flush();
                var vp = sw.ToString();
                DispatchSetCurrentValue(VideoParamsProperty, vp);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MediaPlayerView] video params read failed. Error: {ex}");
            }
            finally
            {
                if (node is not null)
                {
                    MediaPlayer.FreeNode(node);
                }
            }
        }

        private void TryGetAudioTracks()
        {
            if (MediaPlayer == null) return;

            MpvNodeWrap? node = null;

            try
            {
                Debug.WriteLine("[MediaPlayerView] reading audio track-list.");
                node = MediaPlayer.GetPropertyNode(MPVMediaPlayer.Properties.TrackList);
                var tracks = ReadTracks(node.Node, "audio")
                    .Select((track, index) =>
                    {
                        var name = !string.IsNullOrWhiteSpace(track.Title)
                            ? track.Title
                            : !string.IsNullOrWhiteSpace(track.Language)
                                ? track.Language
                                : string.Format(Localize?.AudioTrackFormat ?? "Audio {0}", index + 1);

                        return new AudioTrackModel
                        {
                            Id = track.Id,
                            Name = name,
                            Language = track.Language
                        };
                    })
                    .ToList();

                var newList = new AvaloniaList<AudioTrackModel>(tracks);
                SetCurrentValue(AudioTracksProperty, newList);
                SetCurrentValue(HasAudioTracksProperty, newList.Count > 1);
                SetCurrentValue(SelectedAudioTrackProperty, newList.FirstOrDefault());
                Debug.WriteLine($"[MediaPlayerView] audio track-list read. Count: {newList.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MediaPlayerView] audio track-list read failed. Error: {ex}");
            }
            finally
            {
                if (node is not null)
                {
                    MediaPlayer.FreeNode(node);
                }
            }
        }

        private void TryGetSubtitleTracks()
        {
            if (MediaPlayer == null) return;

            MpvNodeWrap? node = null;

            try
            {
                Debug.WriteLine("[MediaPlayerView] reading subtitle track-list.");
                node = MediaPlayer.GetPropertyNode(MPVMediaPlayer.Properties.TrackList);
                var tracks = ReadTracks(node.Node, "sub")
                    .Select((track, index) => new SubtitleModel
                    {
                        Id = track.Id,
                        Name = !string.IsNullOrWhiteSpace(track.Title)
                            ? track.Title
                            : !string.IsNullOrWhiteSpace(track.Language)
                                ? track.Language
                                : string.Format("Subtitle {0}", index + 1),
                        Url = track.ExternalFilename ?? string.Empty
                    })
                    .ToList();

                if (tracks.Count == 0)
                {
                    SetCurrentValue(HasSubTitlesProperty, SubTitles.Any(s => s.Id != "no"));
                    return;
                }

                var closed = SubTitles.FirstOrDefault(s => s.Id == "no")
                    ?? new SubtitleModel
                    {
                        Id = "no",
                        Name = Localize?.Closed ?? "Closed",
                        Url = string.Empty
                    };

                var newList = new AvaloniaList<SubtitleModel>([closed, .. tracks]);
                SetCurrentValue(SubTitlesProperty, newList);
                SetCurrentValue(HasSubTitlesProperty, true);
                SetCurrentValue(SelectedSubTitleProperty, newList.FirstOrDefault());
                Debug.WriteLine($"[MediaPlayerView] subtitle track-list read. Count: {newList.Count}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MediaPlayerView] subtitle track-list read failed. Error: {ex}");
            }
            finally
            {
                if (node is not null)
                {
                    MediaPlayer.FreeNode(node);
                }
            }
        }

        private static List<MpvTrackInfo> ReadTracks(MpvNode root, string type)
        {
            var tracks = new List<MpvTrackInfo>();

            if (root.format != MpvFormat.MPV_FORMAT_NODE_ARRAY)
            {
                return tracks;
            }

            foreach (var trackNode in root.ReadNodeArray())
            {
                if (trackNode.format != MpvFormat.MPV_FORMAT_NODE_MAP)
                {
                    continue;
                }

                var map = trackNode.ReadNodeMap();
                if (!TryReadString(map, "type", out var trackType) || trackType != type)
                {
                    continue;
                }

                var id = TryReadLong(map, "id", out var trackId)
                    ? trackId.ToString()
                    : TryReadString(map, "id", out var idText)
                        ? idText
                        : null;

                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                TryReadString(map, "title", out var title);
                TryReadString(map, "lang", out var language);
                TryReadString(map, "external-filename", out var externalFilename);
                tracks.Add(new MpvTrackInfo(id, title, language, externalFilename));
            }

            return tracks;
        }

        private static bool TryReadString(Dictionary<string, MpvNode> map, string key, out string? value)
        {
            value = null;

            if (!map.TryGetValue(key, out var node) || node.format != MpvFormat.MPV_FORMAT_STRING)
            {
                return false;
            }

            value = node.ReadString();
            return value is not null;
        }

        private static bool TryReadLong(Dictionary<string, MpvNode> map, string key, out long value)
        {
            value = 0;

            if (!map.TryGetValue(key, out var node) || node.format != MpvFormat.MPV_FORMAT_INT64)
            {
                return false;
            }

            value = node.ReadInt64();
            return true;
        }

        private void TrySwitchSubTitle(object? parameter)
        {
            string? subTitleId = parameter as string;

            if (string.IsNullOrEmpty(subTitleId) || SubTitles == null || MediaPlayer == null)
                return;

            var targetSub = SubTitles.FirstOrDefault(s => s.Id == subTitleId);

            if (targetSub != null)
            {
                //Debug.WriteLine($"[MediaPlayerView] selected subtitle: {JsonSerializer.Serialize(targetSub)}");
                SetCurrentValue(SelectedSubTitleProperty, targetSub);

                if (targetSub.Id == "no")
                {
                    MediaPlayer.SetProperty("sid", "no");
                    return;
                }

                try
                {
                    MediaPlayer.SetProperty("sid", targetSub.Id ?? "no");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[MediaPlayerView] subtitle sid selection failed. Id: {targetSub.Id} Name: {targetSub.Name} Url: {targetSub.Url} Error: {ex}");
                }

                //Debug.WriteLine($"[MediaPlayerView] selected sid: {MediaPlayer.GetPropertyString("sid")}");
            }
        }

        private void TrySwitchAudioTrack(object? parameter)
        {
            string? audioTrackId = parameter as string;

            if (string.IsNullOrEmpty(audioTrackId) || AudioTracks == null || MediaPlayer == null)
                return;

            var targetAudio = AudioTracks.FirstOrDefault(a => a.Id == audioTrackId);

            if (targetAudio != null)
            {
                SetCurrentValue(SelectedAudioTrackProperty, targetAudio);
                MediaPlayer.SetProperty("aid", targetAudio.Id);
            }
        }

        private void TryToggleFullScreen()
        {
            if (TopLevel.GetTopLevel(this) is not Window window)
            {
                return;
            }

            if (window.WindowState == WindowState.FullScreen)
            {
                window.WindowState = _restoreWindowState == WindowState.FullScreen
                    ? WindowState.Normal
                    : _restoreWindowState;
                
                //window.SystemDecorations = SystemDecorations.Full;
                SetCurrentValue(IsFullScreenProperty, false);
                return;
            }

            _restoreWindowState = window.WindowState;
            window.WindowState = WindowState.FullScreen;

            // window.ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome;
            // window.ExtendClientAreaTitleBarHeightHint = -1;
            // window.ExtendClientAreaToDecorationsHint = false;
            // window.SystemDecorations = SystemDecorations.None;
            SetCurrentValue(IsFullScreenProperty, true);
        }

        private void TryStop()
        {
            if (DataContext is PlayerViewModel viewModel)
            {
                viewModel.Close();
            }
        }

        public void AddSubtitles(List<SubtitleModel> subtitles)
        {
            Dispatcher.UIThread.Post(async () =>
            {
                if (MediaPlayer == null) return;

                await Task.Delay(800);

                var mappedSubtitles = MapSubtitlesToMpvTrackIds(subtitles);
                var newList = new AvaloniaList<SubtitleModel>(mappedSubtitles);

                SetCurrentValue(SubTitlesProperty, newList);
                SetCurrentValue(HasSubTitlesProperty, newList.Any(s => s.Id != "no"));

                SetCurrentValue(SelectedSubTitleProperty, newList.FirstOrDefault());
            });
        }

        private List<SubtitleModel> MapSubtitlesToMpvTrackIds(List<SubtitleModel> subtitles)
        {
            var closed = subtitles.FirstOrDefault(s => s.Id == "no")
                ?? new SubtitleModel
                {
                    Id = "no",
                    Name = Localize?.Closed ?? "Closed",
                    Url = string.Empty
                };

            var visibleSubtitles = subtitles.Where(s => s.Id != "no").ToList();
            if (MediaPlayer == null || visibleSubtitles.Count == 0)
            {
                return [closed];
            }

            MpvNodeWrap? node = null;

            try
            {
                node = MediaPlayer.GetPropertyNode(MPVMediaPlayer.Properties.TrackList);
                var mpvSubtitles = ReadTracks(node.Node, "sub");
                var mapped = visibleSubtitles
                    .Select((subtitle, index) =>
                    {
                        var track = FindMatchingSubtitleTrack(mpvSubtitles, subtitle)
                            ?? (mpvSubtitles.Count >= visibleSubtitles.Count
                                ? mpvSubtitles.Skip(mpvSubtitles.Count - visibleSubtitles.Count).ElementAtOrDefault(index)
                                : mpvSubtitles.ElementAtOrDefault(index));

                        //Debug.WriteLine($"[MediaPlayerView] subtitle map. Name: {subtitle.Name} Url: {subtitle.Url} TrackId: {subtitle?.Id} TrackFile: {track?.ExternalFilename}");
                        
                        var sub = new SubtitleModel
                        {
                            Id = subtitle?.Id,
                            Name = subtitle?.Name ?? "",
                            Url = subtitle?.Url ?? ""
                        };

                        //Debug.WriteLine($"[MediaPlayerView] subtitle sub: {JsonSerializer.Serialize(sub)}");

                        return sub;
                    })
                    .ToList();

                return [closed, .. mapped];
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MediaPlayerView] subtitle track-list mapping failed. Error: {ex}");
                return subtitles;
            }
            finally
            {
                if (node is not null)
                {
                    MediaPlayer.FreeNode(node);
                }
            }
        }

        private static MpvTrackInfo? FindMatchingSubtitleTrack(List<MpvTrackInfo> tracks, SubtitleModel subtitle)
        {
            var subtitleKeys = GetSubtitleMatchKeys(subtitle.Url).ToHashSet(StringComparer.OrdinalIgnoreCase);

            return tracks.FirstOrDefault(track =>
                !string.IsNullOrWhiteSpace(track.ExternalFilename)
                && GetSubtitleMatchKeys(track.ExternalFilename).Any(subtitleKeys.Contains));
        }

        private static IEnumerable<string> GetSubtitleMatchKeys(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                yield break;
            }

            yield return value;

            if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
            {
                if (uri.IsFile)
                {
                    yield return uri.LocalPath;
                    yield return Path.GetFullPath(uri.LocalPath);
                }

                yield return uri.AbsoluteUri;
                yield break;
            }

            string? fullPath = null;
            string? fileUri = null;

            try
            {
                fullPath = Path.GetFullPath(value);

                if (File.Exists(fullPath))
                {
                    fileUri = new Uri(fullPath).AbsoluteUri;
                }
            }
            catch
            {
            }

            if (!string.IsNullOrWhiteSpace(fullPath))
            {
                yield return fullPath;
            }

            if (!string.IsNullOrWhiteSpace(fileUri))
            {
                yield return fileUri;
            }
        }

        private sealed record MpvTrackInfo(string Id, string? Title, string? Language, string? ExternalFilename);
    }
}
