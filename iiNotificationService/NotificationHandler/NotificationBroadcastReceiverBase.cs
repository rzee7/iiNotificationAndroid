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
    /// NotificationBroadcastReceiverBase Received Response.
    /// </summary>
    /// <typeparam name="TIntentService"></typeparam>
    public class NotificationBroadcastReceiverBase<TIntentService> : BroadcastReceiver where TIntentService : NotificationServiceBase
    {
        #region Private Declaration

        const string Tag = "NotificationBroadcastReceiver";

        #endregion

        #region Service On Recevied Method

        public override void OnReceive(Context context, Intent intent)
        {
            Log.Verbose(Tag, "OnReceive: " + intent.Action);
            var className = string.Format("{0}{1}", context.PackageName, iiNotificationConstants.iiDefaultIntentServiceClassName);

            Log.Verbose(Tag, "GCM IntentService Class: " + className);

            NotificationServiceBase.RunIntentInService(context, intent, typeof(TIntentService));
            SetResult(Result.Ok, null, null);
        }

        #endregion
    }
}