﻿/* 
Copyright 2015 Dicky Suryadi

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DotNetify
{
   /// <summary>
   /// Base class for all DotNetify view models.  
   /// </summary>
   public class BaseVM : Observable
   {
      protected ConcurrentDictionary<string, object> mChangedProperties = new ConcurrentDictionary<string, object>();
      private List<string> mIgnoredProperties = null;

      public class IgnoreAttribute : Attribute { }

      /// <summary>
      /// Occurs when the view model wants to push updates to the client.
      /// This event is handled by the VMController. 
      /// </summary>
      public event EventHandler RequestPushUpdates;

      /// <summary>
      /// Gets properties that have been changed after the last accept command.
      /// </summary>
      [Ignore]
      public ConcurrentDictionary<string, object> ChangedProperties
      {
         get { return mChangedProperties; }
      }

      /// <summary>
      /// Gets a list of ignored properties.
      /// </summary>
      [Ignore]
      public List<string> IgnoredProperties
      {
         get
         {
            if (mIgnoredProperties == null)
               mIgnoredProperties = GetType().GetProperties().Where(i => i.GetCustomAttribute(typeof(IgnoreAttribute)) != null).ToList().ConvertAll(j => j.Name);
            return mIgnoredProperties;
         }
      }

      /// <summary>
      /// Accepts all changed properties so they won't marked as changed anymore.
      /// </summary>
      public void AcceptChangedProperties()
      {
         mChangedProperties.Clear();
      }

      /// <summary>
      /// Overrides this method if the derived type is a master view model.  The VMController
      /// will call this method to get instances of any view model whose view falls within
      /// this master view in the HTML markup.  The master view model can use this opportunity
      /// to do its own initialization of those subordinate view models, and/or arranging 
      /// communication among them. If null is returned, the VMController will create the 
      /// instance itself.
      /// </summary>
      /// <param name="iVMTypeName">View model type name.</param>
      /// <param name="iVMInstanceId">View model instance identifier.</param>
      /// <param name="iVMArg">View model's initialization argument.</param> 
      /// <returns>View model instance.</returns>
      public virtual BaseVM GetSubVM(string iVMTypeName, string iVMInstanceId)
      {
         return String.IsNullOrEmpty(iVMInstanceId) ? GetSubVM(iVMTypeName) : null;
      }

      /// <summary>
      /// Overload of GetSubVM that only acceptes view model type name.
      /// </summary>
      /// <param name="iVMTypeName">View model type name.</param>
      /// <returns>View model instance.</returns>
      public virtual BaseVM GetSubVM(string iVMTypeName)
      {
         return null;
      }

      /// <summary>
      /// Prevent a property from being bound.
      /// </summary>
      /// <typeparam name="T">Property type.</typeparam>
      /// <param name="iExpression">Expression containing property name, to avoid hardcoding it.</param>
      public void Ignore<T>(Expression<Func<T>> iExpression)
      {
         var propertyName = ((MemberExpression)iExpression.Body).Member.Name;
         if (!mIgnoredProperties.Contains(propertyName))
            mIgnoredProperties.Add(propertyName);
      }

      /// <summary>
      /// Overrides this method to handle a value update from a property path that cannot
      /// be resolved by the VMController.
      /// </summary>
      /// <param name="iVMPath">View model property path.</param>
      /// <param name="iValue">New value.</param>
      public virtual void OnUnresolvedUpdate(string iVMPath, string iValue)
      {
      }

      /// <summary>
      /// Push property changed updates to the client.
      /// </summary>
      public void PushUpdates()
      {
         if (RequestPushUpdates != null)
            RequestPushUpdates(this, null);
      }

      /// <summary>
      /// Fires property changed event.
      /// </summary>
      /// <param name="iPropertyName">Property name.</param>
      protected override void Changed(string iPropertyName)
      {
         base.Changed(iPropertyName);

         // Skip property that's decorated with [Ignore].
         if (IgnoredProperties.Contains(iPropertyName))
            return;

         // Mark property as changed, to allow the server view model to forward changes back to the client view model.
         mChangedProperties[iPropertyName] = GetType().GetProperty(iPropertyName).GetValue(this);
      }
   }
}
