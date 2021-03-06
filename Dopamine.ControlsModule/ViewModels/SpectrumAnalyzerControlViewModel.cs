﻿using Dopamine.Common.Services.Playback;
using Dopamine.Core.Settings;
using Prism.Mvvm;

namespace Dopamine.ControlsModule.ViewModels
{
    public class SpectrumAnalyzerControlViewModel : BindableBase
    {
        #region Variables
        private IPlaybackService playbackService;
        private bool showSpectrumAnalyzer;
        private bool isPlaying;
        #endregion

        #region Properties
        public bool ShowSpectrumAnalyzer
        {
            get { return this.showSpectrumAnalyzer; }
            set { SetProperty<bool>(ref this.showSpectrumAnalyzer, value); }
        }

        public bool IsPlaying
        {
            get { return this.isPlaying; }
            set { SetProperty<bool>(ref this.isPlaying, value); }
        }
        #endregion

        #region Construction
        public SpectrumAnalyzerControlViewModel(IPlaybackService playbackService)
        {
            this.playbackService = playbackService;

            this.playbackService.SpectrumVisibilityChanged += isSpectrumVisible => this.ShowSpectrumAnalyzer = isSpectrumVisible;

            this.playbackService.PlaybackFailed += (_, __) => this.IsPlaying = false;
            this.playbackService.PlaybackStopped += (_, __) => this.IsPlaying = false;
            this.playbackService.PlaybackPaused += (_, __) => this.IsPlaying = false;
            this.playbackService.PlaybackResumed += (_, __) => this.IsPlaying = true;
            this.playbackService.PlaybackSuccess += (_) => this.IsPlaying = true;

            this.ShowSpectrumAnalyzer = XmlSettingsClient.Instance.Get<bool>("Playback", "ShowSpectrumAnalyzer");

            // Initial value
            if (!this.playbackService.IsStopped & this.playbackService.IsPlaying)
            {
                this.IsPlaying = true;
            }
            else
            {
                this.IsPlaying = false;
            }
        }
        #endregion
    }
}
