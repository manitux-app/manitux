using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Labs.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using LibMPVSharp;
using LibMPVSharp.Extensions;
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

        public static readonly StyledProperty<long> VolumeProperty = AvaloniaProperty.Register<MediaPlayerView, long>(nameof(Volume));
        public long Volume
        {
            get => GetValue(VolumeProperty);
            set => SetValue(VolumeProperty, value);
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

        private static Queue<string> _aspectRatio = new Queue<string>();
        private Slider? _timeSlider;
        private DispatcherTimer? _seekDebounceTimer;
        private TimeSpan _pendingSeekTime;
        private bool _isScrubbing;
        private bool _isUpdatingTimeFromPlayer;
        private WindowState _restoreWindowState = WindowState.Normal;
        private readonly DispatcherTimer _controlsIdleTimer;

        static MediaPlayerView()
        {
            MediaPlayerProperty.Changed.AddClassHandler<MediaPlayerView>((s, e) => s.OnPropertyChanged(e));
            TimeProperty.Changed.AddClassHandler<MediaPlayerView>((s, e) => s.OnPropertyChanged(e));
            VolumeProperty.Changed.AddClassHandler<MediaPlayerView>((s, e) => s.OnPropertyChanged(e));
            AspectRatioProperty.Changed.AddClassHandler<MediaPlayerView>((s, e) => s.OnPropertyChanged(e));

            _aspectRatio.Enqueue("no");
            _aspectRatio.Enqueue("16:9");
            _aspectRatio.Enqueue("4:3");
        }

        protected override Type StyleKeyOverride => typeof(MediaPlayerView);

        public MediaPlayerView()
        {
            _controlsIdleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            _controlsIdleTimer.Tick += (_, _) => HideTransientControls();

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

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            ShowTransientControls();
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
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

            _timeSlider = e.NameScope.Get<Slider>("PART_TimeBar");
            _timeSlider.PointerPressed += TimeSliderPointerPressed;
            _timeSlider.PointerReleased += TimeSliderPointerReleased;
            _timeSlider.PointerCaptureLost += TimeSliderPointerCaptureLost;
            _timeSlider.Focus();
            ShowTransientControls();
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

                }
            }
            else if (change.Property == TimeProperty)
            {
                if (MediaPlayer == null || _isUpdatingTimeFromPlayer) return;
                var oldNew = change.GetOldAndNewValue<TimeSpan>();
                if (Math.Abs(oldNew.newValue.TotalSeconds - oldNew.oldValue.TotalSeconds) > 0.25)
                {
                    RequestSeek(oldNew.newValue);
                }
            }
            else if (change.Property == VolumeProperty)
            {
                if (MediaPlayer == null) return;
                var value = change.GetNewValue<long>();
                if (value != MediaPlayer.GetPropertyLong(MPVMediaPlayer.AudioOpts.Volume))
                {
                    MediaPlayer.SetProperty(MPVMediaPlayer.AudioOpts.Volume, value);
                }
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
                    MpvPropertyChanged(sender, mpvEvent.ReadData<MpvEventProperty>());
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
            if (property.name == MPVMediaPlayer.Properties.Duration)
            {
                DispatchSetCurrentValue(DurationProperty, TimeSpan.FromSeconds(property.ReadDoubleValue()));
            }
            else if (property.name == "time-pos")
            {
                DispatchSetTimeFromPlayer(TimeSpan.FromSeconds(property.ReadDoubleValue()));
            }
            else if (property.name == "pause")
            {
                DispatchSetCurrentValue(PlayingProperty, !property.ReadBoolValue());
            }
            else if (property.name == "volume")
            {
                DispatchSetCurrentValue(VolumeProperty, property.ReadLongValue());
            }
            else if (property.name == "speed")
            {
                DispatchSetCurrentValue(SpeedProperty, property.ReadDoubleValue());
            }
        }

        private void MpvFiledLoaded(object? sender)
        {
            Dispatcher.UIThread.InvokeAsync(TryGetVideoParams);
            Dispatcher.UIThread.InvokeAsync(TryGetAudioTracks);
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

        private void ShowTransientControls()
        {
            SetCurrentValue(AreControlsVisibleProperty, true);
            Cursor = null;

            _controlsIdleTimer.Stop();
            _controlsIdleTimer.Start();
        }

        private void HideTransientControls()
        {
            _controlsIdleTimer.Stop();

            if (_isScrubbing)
            {
                ShowTransientControls();
                return;
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
                Title = "Media selector",
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
            var node = MediaPlayer.GetPropertyNode(MPVMediaPlayer.Properties.VideoParams);
            using var sw = new StringWriter();
            using var writer = new IndentedTextWriter(sw);
            node.Node.ReadToWriter(writer);
            writer.Flush();
            MediaPlayer.FreeNode(node);
            var vp = sw.ToString();
            Debug.WriteLine(vp);
            DispatchSetCurrentValue(VideoParamsProperty, vp);
        }

        private void TryGetAudioTracks()
        {
            if (MediaPlayer == null) return;

            MpvNodeWrap? node = null;

            try
            {
                node = MediaPlayer.GetPropertyNode(MPVMediaPlayer.Properties.TrackList);
                var tracks = ReadTracks(node.Node, "audio")
                    .Select((track, index) =>
                    {
                        var name = !string.IsNullOrWhiteSpace(track.Title)
                            ? track.Title
                            : !string.IsNullOrWhiteSpace(track.Language)
                                ? track.Language
                                : $"Audio {index + 1}";

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
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Audio track list could not be read: " + ex);
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
                tracks.Add(new MpvTrackInfo(id, title, language));
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
            Debug.WriteLine("subTitleId: " + subTitleId);

            if (string.IsNullOrEmpty(subTitleId) || SubTitles == null || MediaPlayer == null)
                return;

            var targetSub = SubTitles.FirstOrDefault(s => s.Id == subTitleId);

            if (targetSub != null)
            {
                SetCurrentValue(SelectedSubTitleProperty, targetSub);
                MediaPlayer.SetProperty("sid", targetSub.Id);
            }
        }

        private void TrySwitchAudioTrack(object? parameter)
        {
            string? audioTrackId = parameter as string;
            Debug.WriteLine("audioTrackId: " + audioTrackId);

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
                SetCurrentValue(IsFullScreenProperty, false);
                return;
            }

            _restoreWindowState = window.WindowState;
            window.WindowState = WindowState.FullScreen;
            SetCurrentValue(IsFullScreenProperty, true);
        }

        private void TryStop()
        {
            if (DataContext is PlayerViewModel viewModel)
            {
                viewModel.RequestClose();
            }
        }

        public void AddSubtitles(List<SubtitleModel> subtitles)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (MediaPlayer == null) return;

                var newList = new AvaloniaList<SubtitleModel>(subtitles);

                SetCurrentValue(SubTitlesProperty, newList);
                SetCurrentValue(HasSubTitlesProperty, newList.Any(s => s.Id != "no"));

                SetCurrentValue(SelectedSubTitleProperty, newList.FirstOrDefault());
            });
        }

        private sealed record MpvTrackInfo(string Id, string? Title, string? Language);
    }
}
