﻿using System;
using System.Threading.Tasks;
using VideoLibrary;
using Windows.Storage;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Popups;

namespace SoundboardThreading
{
    class YoutubeDownloader
    {

        private StorageFolder _storageFolder;
        private YouTube _youTube;
        private double progressPercentage = 0;

        public YoutubeDownloader()
        {
            _storageFolder = ApplicationData.Current.LocalFolder;
            _youTube = YouTube.Default;
        }
        
        public string Download(string url)
        {
            YouTubeVideo video = _youTube.GetVideo(url);

            if (video.IsEncrypted)
            {
                var message = new MessageDialog($"{video.FullName} is encrypted!");
                message.ShowAsync();
                return null;
            }

            WriteFileAsync(video);
            return video.FullName + ".mp3";
        }

        public double getProgress()
        {
            return progressPercentage;
        }

        private async void WriteFileAsync(YouTubeVideo video)
        {
            StorageFile mp4StorageFile = await _storageFolder.CreateFileAsync(video.FullName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteBytesAsync(mp4StorageFile, video.GetBytes());
            
            StorageFile mp3StorageFile = await _storageFolder.CreateFileAsync(mp4StorageFile.Name + ".mp3", CreationCollisionOption.ReplaceExisting);
            var profile = MediaEncodingProfile.CreateMp3(AudioEncodingQuality.High);
            await ToAudioAsync(mp4StorageFile, mp3StorageFile, profile);
        }

        private async Task ToAudioAsync(StorageFile source, StorageFile destination, MediaEncodingProfile profile)
        {
            var transcoder = new MediaTranscoder();
            var prepareOp = await transcoder.PrepareFileTranscodeAsync(source, destination, profile);

            if (prepareOp.CanTranscode)
            {
                var transcodeOp = prepareOp.TranscodeAsync();

                transcodeOp.Progress +=
                    new AsyncActionProgressHandler<double>(TranscodeProgress);
                transcodeOp.Completed +=
                    new AsyncActionWithProgressCompletedHandler<double>(TranscodeComplete);
            }
            else
            {
                switch (prepareOp.FailureReason)
                {
                    case TranscodeFailureReason.CodecNotFound:
                        System.Diagnostics.Debug.WriteLine("Codec not found.");
                        break;
                    case TranscodeFailureReason.InvalidProfile:
                        System.Diagnostics.Debug.WriteLine("Invalid profile.");
                        break;
                    default:
                        System.Diagnostics.Debug.WriteLine("Unknown failure.");
                        break;
                }
            }
        }

        private void TranscodeProgress(IAsyncActionWithProgress<double> asyncInfo, double percent)
        {
            // Display or handle progress info.
            progressPercentage = percent;
            System.Diagnostics.Debug.WriteLine(percent);
        }

        private async void TranscodeComplete(IAsyncActionWithProgress<double> asyncInfo, AsyncStatus status)
        {
            asyncInfo.GetResults();
            if (asyncInfo.Status == AsyncStatus.Completed)
            {
                // Display or handle complete info.
                progressPercentage = 100;
                System.Diagnostics.Debug.WriteLine("Conversion success!");

            }
            else if (asyncInfo.Status == AsyncStatus.Canceled)
            {
                // Display or handle cancel info.
                System.Diagnostics.Debug.WriteLine("Conversion canceled!");
            }
            else
            {
                // Display or handle error info.
                System.Diagnostics.Debug.WriteLine("Conversion failed!");
            }
        }
    }
}
