﻿using Dopamine.Common.Services.Indexing;
using Dopamine.Common.Services.Playback;
using Dopamine.Core.Base;
using Dopamine.Core.Database;
using Dopamine.Core.IO;
using Dopamine.Core.Logging;
using Dopamine.Core.Settings;
using Dopamine.Core.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace Dopamine.Common.Services.File
{
    public class FileService : IFileService
    {
        #region Variables
        private IPlaybackService playbackService;
        private List<string> files;
        private object lockObject = new object();
        private Timer addFilesTimer;
        private int addFilesTimeout = 250; // milliseconds
        private string instanceGuid;
        #endregion

        #region Construction
        public FileService(IPlaybackService iPlaybackService)
        {
            this.playbackService = iPlaybackService;

            // Unique identifier which will be used by this instance only to create cached artwork.
            // This prevents the cleanup function to delete artwork which is in use by this instance.
            this.instanceGuid = Guid.NewGuid().ToString();

            this.files = new List<string>();
            this.addFilesTimer = new Timer();
            this.addFilesTimer.Interval = this.addFilesTimeout;
            this.addFilesTimer.Elapsed += AddFilesTimerElapsedHandler;
            this.DeleteFileArtworkFromCacheAsync(this.instanceGuid);
        }
        #endregion

        #region IFileService
        public void ProcessArguments(string[] args)
        {
            this.ProcessArgumentsAsync(args);
        }
        #endregion

        #region Private
        private async Task ProcessArgumentsAsync(string[] args)
        {
            if (args.Length > 1)
            {
                this.addFilesTimer.Stop();

                await Task.Run(() =>
                {
                    LogClient.Instance.Logger.Info("Received commandline arguments.");

                    // Don't process index=0, as this contains the name of the executable.
                    for (int index = 1; index <= args.Length - 1; index++)
                    {
                        lock (this.lockObject)
                        {
                            this.files.Add(args[index]);
                            LogClient.Instance.Logger.Info("Added file '{0}'", args[index]);
                        }
                    }
                });

                this.RestartAddFilesTimer();
            }
        }

        private void RestartAddFilesTimer()
        {
            this.addFilesTimer.Stop();
            this.addFilesTimer.Start();
        }

        private async void AddFilesTimerElapsedHandler(Object sender, ElapsedEventArgs e)
        {
            this.addFilesTimer.Stop();

            // Check if there is only 1 instance (this one) of the application running. If not,
            // that could mean there are other instances trying to send files to this instance.
            if (EnvironmentUtils.IsSingleInstance(ProductInformation.ApplicationAssemblyName))
            {
                lock (this.lockObject)
                {
                    LogClient.Instance.Logger.Info("Finished adding files. Number of files added = {0}", this.files.Count);
                }

                await Application.Current.Dispatcher.BeginInvoke(new Action(async () => await this.ImportFiles()));
            }
            else
            {
                // There are still other instances trying to send files. Check again next time.
                this.RestartAddFilesTimer();
            }
        }

        private async Task ImportFiles()
        {
            var tracks = new List<TrackInfo>();

            await Task.Run(() =>
            {
                lock (this.lockObject)
                {
                    // Sort the files alphabetically
                    this.files.Sort();

                    // Convert the files to tracks
                    foreach (string path in this.files)
                    {
                        if (FileFormats.IsSupportedAudioFile(path))
                        {
                            // The file is a supported audio format: add it directly.
                            tracks.Add(IndexerUtils.Path2TrackInfo(path, "file-" + this.instanceGuid));

                        }
                        else if (FileFormats.IsSupportedPlaylistFile(path))
                        {
                            // The file is a supported playlist format: process the contents of the playlist file.
                            List<string> audioFilePaths = this.ProcessPlaylistFile(path);

                            foreach (string audioFilePath in audioFilePaths)
                            {
                                tracks.Add(IndexerUtils.Path2TrackInfo(audioFilePath, "file-" + this.instanceGuid));
                            }
                        }
                        else if (Directory.Exists(path))
                        {
                            // The file is a directory: get the audio files in that directory.
                            List<string> audioFilePaths = this.ProcessDirectory(path);

                            foreach (string audioFilePath in audioFilePaths)
                            {
                                tracks.Add(IndexerUtils.Path2TrackInfo(audioFilePath, "file-" + this.instanceGuid));
                            }
                        }
                        else
                        {
                            // The file is unknown: do not process it.
                        }
                    }

                    // When done importing files, clear the list.
                    this.files.Clear();
                }
            });

            LogClient.Instance.Logger.Info("Number of tracks to play = {0}", tracks.Count);

            if (tracks.Count > 0)
            {
                LogClient.Instance.Logger.Info("Enqueuing {0} tracks.", tracks.Count);
                await this.playbackService.Enqueue(tracks);
            }
        }

        private List<string> ProcessPlaylistFile(string playlistPath)
        {
            var decoder = new PlaylistDecoder();
            DecodePlaylistResult decodeResult = decoder.DecodePlaylist(playlistPath);

            if (!decodeResult.DecodeResult.Result)
            {
                LogClient.Instance.Logger.Error("Error while decoding playlist file. Exception: {0}", decodeResult.DecodeResult.GetMessages());
            }

            return decodeResult.Paths;
        }

        private List<string> ProcessDirectory(string directoryPath)
        {
            List<string> paths = new List<string>();

            // Create a queue to hold exceptions that have occurred while scanning the directory tree
            var recurseExceptions = new ConcurrentQueue<Exception>();
            FileOperations.TryDirectoryRecursiveGetFiles(directoryPath, paths, FileFormats.SupportedMediaExtensions, recurseExceptions);

            if (recurseExceptions.Count > 0)
            {
                foreach (Exception recurseException in recurseExceptions)
                {
                    LogClient.Instance.Logger.Error("Error while recursively getting files/folders. Exception: {0}", recurseException.ToString());
                }
            }

            return paths;
        }

        private async Task DeleteFileArtworkFromCacheAsync(string exclude)
        {
            await Task.Run(() =>
            {
                string[] artworkFiles = null;

                try
                {
                    string artworkCacheDirectory = System.IO.Path.Combine(XmlSettingsClient.Instance.ApplicationFolder, ApplicationPaths.CacheSubDirectory, ApplicationPaths.CoverArtCacheSubDirectory);

                    if (System.IO.Directory.Exists(artworkCacheDirectory))
                    {
                        artworkFiles = System.IO.Directory.GetFiles(artworkCacheDirectory, "file-*.jpg");
                    }
                }
                catch (Exception ex)
                {
                    LogClient.Instance.Logger.Error("There was a problem while fetching file artwork. Exception: {0}", ex.Message);
                }

                if (artworkFiles != null && artworkFiles.Count() > 0)
                {

                    foreach (string artworkFile in artworkFiles)
                    {
                        try
                        {
                            // Do not delete file from this instance
                            if (!artworkFile.StartsWith("file-" + this.instanceGuid))
                            {
                                System.IO.File.Delete(artworkFile);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogClient.Instance.Logger.Error("There was a problem while deleting cached file artwork {0}. Exception: {1}", artworkFile, ex.Message);
                        }
                    }
                }
            });
        }
        #endregion
    }
}
