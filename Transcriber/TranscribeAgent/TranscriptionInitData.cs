﻿using Transcriber.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Transcriber.TranscribeAgent
{
    /// <summary>
    /// Contains data required to begin the transcription process.
    /// This data gives access to the location of the meeting audio recording file, the set of participant voiceprints,
    /// and the target email to send the transcription.
    /// </summary>
    class TranscriptionInitData
    {
        public TranscriptionInitData(FileInfo meetingRecording, List<Voiceprint> voiceprints, string targetEmail)
        {
            MeetingRecording = meetingRecording;
            Voiceprints = voiceprints;
            TargetEmail = targetEmail;
        }

        public FileInfo MeetingRecording {get; set;}

        public List<Voiceprint> Voiceprints { get; set; }

        public string TargetEmail { get; set; }


    }
}
