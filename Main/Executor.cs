﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using DiScribe.Email;
using DiScribe.Transcriber;
using DiScribe.DatabaseManager;
using DiScribe.Dialer;
using DiScribe.Meeting;
using DiScribe.Scheduler;


namespace DiScribe.Main
{
    static class Executor
    {

        public static void Execute()
        {
            // Set Authentication configurations
            var appConfig = Configurations.LoadAppSettings();

            EmailListener.Initialize(
                appConfig["appId"], //
                appConfig["tenantId"],
                appConfig["clientSecret"],
                appConfig["mailUser"] // bots email account
                ).Wait();

            MeetingController.BOT_EMAIL = appConfig["mailUser"];

            /*Main application loop */
            while (true)
            {
                ListenForInvitations(appConfig).Wait();
            }
        }

        /// <summary>
        /// Listens for a new WebEx invitation to the DiScribe bot email account.
        /// Logic:
        ///     -> Every 10 seconds, read inbox
        ///     -> If there is a message, get access code from it
        ///     -> Call webex API to get meeting time from access code
        ///     -> Schedule the rest of the dial to transcribe workflow
        ///
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        private static async Task ListenForInvitations(IConfigurationRoot appConfig, int seconds = 10)
        {
            Console.WriteLine(">\tBot is Listening for meeting invites...");

            try
            {
                var message = EmailListener.GetEmailAsync().Result; //Get latest email from bot's inbox.

                if (!EmailListener.IsValidWebexInvitation(message))
                {
                    EmailListener.DeleteEmailAsync(message).Wait(); // deletes the email that was read
                    throw new Exception(">\tNot a valid WebEx Invitation. Deleting Email...");
                }

                var meeting_info = EmailListener.GetMeetingInfo(message); //Get access code from bot's invite email

                Console.WriteLine(">\tNew Meeting Found at: " +
                    meeting_info.StartTime.ToLocalTime());

                MeetingController.SendEmailsToAnyUnregisteredUsers(
                    MeetingController.GetAttendeeEmails(meeting_info.AccessCode));

                await SchedulerController.Schedule(Run,
                    meeting_info.AccessCode, appConfig, meeting_info.StartTime);    //Schedule dialer-transcriber workflow

                EmailListener.DeleteEmailAsync(message).Wait(); // deletes the email that was read
            }
            catch (AggregateException exs)
            {
                foreach (var ex in exs.InnerExceptions)
                {
                    Console.Error.WriteLine(ex.Message);
                }
            }

            await Task.Delay(seconds * 1000);            
        }

        /// <summary>
        /// Runs when DiScribe bot dials in to Webex meeting. Performs transcription and speaker
        /// recognition, and emails meeting transcript to all participants.
        /// </summary>
        /// <param name="accessCode"></param>
        /// <param name="appConfig"></param>
        /// <returns></returns>
        static int Run(string accessCode, IConfigurationRoot appConfig)
        {
            try
            {
                // dialing & recording
                var rid = new DialerController(appConfig).CallMeetingAsync(accessCode).Result;
                var recording = new RecordingController(appConfig).DownloadRecordingAsync(rid).Result;

                // retrieving all attendees' emails as a List
                var invitedUsers = MeetingController.GetAttendeeEmails(accessCode);

                // Make controller for accessing registered user profiles in Azure Speaker Recognition endpoint
                var regController = RegistrationController.BuildController(
                    EmailHelper.FromEmailAddressListToStringList(invitedUsers));

                // initializing the transcribe controller
                var transcribeController = new TranscribeController(recording, regController.UserProfiles);

                // performs transcription and speaker recognition
                if (transcribeController.Perform())
                {
                    EmailSender.SendMinutes(invitedUsers, transcribeController.WriteTranscriptionFile(rid));
                    Console.WriteLine(">\tTask Complete!");
                    return 0;
                }
                else
                {
                    EmailSender.SendEmail(invitedUsers, "Failed To Generate Meeting Transcription", "");
                    Console.WriteLine(">\tFailed to generate!");
                    return -1;
                }
            }
            catch (AggregateException exs)
            {
                foreach (var ex in exs.InnerExceptions)
                {
                    Console.Error.WriteLine(ex.Message);
                }
                return -1;
            }
        }
    }
}
