﻿using System;
using transcriber.Data;
using Microsoft.CognitiveServices.Speech.Audio;

namespace transcriber.TranscribeAgent
{
    /// <summary>
    /// Represents a segment of an audio recording which has an offset in milliseconds from the beginning
    /// of the recording. Provides access to the audio data via an audio stream. Also includes a
    /// representation of the user who is speaking in the segment.
    /// </summary>
    public class AudioSegment : System.IComparable
    {
        public AudioSegment(PullAudioInputStream audioStream, long startOffset, long endOffset)
        {
            AudioStream = audioStream;
            StartOffset = startOffset;
            EndOffset = endOffset;
        }


        /// <summary>
        /// Stream providing access to the audio data for this instance.
        /// </summary>
        public PullAudioInputStream AudioStream { get; set; }

        /// <summary>
        /// Offset of audio segment from the beginning of the recording.
        /// </summary>
        public long StartOffset { get; set; }

        public long EndOffset { get; set; }

        /// <summary>
        /// Info about the speaker in this instance.
        /// </summary>
        public User SpeakerInfo { get; set; }


        public int CompareTo(object obj)
        {
            AudioSegment otherSegment = obj as AudioSegment;

            return StartOffset.CompareTo(otherSegment.StartOffset);
        }
    }
}
