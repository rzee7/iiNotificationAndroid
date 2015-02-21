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
    /// iiNotificationHandler will help you to play with service.
    /// </summary>
    public class iiNotificationHandler
    {
        #region Private Constant Declarartion for internal use

        const string Tag = "GCMRegistrar";
        const string BackOffMs = "backoff_ms";
        const string GsfPackage = "com.google.android.gsf";
        const string Preferences = "com.google.android.gcm";
        const int DefaultBackoffMs = 3000;
        const string PropertyRegID = "regId";
        const string PropertyAppVersion = "appVersion";
        const string PropertyOnServer = "onServer";

        #endregion

        #region Device Validation Method

        /// <summary>
        /// This method validate device info.
        /// </summary>
        /// <param name="context">Application current context.</param>
        public static void ValidateDevice(Context context)
        {
            var version = (int)BuildVersionCodes.Froyo;
            if (version < 8)
                throw new InvalidOperationException("Device must be at least API Level 8 (instead of " + version + ")");
            //TODO : Have to throw for Forms Project

            var packageManager = context.PackageManager;

            try
            {
                packageManager.GetPackageInfo(GsfPackage, 0);
            }
            catch
            {
                //TODO : Have to throw for Forms Project
                throw new InvalidOperationException("Device does not have package " + GsfPackage);
            }
        }

        #endregion

        #region Validate Manifest Method

        /// <summary>
        /// This method validate application manifest.
        /// </summary>
        /// <param name="context">Application current context.</param>
        public static void ValidateManifest(Context context)
        {
            var packageManager = context.PackageManager;
            var packageName = context.PackageName;
            var permissionName = string.Format("{0}.permission.C2D_MESSAGE", packageName);

            try
            {
                packageManager.GetPermissionInfo(permissionName, Android.Content.PM.PackageInfoFlags.Permissions);
            }
            catch
            {
                throw new AccessViolationException(string.Format("Application does not define permission: {0} ", permissionName));
            }

            Android.Content.PM.PackageInfo receiversInfo;

            try
            {
                receiversInfo = packageManager.GetPackageInfo(packageName, Android.Content.PM.PackageInfoFlags.Receivers);
            }
            catch
            {
                throw new InvalidOperationException("Could not get receivers for package " + packageName);
            }

            var receivers = receiversInfo.Receivers;

            if (receivers == null || receivers.Count <= 0)
                throw new InvalidOperationException("No Receiver for package " + packageName);

            if (Log.IsLoggable(Tag, LogPriority.Verbose))
                Log.Verbose(Tag, "number of receivers for " + packageName + ": " + receivers.Count);

            var allowedReceivers = new HashSet<string>();

            foreach (var receiver in receivers)
            {
                if (iiNotificationConstants.iiPermissionGcmIntents.Equals(receiver.Permission))
                    allowedReceivers.Add(receiver.Name);
            }

            if (allowedReceivers.Count <= 0)
                throw new InvalidOperationException("No receiver allowed to receive " + iiNotificationConstants.iiPermissionGcmIntents);

            ValidateReceiver(context, allowedReceivers, iiNotificationConstants.iiGcmRegistrationCallback);
            ValidateReceiver(context, allowedReceivers, iiNotificationConstants.iiGcmMessage);
        }

        #endregion

        #region Validate Receiver Method

        /// <summary>
        /// This method validate app receiver.
        /// </summary>
        /// <param name="context">Application current context.</param>
        /// <param name="allowedReceivers">Set of allowed receivers.</param>
        /// <param name="action">Intent Action.</param>
        private static void ValidateReceiver(Context context, HashSet<string> allowedReceivers, string action)
        {
            var pm = context.PackageManager;
            var packageName = context.PackageName;

            var intent = new Intent(action);
            intent.SetPackage(packageName);

            var receivers = pm.QueryBroadcastReceivers(intent, Android.Content.PM.PackageInfoFlags.IntentFilters);

            if (receivers == null || receivers.Count <= 0)
                throw new InvalidOperationException(string.Format("No receivers for action: {0}", action));

            if (Log.IsLoggable(Tag, LogPriority.Verbose))
                Log.Verbose(Tag, string.Format("Found {0} receivers for action: {1}", receivers.Count, action));

            foreach (var receiver in receivers)
            {
                var name = receiver.ActivityInfo.Name;
                if (!allowedReceivers.Contains(name))
                    throw new InvalidOperationException(string.Format("Receiver {0} is not set with permission: {1}", name, iiNotificationConstants.iiPermissionGcmIntents));
            }
        }

        #endregion

        #region Register Device

        /// <summary>
        /// This method register device for GCM service.
        /// </summary>
        /// <param name="context">Application current context.</param>
        /// <param name="senderIds">Application sender IDs.</param>
        public static void RegisterDevice(Context context, params string[] senderIds)
        {
            SetRetryBroadcastReceiver(context);
            ResetBackoff(context);

            internalRegister(context, senderIds);
        }

        internal static void internalRegister(Context context, params string[] senderIds)
        {
            if (senderIds == null || senderIds.Length <= 0)
                throw new ArgumentException("No senderIds");

            var senders = string.Join(",", senderIds);

            Log.Verbose(Tag, "Registering app " + context.PackageName + " of senders " + senders);

            var intent = new Intent(iiNotificationConstants.iiGcmRegistration);
            intent.SetPackage(GsfPackage);
            intent.PutExtra(iiNotificationConstants.iiExtraAppPendingIntent,
                PendingIntent.GetBroadcast(context, 0, new Intent(), 0));
            intent.PutExtra(iiNotificationConstants.iiExtraSender, senders);

            context.StartService(intent);
        }

        #endregion

        #region Un Register Device

        /// <summary>
        /// This method UnRegister device.
        /// </summary>
        /// <param name="context">Application current context.</param>
        public static void UnRegister(Context context)
        {
            SetRetryBroadcastReceiver(context);
            ResetBackoff(context);
            internalUnRegister(context);
        }

        internal static void internalUnRegister(Context context)
        {
            Log.Verbose(Tag, "Unregistering app " + context.PackageName);

            var intent = new Intent(iiNotificationConstants.iiGcmUnRegistration);
            intent.SetPackage(GsfPackage);
            intent.PutExtra(iiNotificationConstants.iiExtraAppPendingIntent,
                PendingIntent.GetBroadcast(context, 0, new Intent(), 0));

            context.StartService(intent);
        }

        #endregion

        #region Retry Method

        static void SetRetryBroadcastReceiver(Context context)
        {
            return;

            /*if (sRetryReceiver == null)
            {
                sRetryReceiver = new GCMBroadcastReceiver();
                var category = context.PackageName;

                var filter = new IntentFilter(GCMConstants.INTENT_FROM_GCM_LIBRARY_RETRY);
                filter.AddCategory(category);

                var permission = category + ".permission.C2D_MESSAGE";

                Log.Verbose(TAG, "Registering receiver");

                context.RegisterReceiver(sRetryReceiver, filter, permission, null);
            }*/
        }

        #endregion

        #region Get Registration ID

        /// <summary>
        /// This method return Registration ID.
        /// </summary>
        /// <param name="context">Application current context.</param>
        /// <returns></returns>
        public static string GetRegistrationID(Context context)
        {
            var prefs = GetGCMPreferences(context);

            var registrationId = prefs.GetString(PropertyRegID, "");

            int oldVersion = prefs.GetInt(PropertyAppVersion, int.MinValue);
            int newVersion = GetAppVersion(context);

            if (oldVersion != int.MinValue && oldVersion != newVersion)
            {
                Log.Verbose(Tag, "App version changed from " + oldVersion + " to " + newVersion + "; resetting registration id");

                ClearRegistrationID(context);
                registrationId = string.Empty;
            }

            return registrationId;
        }

        internal static string ClearRegistrationID(Context context)
        {
            return SetRegistrationID(context, string.Empty);
        }

        internal static string SetRegistrationID(Context context, string registrationId)
        {
            var prefs = GetGCMPreferences(context);

            var oldRegistrationId = prefs.GetString(PropertyRegID, "");
            int appVersion = GetAppVersion(context);
            Log.Verbose(Tag, "Saving registrationId on app version " + appVersion);
            var editor = prefs.Edit();
            editor.PutString(PropertyRegID, registrationId);
            editor.PutInt(PropertyAppVersion, appVersion);
            editor.Commit();
            return oldRegistrationId;
        }

        #endregion

        #region Validate Registration on Server

        public static bool IsRegisteredOnServer(Context context)
        {
            var prefs = GetGCMPreferences(context);
            bool isRegistered = prefs.GetBoolean(PropertyOnServer, false);
            Log.Verbose(Tag, string.Format("Is registered on server: {0}", isRegistered));
            return isRegistered;
        }

        #endregion

        #region Post Registration on Server

        /// <summary>
        /// This method register status on server.
        /// </summary>
        /// <param name="context">Application current context.</param>
        /// <param name="flag">Status.</param>
        public static void PostRegisteredOnServer(Context context, bool flag)
        {
            var prefs = GetGCMPreferences(context);
            Log.Verbose(Tag, "Setting registered on server status as: " + flag);
            var editor = prefs.Edit();
            editor.PutBoolean(PropertyOnServer, flag);
            editor.Commit();
        }

        #endregion

        #region Validate Device Is Registered

        /// <summary>
        /// Validate device is registered.
        /// </summary>
        /// <param name="context">Application current context.</param>
        /// <returns></returns>
        public static bool IsRegistered(Context context)
        {
            return !string.IsNullOrEmpty(GetRegistrationID(context));
        }

        #endregion

        #region Get Application Version

        static int GetAppVersion(Context context)
        {
            try
            {
                var packageInfo = context.PackageManager.GetPackageInfo(context.PackageName, 0);
                return packageInfo.VersionCode;
            }
            catch
            {
                throw new InvalidOperationException("Could not get package name");
            }
        }

        #endregion

        #region Reset Back off

        internal static void ResetBackoff(Context context)
        {
            Log.Debug(Tag, "resetting backoff for " + context.PackageName);
            SetBackoff(context, DefaultBackoffMs);
        }

        #endregion

        #region Get Back Off

        internal static int GetBackoff(Context context)
        {
            var prefs = GetGCMPreferences(context);
            return prefs.GetInt(BackOffMs, DefaultBackoffMs);
        }

        #endregion

        #region Set Back Off

        internal static void SetBackoff(Context context, int backoff)
        {
            var prefs = GetGCMPreferences(context);
            var editor = prefs.Edit();
            editor.PutInt(BackOffMs, backoff);
            editor.Commit();
        }

        #endregion

        #region Get GCM Preferences

        static ISharedPreferences GetGCMPreferences(Context context)
        {
            return context.GetSharedPreferences(Preferences, FileCreationMode.Private);
        }

        #endregion
    }
}