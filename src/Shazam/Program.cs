using OnlineRadio.Core;
using SoundFingerprinting;
using SoundFingerprinting.Audio;
using SoundFingerprinting.Audio.Bass;
using SoundFingerprinting.Builder;
using SoundFingerprinting.DAO.Data;
using SoundFingerprinting.InMemory;
using System;
using System.Windows.Threading;
using Un4seen.Bass;

namespace Shazam
{
    class Program
    {
        private static readonly IModelService modelService = new InMemoryModelService(); // store fingerprints in RAM
        private static readonly IAudioService audioService = new BassAudioService(); 
        private static readonly IStreamingUrlReader streamingUrlReader = new BassStreamingUrlReader();
        private static Radio radio;
        private static AudioSamples audioSamples;
        //private SoundPlayer

        static void Main(string[] args)
        {
            //WMPLib.WindowsMediaPlayer media = new WMPLib.WindowsMediaPlayer();
            ////media.openPlayer("http://cdn.maksnet.tv/em/asmedia/?streamname=index&radio/#play");
            //media.openPlayer("http://bbcmedia.ic.llnwd.net/stream/bbcmedia_radio1_mf_p");
            ////media.URL = "http://bbcmedia.ic.llnwd.net/stream/bbcmedia_radio1_mf_p";
            ////media.play

            
            try
            {
                string jalaBratSong = @"C:\Users\goran.nikolic\Music\Jala Brat x Buba Corelli - Klinka (Official Lyric Video).wav";
                string searchSong = @"C:\Users\goran.nikolic\Music\Jala Brat x Buba Corelli - Klinka (Official Lyric Video).mp3";
                var jalaBratTrack = new TrackData("GBBKS1200164", "Jala Brat", "Klinka", "Klinka", 2012, 290);
                StoreAudioFileFingerprintsInStorageForLaterRetrieval(jalaBratSong, jalaBratTrack);
                var result = GetBestMatchForSong(searchSong);
                Console.WriteLine($"Song: {result.Title}, Artist: {result.Artist}");

                string amarGileSong = @"C:\Users\goran.nikolic\Music\Amar Gile - Imam Samo Jednu Zelju.mp3";
                var amarGileTrack = new TrackData("GBBKS1200165", "Amar Gile ", "Imam Samo Jednu Zelju", "Imam Samo Jednu Zelju", 2012, 290);
                StoreAudioFileFingerprintsInStorageForLaterRetrieval(amarGileSong, amarGileTrack);
                result = GetBestMatchForSong(amarGileSong);
                Console.WriteLine($"Song: {result.Title}, Artist: {result.Artist}");

                StartRadio();

                Console.ReadLine();
            }
            finally
            {
                radio.Dispose();
            }
            
        }

        private static void StartRadio()
        {
            radio = new Radio("http://naxi64.streaming.rs:9160/;stream.nsv");
            radio = new Radio("http://live2.okradio.net:8052/;?.mp3");
            radio = new Radio("http://cdn.maksnet.tv/em/asmedia/?streamname=index&radio");
            radio = new Radio("http://stream.b92.net:7999/radio-b92.mp3");
            radio.OnCurrentSongChanged += (s, eventArgs) =>
            {
                string message = eventArgs.NewSong.Artist + " - " + eventArgs.NewSong.Title;
                Console.WriteLine(message);
            };

            radio.OnStreamUpdate += Radio_OnStreamUpdate;
            
            radio.Start();
        }

        private static void Radio_OnStreamUpdate(object sender, StreamUpdateEventArgs e)
        {
            //if (audioSamples != null)
            //{
            //    var data1 = e.Data;
            //    var floats1 = new float[data1.Length / 4];
            //    Buffer.BlockCopy(data1, 0, floats1, 0, data1.Length);

            //    audioSamples = new AudioSamples(floats1, string.Empty, 44100);
            //}
            //else
            //{
            //    var data = e.Data;
            //    var floats = new float[data.Length / 4];
            //    Buffer.BlockCopy(data, 0, floats, 0, data.Length);



            //    audioSamples = new AudioSamples()
            //}


            var data1 = e.Data;
            var floats1 = new float[data1.Length / 4];
            Buffer.BlockCopy(data1, 0, floats1, 0, data1.Length);

            audioSamples = new AudioSamples(floats1, string.Empty, 44100);

            var hashedFingerprints = FingerprintCommandBuilder.Instance
                                        .BuildFingerprintCommand()
                                        .From(audioSamples)
                                        //.From(pathToAudioFile)
                                        .UsingServices(audioService)
                                        .Hash()
                                        .Result;

            // store hashes in the database for later retrieval
            //modelService.InsertHashDataForTrack(hashedFingerprints, trackReference);
        }

        private static void StoreAudioFileFingerprintsInStorageForLaterRetrieval(string pathToAudioFile, TrackData track)
        {
            // store track metadata in the datasource
            var trackReference = modelService.InsertTrack(track);

            //// //var youtubeUrl = "https://www.youtube.com/watch?v=LXGQ4rWfN8A";
            // var youtubeUrl = "http://stream.b92.net:7999/radio-b92.mp3";
            ////// var stream = Bass.BASS_StreamCreateURL(youtubeUrl, 0, BASSFlag.BASS_STREAM_DECODE,);
            // float[] res = streamingUrlReader.ReadMonoSamples(youtubeUrl, 44100, 5);
            //AudioSamples audioSamples = new AudioSamples(res, string.Empty, 1);

            //create hashed fingerprints
           var hashedFingerprints = FingerprintCommandBuilder.Instance
                                       .BuildFingerprintCommand()
                                       //.From(audioSamples)
                                       .From(pathToAudioFile)
                                       .UsingServices(audioService)
                                       .Hash()
                                       .Result;

            // store hashes in the database for later retrieval
            modelService.InsertHashDataForTrack(hashedFingerprints, trackReference);
        }

        private static TrackData GetBestMatchForSong(string queryAudioFile)
        {
            int secondsToAnalyze = 10; // number of seconds to analyze from query file
            int startAtSecond = 0; // start at the begining

            // query the underlying database for similar audio sub-fingerprints
            var queryResult = QueryCommandBuilder.Instance.BuildQueryCommand()
                                                 .From(queryAudioFile, secondsToAnalyze, startAtSecond)
                                                 .UsingServices(modelService, audioService)
                                                 .Query()
                                                 .Result;

            return queryResult.BestMatch.Track; // successful match has been found
        }
    }
}
