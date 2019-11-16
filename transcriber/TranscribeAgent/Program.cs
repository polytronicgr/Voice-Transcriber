﻿using System;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Intent;
using System.IO;
using transcriber.TranscribeAgent;
using System.Collections.Generic;
using transcriber.Data;
using Microsoft.ProjectOxford.SpeakerRecognition;

namespace transcriber.TranscribeAgent
{
    public class Program
    {

        public static void Main(string[] args)
        {
            /* Creates an instance of a speech config with specified subscription key and service region
               for Azure Speech Recognition service */

            var speechConfig = SpeechConfig.FromSubscription("1558a08d9f6246ffaa1b31def4c2d85f", "centralus");

            /*Subscription key for Azure SpeakerRecognition service. */
            var speakerIDKey = "7fb70665af5b4770a94bb097e15b8ae0";

            FileInfo testRecording = new FileInfo(@"../../../Record/FakeMeeting.wav");
            FileInfo meetingMinutes = new FileInfo(@"../../../transcript/Minutes.txt");

            //Make two test audio samples for user 1 and user 2. Note that audio file header data must
            //be present for SpeakerRecognition API (not for SpeechRecognition API).
            List<MemoryStream> userAudioSampleStream = MakeTestUserVoiceSamples(testRecording);

            /*Set result with List<Voiceprint> containing both voiceprint objects */
            User user1 = new User("Tom", "Tom@example.com", 1);
            User user2 = new User("Maya", "Maya@example.com", 2);


            /////For testing, enroll 2 users to get speaker profiles directly from the audio.
            List<Voiceprint> voiceprints = new List<Voiceprint>()
            {
                new Voiceprint(userAudioSampleStream[0], user1),
                new Voiceprint(userAudioSampleStream[1], user2)
            };
            var enrollTask = EnrollUsers(speakerIDKey, voiceprints);

            enrollTask.Wait();  //Attempt enrolling the 2 users

            /*This TranscriptionInitData instance will be received from the Dialer in method call*/
            var initData = new TranscriptionInitData(testRecording, voiceprints, "");

            /*Setup the TranscribeController instance which manages the details of the transcription procedure */
            var controller = new TranscribeController(speechConfig, speakerIDKey, initData.MeetingRecording, initData.Voiceprints, meetingMinutes);

            /*Start the transcription of all audio segments to produce the meeting minutes file*/
            Console.WriteLine("Creating transcript...");
            Boolean success = controller.DoTranscription();

            Boolean emailSent = false;

            if (success)
            {
                Console.WriteLine("\nTranscription completed");

                string emailSubject = "Meeting minutes for " + DateTime.Now.ToLocalTime().ToString();
                var emailer = new TranscriptionEmailer("someone@ubc.ca", meetingMinutes);
                emailSent = emailer.SendEmail(initData.TargetEmail, emailSubject);
            }

            Console.WriteLine("Please press <Return> to continue.");
            Console.ReadLine();
        }

        /// <summary>
        /// Function which enrolls 2 users for testing purposes. In final system, enrollment will
        /// be done by users.
        /// </summary>
        /// <param name="speakerIDKey"></param>
        /// <param name="audioFile"></param>
        /// <returns></returns>
        public static async Task EnrollUsers(string speakerIDKey, List<Voiceprint> voiceprints, string enrollmentLocale = "en-us")
        {
            /*Create REST client for enrolling users */
            SpeakerIdentificationServiceClient enrollmentClient = new SpeakerIdentificationServiceClient(speakerIDKey);

            List<Task> taskList = new List<Task>();

            /*Make profiles for each user*/
            var profileTask1 = enrollmentClient.CreateProfileAsync(enrollmentLocale);
            var profileTask2 = enrollmentClient.CreateProfileAsync(enrollmentLocale);

            taskList.Add(profileTask1);
            taskList.Add(profileTask2);
            await Task.WhenAll(taskList.ToArray()); //Asychronously wait for profiles to be created.

            /*Get and set the GUID for each profile in the voiceprint objects*/
            voiceprints[0].UserGUID = profileTask1.Result.ProfileId;
            voiceprints[1].UserGUID = profileTask1.Result.ProfileId;

            taskList.Add(enrollmentClient.EnrollAsync(voiceprints[0].AudioStream, voiceprints[0].UserGUID, true));
            taskList.Add(enrollmentClient.EnrollAsync(voiceprints[1].AudioStream, voiceprints[1].UserGUID, true));
            await Task.WhenAll(taskList.ToArray()); //Asynchronously wait for all speakers to be enrolled
        }

        /// <summary>
        /// Method for test purposes to get voice samples from a WAV file
        /// </summary>
        /// <param name="audioFile"></param>
        /// <returns></returns>
        private static List<MemoryStream> MakeTestUserVoiceSamples(FileInfo audioFile)
        {
            AudioFileSplitter splitter = new AudioFileSplitter(audioFile);
            List<Voiceprint> result = new List<Voiceprint>();

            /*Offsets identifying times */
            ulong user1StartOffset = 30 * 1000;
            ulong user1EndOffset = 60 * 1000;
            ulong user2StartOffset = 74 * 1000;
            ulong user2EndOffset = 120 * 1000;


            /*Get byte[] for both users */
            byte[] user1Audio = splitter.SplitAudioGetBuf(user1StartOffset, user1EndOffset);
            byte[] user2Audio = splitter.SplitAudioGetBuf(user2StartOffset, user2EndOffset);

            /*Get memory streams for section of audio containing each user where audio stream begins
             with WAV file RIFF header*/
            return new List<MemoryStream>() { new MemoryStream(AudioFileSplitter.writeWavToBuf(user1Audio)),
                                                new MemoryStream(AudioFileSplitter.writeWavToBuf(user2Audio)) };
        }
    }
}
