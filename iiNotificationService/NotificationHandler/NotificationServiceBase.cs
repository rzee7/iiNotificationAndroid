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
    /// NotificationServiceBase Handling all registration service.
    /// </summary>
    public abstract class NotificationServiceBase : IntentService
    {

        #region Private Declarations

        const string Tag = "GCMBaseIntentService";

        const string WakelockKey = "GCM_LIB";
        static PowerManager.WakeLock sWakeLock;

        static object Lock = new object();
        static int serviceId = 1;

        static string[] SenderIds = new string[] { };

        //int sCounter = 1;
        Random sRandom = new Random();

        const int MaxBackoffMs = 3600000; //1 hour

        string Token = "";
        const string ExtraToken = "token";

        #endregion

        #region Constructor

        protected NotificationServiceBase() : base() { }

        public NotificationServiceBase(params string[] senderIds)
            : base("GCMIntentService-" + (serviceId++).ToString())
        {
            SenderIds = senderIds;
        }

        #endregion

        #region Ovverridable Methods

        protected abstract void OnMessage(Context context, Intent intent);

        protected virtual void OnDeletedMessages(Context context, int total)
        {
        }

        protected virtual bool OnRecoverableError(Context context, string errorId)
        {
            return true;
        }

        protected abstract void OnError(Context context, string errorId);

        protected abstract void OnRegistered(Context context, string registrationId);

        protected abstract void OnUnRegistered(Context context, string registrationId);

        #endregion

        #region Handle Intent Method

        protected override void OnHandleIntent(Intent intent)
        {
            try
            {
                var context = this.ApplicationContext;
                var action = intent.Action;

                if (action.Equals(iiNotificationConstants.iiGcmRegistrationCallback))
                {
                    handleRegistration(context, intent);
                }
                else if (action.Equals(iiNotificationConstants.iiGcmMessage))
                {
                    // checks for special messages
                    var messageType = intent.GetStringExtra(iiNotificationConstants.iiExtraSpecialMessage);
                    if (messageType != null)
                    {
                        if (messageType.Equals(iiNotificationConstants.iiValueDeletedMessages))
                        {
                            var sTotal = intent.GetStringExtra(iiNotificationConstants.iiExtraTotalDeleted);
                            if (!string.IsNullOrEmpty(sTotal))
                            {
                                int nTotal = 0;
                                if (int.TryParse(sTotal, out nTotal))
                                {
                                    Log.Verbose(Tag, "Received deleted messages notification: " + nTotal);
                                    OnDeletedMessages(context, nTotal);
                                }
                                else
                                    Log.Error(Tag, "GCM returned invalid number of deleted messages: " + sTotal);
                            }
                        }
                        else
                        {
                            // application is not using the latest GCM library
                            Log.Error(Tag, "Received unknown special message: " + messageType);
                        }
                    }
                    else
                    {
                        OnMessage(context, intent);
                    }
                }
                else if (action.Equals(iiNotificationConstants.iiGcmLibraryRetry))
                {
                    var token = intent.GetStringExtra(ExtraToken);

                    if (!string.IsNullOrEmpty(token) && !Token.Equals(token))
                    {
                        // make sure intent was generated by this class, not by a
                        // malicious app.
                        Log.Error(Tag, "Received invalid token: " + token);
                        return;
                    }

                    // retry last call
                    if (iiNotificationHandler.IsRegistered(context))
                        iiNotificationHandler.internalUnRegister(context);
                    else
                        iiNotificationHandler.internalRegister(context, SenderIds);
                }
            }
            finally
            {
                // Release the power lock, so phone can get back to sleep.
                // The lock is reference-counted by default, so multiple
                // messages are ok.

                // If OnMessage() needs to spawn a thread or do something else,
                // it should use its own lock.
                lock (Lock)
                {
                    //Sanity check for null as this is a public method
                    if (sWakeLock != null)
                    {
                        Log.Verbose(Tag, "Releasing Wakelock");
                        sWakeLock.Release();
                    }
                    else
                    {
                        //Should never happen during normal workflow
                        Log.Error(Tag, "Wakelock reference is null");
                    }
                }
            }
        }

        #endregion

        #region Run Intent Service

        internal static void RunIntentInService(Context context, Intent intent, Type classType)
        {
            lock (Lock)
            {
                if (sWakeLock == null)
                {
                    // This is called from BroadcastReceiver, there is no init.
                    var pm = PowerManager.FromContext(context);
                    sWakeLock = pm.NewWakeLock(WakeLockFlags.Partial, WakelockKey);
                }
            }

            Log.Verbose(Tag, "Acquiring wakelock");
            sWakeLock.Acquire();
            //intent.SetClassName(context, className);
            intent.SetClass(context, classType);

            context.StartService(intent);
        }

        #endregion

        #region Handle Registration

        private void handleRegistration(Context context, Intent intent)
        {
            var registrationId = intent.GetStringExtra(iiNotificationConstants.iiExtraRegistrationID);
            var error = intent.GetStringExtra(iiNotificationConstants.iiExtraError);
            var unregistered = intent.GetStringExtra(iiNotificationConstants.iiExtraUnregistered);

            Log.Debug(Tag, string.Format("handleRegistration: registrationId = {0}, error =  {1}, unregistered = {2}", registrationId, error, unregistered));

            // registration succeeded
            if (registrationId != null)
            {
                iiNotificationHandler.ResetBackoff(context);
                iiNotificationHandler.SetRegistrationID(context, registrationId);
                OnRegistered(context, registrationId);
                return;
            }

            // unregistration succeeded
            if (unregistered != null)
            {
                // Remember we are unregistered
                iiNotificationHandler.ResetBackoff(context);
                var oldRegistrationId = iiNotificationHandler.ClearRegistrationID(context);
                OnUnRegistered(context, oldRegistrationId);
                return;
            }

            // last operation (registration or unregistration) returned an error;
            Log.Debug(Tag, "Registration error: " + error);
            // Registration failed
            if (iiNotificationConstants.iiErrorSrviceNotAvailable.Equals(error))
            {
                var retry = OnRecoverableError(context, error);

                if (retry)
                {
                    int backoffTimeMs = iiNotificationHandler.GetBackoff(context);
                    int nextAttempt = backoffTimeMs / 2 + sRandom.Next(backoffTimeMs);

                    Log.Debug(Tag, "Scheduling registration retry, backoff = " + nextAttempt + " (" + backoffTimeMs + ")");

                    var retryIntent = new Intent(iiNotificationConstants.iiGcmLibraryRetry);
                    retryIntent.PutExtra(ExtraToken, Token);

                    var retryPendingIntent = PendingIntent.GetBroadcast(context, 0, retryIntent, PendingIntentFlags.OneShot);

                    var am = AlarmManager.FromContext(context);
                    am.Set(AlarmType.ElapsedRealtime, SystemClock.ElapsedRealtime() + nextAttempt, retryPendingIntent);

                    // Next retry should wait longer.
                    if (backoffTimeMs < MaxBackoffMs)
                    {
                        iiNotificationHandler.SetBackoff(context, backoffTimeMs * 2);
                    }
                }
                else
                {
                    Log.Debug(Tag, "Not retrying failed operation");
                }
            }
            else
            {
                // Unrecoverable error, notify app
                OnError(context, error);
            }
        }
        
        #endregion
    }
}