﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using transcriber.Data;
using Microsoft.CognitiveServices.Speech.Audio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace transcriber.TranscribeAgent
{
    /// <summary>
    /// Provides meeting audio file splitting. An audio file is split into <see cref="TranscribeAgent.AudioSegment"></see>
    /// instances.
    /// Splitting is performed by determining when speakers change. Only speakers with a known
    /// voice profile <see cref="Data.Voiceprint"></see> will be identified.    
    /// <para>Uses Speaker Recognition API in <see cref="Microsoft.CognitiveServices.Speech"/> for speaker recognition.</para>
    /// <para>Note that audio file must be a WAV file with the following characteristics: PCM/WAV mono with 16kHz sampling rate and 16 bits per sample. </para>
    /// </summary>
    class AudioFileSplitter
    {

        /// <summary>
        /// Create an AudioFileSplitter instance which uses a List of voiceprints for speaker recognition to
        /// divide an audio file. Allows the divided audio segment data to be accessed via streams.
        /// </summary>
        /// <param name="voiceprints"><see cref="List{Voiceprint}"/>List of <see cref="Voiceprint"/> instances used for speaker recognition</param>
        /// <param name="audioFile"><see cref="FileInfo"/> instance with absolute path to audio file. File must be a WAV file
        /// with mono audio, 16kHz sampling rate, and 16 bits per sample.</param>
        public AudioFileSplitter(List<Voiceprint> voiceprints, FileInfo audioFile)
        {
            Voiceprints = voiceprints;
            AudioFile = audioFile;
            ProcessWavFile(audioFile);
        }


        public const int SAMPLE_RATE = 16000;
        public const int BITS_PER_SAMPLE = 16;
        public const int CHANNELS = 1;

        /// <summary>
        /// List of Voiceprint instances used to identify users in the audio file.
        /// </summary>
        public List<Voiceprint> Voiceprints { get; set; }

        /// <summary>
        /// Info for access to audio file which must be in correct format for Azure Cognitive Services Speech API.
        /// </summary>
        public FileInfo AudioFile { get; set; }

        public byte[] AudioData {get; set;}

        /// <summary>
        /// FOR DEMO: Will only return a sorted list with a single <see cref="AudioSegment"/>.
        ///  
        /// <para>Creates a SortedList of <see cref="AudioSegment"/> instances which are sorted according
        /// to their offset from the beginning of the audio file.
        /// The audio is segmented by identifying the speaker. Each time the speaker changes,
        /// the <see cref="AudioSegment"/> is added to the SortedList. </para>
        /// </summary>
        /// <returns>SortedList of <see cref="AudioSegment"/> instances</returns>
        public SortedList<AudioSegment, AudioSegment> SplitAudio()
        {
            var tempList = new SortedList<AudioSegment, AudioSegment>();

            /*TODO: Divide audio file using recognition here.
              Get offset from beginning of file (start of meeting).
              Also determine who the speaker is and get a matching User object.
            */
            
            /*FOR TESTING */
            MemoryStream stream = new MemoryStream(AudioData);                              //Set up the internal stream with AudioData as backing buffer.
            int offset = 0;
            User participant = new User("Some person", "someone@example.com");

            AudioSegment segment = CreateAudioSegment(stream, offset, participant);
            tempList.Add(segment, segment);

            return tempList;
        }



        /// <summary>
        /// Create a set of AudioSegments corresponding to each time the speaker
        /// in the audio changes.
        /// </summary>
        /// <returns><see cref="SortedList"/>SortedList of AudioSegements sorted by offset.</returns>
        private SortedList<AudioSegment, AudioSegment> IdentifySpeakers()
        {
            return new SortedList<AudioSegment, AudioSegment>();
        }


        
        /// <summary>
        /// Creates buffer with file data. File header is removed.
        /// </summary>
        /// <param name="inFile"></param>
        /// <returns>Byte[] containing raw audio data, without header.</returns>
        private void ReadWavFileRemoveHeader(FileInfo inFile)
        {
            byte[] outData;
            using (var inputReader = new WaveFileReader(inFile.FullName))
            {
                outData = new byte[inputReader.Length];                      //Buffer size is size of data section in wav file.
                inputReader.Read(outData, 0, (int)(inputReader.Length));     //Read entire data section of file into buffer. 
            }

            AudioData = outData;
        }



        /// <summary>
        /// Converts data in Wav file into the specified format and reads data section of file (no header) into AudioData buffer.
        /// </summary>
        /// <param name="originalFile" ></param>
        /// <param name="sampleRate"></param>
        /// <param name="bitRate"></param>
        /// <param name="channels"></param>
        private void ProcessWavFile(FileInfo originalFile, int sampleRate = SAMPLE_RATE, int channels = CHANNELS, int bitPerSample = BITS_PER_SAMPLE)
        {
            /*Convert the file using NAudio library */
            using (var inputReader = new WaveFileReader(originalFile.FullName))
            {
                
                var outFormat = new WaveFormat(sampleRate, channels);
                var resampler = new MediaFoundationResampler(inputReader, outFormat);
                
                resampler.ResamplerQuality = 60;                                           //Use highest quality. Range is 1-60.

                AudioData = new byte[inputReader.Length];
                resampler.Read(AudioData, 0, (int)(inputReader.Length));                  //Read transformed WAV data into buffer WavData (header is removed).
                
            }
                   
        }

        private static AudioSegment CreateAudioSegment(Stream stream, int offset, User participant)
        {
            AudioStreamFormat streamFormat = AudioStreamFormat.GetWaveFormatPCM(SAMPLE_RATE, BITS_PER_SAMPLE, CHANNELS);   //Set up audio stream.
            PullAudioInputStream audioStream = AudioInputStream.CreatePullStream(new BinaryAudioStreamReader(stream), streamFormat);

            return new AudioSegment(audioStream, offset, participant);
    }
    }
}


    