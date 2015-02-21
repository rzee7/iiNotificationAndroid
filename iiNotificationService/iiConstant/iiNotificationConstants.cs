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

namespace iiNotificationService
{
    /// <summary>
    /// iiNotificationContants provide all necessary values.
    /// </summary>
    public class iiNotificationConstants
    {
        #region Constant Values

        public const string iiGcmRegistration = "com.google.android.c2dm.intent.REGISTER";

        /**
        * Intent sent to GCM to unregister the application.
        */
        public const string iiGcmUnRegistration = "com.google.android.c2dm.intent.UNREGISTER";

        /**
        * Intent sent by GCM indicating with the result of a registration request.
        */
        public const string iiGcmRegistrationCallback = "com.google.android.c2dm.intent.REGISTRATION";

        /**
        * Intent used by the GCM library to indicate that the registration call
        * should be retried.
        */
        public const string iiGcmLibraryRetry = "com.google.android.gcm.intent.RETRY";

        /**
        * Intent sent by GCM containing a message.
        */
        public const string iiGcmMessage = "com.google.android.c2dm.intent.RECEIVE";

        /**
        * Extra used on {@link #INTENT_TO_GCM_REGISTRATION} to indicate the sender
        * account (a Google email) that owns the application.
        */
        public const string iiExtraSender = "sender";

        /**
        * Extra used on {@link #INTENT_TO_GCM_REGISTRATION} to get the application
        * id.
        */
        public const string iiExtraAppPendingIntent = "app";

        /**
        * Extra used on {@link #INTENT_FROM_GCM_REGISTRATION_CALLBACK} to indicate
        * that the application has been unregistered.
        */
        public const string iiExtraUnregistered = "unregistered";

        /**
        * Extra used on {@link #INTENT_FROM_GCM_REGISTRATION_CALLBACK} to indicate
        * an error when the registration fails. See constants starting with ERROR_
        * for possible values.
        */
        public const string iiExtraError = "error";

        /**
        * Extra used on {@link #INTENT_FROM_GCM_REGISTRATION_CALLBACK} to indicate
        * the registration id when the registration succeeds.
        */
        public const string iiExtraRegistrationID = "registration_id";

        /**
        * Type of message present in the {@link #INTENT_FROM_GCM_MESSAGE} intent.
        * This extra is only set for special messages sent from GCM, not for
        * messages originated from the application.
        */
        public const string iiExtraSpecialMessage = "message_type";

        /**
        * Special message indicating the server deleted the pending messages.
        */
        public const string iiValueDeletedMessages = "deleted_messages";

        /**
        * Number of messages deleted by the server because the device was idle.
        * Present only on messages of special type
        * {@link #VALUE_DELETED_MESSAGES}
        */
        public const string iiExtraTotalDeleted = "total_deleted";

        /**
        * Permission necessary to receive GCM intents.
        */
        public const string iiPermissionGcmIntents = "com.google.android.c2dm.permission.SEND";

        /**
        * @see GCMBroadcastReceiver
        */
        public const string iiDefaultIntentServiceClassName = ".GCMIntentService";

        /**
        * The device can't read the response, or there was a 500/503 from the
        * server that can be retried later. The application should use exponential
        * back off and retry.
        */
        public const string iiErrorSrviceNotAvailable = "SERVICE_NOT_AVAILABLE";

        /**
        * There is no Google account on the phone. The application should ask the
        * user to open the account manager and add a Google account.
        */
        public const string iiErrorAccountMissing = "ACCOUNT_MISSING";

        /**
        * Bad password. The application should ask the user to enter his/her
        * password, and let user retry manually later. Fix on the device side.
        */
        public const string iiErrorAuthenticationFailed = "AUTHENTICATION_FAILED";

        /**
        * The request sent by the phone does not contain the expected parameters.
        * This phone doesn't currently support GCM.
        */
        public const string iiErrorInvalidParameters = "INVALID_PARAMETERS";
        /**
        * The sender account is not recognized. Fix on the device side.
        */
        public const string iiErrorInvalidSender = "INVALID_SENDER";

        /**
        * Incorrect phone registration with Google. This phone doesn't currently
        * support GCM.
        */
        public const string iiErrorPhoneRegistrationError = "PHONE_REGISTRATION_ERROR";

        #endregion
    }
}