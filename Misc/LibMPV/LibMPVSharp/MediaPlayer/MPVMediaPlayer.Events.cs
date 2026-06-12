using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace LibMPVSharp
{
    public unsafe partial class MPVMediaPlayer
    {
        private Task? _eventLoopTask;
        public event EventHandler<MpvEvent>? MpvEvent;

        private void MPVWeakup(IntPtr ctx)
        {
            if (_eventLoopTask == null)
            {
                _eventLoopTask = Task.Run(() =>
                {
                    while (!_disposed)
                    {
                        try
                        {
                            OnMPVEvents(-1);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                        }
                    }
                });
            }
        }

        private void OnMPVEvents(double timeout)
        {
            if (_disposed || _clientHandle == null)
            {
                return;
            }

            var mpvEvent = Client.MpvWaitEvent(_clientHandle, timeout);
            if (_disposed || mpvEvent == null)
            {
                return;
            }

            switch (mpvEvent->event_id)
            {
                case MpvEventId.MPV_EVENT_COMMAND_REPLY:
                case MpvEventId.MPV_EVENT_GET_PROPERTY_REPLY:
                case MpvEventId.MPV_EVENT_SET_PROPERTY_REPLY:
                    TryMPVEventReply(mpvEvent);
                    break;
            }
            try
            {
                MpvEvent?.Invoke(this, *mpvEvent);
            }
            catch{}
            
            if (mpvEvent != null && mpvEvent->event_id == MpvEventId.MPV_EVENT_SHUTDOWN)
            {
                Dispose();
            }
        }

        private void StopEventLoop(MpvHandle* handle)
        {
            MpvEvent = null;

            try
            {
                _wakeupCallback = _ => { };
                Client.MpvSetWakeupCallback(handle, _wakeupCallback, null);
                Client.MpvWakeup(handle);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MPVMediaPlayer] event loop wakeup failed: {ex}");
            }

            var eventLoopTask = _eventLoopTask;
            if (eventLoopTask is null || eventLoopTask.IsCompleted)
            {
                return;
            }

            if (Task.CurrentId == eventLoopTask.Id)
            {
                return;
            }

            try
            {
                eventLoopTask.Wait(TimeSpan.FromMilliseconds(500));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MPVMediaPlayer] event loop wait failed: {ex}");
            }
        }

        private void TryMPVEventReply(MpvEvent* mpvEvent)
        {
            if (mpvEvent->reply_userdata == 0)
            {
                return;
            }

            var handler = GCHandle.FromIntPtr((IntPtr)mpvEvent->reply_userdata);
            var tcs = handler.Target as TaskCompletionSource;

            if (mpvEvent->error < 0)
            {
                tcs?.TrySetException(new LibMPVException((MpvError)mpvEvent->error, $"{mpvEvent->error}"));
            }
            else
            {
                tcs?.TrySetResult();
            }
            handler.Free();
        }
    }
}
