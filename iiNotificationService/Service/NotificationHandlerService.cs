using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Util;

namespace iiNotificationService
{

    /// <summary>
    /// iiNotificationBroadCastReceiver received all notifications. 
    /// </summary>
    public class iiNotificationBroadCastReceiver : NotificationBroadcastReceiverBase<NotificationHandlerService>
    {
        public static string[] SenderIDs = new string[] { "123456879" };
        public const string Tag = "iiNotificationBroadCastReceiver.cs";
    }

    /// <summary>
    /// NotificationHandlerService is actual running service for notification.
    /// </summary>
    [Service]
    public class NotificationHandlerService : NotificationServiceBase
    {

        public NotificationHandlerService() : base(iiNotificationBroadCastReceiver.SenderIDs) { }

        #region On Message Received Method

        protected override void OnMessage(Context context, Intent intent)
        {
            Log.Info(iiNotificationBroadCastReceiver.Tag, "GCM Message Received!");

            var msg = new StringBuilder();
            var sAlert = string.Empty;

            if (intent != null && intent.Extras != null)
            {
                foreach (var key in intent.Extras.KeySet())
                {
                    if (key == "message")
                        sAlert = intent.Extras.Get(key).ToString();

                    msg.AppendLine(key + "=" + intent.Extras.Get(key).ToString());
                }
            }

            //Store the message
            //  SqueezeMeIn.Common.Android.Storage.savePreference(context, "last_msg", msg.ToString());

            // display the message
            CreateNotification<Activity>(GetString(Resource.String.ApplicationName), sAlert);
        }

        #endregion

        #region OnError Received Method

        protected override void OnError(Context context, string errorId)
        {
            Log.Error(iiNotificationBroadCastReceiver.Tag, "GCM Error: " + errorId);
        }

        #endregion

        #region OnRegistered Method

        protected override void OnRegistered(Context context, string registrationId)
        {
            Log.Verbose(iiNotificationBroadCastReceiver.Tag, "GCM Registered: " + registrationId);

            #region Registered Info on Server

            //Send back to the server
            //var wc = new WebClient();
            //var result = wc.UploadString("http://your.server.com/api/register/", "POST",
            //    "{ 'registrationId' : '" + registrationId + "' }");

            #endregion

            #region Handling Registered Device Token Locally or Remotely

            //string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);
            //Java.IO.File userfile = new Java.IO.File(path, Constants.DeviceToken);
            //string ID = string.Empty;
            //if (userfile.Exists())
            //    ID = System.IO.File.ReadAllText(Path.Combine(path, Constants.DeviceToken));
            //if (string.IsNullOrEmpty(ID))
            //{
            //    FileWriter writer = new FileWriter(userfile);
            //    var DvcID = Guid.NewGuid();
            //    writer.Append(DvcID.ToString());
            //    writer.Flush();
            //    writer.Close();
            //    //
            //    var PushSubscriber = new MobileSubscribers();
            //    PushSubscriber.ID = DvcID;
            //    PushSubscriber.CreatedDate = DateTime.Now;
            //    PushSubscriber.IsActive = true;
            //    PushSubscriber.MobileDeviceID = (int)MobileType.Type.Android;
            //    PushSubscriber.Token = registrationId;
            //    var IsAdded = ServiceHandler.PostData(PushSubscriber, Section.MobileSubscriber);
            //    if (IsAdded)
            //        System.Console.WriteLine("Divice token Added on DB Server");
            //    else
            //        System.Console.WriteLine("Divice token Failed to add on DB Server");
            //}

            #endregion

            // createNotification(GetString(Resource.String.ApplicationName) + " Registered", "The device has been registered.");
        }

        #endregion

        #region OnUnRegistered Method

        protected override void OnUnRegistered(Context context, string registrationId)
        {
            Log.Verbose(iiNotificationBroadCastReceiver.Tag, "GCM Unregistered: " + registrationId);

            #region Remove Token From Server

            //Remove from the web service
            //	var wc = new WebClient();
            //	var result = wc.UploadString("http://your.server.com/api/unregister/", "POST",
            //		"{ 'registrationId' : '" + lastRegistrationId + "' }");

            //createNotification(GetString(Resource.String.app_name) + " Unregistered", "The device has been unregistered.");

            #endregion

        }

        #endregion

        #region OnRecoverable Method

        protected override bool OnRecoverableError(Context context, string errorId)
        {
            Log.Warn(iiNotificationBroadCastReceiver.Tag, "Recoverable Error: " + errorId);
            return base.OnRecoverableError(context, errorId);
        }

        #endregion

        #region Create Notification

        void CreateNotification<T>(string title, string desc, int recID = 0)
        {
            //Create notification
            var notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;

            //Create an intent to show ui
            var uiIntent = new Intent(this, typeof(T)); //TODO : This would be an activity where Notification will show.

            //Create the notification
            var notification = new Notification(recID, title);

            //Auto cancel will remove the notification once the user touches it
            notification.Flags = NotificationFlags.AutoCancel;

            notification.Defaults = NotificationDefaults.All;
            //Set the notification info
            //we use the pending intent, passing our ui intent over which will get called
            //when the notification is tapped.
            notification.SetLatestEventInfo(this, title, desc, PendingIntent.GetActivity(this, 0, uiIntent, 0));
            //TODO: Have to update with latest notification.

            //Show the notification
            notificationManager.Notify(1, notification);
        }

        #endregion
    }
}