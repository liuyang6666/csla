﻿using System;
using System.Configuration;
using Csla.Properties;

namespace Csla.Server
{

    /// <summary>
    /// This class provides a hoook for developers to add custom error handling in the DataPortal. 
    /// 
    /// Typical scenario is to handle non-serializable exception and exceptions from assemblies that 
    /// does not exist on the client side, such as 3rd party database drivers, MQ drivers etc.
    /// </summary>
    public class DataPortalExceptionHandler
    {
        #region Custom Exception Inspector Loader

        private static string _datePortalInspecorName;

        /// <summary>
        /// Gets or sets the fully qualified name of the ExceptionInspector class.
        /// </summary>
        public static string ExceptionInspector
        {
            get
            {
                if (_datePortalInspecorName == null)
                {
                    string setting = ConfigurationManager.AppSettings["CslaDataPortalExceptionInspector"];
                    if (!string.IsNullOrEmpty(setting))
                        _datePortalInspecorName = setting;
                }
                return _datePortalInspecorName;
            }
            set
            {
                _datePortalInspecorName = value;
            }
        }


        /// <summary>
        /// Gets a new exception inspector instance.
        /// </summary>
        /// <returns></returns>
        private static IDataPortalExceptionInspector GetExceptionInspector()
        {
            if (string.IsNullOrEmpty(ExceptionInspector)) return null;

            return (IDataPortalExceptionInspector)Activator.CreateInstance(Type.GetType(ExceptionInspector, true, true));

        }
        #endregion

        #region DataPortal Exception

        /// <summary>
        /// Transforms the exception in a Fetch, Create or Execute method.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="criteria">The criteria.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="ex">The exception.</param>
        /// <returns></returns>
        public Exception ToDataPortalException(Type objectType, object criteria, string methodName, Exception ex)
        {
            Exception handledException;
            if (CallExceptionInspector(objectType, null, criteria, methodName, ex, out handledException))
            {
                ex = handledException;
            }

            return new DataPortalException(
              methodName + " " + Resources.FailedOnServer,
              ex, new DataPortalResult());
        }

        /// <summary>
        /// Transforms the exception in a Update, Delete method
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="businessObject">The business object if available.</param>
        /// <param name="criteria">The criteria.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="ex">The exception.</param>
        /// <returns></returns>
        public Exception ToDataPortalException(Type objectType, object businessObject, object criteria, string methodName, Exception ex)
        {
            Exception handledException;
            if (CallExceptionInspector(objectType, businessObject, criteria, methodName, ex, out handledException))
            {
                ex = handledException;
            }

            return new DataPortalException(
              methodName + " " + Resources.FailedOnServer,
              ex, new DataPortalResult(businessObject));
        }

        /// <summary>
        /// Calls the custom exception inspector.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="businessObject">The business object.</param>
        /// <param name="criteria">The criteria.</param>
        /// <param name="methodName">Name of the method.</param>
        /// <param name="exception">The exception that was throw in business method.</param>
        /// <param name="handledException">The handled exception.</param>
        /// <returns>
        /// true if new exception was thrown else false
        /// </returns>
        private bool CallExceptionInspector(Type objectType, object businessObject, object criteria, string methodName, Exception exception, out Exception handledException)
        {
            handledException = null;
            var inspector = GetExceptionInspector();
            // if no inspector is defined then just return false 
            if (inspector == null) return false;

            try
            {
                // This method should rethrow a new exception to be handled 
                inspector.InspectException(objectType, businessObject, criteria, methodName, exception);
            }
            catch (Exception ex)
            {
                handledException = ex;
                return true;  // new exception returned
            }
            return false;  // exception was not handled 
        }

        #endregion
    }
}